using UnityEngine;

public class QuarterCamera : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-10f, 16f, -10f);
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float firstPersonHeight = 1.6f;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float firstPersonFieldOfView = 70f;
    #endregion

    #region 내부 변수
    private Camera _camera;
    private PlayerController _player;
    private Renderer[] _characterRenderers;
    private bool[] _rendererStates;
    private bool _isFirstPerson;
    private float _yaw;
    private float _pitch;
    #endregion

    private void Start()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            CPrint.Error("Camera NULL => Main Camera 확인");
            enabled = false;
            return;
        }
        if (target == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null) target = player.transform;
        }
        if (target == null)
        {
            CPrint.Error("Target NULL => QuarterCamera의 캐릭터 연결 확인");
            enabled = false;
            return;
        }
        _player = target.GetComponentInParent<PlayerController>();
        CacheRenderers();
        _isFirstPerson = MenuManager.IsHardMode;
        _yaw = target.eulerAngles.y;
        _pitch = 0f;
        if (_isFirstPerson)
        {
            _camera.orthographic = false;
            _camera.nearClipPlane = 0.03f;
            _camera.fieldOfView = firstPersonFieldOfView;
        }
        else
        {
            _camera.orthographic = true;
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(-offset);
        }
        SetCharacterVisible(!_isFirstPerson);
    }

    private void Update()
    {
        if (target == null) return;
        bool canControl = _player == null || _player.CanControl;
        bool lockCursor = _isFirstPerson && canControl;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
        if (!lockCursor) return;
        _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, -75f, 75f);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (_isFirstPerson)
        {
            transform.position = target.position + Vector3.up * firstPersonHeight;
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            return;
        }
        transform.position = Vector3.Lerp(transform.position, target.position + offset, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(-offset);
    }

    private void CacheRenderers()
    {
        Transform character = _player != null ? _player.transform : target;
        _characterRenderers = character.GetComponentsInChildren<Renderer>(true);
        _rendererStates = new bool[_characterRenderers.Length];
        for (int i = 0; i < _characterRenderers.Length; i++)
        {
            _rendererStates[i] = _characterRenderers[i].enabled;
        }
    }

    private void SetCharacterVisible(bool visible)
    {
        if (_characterRenderers == null) return;
        for (int i = 0; i < _characterRenderers.Length; i++)
        {
            if (_characterRenderers[i] == null) continue;
            _characterRenderers[i].enabled = visible && _rendererStates[i];
        }
    }

    public void SetTarget(Transform value)
    {
        SetCharacterVisible(true);
        target = value;
        if (target == null) return;
        _player = target.GetComponentInParent<PlayerController>();
        _yaw = target.eulerAngles.y;
        _pitch = 0f;
        CacheRenderers();
        SetCharacterVisible(!_isFirstPerson);
        transform.position = target.position + offset;
        transform.rotation = Quaternion.LookRotation(-offset);
    }

    private void OnDisable()
    {
        SetCharacterVisible(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
