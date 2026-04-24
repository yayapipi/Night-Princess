using System.Collections;
using TMPro;
using UnityEngine;

namespace NightPrincess.Princess
{
    public class TypewriterEffect : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;
        [SerializeField] private float charactersPerSecond = 35f;

        private Coroutine running;
        public bool IsRunning => running != null;

        public void SetTarget(TMP_Text t) => target = t;

        public void Show(string text)
        {
            Stop();
            if (target == null) return;
            running = StartCoroutine(TypeRoutine(text));
        }

        public void ShowInstant(string text)
        {
            Stop();
            if (target != null) target.text = text;
        }

        public void Stop()
        {
            if (running != null) StopCoroutine(running);
            running = null;
        }

        private IEnumerator TypeRoutine(string text)
        {
            target.text = string.Empty;
            float interval = charactersPerSecond > 0 ? 1f / charactersPerSecond : 0.02f;
            var wait = new WaitForSeconds(interval);
            for (int i = 0; i < text.Length; i++)
            {
                target.text += text[i];
                yield return wait;
            }
            running = null;
        }
    }
}
