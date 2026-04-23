using TMPro;
using UnityEngine;

public class TunerManager : MonoBehaviour
{
    private const int GuitarStringCount = 6;

    public TuningNote[] standardTuning = new TuningNote[]
    {
        new TuningNote { noteName = "E2", targetFrequency = 82.41f },
        new TuningNote { noteName = "A2", targetFrequency = 110.00f },
        new TuningNote { noteName = "D3", targetFrequency = 146.83f },
        new TuningNote { noteName = "G3", targetFrequency = 196.00f },
        new TuningNote { noteName = "B3", targetFrequency = 246.94f },
        new TuningNote { noteName = "E4", targetFrequency = 329.63f }
    };

    public TuningNote[] availableNotes = CreateDefaultAvailableNotes();

    [Tooltip("One dropdown per guitar head knob/string, ordered low E through high E.")]
    public TMP_Dropdown[] stringNoteDropdowns = new TMP_Dropdown[GuitarStringCount];

    [Tooltip("Selected note index in availableNotes for each guitar head knob/string.")]
    public int[] selectedNoteIndices = new int[] { 4, 9, 14, 19, 23, 28 };

    public int selectedStringIndex = 0;

    public void Awake()
    {
        EnsureState();
        ConfigureDropdowns();
        Debug.Log($"[TunerManager Awake] Object: {gameObject.name}, InstanceID: {GetInstanceID()}");
    }

    public TuningNote GetSelectedTarget()
    {
        return GetTargetForString(selectedStringIndex);
    }

    public TuningNote GetTargetForString(int stringIndex)
    {
        EnsureState();

        if (availableNotes == null || availableNotes.Length == 0)
        {
            Debug.LogWarning("[TunerManager] No available notes configured.");
            return null;
        }

        int safeStringIndex = Mathf.Clamp(stringIndex, 0, selectedNoteIndices.Length - 1);
        int noteIndex = Mathf.Clamp(selectedNoteIndices[safeStringIndex], 0, availableNotes.Length - 1);
        return availableNotes[noteIndex];
    }

    public void SelectString(int index)
    {
        EnsureState();
        selectedStringIndex = Mathf.Clamp(index, 0, selectedNoteIndices.Length - 1);
        TuningNote target = GetSelectedTarget();
        Debug.Log($"[TunerManager SelectString] Object: {gameObject.name}, InstanceID: {GetInstanceID()}, Index: {selectedStringIndex}, Note: {target.noteName}, Freq: {target.targetFrequency}");
    }

    public void SetSelectedStringTargetNote(int noteIndex)
    {
        SetStringTargetNote(selectedStringIndex, noteIndex);
    }

    public void SetStringTargetNote(int stringIndex, int noteIndex)
    {
        EnsureState();

        if (availableNotes == null || availableNotes.Length == 0) return;

        int safeStringIndex = Mathf.Clamp(stringIndex, 0, selectedNoteIndices.Length - 1);
        selectedNoteIndices[safeStringIndex] = Mathf.Clamp(noteIndex, 0, availableNotes.Length - 1);

        TuningNote target = GetTargetForString(safeStringIndex);
        Debug.Log($"[TunerManager SetStringTargetNote] String: {safeStringIndex}, Note: {target.noteName}, Freq: {target.targetFrequency}");
    }

    private void ConfigureDropdowns()
    {
        if (stringNoteDropdowns == null || availableNotes == null || availableNotes.Length == 0) return;

        for (int i = 0; i < stringNoteDropdowns.Length; i++)
        {
            TMP_Dropdown dropdown = stringNoteDropdowns[i];
            if (dropdown == null) continue;

            int stringIndex = i;
            dropdown.ClearOptions();

            System.Collections.Generic.List<string> optionNames = new System.Collections.Generic.List<string>();
            for (int noteIndex = 0; noteIndex < availableNotes.Length; noteIndex++)
            {
                optionNames.Add(availableNotes[noteIndex].noteName);
            }

            dropdown.AddOptions(optionNames);
            dropdown.SetValueWithoutNotify(Mathf.Clamp(selectedNoteIndices[stringIndex], 0, availableNotes.Length - 1));
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(noteIndex => SetStringTargetNote(stringIndex, noteIndex));
        }
    }

    private void EnsureState()
    {
        if (selectedNoteIndices == null || selectedNoteIndices.Length != GuitarStringCount)
        {
            selectedNoteIndices = new int[] { 4, 9, 14, 19, 23, 28 };
        }

        selectedStringIndex = Mathf.Clamp(selectedStringIndex, 0, selectedNoteIndices.Length - 1);

        if (availableNotes == null || availableNotes.Length == 0)
        {
            availableNotes = CreateDefaultAvailableNotes();
        }
    }

    private void OnValidate()
    {
        EnsureState();
    }

    private static TuningNote[] CreateDefaultAvailableNotes()
    {
        return new TuningNote[]
        {
            new TuningNote { noteName = "C2", targetFrequency = 65.41f },
            new TuningNote { noteName = "C#2", targetFrequency = 69.30f },
            new TuningNote { noteName = "D2", targetFrequency = 73.42f },
            new TuningNote { noteName = "D#2", targetFrequency = 77.78f },
            new TuningNote { noteName = "E2", targetFrequency = 82.41f },
            new TuningNote { noteName = "F2", targetFrequency = 87.31f },
            new TuningNote { noteName = "F#2", targetFrequency = 92.50f },
            new TuningNote { noteName = "G2", targetFrequency = 98.00f },
            new TuningNote { noteName = "G#2", targetFrequency = 103.83f },
            new TuningNote { noteName = "A2", targetFrequency = 110.00f },
            new TuningNote { noteName = "A#2", targetFrequency = 116.54f },
            new TuningNote { noteName = "B2", targetFrequency = 123.47f },
            new TuningNote { noteName = "C3", targetFrequency = 130.81f },
            new TuningNote { noteName = "C#3", targetFrequency = 138.59f },
            new TuningNote { noteName = "D3", targetFrequency = 146.83f },
            new TuningNote { noteName = "D#3", targetFrequency = 155.56f },
            new TuningNote { noteName = "E3", targetFrequency = 164.81f },
            new TuningNote { noteName = "F3", targetFrequency = 174.61f },
            new TuningNote { noteName = "F#3", targetFrequency = 185.00f },
            new TuningNote { noteName = "G3", targetFrequency = 196.00f },
            new TuningNote { noteName = "G#3", targetFrequency = 207.65f },
            new TuningNote { noteName = "A3", targetFrequency = 220.00f },
            new TuningNote { noteName = "A#3", targetFrequency = 233.08f },
            new TuningNote { noteName = "B3", targetFrequency = 246.94f },
            new TuningNote { noteName = "C4", targetFrequency = 261.63f },
            new TuningNote { noteName = "C#4", targetFrequency = 277.18f },
            new TuningNote { noteName = "D4", targetFrequency = 293.66f },
            new TuningNote { noteName = "D#4", targetFrequency = 311.13f },
            new TuningNote { noteName = "E4", targetFrequency = 329.63f },
            new TuningNote { noteName = "F4", targetFrequency = 349.23f },
            new TuningNote { noteName = "F#4", targetFrequency = 369.99f },
            new TuningNote { noteName = "G4", targetFrequency = 392.00f },
            new TuningNote { noteName = "G#4", targetFrequency = 415.30f },
            new TuningNote { noteName = "A4", targetFrequency = 440.00f }
        };
    }
}
