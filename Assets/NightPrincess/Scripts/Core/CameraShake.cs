using System.Collections;
using UnityEngine;

namespace NightPrincess.Core
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float defaultDuration = 0.15f;
        [SerializeField] private float defaultMagnitude = 0.25f;

        private Vector3 basePosition;
        private Coroutine running;

        private void Awake()
        {
            Instance = this;
            basePosition = transform.localPosition;
        }

        public void Shake() => Shake(defaultDuration, defaultMagnitude);

        public void Shake(float duration, float magnitude)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float t = 0f;
            while (t < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                transform.localPosition = basePosition + new Vector3(x, y, 0f);
                t += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = basePosition;
            running = null;
        }
    }
}
