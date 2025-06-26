using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FirstPersonController : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpForce = 8f;
    public float mouseSensitivity = 2f;

    [Header("潜行设置")]
    public float crouchSpeed = 3f;
    public float crouchHeight = 1f;
    public float standHeight = 2f;

    [Header("地面检测")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 1.1f;

    // 组件引用
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Camera playerCamera;

    // 状态变量
    private bool isGrounded;
    private bool isCrouching;
    private float currentSpeed;
    private float mouseX, mouseY;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerCamera = GetComponentInChildren<Camera>();

        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 设置初始值
        currentSpeed = walkSpeed;

        // 冻结旋转，防止物理系统影响
        rb.freezeRotation = true;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovementInput();
        HandleCrouch();
        HandleJump();
        CheckGrounded();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMouseLook()
    {
        mouseX += Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        mouseY = Mathf.Clamp(mouseY, -90f, 90f);

        // 水平旋转身体
        transform.rotation = Quaternion.Euler(0, mouseX, 0);
        // 垂直旋转摄像机
        playerCamera.transform.localRotation = Quaternion.Euler(mouseY, 0, 0);
    }

    void HandleMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        moveDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        // 选择移动速度
        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && isGrounded)
        {
            currentSpeed = runSpeed;
        }
        else if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
    }

    void HandleMovement()
    {
        Vector3 movement = moveDirection * currentSpeed;
        movement.y = rb.velocity.y; // 保持Y轴速度(重力)
        rb.velocity = movement;
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;

            if (isCrouching)
            {
                capsuleCollider.height = crouchHeight;
                playerCamera.transform.localPosition = new Vector3(0, crouchHeight - 0.5f, 0);
            }
            else
            {
                // 检查头顶是否有障碍物
                if (!Physics.Raycast(transform.position, Vector3.up, standHeight))
                {
                    capsuleCollider.height = standHeight;
                    playerCamera.transform.localPosition = new Vector3(0, standHeight - 0.5f, 0);
                }
                else
                {
                    isCrouching = true; // 无法站起来
                }
            }
        }
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void CheckGrounded()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer);
    }

    // 用于调试
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
    }
}
