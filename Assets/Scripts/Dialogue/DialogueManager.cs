using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public string currentScenePath;
    private DialogueScene sceneData;
    private int currentIndex = 0;
    private Dictionary<string, int> labelledLines = new Dictionary<string, int>();
    private DialogueUIController uiController;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Wait a frame for all components to initialize
        StartCoroutine(InitializeUIController());
    }
    
    private System.Collections.IEnumerator InitializeUIController()
    {
        // Wait for UI controller to initialize
        int attempts = 0;
        while (DialogueUIController.Instance == null && attempts < 10)
        {
            yield return null;
            attempts++;
        }
        
        uiController = DialogueUIController.Instance;
        if (uiController == null)
        {
            Debug.LogError("DialogueUIController not found! Please add it to the scene.");
        }
        else
        {
            Debug.Log("DialogueManager: UI Controller found and connected.");
        }
    }

    public void LoadAndStartScene(string dialogueName)
    {
        // Always use embedded dialogues everywhere
        sceneData = DialogueLoaderEmbedded.LoadFromEmbedded(dialogueName);
        if (sceneData == null)
        {
            Debug.LogError($"Failed to load embedded dialogue scene: {dialogueName}");
            return;
        }
        currentIndex = 0;
        IndexLabelledLines();
        PlayCurrentLine();
    }
    
    public void LoadAndStartSceneFromResources(string resourcePath)
    {
        // Legacy method - redirect to embedded
        string dialogueName = System.IO.Path.GetFileNameWithoutExtension(resourcePath);
        LoadAndStartScene(dialogueName);
    }
    
    public void LoadAndStartSceneFromEmbedded(string dialogueName)
    {
        // Redirect to main method
        LoadAndStartScene(dialogueName);
    }
    

    void IndexLabelledLines() {
        if (sceneData?.lines != null) {
            for (int i = 0; i < sceneData.lines.Count; i++) {
                if (!string.IsNullOrEmpty(sceneData.lines[i].id)) {
                    labelledLines[sceneData.lines[i].id] = i;
                }
            }
        }
    }

    void PlayCurrentLine()
    {
        if (sceneData == null || sceneData.lines == null || currentIndex >= sceneData.lines.Count)
        {
            Debug.Log("Scene finished!");
            if (uiController != null)
                uiController.HideDialogue();
            return;
        }
        var line = sceneData.lines[currentIndex];
        
        // Check if this line has choices
        if (line.options != null && line.options.Count > 0)
        {
            DisplayOptions(line.options);
        }
        
        // Use the new UI system
        if (uiController != null)
        {
            uiController.ShowDialogue(line);
        }
        else
        {
            // Fallback to debug logs
            if (!string.IsNullOrEmpty(line.speaker))
                Debug.Log($"{line.speaker}: {line.text}");
            else if (!string.IsNullOrEmpty(line.text))
                Debug.Log(line.text);
        }
        
        if (!string.IsNullOrEmpty(line.trigger))
            TriggerManager.Instance.Trigger(line.trigger, line);
    }

    public void AdvanceDialogue()
    {
        currentIndex++;
        if (currentIndex < sceneData.lines.Count)
        {
            PlayCurrentLine();
        }
        else
        {
            // End of dialogue
            if (uiController != null)
                uiController.HideDialogue();
        }
    }

    // WARNING: Choice handling must be implemented in C# code!
    // This method shows choices in UI but doesn't handle the logic
    private void DisplayOptions(List<DialogueOption> options) {
        Debug.LogWarning("CHOICES DETECTED: Choice logic must be implemented in C# code!");
        Debug.LogWarning("Available options:");
        foreach (var option in options) {
            Debug.LogWarning($"  - {option.choice} -> {option.next}");
        }
        Debug.LogWarning("Implement choice handling in your game logic!");
    }

    public void LoadScene(string nextScene)
    {
        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.LogWarning("LoadScene called with empty scene name");
            return;
        }
        
        // Check if it's a dialogue scene transition
        if (nextScene.Contains("_") || nextScene.Contains("puzzle") || nextScene.Contains("complete"))
        {
            // Load as dialogue scene from StoryRoutes
            LoadAndStartSceneFromResources($"Assets/StoryRoutes/{nextScene}");
        }
        else
        {
            // Load as Unity scene
            Debug.Log($"Loading Unity scene: {nextScene}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
        }
    }

    public void SelectChoice(string nextLineId)
    {
        if (labelledLines.TryGetValue(nextLineId, out int lineIndex))
        {
            currentIndex = lineIndex;
            PlayCurrentLine();
        }
        else
        {
            Debug.LogWarning($"Line with ID '{nextLineId}' not found!");
            AdvanceDialogue();
        }
    }
    
    public DialogueLine GetCurrentLine()
    {
        if (sceneData != null && sceneData.lines != null && currentIndex >= 0 && currentIndex < sceneData.lines.Count)
        {
            return sceneData.lines[currentIndex];
        }
        return null;
    }
}

