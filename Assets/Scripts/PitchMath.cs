using System;
using UnityEngine;

public static class PitchMath
{
    public static float ComputeRMS(float[] samples)
    {
        double sum = 0.0;
        for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
        return Mathf.Sqrt((float)(sum / samples.Length));
    }

    public static float CentsOff(float freq, float targetFreq)
    {
        if (freq <= 0f || targetFreq <= 0f) return 0f;
        return 1200f * Mathf.Log(freq / targetFreq, 2f);
    }
}