using UnityEngine;

public static class PitchDetector
{
    // Returns estimated fundamental frequency in Hz, or 0 if no reliable pitch
    public static float DetectPitchAutocorrelation(
        float[] buffer, 
        int sampleRate, 
        float minFreq = 70f, 
        float maxFreq = 450f,
        float minRms = 0.01f,
        float expectedFrequency = 0f)
    {
        // Gate on volume so we don't "detect pitch" in silence
        float rms = PitchMath.ComputeRMS(buffer);
        if (rms < minRms) return 0f;

        int size = buffer.Length;

        int minLag = Mathf.FloorToInt(sampleRate / maxFreq); // smallest lag = highest freq
        int maxLag = Mathf.CeilToInt(sampleRate / minFreq);  // largest lag = lowest freq
        maxLag = Mathf.Min(maxLag, size - 1);

        // Remove DC offset (helps stability)
        float mean = 0f;
        for (int i = 0; i < size; i++) mean += buffer[i];
        mean /= size;

        // Autocorrelation search: find lag with highest normalized correlation,
        // lightly biased toward the selected target frequency.
        float bestScore = 0f;
        int bestLag = 0;
        float expectedLag = expectedFrequency > 0f ? (float)sampleRate / expectedFrequency : 0f;

        for (int lag = minLag; lag <= maxLag; lag++)
        {
            float corr = 0f;
            float energyA = 0f;
            float energyB = 0f;

            for (int i = 0; i < size - lag; i++)
            {
                float a = buffer[i] - mean;
                float b = buffer[i + lag] - mean;
                corr += a * b;
                energyA += a * a;
                energyB += b * b;
            }

            if (energyA <= 0f || energyB <= 0f) continue;

            float normalizedCorr = corr / Mathf.Sqrt(energyA * energyB);
            if (normalizedCorr <= 0f) continue;

            float proximityWeight = 1f;
            if (expectedLag > 0f)
            {
                float lagDistance = Mathf.Abs(lag - expectedLag) / Mathf.Max(expectedLag, 1f);
                proximityWeight = Mathf.Clamp01(1.15f - lagDistance);
            }

            float score = normalizedCorr * proximityWeight;

            if (score > bestScore)
            {
                bestScore = score;
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
}
