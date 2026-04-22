using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 40f;
    [SerializeField] private float dashArriveThreshold = 0.15f;
    [SerializeField] private LayerMask wallMask;

    [Header("Line Renderer")]
    [SerializeField] private float trailFadeDuration = 0.3f;
    [SerializeField] private float trailWidth = 0.15f;
    [SerializeField] private Color trailColor = new Color(0.4f, 0.8f, 1f, 0.8f);

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeIntensity = 0.15f;

    [Header("Kill Effect")]
    [SerializeField] private float hitStopDuration = 0.05f;

    [Header("Death")]
    [SerializeField] private GameObject deathParticlePrefab;

    [Header("Animation Clip Names")]
    [SerializeField] private string idleAnim = "Idle";
    [SerializeField] private string walkAnim = "Walk";
    [SerializeField] private string dashAnim = "Dash";

    private Rigidbody2D rb;
    private Animator animator;
    private LineRenderer lineRenderer;
    private Collider2D col;

    private bool isDashing;
    private bool isDead;
    private Vector3 dashOrigin;
    private Vector3 dashTarget;
    private float trailFadeTimer;
    private string currentAnim;
    private bool isInteracting;

    public bool IsDashing => isDashing;
    public Vector3 DashDirection => (dashTarget - dashOrigin).normalized;

    public void SetInteracting(bool interacting)
    {
        isInteracting = interacting;
        if (interacting)
        {
            rb.linearVelocity = Vector2.zero;
            PlayAnim(idleAnim);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        lineRenderer = GetComponent<LineRenderer>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = trailWidth;
        lineRenderer.endWidth = trailWidth * 0.3f;
        lineRenderer.startColor = trailColor;
        lineRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        lineRenderer.useWorldSpace = true;

        if (lineRenderer.material == null || lineRenderer.material.shader.name == "Hidden/InternalErrorShader")
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    private void PlayAnim(string animName)
    {
        if (currentAnim == animName) return;
        currentAnim = animName;
        animator.Play(animName);
    }

    private void Update()
    {
        if (isDead || isInteracting) return;

        if (isDashing)
        {
            UpdateDash();
            return;
        }

        HandleMovement();
        HandleDashInput();
        UpdateTrailFade();
    }

    private void HandleMovement()
    {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;

        Vector2 moveDir = new Vector2(h, v).normalized;
        rb.linearVelocity = moveDir * moveSpeed;

        PlayAnim(moveDir.sqrMagnitude > 0.01f ? walkAnim : idleAnim);

        if (h != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = h > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void HandleDashInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            StartDash(mouseWorld);
        }
    }

    private void StartDash(Vector3 target)
    {
        isDashing = true;
        dashOrigin = transform.position;
        dashTarget = target;

        // Raycast to check for walls between origin and target
        Vector2 dir = (target - dashOrigin);
        float dist = dir.magnitude;
        Vector2 size = col is BoxCollider2D box ? box.size * 0.9f : Vector2.one * 0.3f;
        RaycastHit2D hit = Physics2D.BoxCast(dashOrigin, size, 0f, dir.normalized, dist, wallMask);
        if (hit.collider != null)
        {
            // Stop just before the wall
            dashTarget = (Vector3)hit.point - (Vector3)(dir.normalized * 0.2f);
            dashTarget.z = 0f;
        }

        rb.linearVelocity = Vector2.zero;
        PlayAnim(dashAnim);

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeDuration, shakeIntensity);

        // Flip sprite toward dash target
        float dirX = dashTarget.x - dashOrigin.x;
        if (Mathf.Abs(dirX) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = dirX > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, dashOrigin);
        lineRenderer.SetPosition(1, dashOrigin);

        lineRenderer.startColor = trailColor;
        lineRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        trailFadeTimer = 0f;
    }

    private void UpdateDash()
    {
        Vector3 pos = transform.position;
        float distance = (dashTarget - pos).magnitude;

        if (distance < dashArriveThreshold)
        {
            transform.position = dashTarget;
            rb.linearVelocity = Vector2.zero;
            isDashing = false;

            lineRenderer.SetPosition(1, dashTarget);
            trailFadeTimer = trailFadeDuration;

            PlayAnim(idleAnim);
            return;
        }

        Vector3 newPos = Vector3.MoveTowards(pos, dashTarget, dashSpeed * Time.deltaTime);
        transform.position = newPos;

        lineRenderer.SetPosition(1, newPos);
    }

    private void UpdateTrailFade()
    {
        if (trailFadeTimer <= 0f) return;

        trailFadeTimer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(trailFadeTimer / trailFadeDuration);

        Color startC = trailColor;
        startC.a = trailColor.a * alpha;
        lineRenderer.startColor = startC;

        Color endC = trailColor;
        endC.a = 0f;
        lineRenderer.endColor = endC;

        if (trailFadeTimer <= 0f)
        {
            lineRenderer.positionCount = 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (isDashing && other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                // Check if attacking from behind
                Vector2 dashDir = DashDirection;
                Vector2 enemyFacing = enemy.FacingDirection;

                // Dot > 0 means player dash direction is same as enemy facing = from behind
                float dot = Vector2.Dot(dashDir, enemyFacing);
                if (dot > 0f)
                {
                    enemy.Die();

                    if (CameraShake.Instance != null)
                        CameraShake.Instance.Shake(shakeDuration * 1.5f, shakeIntensity * 1.5f);

                    StartCoroutine(HitStop());
                }
                else
                {
                    // Attacked from front — player dies
                    Die();
                }
            }
        }
    }

    private System.Collections.IEnumerator HitStop()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        // Spawn death particle prefab
        if (deathParticlePrefab != null)
        {
            var fx = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        // Hide sprite
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        Invoke(nameof(RestartScene), 1f);
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
