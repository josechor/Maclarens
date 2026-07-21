using System.Collections.Generic;
using UnityEngine;

public class NpcInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Pulsa E para hablar";

    [Tooltip("Todas las conversaciones de este NPC. El selector elige por prioridad " +
             "Story > Context > Idle entre las disponibles; a igual prioridad gana la primera de la lista.")]
    [SerializeField] private List<ConversationAsset> conversations = new List<ConversationAsset>();

    public string InteractionPrompt => interactionPrompt;

    public void Interact(GameObject interactor)
    {
        ConversationAsset chosen = ConversationSelector.Select(conversations);

        if (chosen == null)
        {
            Debug.LogWarning($"{name}: ninguna conversación disponible ahora mismo.", this);
            return;
        }

        if (DialogueRunner.Instance == null)
        {
            Debug.LogError("No hay ningún DialogueRunner en la escena.", this);
            return;
        }

        DialogueRunner.Instance.StartConversation(chosen, interactor);
    }
}
