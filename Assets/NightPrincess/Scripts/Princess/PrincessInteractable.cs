using UnityEngine;

namespace NightPrincess.Princess
{
    public class PrincessInteractable : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 2f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private DialogPanelUI dialogPanel;
        [SerializeField] private GameObject hintObject;

        private Transform player;

        private void Awake()
        {
            if (dialogPanel == null)
                dialogPanel = Object.FindFirstObjectByType<DialogPanelUI>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        private void Update()
        {
            if (player == null) return;
            bool inRange = (player.position - transform.position).sqrMagnitude <= interactRadius * interactRadius;
            if (hintObject != null && hintObject.activeSelf != inRange) hintObject.SetActive(inRange);

            if (inRange && Input.GetKeyDown(interactKey))
            {
                if (dialogPanel != null && !dialogPanel.IsOpen)
                    dialogPanel.Open();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
