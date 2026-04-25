using UnityEngine;
using UnityEngine.SceneManagement;

namespace NightPrincess
{
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                _instance = Object.FindFirstObjectByType<GameManager>();
#else
                _instance = Object.FindObjectOfType<GameManager>();
#endif
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Restart")]
    public float restartDelay = 1.0f;

    private bool restartScheduled;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void RestartGame()
    {
        if (restartScheduled) return;
        restartScheduled = true;
        Invoke(nameof(DoRestart), restartDelay);
    }

    void DoRestart()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
}
