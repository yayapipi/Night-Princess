using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalPos;
    private float shakeDuration;
    private float shakeIntensity;

    private void Awake()
    {
        Instance = this;
    }

    public void Shake(float duration = 0.12f, float intensity = 0.15f)
    {
        originalPos = transform.localPosition;
        shakeDuration = duration;
        shakeIntensity = intensity;
    }

    private void LateUpdate()
    {
        if (shakeDuration <= 0f) return;

        shakeDuration -= Time.deltaTime;

        float fade = shakeDuration / 0.12f; // fade out toward end
        Vector3 offset = Random.insideUnitCircle * shakeIntensity * Mathf.Min(fade, 1f);
        transform.localPosition = originalPos + offset;

        if (shakeDuration <= 0f)
        {
            transform.localPosition = originalPos;
        }
    }
}
