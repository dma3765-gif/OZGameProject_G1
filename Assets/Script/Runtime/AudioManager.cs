using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }

            return _instance;
        }
    }

    #region 인스펙터
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip checkpointSound;
    [SerializeField] private AudioClip respawnSound;
    [SerializeField] private AudioClip fakeExitSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip gameClearSound;
    #endregion

    #region 내부 변수
    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private AudioSource _walkSource;
    #endregion

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _walkSource = gameObject.AddComponent<AudioSource>();

        _bgmSource.playOnAwake = false;
        _sfxSource.playOnAwake = false;
        _walkSource.playOnAwake = false;

        _bgmSource.loop = true;
        _walkSource.loop = true;
        _walkSource.clip = walkSound;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        _instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopWalk();

        if (scene.name == "Menu" || scene.name == "Game")
        {
            PlayBGM();
        }
    }

    public void PlayBGM()
    {
        if (backgroundMusic == null) return;
        if (_bgmSource.clip == backgroundMusic && _bgmSource.isPlaying) return;

        _bgmSource.clip = backgroundMusic;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void PlayWalk(bool isRunning = false)
    {
        if (walkSound == null) return;

        _walkSource.pitch = isRunning ? 2f : 1f;

        if (_walkSource.isPlaying) return;

        _walkSource.clip = walkSound;
        _walkSource.Play();
    }

    public void StopWalk()
    {
        if (_walkSource == null || !_walkSource.isPlaying) return;

        _walkSource.Stop();
    }

    public void PlayJump() => PlaySFX(jumpSound);
    public void PlayCheckpoint() => PlaySFX(checkpointSound);
    public void PlayRespawn() => PlaySFX(respawnSound);
    public void PlayFakeExit() => PlaySFX(fakeExitSound);
    public void PlayGameOver() => PlaySFX(gameOverSound);
    public void PlayGameClear() => PlaySFX(gameClearSound);

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        _sfxSource.PlayOneShot(clip);
    }
}