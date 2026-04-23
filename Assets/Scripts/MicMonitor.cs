using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MicMonitor : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;
    public Image tuningIndicator;
    public Color noSignalColor = Color.gray;
    public Color sharpFlatColor = Color.red;
    public Color closeColor = Color.yellow;
    public Color tunedColor = Color.green;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource tunedConfirmSource;
    public AudioClip tunedConfirmClip;
    public int sampleRate = 44100;
    public int clipLengthSeconds = 1;
    public float minFreq = 70f;
    public float maxFreq = 450f;
    public float minRmsForPitch = 0.01f;
    private float smoothedHz = 0f;
    public float smoothSpeed = 12f; // higher = faster response
    public float tunedThresholdCents = 5f;
    public float closeThresholdCents = 15f;
    public float dingCooldownSeconds = 1f;

    private string micDevice;
    private float[] rmsBuffer = new float[4096]; // small buffer for RMS and pitch detection
    private bool wasTuned;
    private float lastDingTime = -999f;

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
            statusText.text = "No microphone detected!";
            yield break;
        }

        micDevice = Microphone.devices[0];

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.mute = true;
        audioSource.clip = Microphone.Start(micDevice, true, clipLengthSeconds, sampleRate);
        EnsureTunedConfirmSource();

        while (Microphone.GetPosition(micDevice) <= 0) yield return null; // wait until mic starts

        audioSource.Play();
        statusText.text = $"Using mic: {micDevice}";

    }

    void Update()
    {
        if (audioSource == null || audioSource.clip == null || tunerManager == null) return;

        int micPos = Microphone.GetPosition(micDevice);
        int start = micPos - rmsBuffer.Length;
        if (start < 0) return;

        audioSource.clip.GetData(rmsBuffer, start);

        float hz = PitchDetector.DetectPitchAutocorrelation(rmsBuffer, sampleRate, minFreq, maxFreq, minRmsForPitch);

        if (hz > 0f)
            smoothedHz = Mathf.Lerp(smoothedHz, hz, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        else
            smoothedHz = Mathf.Lerp(smoothedHz, 0f, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

        TuningNote targetNote = tunerManager.GetSelectedTarget();
        if (targetNote == null) return;

        if (smoothedHz <= 0.01f)
        {
            wasTuned = false;
            SetIndicator(noSignalColor);
            statusText.text =
                $"Target: {targetNote.noteName} ({targetNote.targetFrequency:F2} Hz)\n" +
                "Detected: --\n" +
                "Cents Off: --";
            return;
        }

        float centsOff = PitchMath.CentsOff(smoothedHz, targetNote.targetFrequency);
        UpdateTuningFeedback(centsOff);

        statusText.text =
            $"Target: {targetNote.noteName} ({targetNote.targetFrequency:F2} Hz)\n" +
            $"Detected: {smoothedHz:F1} Hz\n" +
            $"Cents Off: {centsOff:+0.0;-0.0;0.0}";
    }

    private void UpdateTuningFeedback(float centsOff)
    {
        float absCents = Mathf.Abs(centsOff);
        bool isTuned = absCents <= tunedThresholdCents;

        if (isTuned)
        {
            SetIndicator(tunedColor);
            PlayDingOnce();
        }
        else if (absCents <= closeThresholdCents)
        {
            SetIndicator(closeColor);
        }
        else
        {
            SetIndicator(sharpFlatColor);
        }

        wasTuned = isTuned;
    }

    private void PlayDingOnce()
    {
        if (wasTuned || Time.time - lastDingTime < dingCooldownSeconds) return;
        if (tunedConfirmSource == null || tunedConfirmClip == null) return;

        tunedConfirmSource.PlayOneShot(tunedConfirmClip);
        lastDingTime = Time.time;
    }

    private void SetIndicator(Color color)
    {
        if (tuningIndicator != null)
        {
            tuningIndicator.color = color;
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
}
