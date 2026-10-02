using UnityEngine;

public enum EMazeTriggerType { Trap, Checkpoint, Fake, Exit }

public class Trigger : MonoBehaviour
{
    private const float CMaxScaleY = 5f;
    private const float CPingPongSpeed = 5f;

    #region 인스펙터
    [SerializeField] private GameManager game;
    [SerializeField] private EMazeTriggerType type;
    [SerializeField] private Transform returnPoint;
    [SerializeField] private bool isHeightScaleChange;
    #endregion

    #region 내부 변수
    private bool _used;
    private float _scaleY;
    #endregion

    public void Init(GameManager value, EMazeTriggerType kind, Transform point = null, bool heightScaleChange = false)
    {
        game = value;
        type = kind;
        returnPoint = point;
        isHeightScaleChange = heightScaleChange;
    }

    public void SetType(EMazeTriggerType kind)
    {
        type = kind;
    }

    private void Awake()
    {
        _scaleY = transform.localScale.y;
    }


    private void Update()
    {
        if (isHeightScaleChange)
        {
            float scaleY = Mathf.PingPong(Time.time * CPingPongSpeed, CMaxScaleY);
            transform.localScale = new Vector3(transform.localScale.x, scaleY, transform.localScale.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Touch(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (type == EMazeTriggerType.Trap) Touch(other);
    }

    private void Touch(Collider other)
    {
        if (game == null || game.State != EState.Playing || !game.IsPlayer(other)) return;

        switch (type)
        {
            case EMazeTriggerType.Trap:
                game.Hit();
                break;

            case EMazeTriggerType.Fake:
                gameObject.SetActive(false);
                game.Fake();
                break;

            case EMazeTriggerType.Exit:
                game.Finish(true);
                break;

            case EMazeTriggerType.Checkpoint:
                if (_used) return;
                _used = true;
                game.Checkpoint(returnPoint.position);
                GetComponent<Renderer>().material.color = Color.cyan;
                break;
        }
    }
}
