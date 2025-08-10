using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueExample : MonoBehaviour
{
    [Header("External File Dialogue (Runtime)")]
    public string yamlFilePath = "example_dialogue.yaml";
    
    [Header("Resources Dialogue (Runtime)")]
    public string resourcesPath = "dialogue/example_dialogue";
    
    [Header("Test Controls")]
    public KeyCode testYamlFile = KeyCode.Alpha1;
    public KeyCode testResources = KeyCode.Alpha2;
    
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) // Test YAML file
            {
                TestYamlFileDialogue();
            }
            
            if (keyboard.digit2Key.wasPressedThisFrame) // Test Resources
            {
                TestResourcesDialogue();
            }
        }
    }
    
    
    void TestYamlFileDialogue()
    {
        Debug.Log("Testing YAML file dialogue...");
        
        if (string.IsNullOrEmpty(yamlFilePath))
        {
            Debug.LogError("YAML file path is not set!");
            return;
        }
        
        DialogueManager.Instance.LoadAndStartScene(yamlFilePath);
    }
    
    void TestResourcesDialogue()
    {
        Debug.Log("Testing Resources dialogue...");
        
        if (string.IsNullOrEmpty(resourcesPath))
        {
            Debug.LogError("Resources path is not set!");
            return;
        }
        
        DialogueManager.Instance.LoadAndStartSceneFromResources(resourcesPath);
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 120, 300, 100), "Dialogue Testing");
        
        if (GUI.Button(new Rect(20, 150, 120, 20), $"YAML File ({testYamlFile})"))
        {
            TestYamlFileDialogue();
        }
        
        if (GUI.Button(new Rect(150, 150, 120, 20), $"Resources ({testResources})"))
        {
            TestResourcesDialogue();
        }
        
        GUI.Label(new Rect(20, 180, 280, 20), "Press the keys to test different dialogue types");
    }
}

/*
USAGE INSTRUCTIONS:

1. YAML FILE DIALOGUE:
   - Put your .yaml file in StreamingAssets folder for builds
   - Use relative paths like "dialogue/example.yaml"
   - Or use absolute paths for development

2. RESOURCES DIALOGUE:
   - Put your .yaml file in a Resources folder
   - Use path without extension: "dialogue/example"
   - Works in builds without extra setup

RECOMMENDATIONS:
- Use YAML files for easy editing and modding support
- Use Resources for simple runtime loading
- Use StreamingAssets for files that need to be editable after build
*/
