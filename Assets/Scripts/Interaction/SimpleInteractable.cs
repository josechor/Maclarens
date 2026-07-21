using System.Collections.Generic;
using UnityEngine;

public class SimpleInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Pulsa E para interactuar";
    [SerializeField] [TextArea] private string message = "...";
    [SerializeField] private List<string> setFlagsOnInteract = new List<string>();

    public string InteractionPrompt => interactionPrompt;

    public bool Interact(GameObject interactor)
    {
        InteractionUI.Instance.ShowMessage(message);

        foreach (var flag in setFlagsOnInteract)
        {
            GameFlags.Set(flag);
        }

        return true;
    }
}
