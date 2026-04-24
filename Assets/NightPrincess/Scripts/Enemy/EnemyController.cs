using System.Collections;
using UnityEngine;

namespace NightPrincess.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class EnemyController : MonoBehaviour
    {
        public enum PatrolAxis { Horizontal, Vertical }

        [Header("Patrol")]
        [SerializeField] private PatrolAxis axis = PatrolAxis.Horizontal;
        [SerializeField] private float patrolDistance = 3f;
        [SerializeField] private float patrolSpeed = 1.5f;

        [Header("Combat")]
        [SerializeField] private float attackMoveSpeed = 9f;
        [SerializeField] private float attackRange = 0.4f;
        [SerializeField] private GameObject deathExplosionPrefab;
        [SerializeField] private string explosionResourceName = "Explosion";

        [Header("Animator States")]
        [SerializeField] private string stateIdle = "Idle";
        [SerializeField] private string stateWalk = "Walk";
        [SerializeField] private string stateAttack = "Attack";
        [SerializeField] private string stateHurt = "Hurt";
        [SerializeField] private float animCrossFade = 0.05f;

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer sr;

        private Vector2 origin;
        private Vector2 facing = Vector2.right;
        private bool movingForward = true;
        private bool attacking;
        private bool dead;
        private string currentState;
        private NightPrincess.Player.PlayerController attackTarget;

        public bool IsDead => dead;
        public Vector2 FacingDirection => facing;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            origin = transform.position;

            if (deathExplosionPrefab == null && !string.IsNullOrEmpty(explosionResourceName))
                deathExplosionPrefab = Resources.Load<GameObject>(explosionResourceName);
        }

        private void FixedUpdate()
        {
            if (dead) return;

            if (attacking)
            {
                AttackUpdate();
                return;
            }

            Patrol();
        }

        private void Patrol()
        {
            Vector2 dir = axis == PatrolAxis.Horizontal ? Vector2.right : Vector2.up;
            if (!movingForward) dir = -dir;

            Vector2 next = rb.position + dir * patrolSpeed * Time.fixedDeltaTime;
            Vector2 delta = next - origin;
            float proj = axis == PatrolAxis.Horizontal ? delta.x : delta.y;

            if (proj > patrolDistance) { movingForward = false; dir = -dir; }
            else if (proj < -patrolDistance) { movingForward = true; dir = -dir; }

            facing = dir.normalized;
            FlipTowards(facing.x);
            PlayState(stateWalk);
            rb.MovePosition(rb.position + dir * patrolSpeed * Time.fixedDeltaTime);
        }

        private void AttackUpdate()
        {
            if (attackTarget == null) { attacking = false; return; }
            Vector2 to = (Vector2)attackTarget.transform.position - rb.position;
            float dist = to.magnitude;
            if (dist <= attackRange)
            {
                PlayState(stateAttack);
                attackTarget.Die();
                attacking = false;
                attackTarget = null;
                return;
            }
            Vector2 dir = to.normalized;
            facing = dir;
            FlipTowards(dir.x);
            PlayState(stateWalk);
            rb.MovePosition(rb.position + dir * attackMoveSpeed * Time.fixedDeltaTime);
        }

        public void AttackPlayer(NightPrincess.Player.PlayerController player)
        {
            if (dead || attacking) return;
            attacking = true;
            attackTarget = player;
            PlayState(stateAttack);
        }

        public void Kill()
        {
            if (dead) return;
            dead = true;
            PlayState(stateHurt);
            if (deathExplosionPrefab != null)
                Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);

            rb.linearVelocity = Vector2.zero;
            StartCoroutine(DelayedDestroy());
        }

        private IEnumerator DelayedDestroy()
        {
            yield return new WaitForSeconds(0.4f);
            Destroy(gameObject);
        }

        private void FlipTowards(float x)
        {
            if (sr == null || Mathf.Abs(x) < 0.01f) return;
            sr.flipX = x < 0f;
        }

        private void PlayState(string state)
        {
            if (animator == null || string.IsNullOrEmpty(state) || currentState == state) return;
            if (!animator.HasState(0, Animator.StringToHash(state))) return;
            if (animCrossFade > 0f) animator.CrossFade(state, animCrossFade);
            else animator.Play(state);
            currentState = state;
        }
    }
}
