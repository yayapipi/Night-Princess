using System.Collections;
using UnityEngine;

namespace NightPrincess
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float dashSpeed = 30f;
    public float dashDuration = 0.18f;
    public float dashMaxDistance = 8f;

    [Header("Dash Effect")]
    public LineRenderer dashLine;
    public float dashLineLifetime = 0.35f;
    public float dashLineWidth = 0.4f;
    public Gradient dashLineGradient;

    [Header("Camera Shake")]
    public Camera targetCamera;
    public float cameraShakeDuration = 0.18f;
    public float cameraShakeStrength = 0.25f;

    [Header("Death")]
    public GameObject deathEffectPrefab;
    public string deathEffectResourcePath = "Explosion";

    [Header("Animator State Names")]
    public string idleState = "Idle";
    public string walkState = "Walk";
    public string dashState = "Dash";

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool isDashing;
    private bool isDead;
    private Vector2 dashStart;
    private Vector2 dashTarget;
    private Vector2 lastDashDir = Vector2.right;
    private float dashTimer;
    private string currentAnim = "";
    private Coroutine dashLineRoutine;

    public bool IsDashing { get { return isDashing; } }
    public Vector2 DashDirection { get { return lastDashDir; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;
        if (dashLine == null) dashLine = GetComponent<LineRenderer>();
        if (dashLine != null)
        {
            dashLine.enabled = false;
            dashLine.positionCount = 0;
            dashLine.useWorldSpace = true;
        }
        if (deathEffectPrefab == null && !string.IsNullOrEmpty(deathEffectResourcePath))
        {
            deathEffectPrefab = Resources.Load<GameObject>(deathEffectResourcePath);
        }
    }

    void Update()
    {
        if (isDead) return;
        if (UILock.Active)
        {
            if (isDashing) StopDash();
            rb.linearVelocity = Vector2.zero;
            SetAnim(idleState);
            return;
        }
        if (!isDashing)
        {
            UpdateMovement();
            CheckDashInput();
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (UILock.Active) return;
        if (isDashing) StepDash();
    }

    void UpdateMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 move = new Vector2(h, v);
        if (move.sqrMagnitude > 1f) move.Normalize();
        rb.linearVelocity = move * walkSpeed;

        if (move.sqrMagnitude > 0.01f)
        {
            SetAnim(walkState);
            if (Mathf.Abs(h) > 0.01f) FaceDirection(h);
        }
        else
        {
            SetAnim(idleState);
        }
    }

    void CheckDashInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = -targetCamera.transform.position.z;
            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(mouseScreen);
            worldPoint.z = transform.position.z;
            BeginDash(worldPoint);
        }
    }

    void BeginDash(Vector2 target)
    {
        dashStart = transform.position;
        Vector2 delta = target - dashStart;
        if (delta.sqrMagnitude < 0.0001f) return;
        if (delta.magnitude > dashMaxDistance)
            target = dashStart + delta.normalized * dashMaxDistance;
        dashTarget = target;
        lastDashDir = (dashTarget - dashStart).normalized;
        dashTimer = 0f;
        isDashing = true;
        FaceDirection(lastDashDir.x);
        SetAnim(dashState);
        rb.linearVelocity = Vector2.zero;

        TriggerCameraShake();
        ShowDashLine(dashStart, dashTarget);
    }

    void StepDash()
    {
        dashTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(dashTimer / dashDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        Vector2 next = Vector2.Lerp(dashStart, dashTarget, eased);
        rb.MovePosition(next);
        if (t >= 1f) StopDash();
    }

    void StopDash()
    {
        if (!isDashing) return;
        isDashing = false;
        rb.linearVelocity = Vector2.zero;
        SetAnim(idleState);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (isDead) return;
        if (other.collider.CompareTag("Enemy"))
        {
            ResolveEnemyContact(other.collider);
            return;
        }
        if (isDashing) StopDash();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.CompareTag("Enemy"))
        {
            ResolveEnemyContact(other);
        }
    }

    void ResolveEnemyContact(Collider2D enemyCollider)
    {
        EnemyController enemy = enemyCollider.GetComponent<EnemyController>();
        if (enemy == null) enemy = enemyCollider.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.IsDead) return;

        if (PlayerIsBehind(enemy))
        {
            enemy.Die();
            StopDash();
        }
        else
        {
            enemy.AttackPlayer(this);
        }
    }

    bool PlayerIsBehind(EnemyController enemy)
    {
        Vector2 facing = enemy.GetFacing();
        if (facing.sqrMagnitude < 0.0001f) return false;
        Vector2 toPlayer = ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
        return Vector2.Dot(toPlayer, facing) < 0f;
    }

    void FaceDirection(float xDir)
    {
        if (Mathf.Abs(xDir) < 0.01f) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (xDir < 0f ? -1f : 1f);
        transform.localScale = s;
    }

    void SetAnim(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        if (currentAnim == stateName) return;
        animator.Play(stateName);
        currentAnim = stateName;
    }

    void TriggerCameraShake()
    {
        if (targetCamera == null) return;
        CameraShake shake = targetCamera.GetComponent<CameraShake>();
        if (shake == null) shake = targetCamera.gameObject.AddComponent<CameraShake>();
        shake.Shake(cameraShakeDuration, cameraShakeStrength);
    }

    void ShowDashLine(Vector2 from, Vector2 to)
    {
        if (dashLine == null) return;
        if (dashLineRoutine != null) StopCoroutine(dashLineRoutine);
        dashLineRoutine = StartCoroutine(DashLineRoutine(from, to));
    }

    IEnumerator DashLineRoutine(Vector2 from, Vector2 to)
    {
        dashLine.enabled = true;
        dashLine.useWorldSpace = true;
        dashLine.positionCount = 2;
        dashLine.startWidth = dashLineWidth;
        dashLine.endWidth = dashLineWidth * 0.2f;
        if (dashLineGradient != null && dashLineGradient.colorKeys != null && dashLineGradient.colorKeys.Length > 0)
            dashLine.colorGradient = dashLineGradient;
        dashLine.SetPosition(0, from);
        dashLine.SetPosition(1, from);

        float t = 0f;
        while (t < dashDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / dashDuration);
            Vector2 head = Vector2.Lerp(from, to, a);
            dashLine.SetPosition(1, head);
            yield return null;
        }

        float life = Mathf.Max(0.05f, dashLineLifetime);
        float t2 = 0f;
        Gradient baseGrad = dashLine.colorGradient;
        while (t2 < life)
        {
            t2 += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t2 / life);
            Gradient g = new Gradient();
            GradientColorKey[] ck = baseGrad.colorKeys;
            GradientAlphaKey[] ak = baseGrad.alphaKeys;
            for (int i = 0; i < ak.Length; i++) ak[i].alpha = Mathf.Clamp01(ak[i].alpha * k);
            g.SetKeys(ck, ak);
            dashLine.colorGradient = g;
            yield return null;
        }
        dashLine.enabled = false;
        dashLine.positionCount = 0;
        dashLineRoutine = null;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
        SetAnim(idleState);
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (dashLine != null) dashLine.enabled = false;
        GameManager.Instance.RestartGame();
    }
}
}
