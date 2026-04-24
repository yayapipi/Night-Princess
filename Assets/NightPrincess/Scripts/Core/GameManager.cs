using System.Collections;
using NightPrincess.Akarion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NightPrincess.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private float restartDelay = 1.5f;
        [SerializeField] private GameObject playerDeathPrefab;
        [SerializeField] private string fallbackDeathPrefabResource = "Explosion";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (playerDeathPrefab == null && !string.IsNullOrEmpty(fallbackDeathPrefabResource))
                playerDeathPrefab = Resources.Load<GameObject>(fallbackDeathPrefabResource);
        }

        public void OnPlayerDied(Vector3 position)
        {
            if (playerDeathPrefab != null)
                Instantiate(playerDeathPrefab, position, Quaternion.identity);

            AkarionEventLogger.Instance?.LogEvent(AkarionEvents.PlayerDied);
            StartCoroutine(RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            yield return new WaitForSeconds(restartDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
