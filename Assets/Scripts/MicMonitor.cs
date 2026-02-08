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

    IEnumerator Start()
    {
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
        if (audioSource.clip == null || audioSource == null) return;

        int micPos = Microphone.GetPosition(micDevice);
        int start = micPos - rmsBuffer.Length;
        if (start < 0) return; // not enough data yet

        audioSource.clip.GetData(rmsBuffer, start);

        float rms = PitchMath.ComputeRMS(rmsBuffer);
        float hz = PitchDetector.DetectPitchAutocorrelation(rmsBuffer, sampleRate, minFreq, maxFreq, minRmsForPitch);

        if (hz > 0f)
        {
            smoothedHz = Mathf.Lerp(smoothedHz, hz, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        }
        else
        {
            smoothedHz = Mathf.Lerp(smoothedHz, 0f, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        }

        // Update the text with percieved note and octave

        int midi = NoteUtil.FrequencyToMidi(smoothedHz);
        float target = (midi >= 0) ? NoteUtil.MidiToFrequency(midi) : 0f;
        float cents = (midi >= 0) ? PitchMath.CentsOff(smoothedHz, target) : 0f;

        statusText.text = $"Mic: {micDevice}\n" +
                          $"RMS: {rms:F4}\n" +
                          $"Pitch: {(smoothedHz > 0f ? smoothedHz.ToString("F1") + " Hz" : "--")}\n" +
                          $"Note: {NoteUtil.MidiToName(midi)} ({cents:+0.0;-0.0;0.0} cents)";

    }
}