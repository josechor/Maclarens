using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TopDownController))]
public class Interactor : MonoBehaviour
{
    [Tooltip("Distancia entre el borde del personaje y el rectángulo de interacción.")]
    [SerializeField] private float forwardOffset = 0.4f;

    [Tooltip("Cuánto se extiende el rectángulo hacia donde miras.")]
    [SerializeField] private float interactionRange = 1.2f;

    [Tooltip("Ancho del rectángulo en el eje perpendicular a la mirada.")]
    [SerializeField] private float interactionWidth = 0.9f;

    private TopDownController controller;
    private IInteractable current;

    private void Awake()
    {
        controller = GetComponent<TopDownController>();
    }

    private void Update()
    {
        if (InteractionUI.Instance == null)
        {
            return;
        }

        if (DialogueRunner.Instance != null && DialogueRunner.Instance.IsBlockingInteraction)
        {
            current = null;
            InteractionUI.Instance.HidePrompt();
            return;
        }

        UpdateTarget();
        HandleInput();
    }

    private void UpdateTarget()
    {
        if (InteractionUI.Instance.IsMessageOpen)
        {
            current = null;
            return;
        }

        Bounds bounds = GetInteractionBounds();
        var hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);

        IInteractable best = null;
        float bestSqrDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
            {
                continue;
            }

            var interactable = hit.GetComponent<IInteractable>();
            if (interactable == null)
            {
                continue;
            }

            float sqrDist = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                best = interactable;
            }
        }

        current = best;

        if (current != null)
        {
            InteractionUI.Instance.ShowPrompt(current.InteractionPrompt);
        }
        else
        {
            InteractionUI.Instance.HidePrompt();
        }
    }

    private void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (InteractionUI.Instance.IsMessageOpen)
        {
            InteractionUI.Instance.CloseMessage();
            controller.InputEnabled = true;
        }
        else if (current != null)
        {
            if (current.Interact(gameObject))
            {
                controller.InputEnabled = false;
            }
        }
    }

    private Bounds GetInteractionBounds()
    {
        Vector2 facing = controller.FacingDirection;

        Vector2 size = Mathf.Abs(facing.x) > 0.5f
            ? new Vector2(interactionRange, interactionWidth)
            : new Vector2(interactionWidth, interactionRange);

        Vector2 center = (Vector2)transform.position + facing * (forwardOffset + interactionRange * 0.5f);

        return new Bounds(center, size);
    }

    private void OnDrawGizmos()
    {
        if (controller == null)
        {
            controller = GetComponent<TopDownController>();
        }

        if (controller == null)
        {
            return;
        }

        Bounds bounds = GetInteractionBounds();
        Gizmos.color = current != null ? new Color(0.3f, 1f, 0.4f, 0.9f) : new Color(1f, 1f, 1f, 0.6f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
