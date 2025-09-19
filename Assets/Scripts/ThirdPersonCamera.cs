using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;      // 拖 Player
    public Vector2 sens = new Vector2(160f, 120f);
    public Vector2 pitchLimits = new Vector2(-30f, 60f);
    public float distance = 4f;
    public float smooth = 10f;

    float yaw, pitch;

    void Start()
    {
        var e = transform.eulerAngles;
        yaw = e.y; pitch = e.x;
        Cursor.lockState = CursorLockMode.Locked; // 鼠标锁定
    }

    void LateUpdate()
    {
        // 鼠标移动（或手柄右摇杆）
        Vector2 look = Vector2.zero;
        if (Mouse.current != null) look += Mouse.current.delta.ReadValue();
        if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * 10f; // 放大手柄灵敏度

        // 与旧 GetAxis * dt 的手感接近：这里保留 * Time.deltaTime
        yaw += look.x * sens.x * Time.deltaTime;
        pitch -= look.y * sens.y * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position - rot * Vector3.forward * distance + Vector3.up * 1.5f;
        transform.position = Vector3.Lerp(transform.position, desiredPos, 1 - Mathf.Exp(-smooth * Time.deltaTime));
        transform.rotation = rot;
    }
}
