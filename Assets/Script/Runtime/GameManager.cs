using UnityEngine;
using UnityEngine.SceneManagement;

public enum EState { Ready, Playing, Pause, Clear, GameOver }

public class GameManager : MonoBehaviour
{
    public const float CTimeLimit = 360f;
    public const float CScreenWidth = 1280f;
    public const float CScreenHeight = 720f;

    #region 인스펙터
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform startPoint;
    [SerializeField] private float timeLimit = CTimeLimit;
    [SerializeField] private Transform[] FakeOrExitList;
    #endregion

    #region 내부 변수
    public EState State { get; private set; } = EState.Ready;
    private Rigidbody _body;
    private Vector3 _returnPosition;
    private float _remaining;
    private float _safeUntil;
    private float _messageUntil;
    private string _message;
    private int _fakeExitsIndex;
    private bool _isHintFlag;
    private Vector3 _pauseVelocity;
    private bool _isCheatMode;
    private int _mistakes;
    #endregion

    private void Awake()
    {
        if (player == null || startPoint == null)
        {
            CPrint.Error("플레이어 또는 시작 지점 NULL => 미로 구성 메뉴 실행");
            enabled = false;
            return;
        }

        if (FakeOrExitList == null || FakeOrExitList.Length <= 1)
        {
            CPrint.Error("FakeOrExitList가 비어있음 (2개 이상 필요) => 미로 구성 메뉴 실행");
            enabled = false;
            return;
        }

        int randomIndex = Random.Range(0, FakeOrExitList.Length);
        _fakeExitsIndex = randomIndex;
        CPrint.Log($"FakeOrExitList에서 랜덤 인덱스 선택: {randomIndex}");
        var obj = FakeOrExitList[randomIndex].gameObject;
        Trigger trigger = obj.GetComponent<Trigger>();
        trigger.SetType(EMazeTriggerType.Exit);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _remaining = timeLimit;
    }

    private void Start()
    {
        if (player == null || startPoint == null)
        {
            CPrint.Error("플레이어 또는 시작 지점 NULL => 미로 구성 메뉴 실행");
            enabled = false;
            return;
        }
        _body = player.GetComponent<Rigidbody>();
        _returnPosition = startPoint.position;
        player.SetControl(false);
        Teleport();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            _isCheatMode = !_isCheatMode;
            CPrint.Log($"치트 모드: {_isCheatMode}");
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (State == EState.Playing)
            {
                Pause();
                return;
            }

            if (State == EState.Pause)
            {
                Resume();
                return;
            }
        }

        if (State == EState.Ready && Input.GetKeyDown(KeyCode.Return)) Begin();
        if ((State == EState.Clear || State == EState.GameOver) && Input.GetKeyDown(KeyCode.R)) Restart();
        if (State != EState.Playing) return;

        _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);

        if (_remaining <= timeLimit - 10f && !_isHintFlag)
        {
            _isHintFlag = true;

            switch (_fakeExitsIndex)
            {
                case 0:
                    ShowMessage("빠르게 올라가야해....");
                    break;
                case 1:
                    ShowMessage("합리적으로 가까운곳은?!");
                    break;
                case 2:
                    ShowMessage("때로는 가장 쉬운 길이 답일 때도 있지...");
                    break;
            }
        }

        if (_remaining <= 0f)
        {
            Finish(false);
            return;
        }

        if (player.transform.position.y < -5f) Hit();
    }

    private void Begin()
    {
        State = EState.Playing;
        _safeUntil = Time.time + 1f;
        player.SetControl(true);
        ShowMessage("폭발 장치가 작동했다!! 초록색 출구를 찾아라!");
    }

    private void Pause()
    {
        if (State != EState.Playing) return;

        _pauseVelocity = _body.velocity;
        State = EState.Pause;
        player.SetControl(false);
        AudioManager.Instance.StopWalk();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CPrint.Log("일시정지");
    }

    private void Resume()
    {
        if (State != EState.Pause) return;

        State = EState.Playing;
        Time.timeScale = 1f;
        player.SetControl(true);
        _body.velocity = _pauseVelocity;

        CPrint.Log("계속하기");
    }

    public bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<PlayerController>() == player;
    }    

    public void Hit()
    {
        if (_isCheatMode)
        {
            ShowMessage("치트 모드 활성화! 함정 무시!");
            return;
        }
        if (State != EState.Playing || Time.time < _safeUntil) return;
        _safeUntil = Time.time + 1.5f;
        _mistakes++;
        Teleport();
        ShowMessage("함정에 닿았다! 체크포인트로 복귀!");
        AudioManager.Instance.PlayRespawn();
    }

    public void Fake()
    {
        if (State != EState.Playing || Time.time < _safeUntil) return;
        _safeUntil = Time.time + 1.5f;
        _mistakes++;
        Teleport();
        ShowMessage("젠장! 가짜 출구였다! 체크포인트로 복귀!");
        AudioManager.Instance.PlayFakeExit();
    }

    private void Teleport()
    {
        _body.velocity = Vector3.zero;
        _body.angularVelocity = Vector3.zero;
        _body.position = _returnPosition;
        player.transform.position = _returnPosition;
        player.ResetMotion();
        Physics.SyncTransforms();
    }

    public void Checkpoint(Vector3 position)
    {
        if (State != EState.Playing) return;
        _returnPosition = position;
        ShowMessage("체크포인트");
        player.SetPlayerStamina(1f);
        AudioManager.Instance.PlayCheckpoint();
    }

    public void Finish(bool clear)
    {
        if (State != EState.Playing) return;
        State = clear ? EState.Clear : EState.GameOver;
        player.SetControl(false);
        _body.isKinematic = true;
        ShowMessage(clear ? "출구 개방! 탈출 성공!" : "제한시간 종료! 당신은 폭발했습니다!!");
        if (clear) AudioManager.Instance.PlayGameClear();
        else AudioManager.Instance.PlayGameOver();
    }

    private void ShowMessage(string text)
    {
        _message = text;
        _messageUntil = Time.time + 3f;
        CPrint.Log(text);
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Menu");
    }

    private void OnGUI()
    {
        float targetAspect = CScreenWidth / CScreenHeight;
        float currentAspect = (float)Screen.width / Screen.height;
        float scale = currentAspect > targetAspect ? (float)Screen.height / CScreenHeight : (float)Screen.width / CScreenWidth;

        Vector3 scaleVector = new Vector3(scale, scale, 1f);
        Vector3 offset = Vector3.zero;

        if (currentAspect > targetAspect)
        {
            offset.x = (Screen.width - (CScreenWidth * scale)) * 0.5f;
        }
        else
        {
            offset.y = (Screen.height - (CScreenHeight * scale)) * 0.5f;
        }

        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, scaleVector);

        GUIStyle lblStyle = new GUIStyle(GUI.skin.label);
        lblStyle.alignment = TextAnchor.MiddleCenter;
        lblStyle.fontStyle = FontStyle.Bold;
        lblStyle.fontSize = 52;
        lblStyle.normal.textColor = _remaining <= 30f ? Color.red : Color.white;

        int seconds = Mathf.CeilToInt(_remaining);
        float boxWidth = 200f;
        float boxX = (CScreenWidth - boxWidth) * 0.5f;

        GUI.Box(new Rect(boxX, 10, boxWidth, 80), string.Empty);
        GUI.Label(new Rect(boxX, 10, boxWidth, 80), $"{seconds / 60:00}:{seconds % 60:00}", lblStyle);

        lblStyle.fontSize = 20;
        lblStyle.normal.textColor = Color.white;
        GUI.Box(new Rect(10, 10, 460, 70), string.Empty);
        lblStyle.alignment = TextAnchor.MiddleLeft;
        GUI.Label(new Rect(20, 15, 450, 30), "WASD / 방향키 이동 · Shift 달리기 · Space 점프", lblStyle);        
        GUI.Label(new Rect(20, 45, 450, 30), "파랑 = 체크포인트, 초록 = 출구", lblStyle);
        lblStyle.alignment = TextAnchor.MiddleCenter;

        DrawStaminaBar(player.StaminaAmount);

        GUI.color = Color.white;
        if (Time.time < _messageUntil) GUI.Label(new Rect(90, 330, 1100, 45), _message, lblStyle);

        if (State == EState.Playing)
        {
            GUI.matrix = oldMatrix;
            return;
        }

        if (State == EState.Pause)
        {
            DrawPauseMenu();
            GUI.matrix = oldMatrix;
            return;
        }

        if (State == EState.GameOver)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 0.1f, 0.05f, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, CScreenWidth, CScreenHeight), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        GUI.Box(new Rect(340, 245, 600, 330), string.Empty);
        lblStyle.fontSize = 32;
        GUI.Label(new Rect(350, 265, 580, 55), StateText(), lblStyle);

        lblStyle.fontSize = 22;
        string description = State == EState.Ready ? "제한시간 내에 탈출해야한다" : State == EState.Clear ? "탈출에 성공했습니다!" : "폭발! 탈출에 실패했다!!";
        GUI.Label(new Rect(350, 335, 580, 45), description, lblStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 22;
        if (State == EState.Ready)
        {
            if (GUI.Button(new Rect(480, 405, 320, 55), "플레이 시작 (Enter)", btnStyle)) Begin();
        }
        else
        {
            if (GUI.Button(new Rect(480, 405, 320, 55), "재도전 (R)", btnStyle)) Restart();
        }

        if (GUI.Button(new Rect(480, 480, 320, 55), "메뉴로 이동", btnStyle)) GoMenu();

        GUI.matrix = oldMatrix;
    }

    private void DrawPauseMenu()
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0f, 0f, CScreenWidth, CScreenHeight), Texture2D.whiteTexture);
        GUI.color = oldColor;

        GUI.Box(new Rect(340f, 190f, 600f, 390f), string.Empty);

        GUIStyle lblStyle = new GUIStyle(GUI.skin.label);
        lblStyle.fontSize = 36;
        lblStyle.fontStyle = FontStyle.Bold;
        lblStyle.alignment = TextAnchor.MiddleCenter;
        lblStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(350f, 215f, 580f, 60f), "일시정지", lblStyle);


        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 22;
        if (GUI.Button(new Rect(480f, 310f, 320f, 55f), "계속 하기 (ESC)", btnStyle)) Resume();
        if (GUI.Button(new Rect(480f, 390f, 320f, 55f), "재시작", btnStyle)) Restart();
        if (GUI.Button(new Rect(480f, 470f, 320f, 55f), "메뉴 이동", btnStyle)) GoMenu();
    }

    private void DrawStaminaBar(float currentStamina)
    {
        float barWidth = 300f;
        float barHeight = 25f;
        float barX = 10f;
        float barY = CScreenHeight - barHeight - 10f;

        GUI.Box(new Rect(barX, barY, barWidth, barHeight), string.Empty);

        float fillWidth = barWidth * Mathf.Clamp01(currentStamina);

        if (fillWidth > 0f)
        {
            Color oldColor = GUI.color;
            GUI.color = currentStamina <= 0.2f ? Color.red : Color.cyan;
            GUI.DrawTexture(new Rect(barX + 2, barY + 2, fillWidth - 4, barHeight - 4), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }

    private string StateText()
    {
        if (State == EState.Ready) return "Are you ready?";
        if (State == EState.Playing) return "탈출 진행 중";
        if (State == EState.Clear) return "탈출 성공";
        return "탈출 실패";
    }
}
