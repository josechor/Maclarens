using UnityEngine;

public class NpcInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Pulsa E para hablar";
    [SerializeField] private DialogueData dialogue;

    public string InteractionPrompt => interactionPrompt;

    public void Interact(GameObject interactor)
    {
        if (dialogue == null)
        {
            Debug.LogWarning($"{name}: no tiene ningún DialogueData asignado.", this);
            return;
        }

        if (DialogueRunner.Instance == null)
        {
            Debug.LogError("No hay ningún DialogueRunner en la escena.", this);
            return;
        }

        DialogueRunner.Instance.StartDialogue(dialogue, interactor);
    }
}
