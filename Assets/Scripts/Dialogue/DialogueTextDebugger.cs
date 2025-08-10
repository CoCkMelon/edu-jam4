using UnityEngine;
using System.Text;
using System.Globalization;

public class DialogueTextDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebug = true;
    public KeyCode debugKey = KeyCode.F1;
    
    [Header("Test Text")]
    [TextArea(3, 10)]
    public string testText = "Hello, this is a test dialogue! 😊";
    
    private void Update()
    {
        if (enableDebug && Input.GetKeyDown(debugKey))
        {
            DebugText(testText);
        }
    }
    
    public void DebugText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("Text is null or empty");
            return;
        }
        
        Debug.Log("=== TEXT DEBUGGING ===");
        Debug.Log($"Original text: '{text}'");
        Debug.Log($"Length: {text.Length}");
        Debug.Log($"Byte count (UTF8): {Encoding.UTF8.GetByteCount(text)}");
        
        // Check for control characters
        bool hasControlChars = false;
        var controlChars = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsControl(c))
            {
                hasControlChars = true;
                controlChars.Append($"[{i}]={((int)c):X2} ");
            }
        }
        
        if (hasControlChars)
        {
            Debug.LogWarning($"Found control characters: {controlChars}");
        }
        else
        {
            Debug.Log("No control characters found");
        }
        
        // Check Unicode normalization
        var normalizedText = text.Normalize(NormalizationForm.FormC);
        if (normalizedText != text)
        {
            Debug.LogWarning($"Text normalization changed content. Normalized: '{normalizedText}'");
        }
        
        // Check each character
        var stringInfo = new StringInfo(text);
        Debug.Log($"Text elements count: {stringInfo.LengthInTextElements}");
        
        for (int i = 0; i < Mathf.Min(10, stringInfo.LengthInTextElements); i++)
        {
            string element = stringInfo.SubstringByTextElements(i, 1);
            Debug.Log($"Element {i}: '{element}' (Unicode: {GetUnicodeInfo(element)})");
        }
        
        // Test the dialogue system with this text
        TestDialogueSystem();
    }
    
    private string GetUnicodeInfo(string element)
    {
        if (string.IsNullOrEmpty(element)) return "empty";
        
        var sb = new StringBuilder();
        foreach (char c in element)
        {
            sb.Append($"U+{((int)c):X4} ");
        }
        return sb.ToString().Trim();
    }
    
    private void TestDialogueSystem()
    {
        var dialogueUI = DialogueUIController.Instance;
        if (dialogueUI == null)
        {
            Debug.LogError("DialogueUIController not found");
            return;
        }
        
        var testLine = new DialogueLine
        {
            speaker = "Debug Tester",
            text = testText,
            animation = "happy"
        };
        
        Debug.Log("Testing dialogue system with debug text...");
        dialogueUI.ShowDialogue(testLine);
    }
    
    [ContextMenu("Debug Current Test Text")]
    public void DebugCurrentTestText()
    {
        DebugText(testText);
    }
    
    [ContextMenu("Test Clean Text")]
    public void TestCleanText()
    {
        testText = "Hello, this is clean ASCII text for testing.";
        DebugText(testText);
    }
    
    [ContextMenu("Test Unicode Text")]
    public void TestUnicodeText()
    {
        testText = "Hello! 😊 This has émojis and accénts.";
        DebugText(testText);
    }
    
    [ContextMenu("Test Problematic Text")]
    public void TestProblematicText()
    {
        // This creates a string with potential control characters
        var sb = new StringBuilder();
        sb.Append("Normal text");
        sb.Append((char)0x0002); // STX control character
        sb.Append("More text");
        sb.Append((char)0xFEFF); // BOM
        sb.Append("End text");
        
        testText = sb.ToString();
        DebugText(testText);
    }
}
