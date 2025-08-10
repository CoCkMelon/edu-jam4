# Dialogue System Example Scene Setup

This guide will help you set up a complete example scene that demonstrates the dialogue system functionality.

## 🚀 Quick Setup (Automatic)

### Option 1: Use the Scene Builder Script

1. **Create a new Unity scene**
2. **Create an empty GameObject** and name it "DialogueSystemSetup"
3. **Attach the `DialogueExampleSceneSetup` script** to this GameObject
4. **Assign the UI files in the inspector:**
   - Drag `Examples/UI/DialogueSystemUI.uxml` to the "Dialogue UIXML" field
   - Drag `Examples/UI/DialogueSystemUI.uss` to the "Dialogue Style Sheet" field
5. **Configure options** in the inspector:
   - ✅ Create Example NPCs
   - ✅ Create Player Controller  
   - ⬜ Auto Start Dialogue (optional)
   - Set Initial Dialogue to "museum_entrance"
6. **Press Play** - the scene will automatically set up!

### What Gets Created Automatically:

- **Core Systems**: DialogueManager, DialogueUIController, TriggerManager
- **UI System**: Complete dialogue interface with UXML/USS
- **Example NPCs**: Guard and Narrator trigger objects (cyan capsules)
- **Player Controller**: Blue cube with WASD movement and Space jump
- **Camera**: Third-person camera following the player
- **Debug Tools**: Built-in testing and debugging utilities

## 🎮 Controls

### Player Movement
- **WASD**: Move around
- **Space**: Jump
- **E**: Interact with NPCs when in range

### Dialogue Testing
- **T**: Start test dialogue (museum_entrance)
- **F1**: Toggle debug mode
- **Click buttons**: Use dialogue UI buttons
- **Enter**: Continue dialogue (alternative to clicking)

### GUI Controls
- **Test Dialogue Button**: Start the example dialogue
- **Debug Mode Button**: Toggle system debugging

## 🛠️ Manual Setup (Advanced)

If you prefer to set up components manually:

### 1. Core Components Setup

Create empty GameObjects and add these components:

```
Scene Hierarchy:
├── DialogueManager (DialogueManager script)
├── DialogueUIController (DialogueUIController + UIDocument)
├── TriggerManager (TriggerManager script)
├── StoryRouteManager (StoryRouteManager script) [Optional]
└── DialogueSystemDebugger (DialogueSystemDebugger script) [Optional]
```

### 2. UI Document Setup

1. **Create UIDocument GameObject**
2. **Assign UXML**: Drag `Examples/UI/DialogueSystemUI.uxml` to Visual Tree Asset
3. **Assign USS**: Add `Examples/UI/DialogueSystemUI.uss` to Style Sheets
4. **Link to Controller**: Set the UIDocument reference in DialogueUIController

### 3. Create Example NPCs

```csharp
// Create NPC with dialogue trigger
GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
npc.AddComponent<DialogueTrigger>().dialogueScenePath = "museum_entrance";
npc.AddComponent<SphereCollider>().isTrigger = true;
```

### 4. Player Setup

```csharp
// Create player
GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cube);
player.tag = "Player";
player.AddComponent<Rigidbody>();
player.AddComponent<SimplePlayerController>(); // From example script
```

## 📝 Testing the System

### Available Test Dialogues

The system comes with these embedded dialogues:
- **museum_entrance**: Example with guard dialogue
- **test_dialogue_**: Simple test conversation

### Testing Checklist

1. ✅ **Start Scene**: Play mode should show GUI controls
2. ✅ **UI Elements**: All dialogue UI elements should be properly named
3. ✅ **NPC Interaction**: Walk into cyan NPCs to trigger dialogues
4. ✅ **Dialogue Display**: Text should appear with typewriter effect
5. ✅ **Controls**: Continue button and close button should work
6. ✅ **Debug Mode**: F1 should toggle debug information

## 🐛 Troubleshooting

### Common Issues

**"DialogueUIController.Instance is null"**
- Ensure UIDocument is assigned to DialogueUIController
- Check that UXML file contains elements with correct names

**"Dialogue not found"**
- Run `python3 generate_embedded_dialogues.py` to regenerate dialogues
- Verify YAML files exist in DialogueData folder

**UI Not Displaying**
- Check UXML element names match expected IDs
- Verify USS stylesheet is applied
- Ensure UIDocument has proper Visual Tree Asset

**NPCs Not Triggering**
- Verify Player GameObject has "Player" tag
- Check NPC colliders are set to isTrigger = true
- Ensure DialogueTrigger component has correct dialogue name

### Debug Tools

**Console Logging**
```csharp
// Enable debug mode for verbose logging
var debugger = FindObjectOfType<DialogueSystemDebugger>();
debugger.debugMode = true;
```

**Manual Testing**
```csharp
// Test dialogue directly
DialogueManager.Instance.LoadAndStartScene("museum_entrance");

// Check available dialogues
string[] dialogues = DialogueLoaderEmbedded.GetAvailableDialogues();
```

## 📁 File Structure

After setup, your project should have:

```
Assets/
├── Scripts/
│   ├── Dialogue/
│   │   ├── DialogueManager.cs
│   │   ├── DialogueUIController.cs
│   │   ├── Examples/
│   │   │   ├── ExampleSceneBuilder.cs
│   │   │   ├── EXAMPLE_SCENE_SETUP.md
│   │   │   └── UI/
│   │   │       ├── DialogueSystemUI.uxml
│   │   │       └── DialogueSystemUI.uss
│   │   └── ... (other dialogue scripts)
│   └── Generated/
│       └── EmbeddedDialogues.cs
└── DialogueData/
    ├── museum_entrance.yaml
    └── test_dialogue_.yaml
```

## 🎯 Next Steps

After setting up the example scene:

1. **Explore the UI**: Try all dialogue features
2. **Create Custom Dialogues**: Add new YAML files to DialogueData
3. **Implement Triggers**: Add custom trigger logic in TriggerManager
4. **Add Choice Logic**: Implement choice consequences in your game code
5. **Customize UI**: Modify UXML/USS files to match your game's style

## 💡 Tips

- **Always regenerate** embedded dialogues after changing YAML files
- **Use debug mode** during development for detailed logging
- **Test on multiple platforms** - embedded dialogues work everywhere
- **Keep dialogue files small** for better performance
- **Use consistent naming** for dialogue files and trigger IDs

## 🔗 Related Documentation

- **Main README.md**: Complete system documentation
- **Individual Scripts**: Each component has detailed code comments
- **YAML Format**: See examples in DialogueData folder
