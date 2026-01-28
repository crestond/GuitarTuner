using UnityEngine;

public static class PitchDetector
{
    // Returns estimated fundamental frequency in Hz, or 0 if no reliable pitch
    public static float DetectPitchAutocorrelation(
        float[] buffer, 
        int sampleRate, 
        float minFreq = 70f, 
        float maxFreq = 450f,
        float minRms = 0.01f)
    {
        // Gate on volume so we don't "detect pitch" in silence
        float rms = ComputeRMS(buffer);
        if (rms < minRms) return 0f;

        int size = buffer.Length;

        int minLag = Mathf.FloorToInt(sampleRate / maxFreq); // smallest lag = highest freq
        int maxLag = Mathf.CeilToInt(sampleRate / minFreq);  // largest lag = lowest freq
        maxLag = Mathf.Min(maxLag, size - 1);

        // Remove DC offset (helps stability)
        float mean = 0f;
        for (int i = 0; i < size; i++) mean += buffer[i];
        mean /= size;

        // Autocorrelation search: find lag with highest correlation
        float bestCorr = 0f;
        int bestLag = 0;

        for (int lag = minLag; lag <= maxLag; lag++)
        {
            float corr = 0f;
            for (int i = 0; i < size - lag; i++)
            {
                float a = buffer[i] - mean;
                float b = buffer[i + lag] - mean;
                corr += a * b;
            }

            if (corr > bestCorr)
            {
                bestCorr = corr;
                bestLag = lag;
            }
        }

        if (bestLag == 0) return 0f;

        // Convert lag to frequency
        float freq = (float)sampleRate / bestLag;

        // Basic sanity check
        if (freq < minFreq || freq > maxFreq) return 0f;
        return freq;
    }

    private static float ComputeRMS(float[] samples)
    {
        double sum = 0.0;
        for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
        return Mathf.Sqrt((float)(sum / samples.Length));
    }
}