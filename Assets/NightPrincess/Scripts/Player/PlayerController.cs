using NightPrincess.Akarion;
using NightPrincess.Core;
using UnityEngine;

namespace NightPrincess.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float dashSpeed = 24f;
        [SerializeField] private float dashEndDistance = 0.15f;

        [Header("Combat")]
        [SerializeField] private float backstabDotThreshold = 0.0f;
        [SerializeField] private GameObject deathPrefab;

        [Header("Effects")]
        [SerializeField] private float cameraShakeDuration = 0.18f;
        [SerializeField] private float cameraShakeMagnitude = 0.3f;

        [Header("Animator")]
        [SerializeField] private string stateIdle = "Idle";
        [SerializeField] private string stateWalk = "Walk";
        [SerializeField] private string stateDash = "Dash";
        [SerializeField] private float animCrossFade = 0.05f;

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer sr;

        private bool isDashing;
        private Vector2 dashTarget;
        private Vector2 lastFacing = Vector2.right;
        private bool isDead;
        private string currentState;

        public bool IsDashing => isDashing;
        public Vector2 Facing => lastFacing;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private void Update()
        {
            if (isDead) return;

            if (isDashing)
            {
                UpdateDash();
                return;
            }

            HandleMovement();
            HandleMouseDash();
        }

        private void HandleMovement()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            Vector2 input = new Vector2(x, y);
            if (input.sqrMagnitude > 1f) input.Normalize();

            rb.linearVelocity = input * moveSpeed;

            bool walking = input.sqrMagnitude > 0.001f;
            PlayState(walking ? stateWalk : stateIdle);

            if (walking)
            {
                lastFacing = input;
                FlipTowards(input.x);
            }
        }

        private void HandleMouseDash()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            // Don't dash when clicking on UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            StartDash(mouseWorld);
        }

        public void StartDash(Vector2 target)
        {
            isDashing = true;
            dashTarget = target;
            Vector2 dir = (target - (Vector2)transform.position).normalized;
            if (dir.sqrMagnitude > 0.01f) { lastFacing = dir; FlipTowards(dir.x); }

            PlayState(stateDash);

            CameraShake.Instance?.Shake(cameraShakeDuration, cameraShakeMagnitude);

            var trail = GetComponent<PlayerDashEffect>();
            trail?.BeginTrail();
        }

        private void UpdateDash()
        {
            Vector2 pos = rb.position;
            Vector2 toTarget = dashTarget - pos;
            float dist = toTarget.magnitude;

            if (dist <= dashEndDistance)
            {
                EndDash();
                return;
            }

            Vector2 step = toTarget.normalized * dashSpeed * Time.deltaTime;
            if (step.magnitude > dist) step = toTarget;
            rb.MovePosition(pos + step);
        }

        private void EndDash()
        {
            isDashing = false;
            rb.linearVelocity = Vector2.zero;
            PlayState(stateIdle);

            var trail = GetComponent<PlayerDashEffect>();
            trail?.EndTrail();
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryHitEnemy(collision.collider);
        private void OnTriggerEnter2D(Collider2D other) => TryHitEnemy(other);

        private void TryHitEnemy(Collider2D other)
        {
            if (isDead) return;
            if (!other.CompareTag("Enemy")) return;

            var enemy = other.GetComponentInParent<NightPrincess.Enemy.EnemyController>();
            if (enemy == null || enemy.IsDead) return;

            Vector2 attackDir = ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
            float dot = Vector2.Dot(enemy.FacingDirection, attackDir);

            // Hitting enemy from behind: enemy faces one way, player is on the opposite side
            if (dot < backstabDotThreshold && isDashing)
            {
                enemy.Kill();
                AkarionEventLogger.Instance?.LogEvent(AkarionEvents.EnemyKilled);
                EndDash();
            }
            else
            {
                // Player hit enemy from front — enemy kills player
                enemy.AttackPlayer(this);
            }
        }

        public void Die()
        {
            if (isDead) return;
            isDead = true;
            rb.linearVelocity = Vector2.zero;
            if (deathPrefab != null)
                Instantiate(deathPrefab, transform.position, Quaternion.identity);

            GameManager.Instance?.OnPlayerDied(transform.position);
            gameObject.SetActive(false);
        }

        private void PlayState(string state)
        {
            if (animator == null || string.IsNullOrEmpty(state) || currentState == state) return;
            if (!animator.HasState(0, Animator.StringToHash(state))) return;
            if (animCrossFade > 0f) animator.CrossFade(state, animCrossFade);
            else animator.Play(state);
            currentState = state;
        }

        private void FlipTowards(float x)
        {
            if (sr == null || Mathf.Abs(x) < 0.01f) return;
            sr.flipX = x < 0f;
        }
    }
}
