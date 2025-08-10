using System.Collections.Generic;

public class DialogueLine
{
    public string id;
    public string speaker;
    public string text;
    public string sprite;
    public string sound;
    public string animation;
    public string cutscene;
    public string trigger;
    public string quest_id;
    public string next_scene;
    public List<DialogueOption> options;
}

public class DialogueOption
{
    public string choice;
    public string next;
}

public class DialogueScene
{
    public string scene;
    public List<DialogueLine> lines;
}

