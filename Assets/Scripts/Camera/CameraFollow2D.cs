using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;
    [SerializeField] private float smoothTime = 0.15f;

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void Configure(Transform followTarget, Vector2 min, Vector2 max)
    {
        target = followTarget;
        minBounds = min;
        maxBounds = max;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float clampedX = ClampAxis(target.position.x, minBounds.x, maxBounds.x, halfWidth);
        float clampedY = ClampAxis(target.position.y, minBounds.y, maxBounds.y, halfHeight);

        var desired = new Vector3(clampedX, clampedY, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    private static float ClampAxis(float targetPos, float min, float max, float halfExtent)
    {
        float low = min + halfExtent;
        float high = max - halfExtent;

        if (low > high)
        {
            return (min + max) * 0.5f;
        }

        return Mathf.Clamp(targetPos, low, high);
    }
}
