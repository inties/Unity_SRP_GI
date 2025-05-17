using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("移动速度")]
    public float moveSpeed = 5f;
    [Tooltip("按住Shift键时的移动速度倍率")]
    public float shiftMultiplier = 2f;

    [Header("旋转设置")]
    [Tooltip("旋转速度")]
    public float rotateSpeed = 2f;
    [Tooltip("是否反转Y轴旋转")]
    public bool invertY = true;

    [Header("FOV设置")]
    [Tooltip("最小FOV")]
    public float minFOV = 30f;
    [Tooltip("最大FOV")]
    public float maxFOV = 90f;
    [Tooltip("FOV变化速度")]
    public float fovSpeed = 5f;

    private Camera cam;
    private float currentFOV;
    private Vector3 lastMousePosition;
    private bool isRotating = false;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("未找到Camera组件！");
            return;
        }
        currentFOV = cam.fieldOfView;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleFOV();
    }

    private void HandleMovement()
    {
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            speed *= shiftMultiplier;
        }

        Vector3 moveDirection = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) moveDirection += transform.forward;
        if (Input.GetKey(KeyCode.S)) moveDirection -= transform.forward;
        if (Input.GetKey(KeyCode.A)) moveDirection -= transform.right;
        if (Input.GetKey(KeyCode.D)) moveDirection += transform.right;

        // 保持Y轴不变，只在地平面上移动
        //moveDirection.y = 0;
        moveDirection.Normalize();

        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        // 按下鼠标右键开始旋转
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }
        // 松开鼠标右键停止旋转
        else if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }

        if (isRotating)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            
            // 水平旋转（绕Y轴）
            transform.Rotate(Vector3.up, delta.x * rotateSpeed, Space.World);
            
            // 垂直旋转（绕X轴）
            float verticalRotation = delta.y * rotateSpeed * (invertY ? -1 : 1);
            transform.Rotate(Vector3.right, verticalRotation, Space.Self);

            lastMousePosition = Input.mousePosition;
        }
    }

    private void HandleFOV()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentFOV = Mathf.Clamp(currentFOV - scroll * fovSpeed, minFOV, maxFOV);
            cam.fieldOfView = currentFOV;
        }
    }
} 