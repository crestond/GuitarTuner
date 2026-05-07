using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MicMonitor : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;
    public Image tuningIndicator;
    public RectTransform tuningIndicatorRect;
    public TextMeshProUGUI directionText;
    public Color noSignalColor = Color.gray;
    public Color flatColor = new Color(0.8f, 0.35f, 0.35f, 1f);
    public Color sharpColor = new Color(0.8f, 0.35f, 0.35f, 1f);
    public Color sharpFlatColor = Color.red;
    public Color closeColor = Color.yellow;
    public Color tunedColor = Color.green;
    public float indicatorTravelPixels = 140f;
    public float maxVisualCents = 30f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource tunedConfirmSource;
    public AudioClip tunedConfirmClip;
    public int sampleRate = 44100;
    public int clipLengthSeconds = 1;
    public float minFreq = 70f;
    public float maxFreq = 450f;
    public float targetWindowLowerMultiplier = 0.75f;
    public float targetWindowUpperMultiplier = 1.35f;
    public float minRmsForPitch = 0.01f;
    private float smoothedHz = 0f;
    public float smoothSpeed = 12f; // higher = faster response
    public int detectionMedianSampleCount = 5;
    public float tunedThresholdCents = 5f;
    public float closeThresholdCents = 15f;
    public float requiredStableTuneSeconds = 0.6f;
    public float dingCooldownSeconds = 1f;
    public float micSuppressAfterDingSeconds = 0.4f;
    public float indicatorSmoothTime = 0.08f;
    public float displayedCentsClamp = 99.9f;
    public float displayedCentsStep = 0.5f;
    public float higherStringDisplayHoldSeconds = 0.45f;

    private string micDevice;
    private float[] rmsBuffer = new float[4096]; // small buffer for RMS and pitch detection
    private bool isConfirmedTuned;
    private float lastDingTime = -999f;
    private float micSuppressedUntil = -999f;
    private float inTuneHoldTimer = 0f;
    private Vector2 indicatorBasePosition;
    private bool indicatorBasePositionInitialized;
    private float currentIndicatorOffset;
    private float indicatorOffsetVelocity;
    private readonly List<float> recentHzSamples = new List<float>();
    private bool hasVisibleCentsOff;
    private float visibleCentsOff;
    private float lastTrustedPitchTime = -999f;

    public TunerManager tunerManager;

    IEnumerator Start()
    {

        Debug.Log($"[MicMonitor Start] TunerManager ref = {(tunerManager != null ? tunerManager.gameObject.name : "NULL")}");
    
        if (tunerManager != null)
        {
            Debug.Log($"[MicMonitor Start] TunerManager InstanceID = {tunerManager.GetInstanceID()}");
        }
        if (Microphone.devices.Length == 0)
        {
            statusText.text = "--";
            yield break;
        }

        micDevice = Microphone.devices[0];

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.mute = true;
        audioSource.clip = Microphone.Start(micDevice, true, clipLengthSeconds, sampleRate);
        EnsureTunedConfirmSource();
        CacheIndicatorBasePosition();

        while (Microphone.GetPosition(micDevice) <= 0) yield return null; // wait until mic starts

        audioSource.Play();
        UpdateTargetNoteLabel();

    }

    void Update()
    {
        if (audioSource == null || audioSource.clip == null || tunerManager == null) return;

        TuningNote targetNote = tunerManager.GetSelectedTarget();
        if (targetNote == null) return;

        if (Time.time < micSuppressedUntil)
        {
            UpdateTargetNoteLabel();
            return;
        }

        int micPos = Microphone.GetPosition(micDevice);
        int start = micPos - rmsBuffer.Length;
        if (start < 0) return;

        audioSource.clip.GetData(rmsBuffer, start);

        float detectionMinFreq = Mathf.Max(minFreq, targetNote.targetFrequency * targetWindowLowerMultiplier);
        float detectionMaxFreq = Mathf.Min(maxFreq, targetNote.targetFrequency * targetWindowUpperMultiplier);
        if (detectionMaxFreq <= detectionMinFreq)
        {
            detectionMinFreq = minFreq;
            detectionMaxFreq = maxFreq;
        }

        float hz = PitchDetector.DetectPitchAutocorrelation(
            rmsBuffer,
            sampleRate,
            detectionMinFreq,
            detectionMaxFreq,
            minRmsForPitch
        );

        if (hz > 0f)
        {
            float filteredHz = AddAndFilterHzSample(hz);
            smoothedHz = Mathf.Lerp(smoothedHz, filteredHz, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        }
        else
        {
            recentHzSamples.Clear();
            smoothedHz = Mathf.Lerp(smoothedHz, 0f, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        }

        bool pitchIsTrusted = hz > 0f || (smoothedHz >= detectionMinFreq && smoothedHz <= detectionMaxFreq);
        bool keepHigherStringDisplayAlive = ShouldHoldDisplayForHigherString(targetNote) &&
            Time.time - lastTrustedPitchTime <= higherStringDisplayHoldSeconds &&
            hasVisibleCentsOff;

        if (!pitchIsTrusted || smoothedHz <= 0.01f)
        {
            if (keepHigherStringDisplayAlive)
            {
                SetIndicator(closeColor);
                UpdateTargetNoteLabel();
                return;
            }

            isConfirmedTuned = false;
            inTuneHoldTimer = 0f;
            hasVisibleCentsOff = false;
            SetIndicator(noSignalColor);
            SetDirectionText(string.Empty);
            SetIndicatorPosition(0f);
            UpdateTargetNoteLabel();
            return;
        }

        lastTrustedPitchTime = Time.time;
        float centsOff = PitchMath.CentsOff(smoothedHz, targetNote.targetFrequency);
        visibleCentsOff = GetDisplayCentsValue(centsOff);
        hasVisibleCentsOff = true;
        UpdateTuningFeedback(centsOff);
        UpdateTargetNoteLabel();
    }

    private void UpdateTuningFeedback(float centsOff)
    {
        float absCents = Mathf.Abs(centsOff);
        bool withinTuneWindow = absCents <= tunedThresholdCents;
        bool withinCloseWindow = absCents <= closeThresholdCents;
        UpdateDirectionalIndicator(centsOff, absCents);

        if (withinTuneWindow)
        {
            inTuneHoldTimer += Time.deltaTime;
            bool hasHeldTuneLongEnough = inTuneHoldTimer >= requiredStableTuneSeconds;

            if (hasHeldTuneLongEnough)
            {
                SetIndicator(tunedColor);
                SetDirectionText("IN TUNE");
                PlayDingOnce();
                isConfirmedTuned = true;
            }
            else
            {
                SetIndicator(closeColor);
                SetDirectionText(centsOff < 0f ? "TUNE UP" : "TUNE DOWN");
                isConfirmedTuned = false;
            }
        }
        else if (withinCloseWindow)
        {
            inTuneHoldTimer = 0f;
            SetIndicator(closeColor);
            SetDirectionText(centsOff < 0f ? "TUNE UP" : "TUNE DOWN");
            isConfirmedTuned = false;
        }
        else
        {
            inTuneHoldTimer = 0f;
            SetIndicator(centsOff < 0f ? flatColor : sharpColor);
            SetDirectionText(centsOff < 0f ? "TUNE UP" : "TUNE DOWN");
            isConfirmedTuned = false;
        }
    }

    private void UpdateDirectionalIndicator(float centsOff, float absCents)
    {
        float normalizedOffset = 0f;
        if (maxVisualCents > 0f)
        {
            normalizedOffset = Mathf.Clamp(centsOff / maxVisualCents, -1f, 1f);
        }

        SetIndicatorPosition(normalizedOffset);

        if (absCents > closeThresholdCents && sharpFlatColor != Color.clear)
        {
            SetIndicator(centsOff < 0f ? flatColor : sharpColor);
        }
    }

    private void PlayDingOnce()
    {
        if (isConfirmedTuned || Time.time - lastDingTime < dingCooldownSeconds) return;
        if (tunedConfirmSource == null || tunedConfirmClip == null) return;

        tunedConfirmSource.PlayOneShot(tunedConfirmClip);
        lastDingTime = Time.time;
        micSuppressedUntil = Time.time + micSuppressAfterDingSeconds;
    }

    private void SetIndicator(Color color)
    {
        if (tuningIndicator != null)
        {
            tuningIndicator.color = color;
        }
    }

    private void SetIndicatorPosition(float normalizedOffset)
    {
        CacheIndicatorBasePosition();
        if (tuningIndicatorRect == null || !indicatorBasePositionInitialized) return;

        float targetOffset = normalizedOffset * indicatorTravelPixels;
        currentIndicatorOffset = Mathf.SmoothDamp(
            currentIndicatorOffset,
            targetOffset,
            ref indicatorOffsetVelocity,
            indicatorSmoothTime
        );

        tuningIndicatorRect.anchoredPosition = indicatorBasePosition + new Vector2(currentIndicatorOffset, 0f);
    }

    private void SetDirectionText(string value)
    {
        if (directionText != null)
        {
            directionText.text = value;
        }
    }

    private void EnsureTunedConfirmSource()
    {
        if (tunedConfirmSource != null) return;

        GameObject confirmAudioObject = new GameObject("TunedConfirmAudio");
        confirmAudioObject.transform.SetParent(transform, false);
        tunedConfirmSource = confirmAudioObject.AddComponent<AudioSource>();
        tunedConfirmSource.playOnAwake = false;
    }

    private void CacheIndicatorBasePosition()
    {
        if (tuningIndicatorRect == null && tuningIndicator != null)
        {
            tuningIndicatorRect = tuningIndicator.rectTransform;
        }

        if (tuningIndicatorRect != null && !indicatorBasePositionInitialized)
        {
            indicatorBasePosition = tuningIndicatorRect.anchoredPosition;
            indicatorBasePositionInitialized = true;
        }
    }

    private void UpdateTargetNoteLabel()
    {
        if (statusText == null || tunerManager == null) return;

        TuningNote targetNote = tunerManager.GetSelectedTarget();
        if (targetNote == null)
        {
            statusText.text = "--\n--";
            return;
        }

        string centsLine = hasVisibleCentsOff ? $"{visibleCentsOff:+0.0;-0.0;0.0}" : "--";
        statusText.text = $"{targetNote.noteName}\n{centsLine}";
    }

    private float AddAndFilterHzSample(float hz)
    {
        recentHzSamples.Add(hz);

        int maxSamples = Mathf.Max(1, detectionMedianSampleCount);
        while (recentHzSamples.Count > maxSamples)
        {
            recentHzSamples.RemoveAt(0);
        }

        List<float> sortedSamples = new List<float>(recentHzSamples);
        sortedSamples.Sort();

        int middleIndex = sortedSamples.Count / 2;
        if (sortedSamples.Count % 2 == 1)
        {
            return sortedSamples[middleIndex];
        }

        return 0.5f * (sortedSamples[middleIndex - 1] + sortedSamples[middleIndex]);
    }

    private float GetDisplayCentsValue(float centsOff)
    {
        float clampedCents = Mathf.Clamp(centsOff, -displayedCentsClamp, displayedCentsClamp);
        float step = Mathf.Max(0.1f, displayedCentsStep);
        return Mathf.Round(clampedCents / step) * step;
    }

    private bool ShouldHoldDisplayForHigherString(TuningNote targetNote)
    {
        return targetNote != null && targetNote.targetFrequency >= 246f;
    }
}
