using UnityEngine;

[DisallowMultipleComponent]
public class SimpleFlyOrbitCamera : MonoBehaviour
{
    [Header("Look")]
    public bool holdRightMouseToLook = true;
    public float lookSensitivity = 2.0f;     // degrees per mouse delta unit
    public bool invertY = false;
    [Range(-89f, 0f)] public float minPitch = -89f;
    [Range(0f, 89f)] public float maxPitch = 89f;

    [Header("Fly Move")]
    public float moveSpeed = 8f;             // base speed (units/sec)
    public float fastMultiplier = 4f;        // when holding Shift
    public float slowMultiplier = 0.25f;     // when holding Alt
    public KeyCode ascendKey = KeyCode.Space;
    public KeyCode descendKey = KeyCode.LeftControl;

    [Header("Orbit (hold Alt + LMB)")]
    public Transform orbitTarget;            // optional; if null uses orbitPivot
    public Vector3 orbitPivot = Vector3.zero;
    public float orbitDistance = 10f;
    public float minOrbitDistance = 0.5f;
    public float maxOrbitDistance = 200f;

    float _yaw;
    float _pitch;
    bool _looking;

    void Start()
    {
        Vector3 e = transform.eulerAngles;
        _yaw = e.y;
        _pitch = NormalizePitch(e.x);
    }

    void Update()
    {
        // Escape always releases cursor
        if (Input.GetKeyDown(KeyCode.Escape))
            ReleaseCursor();

        // Detect look engagement
        if (holdRightMouseToLook)
        {
            if (Input.GetMouseButtonDown(1)) AcquireCursor();
            if (Input.GetMouseButtonUp(1)) ReleaseCursor();
            _looking = Input.GetMouseButton(1);
        }
        else
        {
            _looking = true; // always mouselook
            if (!Cursor.lockState.Equals(CursorLockMode.Locked)) AcquireCursor();
        }

        // Check orbit mode (Alt + LMB)
        bool orbiting = Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0);

        // Mouse rotation input
        float mx = _looking || orbiting ? Input.GetAxisRaw("Mouse X") : 0f;
        float my = _looking || orbiting ? Input.GetAxisRaw("Mouse Y") : 0f;
        if (invertY) my = -my;

        if (orbiting)
        {
            DoOrbit(mx, my);
        }
        else
        {
            // Free look
            if (_looking)
            {
                _yaw += mx * lookSensitivity;
                _pitch -= my * lookSensitivity;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            // Fly movement
            DoFlyMove();
        }
    }

    void DoFlyMove()
    {
        Vector3 move = Vector3.zero;

        float horiz = Input.GetAxisRaw("Horizontal"); // A/D
        float vert = Input.GetAxisRaw("Vertical");   // W/S
        move += transform.forward * vert;
        move += transform.right * horiz;

        if (Input.GetKey(ascendKey)) move += Vector3.up;
        if (Input.GetKey(descendKey)) move += Vector3.down;

        // speed modifiers
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= fastMultiplier;
        if (Input.GetKey(KeyCode.LeftAlt)) speed *= slowMultiplier;

        // Scroll to tweak fly speed
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            moveSpeed = Mathf.Clamp(moveSpeed * Mathf.Pow(1.1f, scroll), 0.1f, 500f);
        }

        if (move.sqrMagnitude > 0f)
        {
            transform.position += move.normalized * speed * Time.unscaledDeltaTime;
        }
    }

    void DoOrbit(float mx, float my)
    {
        // Choose pivot
        Vector3 pivot = orbitTarget ? orbitTarget.position : orbitPivot;

        // Adjust yaw/pitch
        _yaw += mx * lookSensitivity;
        _pitch -= my * lookSensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        // Zoom orbit distance with scroll
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            orbitDistance = Mathf.Clamp(orbitDistance * Mathf.Pow(1.1f, -scroll),
                                        minOrbitDistance, maxOrbitDistance);
        }

        // Recompute position from pivot + rotation + distance
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 dir = rot * Vector3.forward;           // camera looks along +Z in local space
        transform.position = pivot - dir * orbitDistance;
        transform.rotation = rot;
    }

    float NormalizePitch(float xDegrees)
    {
        // Convert 0..360 to -180..180 then clamp
        float p = xDegrees;
        if (p > 180f) p -= 360f;
        return Mathf.Clamp(p, minPitch, maxPitch);
    }

    void AcquireCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
