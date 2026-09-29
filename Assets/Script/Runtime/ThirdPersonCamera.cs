using UnityEngine;

public class MissionThirdPersonCamera : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float distance = 5f;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float followSpeed = 12f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 75f;
    #endregion

    #region 내부 변수
    private float _yaw;
    private float _pitch = 15f;
    #endregion

    private void Start()
    {
        _yaw = transform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _yaw += mouseX * mouseSensitivity;
        _pitch -= mouseY * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 focusPosition = target.position + targetOffset;
        Vector3 targetPosition = focusPosition - rotation * Vector3.forward * distance;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        transform.rotation = rotation;
    }

    public void SetTarget(Transform value)
    {
        target = value;
    }
}