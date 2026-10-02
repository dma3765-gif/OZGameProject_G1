using UnityEngine;

public class MovingTrap : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private GameManager game;
    [SerializeField] private Vector3 movement = new Vector3(0f, 0f, 2.4f);
    [SerializeField] private float speed = 2f;
    #endregion

    #region 내부 변수
    private Vector3 _startPosition;
    private float _distance;
    private bool _forward = true;
    private Rigidbody _body; 
    #endregion

    public void Init(GameManager value)
    {
        game = value;
    }

    private void Awake()
    {
        _startPosition = transform.position;
        _body = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (game == null || game.State != EState.Playing)
        {
            return;
        }

        float currentSpeed = _forward ? speed : -speed;
        _distance += currentSpeed * Time.fixedDeltaTime;

        float maxDistance = movement.magnitude;

        if (_distance >= maxDistance)
        {
            _forward = false;
        }
        else if (_distance <= 0f)
        {
            _forward = true;
        }

        _distance = Mathf.Clamp(_distance, 0f, maxDistance);

        Vector3 targetPosition = _startPosition + (movement.normalized * _distance);
        _body.MovePosition(targetPosition);
    }
}
