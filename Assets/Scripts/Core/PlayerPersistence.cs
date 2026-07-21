using UnityEngine;

// Conecta al jugador con el guardado: al arrancar coloca al jugador donde estaba (si se cargó una
// partida) y le da a SaveSystem una forma de leer su posición actual cada vez que autoguarda.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerPersistence : MonoBehaviour
{
    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Si venimos de "Continuar" con una posición guardada, teletransporta al jugador allí.
        if (SaveSystem.TryConsumePendingPosition(out Vector2 saved))
        {
            transform.position = saved;
            if (body != null)
            {
                body.position = saved; // evita que la interpolación del Rigidbody arrastre desde el origen
            }
        }

        // A partir de ahora, cada Save() lee la posición actual del jugador desde aquí.
        SaveSystem.PlayerPositionProvider = () => transform.position;
    }

    private void OnDestroy()
    {
        // No dejar colgando un proveedor que apunta a un objeto destruido (cambio de escena, etc.).
        if (SaveSystem.PlayerPositionProvider != null && SaveSystem.PlayerPositionProvider.Target == (object)this)
        {
            SaveSystem.PlayerPositionProvider = null;
        }
    }
}
