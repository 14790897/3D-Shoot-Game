using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;
    public Transform cameraPivot;

    CharacterController cc;
    Vector3 velocity;
    bool grounded;

    void Awake() { cc = GetComponent<CharacterController>(); }

    void Update()
    {
        grounded = cc.isGrounded;
        if (grounded && velocity.y < 0) velocity.y = -2f;

        // 读输入（键盘 WASD/方向键 或 手柄摇杆）
        Vector2 moveAxis = Vector2.zero;
        if (Gamepad.current != null)
        {
            moveAxis = Gamepad.current.leftStick.ReadValue();
        }
        if (Keyboard.current != null)
        {
            float kh = 0f, kv = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) kh -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) kh += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) kv -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) kv += 1f;
            Vector2 kb = new Vector2(kh, kv);
            if (kb.sqrMagnitude > moveAxis.sqrMagnitude) moveAxis = kb; // 键盘覆盖更强输入
        }
        moveAxis = Vector2.ClampMagnitude(moveAxis, 1f);
        float h = moveAxis.x;
        float v = moveAxis.y;

        // 相机朝向投影到水平面，保证“前后左右”随相机方向变化
        Vector3 camForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1,0,1)).normalized;
        Vector3 camRight = Camera.main.transform.right;
        Vector3 move = (camForward * v + camRight * h).normalized;

        cc.Move(move * moveSpeed * Time.deltaTime);

        // 跳跃（空格 / 手柄A）
        bool jumpPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                           || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (jumpPressed && grounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // 重力
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);

        // 面朝移动方向
        if (move.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 0.15f);
    }
}
