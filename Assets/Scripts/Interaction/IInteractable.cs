using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }

    // Devuelve true si la interacción realmente abrió algo (mensaje o diálogo) que debe
    // bloquear el movimiento del jugador hasta que se cierre.
    bool Interact(GameObject interactor);
}
