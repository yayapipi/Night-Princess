using UnityEngine;
using UnityEngine.UI;

public class PrincessNPC : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    [SerializeField] private DialoguePanel dialoguePanel;
    [SerializeField] private GameObject interactHint; // "Press E" prompt

    private Transform player;
    private PlayerController playerController;
    private bool isDialogueOpen;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.OnClose = OnDialogueClosed;
            dialoguePanel.gameObject.SetActive(false);
        }

        if (interactHint != null)
            interactHint.SetActive(false);
    }

    private void Update()
    {
        if (player == null || isDialogueOpen) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool inRange = dist <= interactRange;

        if (interactHint != null)
            interactHint.SetActive(inRange);

        if (inRange && Input.GetKeyDown(interactKey))
        {
            OpenDialogue();
        }
    }

    private void OpenDialogue()
    {
        isDialogueOpen = true;

        if (interactHint != null)
            interactHint.SetActive(false);

        if (playerController != null)
            playerController.SetInteracting(true);

        dialoguePanel.Open();
    }

    private void OnDialogueClosed()
    {
        isDialogueOpen = false;

        if (playerController != null)
            playerController.SetInteracting(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
