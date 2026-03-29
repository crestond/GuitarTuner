using UnityEngine;

public class TunerManager : MonoBehaviour
{
    public TuningNote[] standardTuning = new TuningNote[]
    {
        new TuningNote { noteName = "E2", targetFrequency = 82.41f },
        new TuningNote { noteName = "A2", targetFrequency = 110.00f },
        new TuningNote { noteName = "D3", targetFrequency = 146.83f },
        new TuningNote { noteName = "G3", targetFrequency = 196.00f },
        new TuningNote { noteName = "B3", targetFrequency = 246.94f },
        new TuningNote { noteName = "E4", targetFrequency = 329.63f }
    };

    public int selectedStringIndex = 0;

    public void Awake()
    {
        Debug.Log($"[TunerManager Awake] Object: {gameObject.name}, InstanceID: {GetInstanceID()}");
    }

    public TuningNote GetSelectedTarget()
    {
        return standardTuning[selectedStringIndex];
    }

    public void SelectString(int index)
    {
        selectedStringIndex = index;
        TuningNote target = GetSelectedTarget();
        Debug.Log($"[TunerManager SelectString] Object: {gameObject.name}, InstanceID: {GetInstanceID()}, Index: {index}, Note: {target.noteName}, Freq: {target.targetFrequency}");
    }
}