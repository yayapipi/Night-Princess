using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
    public enum PatrolAxis { Horizontal, Vertical }

    [Header("Patrol")]
    [SerializeField] private PatrolAxis patrolAxis = PatrolAxis.Horizontal;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolDistance = 3f;

    [Header("Detection (Front Arc)")]
    [SerializeField] private float detectRange = 4f;
    [SerializeField] private float detectArcAngle = 90f;
    [SerializeField] private bool showArcInGame = true;
    [SerializeField] private Color arcColor = new Color(1f, 0f, 0f, 0.15f);
    [SerializeField] private int arcSegments = 20;

    [Header("Attack")]
    [SerializeField] private float chargeSpeed = 12f;

    [Header("Death")]
    [SerializeField] private GameObject deathParticlePrefab;

    [Header("Animation Clip Names")]
    [SerializeField] private string idleAnim = "Idle";
    [SerializeField] private string walkAnim = "Walk";
    [SerializeField] private string chargeAnim = "Dash";
    [SerializeField] private string attackAnim = "Attack";

    private Rigidbody2D rb;
    private Animator animator;
    private string currentAnim;

    private Vector3 patrolOrigin;
    private int patrolDir = 1;
    private bool isDead;

    // Patrol: target position we're walking toward
    private Vector3 patrolTargetPos;

    // Runtime arc mesh
    private GameObject arcObject;
    private MeshFilter arcMeshFilter;
    private MeshRenderer arcMeshRenderer;
    private Mesh arcMesh;

    private enum State { Patrol, Charge }
    private State state = State.Patrol;
    private Transform targetPlayer;

    public Vector2 FacingDirection
    {
        get
        {
            if (patrolAxis == PatrolAxis.Horizontal)
                return new Vector2(patrolDir, 0f);
            else
                return new Vector2(0f, patrolDir);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        patrolOrigin = transform.position;
        SetPatrolTarget();

        if (showArcInGame)
            CreateArcVisual();
    }

    private void SetPatrolTarget()
    {
        if (patrolAxis == PatrolAxis.Horizontal)
            patrolTargetPos = patrolOrigin + Vector3.right * patrolDir * patrolDistance;
        else
            patrolTargetPos = patrolOrigin + Vector3.up * patrolDir * patrolDistance;
    }

    private void PlayAnim(string animName)
    {
        if (currentAnim == animName) return;
        currentAnim = animName;
        animator.Play(animName);
    }

    private void Update()
    {
        if (isDead) return;

        switch (state)
        {
            case State.Patrol:
                UpdatePatrol();
                DetectPlayer();
                break;
            case State.Charge:
                UpdateCharge();
                break;
        }

        if (showArcInGame && arcObject != null)
            UpdateArcVisual();
    }

    private void UpdatePatrol()
    {
        // Check if we've reached (or passed) the target
        float remaining;
        if (patrolAxis == PatrolAxis.Horizontal)
            remaining = (patrolTargetPos.x - transform.position.x) * patrolDir;
        else
            remaining = (patrolTargetPos.y - transform.position.y) * patrolDir;

        if (remaining <= 0f)
        {
            // Flip direction and set new target
            patrolDir *= -1;
            SetPatrolTarget();
        }

        // Apply velocity
        Vector2 velocity;
        if (patrolAxis == PatrolAxis.Horizontal)
        {
            velocity = new Vector2(patrolDir * patrolSpeed, 0f);
            Vector3 scale = transform.localScale;
            scale.x = patrolDir > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else
        {
            velocity = new Vector2(0f, patrolDir * patrolSpeed);
        }

        rb.linearVelocity = velocity;
        PlayAnim(walkAnim);
    }

    private void DetectPlayer()
    {
        if (targetPlayer == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            targetPlayer = player.transform;
        }

        Vector2 toPlayer = (targetPlayer.position - transform.position);
        float dist = toPlayer.magnitude;

        if (dist > detectRange) return;

        float angle = Vector2.Angle(FacingDirection, toPlayer);
        if (angle <= detectArcAngle * 0.5f)
        {
            state = State.Charge;
            PlayAnim(chargeAnim);
        }
    }

    private void UpdateCharge()
    {
        if (targetPlayer == null)
        {
            state = State.Patrol;
            return;
        }

        Vector2 dir = (targetPlayer.position - transform.position).normalized;
        rb.linearVelocity = dir * chargeSpeed;

        if (Mathf.Abs(dir.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = dir.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (state != State.Charge) return;

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && !player.IsDashing)
            {
                // Play attack animation, then kill player
                rb.linearVelocity = Vector2.zero;
                PlayAnim(attackAnim);
                player.Die();
            }
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.linearVelocity = Vector2.zero;

        if (deathParticlePrefab != null)
        {
            var fx = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        Destroy(gameObject);
    }

    // ───────── Runtime Arc Visual ─────────

    private void CreateArcVisual()
    {
        arcObject = new GameObject("DetectionArc");
        arcObject.transform.SetParent(transform, false);
        arcObject.transform.localPosition = Vector3.zero;

        arcMeshFilter = arcObject.AddComponent<MeshFilter>();
        arcMeshRenderer = arcObject.AddComponent<MeshRenderer>();

        arcMesh = new Mesh();
        arcMeshFilter.mesh = arcMesh;

        // Transparent material
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = arcColor;
        arcMeshRenderer.material = mat;
        arcMeshRenderer.sortingLayerName = "Default";
        arcMeshRenderer.sortingOrder = 100;
    }

    private void UpdateArcVisual()
    {
        if (state == State.Charge)
        {
            arcMeshRenderer.enabled = false;
            return;
        }
        arcMeshRenderer.enabled = true;

        // Build fan mesh in local space
        // We need to account for the parent's flipped scale
        float facingAngle;
        if (patrolAxis == PatrolAxis.Horizontal)
            facingAngle = patrolDir > 0 ? 0f : 180f;
        else
            facingAngle = patrolDir > 0 ? 90f : -90f;

        // If parent scale.x is negative, local space is mirrored, so flip the angle
        float scaleX = transform.localScale.x;
        float sign = scaleX < 0 ? -1f : 1f;

        float halfArc = detectArcAngle * 0.5f;
        float startAngle = facingAngle * sign - halfArc;
        float stepAngle = detectArcAngle / arcSegments;

        var verts = new Vector3[arcSegments + 2];
        var tris = new int[arcSegments * 3];
        var colors = new Color[arcSegments + 2];

        verts[0] = Vector3.zero;
        colors[0] = arcColor;

        for (int i = 0; i <= arcSegments; i++)
        {
            float a = (startAngle + stepAngle * i) * Mathf.Deg2Rad;
            verts[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * detectRange;
            // Fade alpha toward edges
            float edgeT = Mathf.Abs(i - arcSegments * 0.5f) / (arcSegments * 0.5f);
            Color c = arcColor;
            c.a = arcColor.a * (1f - edgeT * 0.5f);
            colors[i + 1] = c;
        }

        for (int i = 0; i < arcSegments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        arcMesh.Clear();
        arcMesh.vertices = verts;
        arcMesh.triangles = tris;
        arcMesh.colors = colors;
    }

    // ───────── Editor Gizmos ─────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 facing = patrolAxis == PatrolAxis.Horizontal
            ? new Vector3(patrolDir, 0f, 0f)
            : new Vector3(0f, patrolDir, 0f);

        float halfArc = detectArcAngle * 0.5f;

        Vector3 leftEdge = Quaternion.Euler(0, 0, halfArc) * facing * detectRange;
        Vector3 rightEdge = Quaternion.Euler(0, 0, -halfArc) * facing * detectRange;

        Gizmos.DrawLine(transform.position, transform.position + leftEdge);
        Gizmos.DrawLine(transform.position, transform.position + rightEdge);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.yellow;
        Vector3 origin = Application.isPlaying ? patrolOrigin : transform.position;
        if (patrolAxis == PatrolAxis.Horizontal)
        {
            Gizmos.DrawLine(origin + Vector3.left * patrolDistance, origin + Vector3.right * patrolDistance);
        }
        else
        {
            Gizmos.DrawLine(origin + Vector3.down * patrolDistance, origin + Vector3.up * patrolDistance);
        }
    }
}
