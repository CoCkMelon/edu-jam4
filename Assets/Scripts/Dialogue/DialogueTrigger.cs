using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public string dialogueScenePath;
    public string triggerId;

    public bool destroyOnTrigger = false;

    private void OnTriggerEnter(Collider other)
    {
        bool shouldDestroy = false;
        if (!other.CompareTag("Player")) return;

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

