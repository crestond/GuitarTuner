using UnityEngine;

public class NoteUtil
{
    private static readonly string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public static int FrequencyToMidi(float freq)
    {
        if (freq <= 0f) return -1;
        return Mathf.RoundToInt(69f+ 12f * Mathf.Log(freq / 440f, 2f));

    }

    public static float MidiToFrequency(int midi)
    {
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }

    public static string MidiToName(int midi)
    {
        if (midi < 0) return "N/A";
        int note = ((midi % 12) + 12) % 12;
        int octave = (midi / 12) - 1;
        return $"{noteNames[note]}{octave}";
    }
}
