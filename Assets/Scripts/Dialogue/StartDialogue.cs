using UnityEngine;

public class StartDialogue : MonoBehaviour
{
    public string dialogueScenePath;
    public string triggerId;

    public bool destroyOnTrigger = true;

    private void Start()
    {
        Invoke(nameof(BeginDialogue), 1f);
    }

    private void BeginDialogue()
    {
        if (!string.IsNullOrEmpty(dialogueScenePath))
        {
            DialogueManager.Instance.LoadAndStartScene(dialogueScenePath);
        }
        if (!string.IsNullOrEmpty(triggerId))
        {
            TriggerManager.Instance.Trigger(triggerId, null);
        }
        if (destroyOnTrigger) {
            Destroy(gameObject);
        }
    }
}

