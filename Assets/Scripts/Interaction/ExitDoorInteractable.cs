using UnityEngine;
using UnityEngine.SceneManagement;

// La puerta de salida del McClarens. Si el jugador tiene la llave (flag), salir termina el juego:
// marca la partida como completada, guarda y vuelve al menú principal. Si no, la puerta está cerrada.
public class ExitDoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Pulsa E para salir";
    [SerializeField] private string requiredKeyFlag = "has_key";
    [SerializeField] [TextArea] private string lockedMessage = "La puerta está cerrada. Necesitas una llave.";
    [SerializeField] private string menuSceneName = "MainMenu";

    public string InteractionPrompt => interactionPrompt;

    public bool Interact(GameObject interactor)
    {
        if (!GameFlags.IsSet(requiredKeyFlag))
        {
            InteractionUI.Instance.ShowMessage(lockedMessage);
            return true;
        }

        // Fin del juego: dejar constancia en el guardado (partida terminada, no continuable) y volver al menú.
        GameFlags.Set(SaveSystem.CompletedFlag);
        SaveSystem.Save();
        SceneManager.LoadScene(menuSceneName);
        return true;
    }
}
