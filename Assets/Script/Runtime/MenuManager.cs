using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    #region 내부 변수
    public static bool IsHardMode { get; private set; }
    private bool _isLoading;
    #endregion

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartNormal()
    {
        StartGame(false);
    }

    public void StartHard()
    {
        StartGame(true);
    }

    private void StartGame(bool hardMode)
    {
        if (_isLoading) return;
        _isLoading = true;
        IsHardMode = hardMode;
        CPrint.Log(hardMode ? "어려움: 1인칭으로 시작" : "보통: 쿼터뷰로 시작");
        SceneManager.LoadScene("Game");
    }

    public void Quit()
    {
        CPrint.Log("게임 종료");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
