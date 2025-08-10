using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(UIDocument))]
public class DialogueUIController : MonoBehaviour
{
    public static DialogueUIController Instance { get; private set; }

    [Header("UI Documents")]
    [SerializeField] private UIDocument hudDocument;

    [Header("UI Elements (Dialogue Root)")]
    private VisualElement dialogueRoot;
    private VisualElement characterPortraitContainer;
    private VisualElement characterPortrait;
    private Label animationStatus;
    private VisualElement dialogueContentArea;
    private Label speakerName;
    private Label moodIcon;
    private Label dialogueText;
    private VisualElement typingIndicator;
    private VisualElement dialogueChoicesContainer;
    private VisualElement choicesList;
    private Button continueButton;
    private Button skipButton;
    private Button historyButton;
    private Button closeButton;

    [Header("UI Elements (History)")]
    private VisualElement dialogueHistoryOverlay;
    private VisualElement historyContent;
    private Button historyCloseButton;

    [Header("UI Elements (World Space)")]
    private VisualElement speechBubbleContainer;
    private Label bubbleText;
    private VisualElement interactionPrompt;
    private Label interactionKey;

    private DialogueManager dialogueManager;
    private List<string> dialogueHistory = new List<string>();
    private bool isTypingComplete = false;
    private bool showFullText = false;
    private Coroutine currentTypewriterCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        dialogueManager = DialogueManager.Instance;
        
        if (hudDocument == null)
        {
            Debug.LogError("HudDocument is not assigned in DialogueUIController!");
            return;
        }
        
        var root = hudDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("Root visual element is null!");
            return;
        }
        
        Debug.Log("DialogueUIController: Initializing UI elements...");

// Main Dialogue UI
        dialogueRoot = root.Q<VisualElement>("dialogue-root");
        characterPortrait = root.Q<VisualElement>("character-portrait");
        animationStatus = root.Q<Label>("animation-status");
        speakerName = root.Q<Label>("speaker-name");
        moodIcon = root.Q<Label>("mood-icon");
        dialogueText = root.Q<Label>("dialogue-text");
        typingIndicator = root.Q<VisualElement>("typing-indicator");
        dialogueChoicesContainer = root.Q<VisualElement>("dialogue-choices-container");
        choicesList = root.Q<VisualElement>("choices-list");
        continueButton = root.Q<Button>("continue-button");
        dialogueRoot.RegisterCallback<ClickEvent>(evt=>OnContinueClicked());
        skipButton = root.Q<Button>("skip-button");
        historyButton = root.Q<Button>("history-button");
        closeButton = root.Q<Button>("close-button");

        // History UI
        dialogueHistoryOverlay = root.Q<VisualElement>("dialogue-history-overlay");
        historyContent = root.Q<VisualElement>("history-content");
        historyCloseButton = root.Q<Button>("history-close-button");

        // World Space UI
        speechBubbleContainer = root.Q<VisualElement>("speech-bubble-container");
        bubbleText = root.Q<Label>("bubble-text");
        interactionPrompt = root.Q<VisualElement>("interaction-prompt");
        interactionKey = root.Q<Label>("interaction-key");
        
        // Check if all required elements are found
        CheckRequiredElements();

        // Register callbacks
        if (continueButton != null) continueButton.clicked += OnContinueClicked;
        if (skipButton != null) skipButton.clicked += OnSkipClicked;
        if (historyButton != null) historyButton.clicked += OnHistoryClicked;
        if (closeButton != null) closeButton.clicked += OnCloseClicked;
        if (historyCloseButton != null) historyCloseButton.clicked += OnHistoryCloseClicked;

        // Hide all UI initially
        HideAllUI();
        
        Debug.Log("DialogueUIController: Initialization complete.");
    }
    
    private void Update()
    {
        // Handle keyboard input for dialogue continuation
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
        {
            if (dialogueRoot != null && dialogueRoot.style.display == DisplayStyle.Flex)
            {
                OnContinueClicked();
            }
        }
    }
    
    private void CheckRequiredElements()
    {
        if (dialogueRoot == null) Debug.LogError("dialogue-root element not found!");
        if (speakerName == null) Debug.LogError("speaker-name element not found!");
        if (dialogueText == null) Debug.LogError("dialogue-text element not found!");
        if (continueButton == null) Debug.LogError("continue-button element not found!");
        if (closeButton == null) Debug.LogError("close-button element not found!");
        
        Debug.Log("DialogueUIController: Required elements check complete.");
    }

    private void HideAllUI()
    {
        if (dialogueRoot != null)
            dialogueRoot.style.display = DisplayStyle.None;
        if (dialogueHistoryOverlay != null)
            dialogueHistoryOverlay.style.display = DisplayStyle.None;
        if (speechBubbleContainer != null)
            speechBubbleContainer.style.display = DisplayStyle.None;
        if (interactionPrompt != null)
            interactionPrompt.style.display = DisplayStyle.None;
    }

    // --- Public Methods to Control UI ---

    public void ShowDialogue(DialogueLine line)
    {
        // Validate the dialogue line first
        if (!ValidateDialogueLine(line))
        {
            Debug.LogError("Invalid dialogue line, cannot display!");
            return;
        }
        
        // Debug the incoming text to identify issues
        Debug.Log($"ShowDialogue called with speaker: {line.speaker}");
        Debug.Log($"Raw text length: {line.text?.Length ?? 0}");
        if (!string.IsNullOrEmpty(line.text))
        {
            Debug.Log($"First 50 chars: '{line.text.Substring(0, Mathf.Min(50, line.text.Length))}'");
            Debug.Log($"Text bytes: {System.Text.Encoding.UTF8.GetByteCount(line.text)}");
        }
        
        if (dialogueRoot == null)
        {
            Debug.LogError("Cannot show dialogue: dialogueRoot is null!");
            return;
        }
        
        dialogueRoot.style.display = DisplayStyle.Flex;
        Debug.Log("Dialogue root set to display: Flex");
        
        if (speakerName != null)
            speakerName.text = line.speaker ?? "Narrator";
        else
            Debug.LogError("speakerName element is null!");
            
        if (dialogueText != null)
        {
            if (currentTypewriterCoroutine != null)
                StopCoroutine(currentTypewriterCoroutine);
            currentTypewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
        }
        else
            Debug.LogError("dialogueText element is null!");
        
        // Update portrait, animation, mood, etc. based on line data
        // For now, we'll use placeholders
        if (animationStatus != null)
            animationStatus.text = line.animation ?? "idle";
        if (moodIcon != null)
            moodIcon.text = GetMoodIcon(line.animation);
        
        // Handle choices
        if (line.options != null && line.options.Count > 0)
        {
            Debug.LogWarning("CHOICE SYSTEM NOT IMPLEMENTED: This dialogue has choices but the choice system is not implemented in C#!");
            Debug.LogWarning("You need to implement choice handling logic in your game code.");
            
            if (dialogueChoicesContainer != null)
                dialogueChoicesContainer.style.display = DisplayStyle.Flex;
            if (continueButton != null)
                continueButton.style.display = DisplayStyle.None;
            PopulateChoices(line.options);
        }
        else
        {
            if (dialogueChoicesContainer != null)
                dialogueChoicesContainer.style.display = DisplayStyle.None;
            if (continueButton != null)
                continueButton.style.display = DisplayStyle.Flex;
        }
        
        // Add to history
        AddToHistory(line.speaker, line.text);
        
        Debug.Log("ShowDialogue completed");
    }

    public void HideDialogue()
    {
        dialogueRoot.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Shows a world-space prompt for interaction.
    /// </summary>
    /// <param name="worldPosition">The world position to anchor the prompt to.</param>
    /// <param name="text">The text to display in the prompt (e.g., "[E] Use Panel").</param>
    public void ShowInteractionPrompt(Vector3 worldPosition, string text)
    {
        if (interactionPrompt == null || hudDocument == null)
        {
            Debug.LogWarning("ShowInteractionPrompt: UI elements not initialized yet.");
            return;
        }
        
        interactionPrompt.style.display = DisplayStyle.Flex;
        if (interactionKey != null)
            interactionKey.text = text;
        
        // Position prompt in screen space with null checks
        try
        {
            var panel = hudDocument.rootVisualElement?.panel;
            var camera = Camera.main;
            
            if (panel != null && camera != null)
            {
                Vector2 screenPos = RuntimePanelUtils.CameraTransformWorldToPanel(panel, worldPosition, camera);
                interactionPrompt.style.left = screenPos.x;
                interactionPrompt.style.top = screenPos.y;
            }
            else
            {
                // Fallback: position at center of screen
                interactionPrompt.style.left = Screen.width * 0.5f;
                interactionPrompt.style.top = Screen.height * 0.3f;
                Debug.LogWarning("ShowInteractionPrompt: Panel or Camera is null, using fallback positioning.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ShowInteractionPrompt error: {e.Message}");
            // Fallback positioning
            interactionPrompt.style.left = Screen.width * 0.5f;
            interactionPrompt.style.top = Screen.height * 0.3f;
        }
    }

    public void HideInteractionPrompt()
    {
        interactionPrompt.style.display = DisplayStyle.None;
    }

    public void ShowSpeechBubble(string text, Transform target, Vector3 offset)
    {
        speechBubbleContainer.style.display = DisplayStyle.Flex;
        bubbleText.text = text;
        StartCoroutine(UpdateSpeechBubblePosition(target, offset));
    }

    public void HideSpeechBubble()
    {
        speechBubbleContainer.style.display = DisplayStyle.None;
        StopAllCoroutines(); // Stop position updates
    }

    // --- Helper Methods & Coroutines ---

private IEnumerator TypewriterEffect(string text)
    {
        // Clean and validate the text first
        if (string.IsNullOrEmpty(text))
        {
            dialogueText.text = "";
            typingIndicator.style.display = DisplayStyle.None;
            isTypingComplete = true;
            yield break;
        }
        
        // Sanitize the text to remove any potential control characters
        text = SanitizeText(text);
        
        dialogueText.text = "";
        typingIndicator.style.display = DisplayStyle.Flex;
        isTypingComplete = false;
        showFullText = false;
        
        // Use StringInfo to properly handle Unicode characters
        var stringInfo = new System.Globalization.StringInfo(text);
        int totalCharacters = stringInfo.LengthInTextElements;
        
        for (int i = 0; i < totalCharacters; i++)
        {
            if (showFullText) break;
            
            // Get the character at the current position (handles multi-byte characters correctly)
            string currentChar = stringInfo.SubstringByTextElements(i, 1);
            dialogueText.text += currentChar;
            
            yield return new WaitForSeconds(0.03f);
        }
        
        // Ensure the full text is displayed
        if (!showFullText)
        {
            dialogueText.text = text;
            isTypingComplete = true;
        }
        
        typingIndicator.style.display = DisplayStyle.None;
    }
    
    private string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        // Remove control characters that might cause display issues
        var sanitized = new System.Text.StringBuilder();
        foreach (char c in text)
        {
            // Keep printable characters, spaces, and common whitespace
            if (char.IsControl(c))
            {
                // Allow common whitespace characters
                if (c == '\n' || c == '\r' || c == '\t')
                {
                    sanitized.Append(c);
                }
                // Skip other control characters
            }
            else
            {
                sanitized.Append(c);
            }
        }
        
        return sanitized.ToString();
    }
    
    private bool ValidateDialogueLine(DialogueLine line)
    {
        if (line == null)
        {
            Debug.LogError("DialogueLine is null!");
            return false;
        }
        
        if (string.IsNullOrEmpty(line.text))
        {
            Debug.LogWarning("DialogueLine has empty text!");
            return false;
        }
        
        // Check for potential encoding issues
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(line.text);
            var reconstructed = System.Text.Encoding.UTF8.GetString(bytes);
            if (reconstructed != line.text)
            {
                Debug.LogWarning($"Potential encoding issue detected in text: '{line.text}'");
                line.text = reconstructed; // Fix the encoding
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error validating text encoding: {e.Message}");
            return false;
        }
        
        return true;
    }

    private void PopulateChoices(List<DialogueOption> options)
    {
        choicesList.Clear();
        foreach (var option in options)
        {
            var choiceButton = new Button(() => OnChoiceSelected(option.next))
            {
                text = option.choice,
                name = $"choice-{option.next}"
            };
            choiceButton.AddToClassList("choice-button");
            choicesList.Add(choiceButton);
        }
    }

    private IEnumerator UpdateSpeechBubblePosition(Transform target, Vector3 offset)
    {
        while (speechBubbleContainer != null && speechBubbleContainer.style.display == DisplayStyle.Flex)
        {
            if (target == null || hudDocument == null) break;
            
            try
            {
                Vector3 worldPos = target.position + offset;
                var panel = hudDocument.rootVisualElement?.panel;
                var camera = Camera.main;
                
                if (panel != null && camera != null)
                {
                    Vector2 screenPos = RuntimePanelUtils.CameraTransformWorldToPanel(panel, worldPos, camera);
                    speechBubbleContainer.style.left = screenPos.x - speechBubbleContainer.resolvedStyle.width / 2;
                    speechBubbleContainer.style.top = screenPos.y - speechBubbleContainer.resolvedStyle.height;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UpdateSpeechBubblePosition error: {e.Message}");
                break;
            }
            
            yield return null;
        }
    }

    private void AddToHistory(string speaker, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        dialogueHistory.Add($"<b>{speaker ?? "Narrator"}:</b> {text}");
    }

    private void UpdateHistoryDisplay()
    {
        historyContent.Clear();
        foreach (var entry in dialogueHistory)
        {
            var historyEntry = new VisualElement();
            historyEntry.AddToClassList("history-entry");
            var historyText = new Label(entry);
            historyText.AddToClassList("history-text");
            historyEntry.Add(historyText);
            historyContent.Add(historyEntry);
        }
    }
    
    private string GetMoodIcon(string animation)
    {
        if (string.IsNullOrEmpty(animation)) return "😊";
        switch (animation.ToLower())
        {
            case "happy": return "😄";
            case "sad": return "😢";
            case "angry": return "😠";
            case "surprised": return "😮";
            default: return "😊";
        }
    }

    // --- Button Handlers ---

    private void OnContinueClicked()
    {
        // If text is still typing, show full text first
        if (!isTypingComplete)
        {
            showFullText = true;
            if (currentTypewriterCoroutine != null)
                StopCoroutine(currentTypewriterCoroutine);
            
            // Get the current line's full text and display it
            var currentLine = dialogueManager.GetCurrentLine();
            if (currentLine != null)
            {
                dialogueText.text = currentLine.text;
                typingIndicator.style.display = DisplayStyle.None;
                isTypingComplete = true;
            }
        }
        else
        {
            // Text is fully shown, proceed to next line
            dialogueManager.AdvanceDialogue();
        }
    }

    private void OnChoiceSelected(string nextLineId)
    {
        Debug.LogWarning($"CHOICE SELECTED: {nextLineId} - But choice logic is not implemented!");
        Debug.LogWarning("You need to implement choice handling in your game code.");
        Debug.LogWarning("Choices must be implemented in C# code, not in YAML files!");
        
        // For now, just continue to next line
        dialogueManager.AdvanceDialogue();
    }
    

    private void OnSkipClicked()
    {
        // Logic to skip the current scene
        dialogueManager.LoadScene(null); // Example of ending dialogue
        HideDialogue();
    }

    private void OnHistoryClicked()
    {
        dialogueHistoryOverlay.style.display = DisplayStyle.Flex;
        UpdateHistoryDisplay();
    }

    private void OnHistoryCloseClicked()
    {
        dialogueHistoryOverlay.style.display = DisplayStyle.None;
    }

    private void OnCloseClicked()
    {
        HideDialogue();
    }
}
