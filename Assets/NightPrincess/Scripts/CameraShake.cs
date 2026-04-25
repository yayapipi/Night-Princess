using UnityEngine;

namespace NightPrincess
{
public class CameraShake : MonoBehaviour
{
    private Vector3 baseLocalPos;
    private float shakeTimeLeft;
    private float strength;
    private bool initialized;
    private bool shaking;

    void OnEnable()
    {
        if (!initialized)
        {
            baseLocalPos = transform.localPosition;
            initialized = true;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        if (!shaking)
        {
            baseLocalPos = transform.localPosition;
        }
        shakeTimeLeft = Mathf.Max(shakeTimeLeft, duration);
        strength = Mathf.Max(strength, magnitude);
        shaking = true;
    }

    void LateUpdate()
    {
        if (!shaking) return;
        if (shakeTimeLeft > 0f)
        {
            Vector2 offset = Random.insideUnitCircle * strength;
            transform.localPosition = baseLocalPos + new Vector3(offset.x, offset.y, 0f);
            shakeTimeLeft -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = baseLocalPos;
            strength = 0f;
            shaking = false;
        }
    }
}
}
