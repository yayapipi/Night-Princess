using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NightPrincess.Core
{
    public class FlashEffect : MonoBehaviour
    {
        public static FlashEffect Instance { get; private set; }

        [SerializeField] private Image flashImage;
        [SerializeField] private float defaultDuration = 0.12f;
        [SerializeField] private Color flashColor = Color.white;

        private Coroutine running;

        private void Awake()
        {
            Instance = this;
            EnsureImage();
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            flashImage.raycastTarget = false;
        }

        private void EnsureImage()
        {
            if (flashImage != null) return;
            var canvasGo = new GameObject("FlashCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasGo.AddComponent<CanvasScaler>();
            var imgGo = new GameObject("FlashImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            flashImage = imgGo.AddComponent<Image>();
            var rt = flashImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void Flash() => Flash(defaultDuration);

        public void Flash(float duration)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(FlashRoutine(duration));
        }

        private IEnumerator FlashRoutine(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                float k = 1f - (t / duration);
                flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, k);
                t += Time.deltaTime;
                yield return null;
            }
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            running = null;
        }
    }
}
