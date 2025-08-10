using UnityEngine;
using YamlDotNet.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class RouteContent
{
    public string place;
    public string description;
    public List<string> values;
}

public class YAMLContentLoader : MonoBehaviour
{
    private Dictionary<string, RouteContent> routeContents = new Dictionary<string, RouteContent>();

    void Start()
    {
        LoadAllRoutes();
    }

    void LoadAllRoutes()
    {
        LoadRoute("StoryRoutes/VillageEntrance.yaml");
        LoadRoute("StoryRoutes/VillageCenter.yaml");
        LoadRoute("StoryRoutes/ForestPath.yaml");
    }

    void LoadRoute(string path)
    {
        var yaml = File.ReadAllText(Application.dataPath + "/" + path);
        var deserializer = new DeserializerBuilder()
            .Build();

        RouteContent loadedContent = deserializer.Deserialize<RouteContent>(yaml);
        routeContents[loadedContent.place] = loadedContent;
    }

    public RouteContent GetContent(string place)
    {
        if (routeContents.TryGetValue(place, out RouteContent content))
        {
            return content;
        }
        else
        {
            throw new Exception("Content not found for place: " + place);
        }
    }
}
