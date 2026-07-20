using UnityEngine;

public class SimpleInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Pulsa E para interactuar";
    [SerializeField] [TextArea] private string message = "...";

    public string InteractionPrompt => interactionPrompt;

    public void Interact(GameObject interactor)
    {
        InteractionUI.Instance.ShowMessage(message);
    }
}
