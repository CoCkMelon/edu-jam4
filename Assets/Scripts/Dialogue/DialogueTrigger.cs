using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public string dialogueScenePath;
    public string triggerId;

    public bool destroyOnTrigger = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        TryLoadDialogue();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        TryLoadDialogue();
    }
    void TryLoadDialogue()
    {
        bool shouldDestroy = false;

        if (!string.IsNullOrEmpty(dialogueScenePath))
        {
            DialogueManager.Instance.LoadAndStartScene(dialogueScenePath);
            shouldDestroy = true;
        }
        if (!string.IsNullOrEmpty(triggerId))
        {
            TriggerManager.Instance.Trigger(triggerId, null);
        }
        if(shouldDestroy && destroyOnTrigger) {
            Destroy(gameObject);
        }
    }
}

