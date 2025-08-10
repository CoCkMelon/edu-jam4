# Dialogue System Examples

This folder contains example implementations and UI files for the Dialogue System. These are separate from the core system files and can be used as templates or starting points for your own implementation.

## 📁 Folder Structure

```
Examples/
├── README_Examples.md              # This file
├── EXAMPLE_SCENE_SETUP.md         # Complete setup instructions
├── ExampleSceneBuilder.cs         # Automatic scene setup script
└── UI/
    ├── DialogueSystemUI.uxml       # Example UXML UI layout
    └── DialogueSystemUI.uss        # Example USS stylesheet
```

## 🎯 What's Included

### UI Templates
- **DialogueSystemUI.uxml**: Complete dialogue interface layout
- **DialogueSystemUI.uss**: Advanced styling (fixed for Unity UI Toolkit)
- **DialogueSystemUI_Simple.uss**: Simple, error-free styling (recommended)

### Scene Setup
- **ExampleSceneBuilder.cs**: Automatic scene builder script
- **EXAMPLE_SCENE_SETUP.md**: Step-by-step setup guide

## 🚀 Quick Start

1. **Copy the UI files** to your project's UI folder
2. **Use the ExampleSceneBuilder.cs** in a new scene
3. **Follow EXAMPLE_SCENE_SETUP.md** for detailed instructions

## 💡 Usage Tips

- These are **examples** - modify them to fit your game's style
- The UI uses **Unity UI Toolkit** (UI Elements)
- All examples work with the **embedded dialogue system**
- Compatible with **all platforms** including WebGL

## 🔗 Integration

To use these examples with the core dialogue system:

1. Ensure your `DialogueUIController` references the correct UXML file
2. Make sure all UI element names match what the controller expects
3. The ExampleSceneBuilder will handle most setup automatically

## ⚠️ Important Notes

- These files are **separate from the core system**
- Safe to modify or delete without affecting core functionality
- Always regenerate embedded dialogues after changing YAML files
- Test on your target platforms to ensure compatibility

## 📖 Related Documentation

- **../README.md**: Main system documentation
- **EXAMPLE_SCENE_SETUP.md**: Detailed setup instructions
- **Individual core scripts**: Each has comprehensive code comments
