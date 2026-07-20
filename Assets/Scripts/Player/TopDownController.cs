using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownController : MonoBehaviour
{
    private enum Direction { Up, Down, Left, Right }

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform facingIndicator;
    [SerializeField] private float facingIndicatorDistance = 0.42f;

    public bool InputEnabled { get; set; } = true;
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private readonly bool[] held = new bool[4];
    private readonly List<Direction> pressOrder = new List<Direction>(4);

    public void SetFacingIndicator(Transform indicator, float distance)
    {
        facingIndicator = indicator;
        facingIndicatorDistance = distance;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        if (!InputEnabled)
        {
            ResetInput();
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            ResetInput();
            return;
        }

        UpdateDirection(Direction.Up, keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed, keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame);
        UpdateDirection(Direction.Down, keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed, keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame);
        UpdateDirection(Direction.Left, keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed, keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame);
        UpdateDirection(Direction.Right, keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed, keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame);

        Vector2 input = Vector2.zero;
        if (held[(int)Direction.Up]) input += Vector2.up;
        if (held[(int)Direction.Down]) input += Vector2.down;
        if (held[(int)Direction.Left]) input += Vector2.left;
        if (held[(int)Direction.Right]) input += Vector2.right;
        moveInput = input.normalized;

        // La mirada siempre es un solo eje (nunca diagonal): la última tecla
        // que sigue mantenida entre las que tocan movimiento.
        if (pressOrder.Count > 0)
        {
            FacingDirection = ToVector(pressOrder[pressOrder.Count - 1]);
        }
    }

    private void UpdateDirection(Direction dir, bool isPressed, bool wasPressedThisFrame)
    {
        int i = (int)dir;
        held[i] = isPressed;

        if (wasPressedThisFrame)
        {
            pressOrder.Remove(dir);
            pressOrder.Add(dir);
        }
        else if (!isPressed)
        {
            pressOrder.Remove(dir);
        }
    }

    private void ResetInput()
    {
        moveInput = Vector2.zero;
        pressOrder.Clear();
        for (int i = 0; i < 4; i++)
        {
            held[i] = false;
        }
    }

    private static Vector2 ToVector(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Vector2.up;
            case Direction.Down: return Vector2.down;
            case Direction.Left: return Vector2.left;
            case Direction.Right: return Vector2.right;
            default: return Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void LateUpdate()
    {
        if (facingIndicator != null)
        {
            facingIndicator.localPosition = FacingDirection * facingIndicatorDistance;
        }
    }
}
