using UnityEngine;

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
        yaw += Input.GetAxis("Mouse X") * sens.x * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * sens.y * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position - rot * Vector3.forward * distance + Vector3.up * 1.5f;
        transform.position = Vector3.Lerp(transform.position, desiredPos, 1 - Mathf.Exp(-smooth * Time.deltaTime));
        transform.rotation = rot;
    }
}
