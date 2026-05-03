using TMPro;
using UnityEngine;

[System.Serializable]
public class GuitarStringOptionSet
{
    public string stringName;
    public TuningNote[] allowedNotes;
    public int selectedNoteIndex;
}

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

    [Tooltip("One dropdown per guitar head knob/string, ordered low E through high E.")]
    public TMP_Dropdown[] stringNoteDropdowns = new TMP_Dropdown[GuitarStringCount];

    [Tooltip("Allowed tuning targets for each string, ordered low E through high E.")]
    public GuitarStringOptionSet[] stringOptions = CreateDefaultStringOptions();

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

        if (stringOptions == null || stringOptions.Length == 0)
        {
            Debug.LogWarning("[TunerManager] No string options configured.");
            return null;
        }

        int safeStringIndex = Mathf.Clamp(stringIndex, 0, stringOptions.Length - 1);
        GuitarStringOptionSet optionSet = stringOptions[safeStringIndex];

        if (optionSet.allowedNotes == null || optionSet.allowedNotes.Length == 0)
        {
            Debug.LogWarning($"[TunerManager] No allowed notes configured for string index {safeStringIndex}.");
            return null;
        }

        int noteIndex = Mathf.Clamp(optionSet.selectedNoteIndex, 0, optionSet.allowedNotes.Length - 1);
        return optionSet.allowedNotes[noteIndex];
    }

    public void SelectString(int index)
    {
        EnsureState();
        selectedStringIndex = Mathf.Clamp(index, 0, stringOptions.Length - 1);
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

        if (stringOptions == null || stringOptions.Length == 0) return;

        int safeStringIndex = Mathf.Clamp(stringIndex, 0, stringOptions.Length - 1);
        GuitarStringOptionSet optionSet = stringOptions[safeStringIndex];
        if (optionSet.allowedNotes == null || optionSet.allowedNotes.Length == 0) return;

        optionSet.selectedNoteIndex = Mathf.Clamp(noteIndex, 0, optionSet.allowedNotes.Length - 1);

        TuningNote target = GetTargetForString(safeStringIndex);
        Debug.Log($"[TunerManager SetStringTargetNote] String: {safeStringIndex}, Note: {target.noteName}, Freq: {target.targetFrequency}");
    }

    private void ConfigureDropdowns()
    {
        if (stringNoteDropdowns == null || stringOptions == null || stringOptions.Length == 0) return;

        for (int i = 0; i < stringNoteDropdowns.Length; i++)
        {
            TMP_Dropdown dropdown = stringNoteDropdowns[i];
            if (dropdown == null) continue;

            int stringIndex = i;
            GuitarStringOptionSet optionSet = stringOptions[Mathf.Clamp(stringIndex, 0, stringOptions.Length - 1)];
            if (optionSet.allowedNotes == null || optionSet.allowedNotes.Length == 0) continue;

            dropdown.ClearOptions();

            System.Collections.Generic.List<string> optionNames = new System.Collections.Generic.List<string>();
            for (int noteIndex = 0; noteIndex < optionSet.allowedNotes.Length; noteIndex++)
            {
                optionNames.Add(optionSet.allowedNotes[noteIndex].noteName);
            }

            dropdown.AddOptions(optionNames);
            dropdown.SetValueWithoutNotify(Mathf.Clamp(optionSet.selectedNoteIndex, 0, optionSet.allowedNotes.Length - 1));
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(noteIndex => SetStringTargetNote(stringIndex, noteIndex));
        }
    }

    private void EnsureState()
    {
        if (stringOptions == null || stringOptions.Length != GuitarStringCount)
        {
            stringOptions = CreateDefaultStringOptions();
        }

        for (int i = 0; i < stringOptions.Length; i++)
        {
            if (stringOptions[i] == null)
            {
                stringOptions[i] = CreateDefaultStringOptions()[i];
                continue;
            }

            if (stringOptions[i].allowedNotes == null || stringOptions[i].allowedNotes.Length == 0)
            {
                stringOptions[i].allowedNotes = CreateDefaultStringOptions()[i].allowedNotes;
            }

            stringOptions[i].selectedNoteIndex = Mathf.Clamp(
                stringOptions[i].selectedNoteIndex,
                0,
                stringOptions[i].allowedNotes.Length - 1
            );
        }

        selectedStringIndex = Mathf.Clamp(selectedStringIndex, 0, stringOptions.Length - 1);
    }

    private void OnValidate()
    {
        EnsureState();
    }

    private static GuitarStringOptionSet[] CreateDefaultStringOptions()
    {
        return new GuitarStringOptionSet[]
        {
            new GuitarStringOptionSet
            {
                stringName = "Low E",
                selectedNoteIndex = 4,
                allowedNotes = new TuningNote[]
                {
                    new TuningNote { noteName = "C2", targetFrequency = 65.41f },
                    new TuningNote { noteName = "C#2", targetFrequency = 69.30f },
                    new TuningNote { noteName = "D2", targetFrequency = 73.42f },
                    new TuningNote { noteName = "D#2", targetFrequency = 77.78f },
                    new TuningNote { noteName = "E2", targetFrequency = 82.41f }
                }
            },
            new GuitarStringOptionSet
            {
                stringName = "A",
                selectedNoteIndex = 2,
                allowedNotes = new TuningNote[]
                {
                    new TuningNote { noteName = "G2", targetFrequency = 98.00f },
                    new TuningNote { noteName = "G#2", targetFrequency = 103.83f },
                    new TuningNote { noteName = "A2", targetFrequency = 110.00f },
                    new TuningNote { noteName = "A#2", targetFrequency = 116.54f },
                    new TuningNote { noteName = "B2", targetFrequency = 123.47f }
                }
            },
            new GuitarStringOptionSet
            {
                stringName = "D",
                selectedNoteIndex = 2,
                allowedNotes = new TuningNote[]
                {
                    new TuningNote { noteName = "C3", targetFrequency = 130.81f },
                    new TuningNote { noteName = "C#3", targetFrequency = 138.59f },
                    new TuningNote { noteName = "D3", targetFrequency = 146.83f },
                    new TuningNote { noteName = "D#3", targetFrequency = 155.56f },
                    new TuningNote { noteName = "E3", targetFrequency = 164.81f }
                }
            },
            new GuitarStringOptionSet
            {
                stringName = "G",
                selectedNoteIndex = 2,
                allowedNotes = new TuningNote[]
                {
                    new TuningNote { noteName = "F3", targetFrequency = 174.61f },
                    new TuningNote { noteName = "F#3", targetFrequency = 185.00f },
                    new TuningNote { noteName = "G3", targetFrequency = 196.00f },
                    new TuningNote { noteName = "G#3", targetFrequency = 207.65f },
                    new TuningNote { noteName = "A3", targetFrequency = 220.00f }
                }
            },
            new GuitarStringOptionSet
            {
                stringName = "B",
                selectedNoteIndex = 2,
                allowedNotes = new TuningNote[]
                {
                    new TuningNote { noteName = "A3", targetFrequency = 220.00f },
                    new TuningNote { noteName = "A#3", targetFrequency = 233.08f },
                    new TuningNote { noteName = "B3", targetFrequency = 246.94f },
                    new TuningNote { noteName = "C4", targetFrequency = 261.63f },
                    new TuningNote { noteName = "C#4", targetFrequency = 277.18f }
                }
            },
            new GuitarStringOptionSet
            {
                stringName = "High E",
                selectedNoteIndex = 2,
                allowedNotes = new TuningNote[]
                {
                    new TuningNote { noteName = "D4", targetFrequency = 293.66f },
                    new TuningNote { noteName = "D#4", targetFrequency = 311.13f },
                    new TuningNote { noteName = "E4", targetFrequency = 329.63f },
                    new TuningNote { noteName = "F4", targetFrequency = 349.23f },
                    new TuningNote { noteName = "F#4", targetFrequency = 369.99f }
                }
            }
        };
    }
}
