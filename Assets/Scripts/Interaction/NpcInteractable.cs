using System.Collections.Generic;
using UnityEngine;

public class NpcInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Pulsa E para hablar";

    [Tooltip("Se evalúan en orden; gana la primera cuyas condiciones se cumplan. Pon la opción sin condiciones la última, como conversación por defecto.")]
    [SerializeField] private List<ConditionalDialogue> dialogueOptions = new List<ConditionalDialogue>();

    public string InteractionPrompt => interactionPrompt;

    public void Interact(GameObject interactor)
    {
        DialogueData chosen = null;

        foreach (var option in dialogueOptions)
        {
            if (option.Matches())
            {
                chosen = option.dialogue;
                break;
            }
        }

        if (chosen == null)
        {
            Debug.LogWarning($"{name}: ninguna condición de diálogo coincide (y no hay ninguna por defecto).", this);
            return;
        }

        if (DialogueRunner.Instance == null)
        {
            Debug.LogError("No hay ningún DialogueRunner en la escena.", this);
            return;
        }

        DialogueRunner.Instance.StartDialogue(chosen, interactor);
    }
}
