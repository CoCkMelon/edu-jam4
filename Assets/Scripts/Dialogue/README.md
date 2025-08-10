# Dialogue System for Unity

A comprehensive YAML-based dialogue system for Unity with embedded dialogues, choice support, trigger system, and Unity UI Elements integration.

## 🚀 Features

- **YAML-based dialogue authoring**: Write dialogues in human-readable YAML format
- **Embedded dialogue support**: WebGL-compatible with pre-compiled dialogue strings
- **UI Elements integration**: Modern Unity UI Toolkit support with customizable interfaces
- **Trigger system**: Integrate dialogues with game mechanics and events
- **Choice system framework**: Foundation for branching dialogue (requires C# implementation)
- **Multiple loading methods**: Support for Resources, StreamingAssets, and embedded strings
- **Story progression tracking**: Built-in story route and value management
- **Debug tools**: Comprehensive debugging utilities for development

## 📁 System Architecture

### Core Components

| Component | Description | Purpose |
|-----------|-------------|---------|
| `DialogueManager` | Main dialogue controller | Manages dialogue playback and scene transitions |
| `DialogueUIController` | UI presentation layer | Handles dialogue display with Unity UI Elements |
| `DialogueLoaderEmbedded` | Embedded dialogue loader | Loads pre-compiled YAML strings (WebGL compatible) |
| `DialogueLoader` | File-based dialogue loader | Loads YAML files directly (Editor/development) |
| `TriggerManager` | Event system integration | Handles dialogue triggers and game events |
| `StoryRouteManager` | Story progression tracker | Manages story state and branching paths |

### Data Structures

| Class | Description | Key Properties |
|-------|-------------|----------------|
| `DialogueLine` | Individual dialogue line | `speaker`, `text`, `trigger`, `options`, `animation` |
| `DialogueOption` | Choice option | `choice`, `next` |
| `DialogueScene` | Complete dialogue sequence | `scene`, `lines` |

### Utility Components

- `DialogueTrigger`: Unity component for trigger-based dialogue activation
- `DialogueSystemDebugger`: Development debugging tools
- `DialogueExample`: Example usage and testing
- `YAMLContentLoader`: Legacy story route content loader

## 📝 YAML Dialogue Format

### Basic Structure

```yaml
title: "Dialogue Title"
lines:
  - id: "unique_line_id"           # Optional: For jumps and choices
    speaker: "Character Name"       # Optional: Narrator if empty
    text: "What the character says"
    animation: "happy"              # Optional: Character animation
    sprite: "character_portrait"    # Optional: Character portrait
    sound: "voice_clip"            # Optional: Audio clip
    trigger: "game_event"          # Optional: Trigger game event
    quest_id: "quest_name"         # Optional: Quest integration
    next_scene: "next_dialogue"    # Optional: Scene transition
    
    # Choice system (requires C# implementation)
    options:
      - choice: "Option 1 text"
        next: "line_id_to_go_to"
      - choice: "Option 2 text"  
        next: "different_line_id"
```

### Example Dialogue

```yaml
title: "Museum Entrance"
lines:
  - id: "start"
    speaker: "Guard"
    text: "Stop! The museum is closed. What are you doing here?"
    animation: "suspicious"
    
  - speaker: "Narrator"
    text: "You need to choose how to respond to the guard."
    options:
      - choice: "I'm sorry, I'll leave immediately"
        next: "apologetic_response"
      - choice: "I'm here on official business"
        next: "official_response"
        
  - id: "apologetic_response"
    speaker: "Player"
    text: "I'm sorry, I didn't know. I'll leave right away."
    trigger: "add_politeness_points"
    
  - id: "official_response"
    speaker: "Player"
    text: "I'm here on official business. Check your list."
    trigger: "add_confidence_points"
```

## 🔧 Setup Guide

### 1. Basic Setup

1. **Add Core Components to Scene:**
   ```csharp
   // Create empty GameObjects and add these components:
   - DialogueManager
   - DialogueUIController  
   - TriggerManager
   - StoryRouteManager (optional)
   ```

2. **Setup UI Elements:**
   - Create a UI Document with dialogue UI elements
   - Assign UI Document to DialogueUIController
   - Ensure UI element names match the expected IDs (see UI section)

3. **Generate Embedded Dialogues:**
   ```bash
   # Run the Python script to generate embedded dialogues
   python3 generate_embedded_dialogues.py
   ```

### 2. UI Elements Setup

Your UXML should include these named elements:

```xml
<!-- Main dialogue UI -->
<VisualElement name="dialogue-root" class="dialogue-container">
    <VisualElement name="character-portrait" class="portrait"/>
    <Label name="speaker-name" class="speaker"/>
    <Label name="dialogue-text" class="dialogue-text"/>
    <VisualElement name="dialogue-choices-container">
        <VisualElement name="choices-list"/>
    </VisualElement>
    <Button name="continue-button" text="Continue"/>
    <Button name="close-button" text="Close"/>
</VisualElement>

<!-- Optional: History overlay -->
<VisualElement name="dialogue-history-overlay" class="history-overlay">
    <VisualElement name="history-content"/>
    <Button name="history-close-button" text="Close"/>
</VisualElement>
```

**💡 Tip**: See `Examples/UI/DialogueSystemUI.uxml` for a complete, ready-to-use UXML template.

### 3. Dialogue Creation Workflow

1. **Create YAML files** in `Assets/DialogueData/`
2. **Run the generator script** to create embedded dialogues
3. **Load dialogues in code:**
   ```csharp
   DialogueManager.Instance.LoadAndStartScene("dialogue_filename");
   ```

## 💻 Usage Examples

### Starting a Dialogue

```csharp
// Load and start dialogue by filename (without extension)
DialogueManager.Instance.LoadAndStartScene("museum_entrance");

// Alternative methods (legacy/fallback)
DialogueManager.Instance.LoadAndStartSceneFromResources("dialogue/museum_entrance");
DialogueManager.Instance.LoadAndStartSceneFromEmbedded("museum_entrance");
```

### Handling Triggers

Implement trigger logic in `TriggerManager.cs`:

```csharp
public void Trigger(string triggerName, DialogueLine line)
{
    switch (triggerName)
    {
        case "add_item":
            inventory.AddItem(line.quest_id);
            break;
            
        case "start_quest":
            questManager.StartQuest(line.quest_id);
            break;
            
        case "change_scene":
            SceneManager.LoadScene(line.next_scene);
            break;
            
        case "add_politeness_points":
            storyManager.AddValue("Politeness", 5);
            break;
    }
}
```

### Using Dialogue Triggers

Add `DialogueTrigger` component to GameObjects:

```csharp
public class DialogueTrigger : MonoBehaviour
{
    public string dialogueScenePath = "museum_entrance";
    public bool destroyOnTrigger = true;
    
    // Automatically triggers when player enters collider
}
```

### Creating Interactive Dialogue

```csharp
// In your interaction system
public class NPCInteraction : MonoBehaviour
{
    [SerializeField] private string dialogueName = "npc_greeting";
    
    void OnInteract()
    {
        DialogueManager.Instance.LoadAndStartScene(dialogueName);
    }
}
```

## ⚠️ Choice System Implementation

**Important**: The choice system provides UI display but **requires C# implementation**:

```csharp
// You must implement choice logic in your game code
public class YourChoiceHandler : MonoBehaviour
{
    void HandleDialogueChoice(string choiceId, DialogueOption option)
    {
        // Implement your choice consequences here
        switch (choiceId)
        {
            case "be_polite":
                // Award politeness points, change relationship, etc.
                break;
            case "be_rude":
                // Different consequences
                break;
        }
        
        // Then continue dialogue
        DialogueManager.Instance.SelectChoice(option.next);
    }
}
```

## 🔄 Build Pipeline Integration

### Development Workflow

1. **Edit YAML files** in `Assets/DialogueData/`
2. **Run generator script**:
   ```bash
   python3 generate_embedded_dialogues.py
   ```
3. **Test in editor** using DialogueSystemDebugger
4. **Build project** - embedded dialogues work in all platforms including WebGL

### Automated Generation

Add to your build scripts:

```bash
# Pre-build step
cd Assets/Scripts/Dialogue
python3 generate_embedded_dialogues.py
```

Or integrate with Unity's build pipeline:

```csharp
[InitializeOnLoad]
public class DialogueBuildProcessor
{
    static DialogueBuildProcessor()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
    }
    
    static void BuildPlayerHandler(BuildPlayerOptions options)
    {
        // Run Python script before build
        System.Diagnostics.Process.Start("python3", "generate_embedded_dialogues.py");
    }
}
```

## 🛠️ Development Tools

### DialogueSystemDebugger

Add to scene for development:
- **Component checking**: Verifies all components are properly connected
- **Test dialogue**: Creates test dialogue lines
- **UI validation**: Checks UI elements are properly named and connected

### Console Commands

Access available dialogues:
```csharp
// Get all available dialogue names
string[] dialogues = DialogueLoaderEmbedded.GetAvailableDialogues();

// Check if dialogue exists
bool exists = DialogueLoaderEmbedded.HasDialogue("museum_entrance");
```

### Debug Features

Enable debug mode for verbose logging:
```csharp
public class DialogueSystemDebugger : MonoBehaviour
{
    public bool debugMode = true; // Enable for development
}
```

## 🔧 Configuration

### File Paths Configuration

Modify paths in `generate_embedded_dialogues.py`:
```python
YAML_FOLDER = "../../DialogueData"     # Input folder
OUTPUT_FOLDER = "../Generated"         # Output folder  
OUTPUT_CLASS = "EmbeddedDialogues"     # Generated class name
NAMESPACE = "DialogueSystem"           # C# namespace
```

### UI Customization

Customize UI elements in your USS stylesheet:
```css
.dialogue-container {
    background-color: rgba(0, 0, 0, 0.8);
    padding: 20px;
    border-radius: 10px;
}

.speaker {
    font-size: 18px;
    font-weight: bold;
    color: #FFD700;
}

.dialogue-text {
    font-size: 16px;
    color: white;
    white-space: normal;
}
```

## 📦 Platform Support

| Platform | Embedded | Resources | File System |
|----------|----------|-----------|-------------|
| **Editor** | ✅ | ✅ | ✅ |
| **Windows/Mac/Linux** | ✅ | ✅ | ✅ |
| **WebGL** | ✅ | ✅ | ❌ |
| **Mobile** | ✅ | ✅ | ⚠️ Limited |
| **Console** | ✅ | ✅ | ⚠️ Limited |

**Recommended**: Use embedded dialogues for best compatibility.

## 🐛 Troubleshooting

### Common Issues

1. **"DialogueUIController.Instance is null"**
   - Ensure DialogueUIController is in the scene
   - Check UI Document is assigned
   - Verify UI elements have correct names

2. **"Dialogue not found in embedded dialogues"**
   - Run `generate_embedded_dialogues.py`
   - Check YAML file is in DialogueData folder
   - Verify filename matches (without extension)

3. **Choices not working**
   - Choices require C# implementation
   - See Choice System Implementation section
   - Implement choice logic in your game code

4. **UI not displaying**
   - Check UXML elements have correct names
   - Verify UI Document is assigned to DialogueUIController
   - Check USS stylesheet is applied

### Debug Steps

1. **Enable debug mode** in DialogueSystemDebugger
2. **Check console logs** for component validation
3. **Use test dialogue** button to verify basic functionality
4. **Verify YAML syntax** with online YAML validators

## 📋 Best Practices

### Performance
- Use embedded dialogues for production builds
- Limit dialogue history size for memory management
- Preload frequently used dialogues

### Organization
- Group related dialogues in subfolders
- Use consistent naming conventions
- Document trigger names and their purposes

### Localization
- Keep text separate from logic
- Use consistent speaker names
- Plan for text expansion in different languages

### Testing
- Test all dialogue branches
- Verify trigger integration
- Test on target platforms

## 🔄 Migration Guide

### From File-based to Embedded

1. Move YAML files to `Assets/DialogueData/`
2. Run `generate_embedded_dialogues.py`
3. Replace `LoadAndStartSceneFromResources()` with `LoadAndStartScene()`
4. Update dialogue names to match filenames

### Upgrading System

1. Backup existing dialogues
2. Update script files
3. Regenerate embedded dialogues
4. Test all dialogue flows
5. Update custom trigger implementations

## 📂 Examples

The `Examples/` folder contains:
- **Complete UI templates** (UXML + USS)
- **Automatic scene setup** script
- **Step-by-step tutorials**
- **Ready-to-use implementations**

See `Examples/README_Examples.md` for details.

## 🤝 Contributing

When adding new features:
1. Update both embedded and file-based loaders
2. Add comprehensive examples
3. Update this documentation
4. Test on multiple platforms
5. Add appropriate debug logging

## 📄 License

This dialogue system is part of the Kittycube Team Unity project. See project license for usage terms.
