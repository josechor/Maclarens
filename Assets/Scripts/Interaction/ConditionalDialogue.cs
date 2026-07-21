using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConditionalDialogue
{
    [Tooltip("Todas estas flags deben estar activas para usar este diálogo.")]
    public List<string> requiredFlagsSet = new List<string>();

    [Tooltip("Todas estas flags deben estar SIN activar para usar este diálogo.")]
    public List<string> requiredFlagsUnset = new List<string>();

    public DialogueData dialogue;

    public bool Matches()
    {
        foreach (var flag in requiredFlagsSet)
        {
            if (!GameFlags.IsSet(flag))
            {
                return false;
            }
        }

        foreach (var flag in requiredFlagsUnset)
        {
            if (GameFlags.IsSet(flag))
            {
                return false;
            }
        }

        return true;
    }
}
