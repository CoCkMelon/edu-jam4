using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StoryLocation
{
    public string name;
    public string description;
    public List<string> values;
}


public enum StoryLocationEnum
{
    MuseumEntrance,
    MuseumExit
}

public class StoryRouteManager : MonoBehaviour
{
    public static StoryRouteManager Instance;

    // Add your story progression logic here
    [Header("Story Progress")]
    public StoryLocationEnum currentLocation = StoryLocationEnum.MuseumEntrance;
    public Dictionary<string, int> valuePoints = new Dictionary<string, int>();
    
    // Add your story content initialization here
    [Header("Story Content")]
    private Dictionary<StoryLocationEnum, StoryLocationData> locationContent = new Dictionary<StoryLocationEnum, StoryLocationData>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Only apply DontDestroyOnLoad if this is a root GameObject
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            // GUIDE: Initialize your story system here
            // InitializeValues();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // GUIDE: Initialize your story content here
        // LoadLocationContent();
        // BeginStory();
    }
    
    void InitializeValues()
    {
        valuePoints["Loot"] = 0;
    }
    
    void LoadLocationContent()
    {
        // Load content from YAML files - simplified without YamlDotNet dependency
        // In a real implementation, you would load from the YAML files
        // For now, we'll use hardcoded content
    }
    
    public void BeginStory()
    {
        currentLocation = StoryLocationEnum.MuseumEntrance;
        ProcessCurrentLocation();
    }
    
    public void MakeChoice(string choice)
    {
        switch (currentLocation)
        {
            case StoryLocationEnum.MuseumEntrance:
                ProcessMuseumEntrance(choice);
                break;
        }
    }
    
    void ProcessCurrentLocation()
    {
        Debug.Log($"Current location: {currentLocation}");
        // Display location content and choices
    }
    
    void ProcessMuseumEntrance(string choice)
    {
        ProcessCurrentLocation();
    }
    
    
    StoryLocationEnum DetermineEnding()
    {
        // Calculate which values are highest to determine ending
        // if (valuePoints["Cool"] >= 20 && IsBalanced())
        // {
            // Cool
        // }
        return StoryLocationEnum.MuseumExit;
    }
    
    bool IsBalanced()
    {
        // Check if all core values are reasonably balanced (harmony principle)
        // string[] coreValues = { "Logic", "Activity", "Planning", "HardWork" };
        // int minValue = int.MaxValue;
        // int maxValue = int.MinValue;
        
        // foreach (string value in coreValues)
        // {
            // int points = valuePoints[value];
            // minValue = Mathf.Min(minValue, points);
            // maxValue = Mathf.Max(maxValue, points);
        // }
        
        // Values should be within 80% of each other for balance
        // return (float)minValue / maxValue >= 0.8f;
        return true;
    }
    
    void AwardValue(string valueName, int points)
    {
        if (valuePoints.ContainsKey(valueName))
        {
            valuePoints[valueName] += points;
            Debug.Log($"Awarded {points} {valueName} points. Total: {valuePoints[valueName]}");
        }
    }
    
    public Dictionary<string, int> GetValuePoints()
    {
        return new Dictionary<string, int>(valuePoints);
    }
    
    public StoryLocationEnum GetCurrentLocation()
    {
        return currentLocation;
    }
}

[System.Serializable]
public class StoryLocationData
{
    public string name;
    public string description;
    public List<string> availableChoices;
}
