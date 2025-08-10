using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class DialogueSystemDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool debugMode = true;
    public KeyCode testKey = KeyCode.T;
    
    [Header("Test Dialogue")]
    public string testDialoguePath = "Assets/example_dialogue.yaml";
    
    void Start()
    {
        if (debugMode)
        {
            Debug.Log("=== Dialogue System Debug Start ===");
            CheckComponents();
        }
    }
    
    void Update()
    {
        if (debugMode)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tKey.wasPressedThisFrame)
            {
                TestDialogue();
            }
        }
    }
    
    void CheckComponents()
    {
        Debug.Log("Checking DialogueManager...");
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance is null!");
        }
        else
        {
            Debug.Log("DialogueManager found.");
        }
        
        Debug.Log("Checking DialogueUIController...");
        if (DialogueUIController.Instance == null)
        {
            Debug.LogError("DialogueUIController.Instance is null!");
        }
        else
        {
            Debug.Log("DialogueUIController found.");
            CheckUIElements();
        }
    }
    
    void CheckUIElements()
    {
        var uiController = DialogueUIController.Instance;
        var field = typeof(DialogueUIController).GetField("dialogueDocument", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            var document = field.GetValue(uiController) as UIDocument;
            if (document == null)
            {
                Debug.LogError("UIDocument is not assigned in DialogueUIController!");
            }
            else
            {
                Debug.Log("UIDocument found.");
                var root = document.rootVisualElement;
                if (root == null)
                {
                    Debug.LogError("Root visual element is null!");
                }
                else
                {
                    Debug.Log($"Root visual element found with {root.childCount} children.");
                    
                    // Check for dialogue-root element
                    var dialogueRoot = root.Q<VisualElement>("dialogue-root");
                    if (dialogueRoot == null)
                    {
                        Debug.LogError("dialogue-root element not found in UXML!");
                    }
                    else
                    {
                        Debug.Log("dialogue-root element found.");
                    }
                }
            }
        }
    }
    
    void TestDialogue()
    {
        Debug.Log("Testing dialogue system...");
        
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("Cannot test: DialogueManager.Instance is null!");
            return;
        }
        
        if (DialogueUIController.Instance == null)
        {
            Debug.LogError("Cannot test: DialogueUIController.Instance is null!");
            return;
        }
        
        // Create a simple test dialogue line
        var testLine = new DialogueLine
        {
            speaker = "Test Character",
            text = "This is a test dialogue line. If you can see this, the system is working!",
            animation = "happy"
        };
        
        Debug.Log("Showing test dialogue...");
        DialogueUIController.Instance.ShowDialogue(testLine);
        
        // Alternative: Try loading from file
        if (System.IO.File.Exists(testDialoguePath))
        {
            Debug.Log($"Loading dialogue from: {testDialoguePath}");
            DialogueManager.Instance.LoadAndStartScene(testDialoguePath);
        }
        else
        {
            Debug.LogWarning($"Test dialogue file not found: {testDialoguePath}");
        }
    }
    
    void OnGUI()
    {
        if (!debugMode) return;
        
        GUI.Box(new Rect(10, 10, 300, 100), "Dialogue System Debug");
        
        if (GUI.Button(new Rect(20, 40, 120, 20), "Test Dialogue"))
        {
            TestDialogue();
        }
        
        if (GUI.Button(new Rect(150, 40, 120, 20), "Check Components"))
        {
            CheckComponents();
        }
        
        GUI.Label(new Rect(20, 70, 280, 20), $"Press {testKey} to test dialogue");
    }
}
