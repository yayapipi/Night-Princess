using UnityEngine;

namespace NightPrincess
{
    public class PrincessInteraction : MonoBehaviour
    {
        [Header("Interaction")]
        public Transform player;
        public string playerTag = "Player";
        public float interactionDistance = 2.5f;
        public KeyCode interactKey = KeyCode.E;

        [Header("References")]
        public DialogPanelController dialogPanel;

        [Header("Visual Hint")]
        public SpriteRenderer princessRenderer;
        public Color hintColor = new Color(1f, 1f, 0.6f, 1f);
        public Color normalColor = Color.white;

        private bool playerInRange;

        void Awake()
        {
            if (princessRenderer == null) princessRenderer = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag(playerTag);
                if (p != null) player = p.transform;
            }
        }

        void Update()
        {
            if (player == null) return;

            float d = Vector2.Distance(player.position, transform.position);
            bool inRange = d <= interactionDistance;
            if (inRange != playerInRange)
            {
                playerInRange = inRange;
                if (princessRenderer != null)
                    princessRenderer.color = playerInRange ? hintColor : normalColor;
            }

            if (!playerInRange) return;
            if (dialogPanel == null) return;
            if (dialogPanel.IsOpen) return;

            if (Input.GetKeyDown(interactKey))
                dialogPanel.OpenDialog();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}
