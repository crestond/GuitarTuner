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

        audioSource.clip.GetData(rmsBuffer, start);

        float rms = ComputeRMS(rmsBuffer);
        statusText.text = $"Mic: {micDevice}\nRMS Level: {rms:F4}";
    }

        private float ComputeRMS(float[] samples)
    {
        double sum = 0.0;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        return Mathf.Sqrt((float)(sum / samples.Length));
    }
}