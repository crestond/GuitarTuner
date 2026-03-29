using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class MicMonitor : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;

    [Header("Audio")]
    public AudioSource audioSource;
    public int sampleRate = 44100;
    public int clipLengthSeconds = 1;
    public float minFreq = 70f;
    public float maxFreq = 450f;
    public float minRmsForPitch = 0.01f;
    private float smoothedHz = 0f;
    public float smoothSpeed = 12f; // higher = faster response
    private String micDevice;
    private float[] rmsBuffer = new float[4096]; // small buffer for RMS and pitch detection

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
        float centsOff = PitchMath.CentsOff(smoothedHz, targetNote.targetFrequency);

        statusText.text =
            $"Target: {targetNote.noteName} ({targetNote.targetFrequency:F2} Hz)\n" +
            $"Detected: {(smoothedHz > 0f ? smoothedHz.ToString("F1") + " Hz" : "--")}\n" +
            $"Cents Off: {centsOff:+0.0;-0.0;0.0}";
    }
}