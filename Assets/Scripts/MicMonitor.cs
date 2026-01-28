using UnityEngine;
using TMPro;
using System;

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
    private float[] rmsBuffer = new float[1024];

    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            statusText.text = "No Microphone Detected";
            Debug.LogError("No microphone devices found.");
            return;
        }
        
        micDevice = Microphone.devices[0];

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.mute = true; // prevent feedback
        audioSource.clip = Microphone.Start(micDevice, true, clipLengthSeconds, sampleRate);

        // Wait until the microphone starts recording
        while (Microphone.GetPosition(micDevice) <= 0) { }

        audioSource.Play();

        statusText.text = $"Mic: {micDevice} (recording)";
        Debug.Log($"Mic started: {micDevice}");
    }

    void Update()
    {
        if (audioSource == null || audioSource.clip == null) return;

    int micPos = Microphone.GetPosition(micDevice);
    if (micPos <= 0) return;

    int start = micPos - rmsBuffer.Length;
    if (start < 0) return;

    // This line is the missing link: gets real mic samples
    audioSource.clip.GetData(rmsBuffer, start);

    float rms = ComputeRMS(rmsBuffer);

    float hz = PitchDetector.DetectPitchAutocorrelation(
        rmsBuffer, sampleRate, minFreq, maxFreq, minRmsForPitch
    );

    if (hz > 0f)
        smoothedHz = Mathf.Lerp(smoothedHz, hz, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
    else
        smoothedHz = Mathf.Lerp(smoothedHz, 0f, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

    statusText.text =
        $"Mic: {micDevice}\n" +
        $"RMS: {rms:F4}\n" +
        $"Pitch: {(smoothedHz > 0 ? smoothedHz.ToString("F1") + " Hz" : "--")}";

    }

        private float ComputeRMS(float[] samples)
    {
        double sum = 0.0;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        return Mathf.Sqrt((float)(sum / samples.Length));
    }
}