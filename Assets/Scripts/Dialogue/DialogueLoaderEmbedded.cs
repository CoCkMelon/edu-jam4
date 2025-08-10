using UnityEngine;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using DialogueSystem;

public static class DialogueLoaderEmbedded
{
    private static IDeserializer _deserializer;
    
    static DialogueLoaderEmbedded()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }
    
    /// <summary>
    /// Load dialogue from embedded YAML strings (WebGL compatible)
    /// </summary>
    public static DialogueScene LoadFromEmbedded(string dialogueName)
    {
        try
        {
            string yamlContent = EmbeddedDialogues.GetDialogue(dialogueName);
            
            if (string.IsNullOrEmpty(yamlContent))
            {
                Debug.LogError($"Dialogue '{dialogueName}' not found in embedded dialogues!");
                return null;
            }
            
            return _deserializer.Deserialize<DialogueScene>(yamlContent);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load embedded dialogue '{dialogueName}': {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get list of all available embedded dialogues
    /// </summary>
    public static string[] GetAvailableDialogues()
    {
        return EmbeddedDialogues.GetAllDialogueNames();
    }
    
    /// <summary>
    /// Check if a dialogue exists in embedded data
    /// </summary>
    public static bool HasDialogue(string dialogueName)
    {
        return EmbeddedDialogues.HasDialogue(dialogueName);
    }
    
    /// <summary>
    /// Fallback: Load from file (for development/editor only)
    /// This won't work in WebGL builds
    /// </summary>
    public static DialogueScene LoadFromFile(string path)
    {
        #if UNITY_EDITOR
        try
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Dialogue file not found: {path}");
                return null;
            }
            
            string yamlContent = File.ReadAllText(path);
            return _deserializer.Deserialize<DialogueScene>(yamlContent);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load dialogue from file '{path}': {e.Message}");
            return null;
        }
        #else
        Debug.LogWarning($"File loading not supported in builds. Use LoadFromEmbedded() instead for '{path}'");
        return null;
        #endif
    }
    
    /// <summary>
    /// Load from Resources (still works in WebGL but requires Resources folder)
    /// </summary>
    public static DialogueScene LoadFromResources(string resourcePath)
    {
        try
        {
            TextAsset yamlFile = Resources.Load<TextAsset>(resourcePath);
            
            if (yamlFile == null)
            {
                Debug.LogError($"Dialogue resource not found: {resourcePath}");
                return null;
            }
            
            return _deserializer.Deserialize<DialogueScene>(yamlFile.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load dialogue from resources '{resourcePath}': {e.Message}");
            return null;
        }
    }
}
