using UnityEngine;

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

        // 读输入（WASD / 方向键）
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 相机朝向投影到水平面，保证“前后左右”随相机方向变化
        Vector3 camForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1,0,1)).normalized;
        Vector3 camRight = Camera.main.transform.right;
        Vector3 move = (camForward * v + camRight * h).normalized;

        cc.Move(move * moveSpeed * Time.deltaTime);

        // 跳跃（空格）
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // 重力
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);

        // 面朝移动方向
        if (move.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 0.15f);
    }
}
