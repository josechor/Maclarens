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

        if (setFlagsOnInteract.Count > 0)
        {
            foreach (var flag in setFlagsOnInteract)
            {
                GameFlags.Set(flag);
            }

            // Autoguardado: solo si esta interacción cambió el estado del mundo.
            SaveSystem.Save();
        }

        return true;
    }
}
