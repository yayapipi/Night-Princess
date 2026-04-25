using UnityEngine;

namespace NightPrincess
{
[RequireComponent(typeof(Animator))]
public class EnemyController : MonoBehaviour
{
    public enum PatrolAxis { None, Horizontal, Vertical }

    [Header("Patrol")]
    public PatrolAxis patrolAxis = PatrolAxis.Horizontal;
    public float patrolDistance = 2f;
    public float patrolSpeed = 1.5f;
    public bool startMovingForward = true;

    [Header("Charge")]
    public float chargeSpeed = 9f;
    public float chargeStopDistance = 0.05f;
    public float attackRecoverTime = 0.6f;

    [Header("Effects")]
    public GameObject explosionPrefab;
    public string explosionResourcePath = "Explosion";

    [Header("Animator State Names")]
    public string idleState = "Idle";
    public string walkState = "Walk";
    public string attackState = "Attack";
    public string hurtState = "Hurt";
    public string deathState = "Death";

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 startPos;
    private bool movingForward;
    private bool isCharging;
    private bool isAttacking;
    private bool isDead;
    private Transform chargeTarget;
    private string currentAnim = "";

    public bool IsDead { get { return isDead; } }
    public bool IsCharging { get { return isCharging; } }
    public Vector2 StartPos { get { return startPos; } }

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPos = transform.position;
        movingForward = startMovingForward;
        if (explosionPrefab == null && !string.IsNullOrEmpty(explosionResourcePath))
            explosionPrefab = Resources.Load<GameObject>(explosionResourcePath);
    }

    void Update()
    {
        if (isDead) return;
        if (isAttacking) return;

        if (isCharging && chargeTarget != null) DoCharge();
        else DoPatrol();
    }

    void DoPatrol()
    {
        if (patrolAxis == PatrolAxis.None)
        {
            SetAnim(idleState);
            return;
        }

        Vector3 axis = patrolAxis == PatrolAxis.Horizontal ? Vector3.right : Vector3.up;
        float offset = patrolAxis == PatrolAxis.Horizontal
            ? transform.position.x - startPos.x
            : transform.position.y - startPos.y;

        if (movingForward && offset >= patrolDistance) { movingForward = false; FlipFacing(); }
        else if (!movingForward && offset <= -patrolDistance) { movingForward = true; FlipFacing(); }

        Vector3 dir = (movingForward ? 1f : -1f) * axis;
        transform.position += dir * patrolSpeed * Time.deltaTime;
        SetAnim(walkState);
    }

    void DoCharge()
    {
        if (chargeTarget == null) { isCharging = false; return; }
        Vector2 toTarget = (Vector2)chargeTarget.position - (Vector2)transform.position;
        if (toTarget.magnitude <= chargeStopDistance) return;
        Vector3 step = (Vector3)toTarget.normalized * chargeSpeed * Time.deltaTime;
        transform.position += step;

        if (patrolAxis == PatrolAxis.Horizontal)
        {
            float wantSign = Mathf.Sign(toTarget.x);
            if (Mathf.Abs(wantSign - Mathf.Sign(transform.localScale.x)) > 0.01f)
                FlipFacing();
        }
        SetAnim(walkState);
    }

    void FlipFacing()
    {
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    public Vector2 GetFacing()
    {
        if (patrolAxis == PatrolAxis.Vertical)
            return movingForward ? Vector2.up : Vector2.down;
        return new Vector2(Mathf.Sign(transform.localScale.x), 0f);
    }

    public void OnPlayerInArc(Transform player)
    {
        if (isDead || isCharging || isAttacking) return;
        isCharging = true;
        chargeTarget = player;
    }

    public void AttackPlayer(PlayerController player)
    {
        if (isDead || isAttacking || player == null) return;
        isAttacking = true;
        SetAnim(attackState);
        player.Die();
        Invoke(nameof(EndAttack), attackRecoverTime);
    }

    void EndAttack()
    {
        isAttacking = false;
        isCharging = false;
        chargeTarget = null;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        SetAnim(hurtState);
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject, 0.5f);
    }

    void SetAnim(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        if (currentAnim == stateName) return;
        animator.Play(stateName);
        currentAnim = stateName;
    }
}
}
