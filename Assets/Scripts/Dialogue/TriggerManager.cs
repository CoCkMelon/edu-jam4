using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// TriggerManager handles dialogue triggers and game events
/// Guide: Add your game-specific logic here based on dialogue triggers
/// </summary>
public class TriggerManager : MonoBehaviour
{
    public static TriggerManager Instance;
    
    // GUIDE: Add references to your game systems here
    // Example:
    // [Header("System References")]
    // [SerializeField] private DialogueManager dialogueManager;
    // [SerializeField] private YourGameManager gameManager;
    // [SerializeField] private YourUIController uiController;
    
    // GUIDE: Add game state variables here
    // Example:
    // [Header("Game State")]
    // [SerializeField] private bool playerHasKey = false;
    // [SerializeField] private bool questCompleted = false;
    // [SerializeField] private int playerScore = 0;
    
    // GUIDE: Add Unity Events for your game systems
    // Example:
    // [Header("Events")]
    // public UnityEvent OnQuestStart;
    // public UnityEvent OnPlayerDeath;
    // public UnityEvent OnLevelComplete;
    
    // GUIDE: Add private variables for tracking game progression
    // Example:
    // private int currentLevel = 1;
    // private bool isInCombat = false;
    
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
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // GUIDE: Initialize your game systems here
        // Example:
        // if (dialogueManager == null)
        //     dialogueManager = FindFirstObjectByType<DialogueManager>();
        // if (gameManager == null)
        //     gameManager = FindFirstObjectByType<YourGameManager>();
        //     
        // SetupGameEvents();
    }
    
    private void SetupGameEvents()
    {
        // GUIDE: Setup event listeners for your game systems here
        // Example:
        // if (yourPuzzle != null)
        //     yourPuzzle.OnPuzzleComplete.AddListener(OnYourPuzzleComplete);
        // if (yourEnemy != null)
        //     yourEnemy.OnEnemyDefeated.AddListener(OnEnemyDefeated);
    }

    public void Trigger(string triggerName, DialogueLine line)
    {
        Debug.Log($"Trigger activated: {triggerName}");
        
        // GUIDE: Add your trigger handling logic here
        // This method is called when dialogue encounters a trigger
        // Example:
        switch (triggerName)
        {
            // GUIDE: Add cases for your specific triggers
            // case "start_quest":
            //     StartQuest();
            //     break;
            // case "give_item":
            //     GiveItemToPlayer(line.item_id);
            //     break;
            // case "change_scene":
            //     LoadScene(line.next_scene);
            //     break;
            // case "unlock_ability":
            //     UnlockPlayerAbility(line.ability_name);
            //     break;
            case "loose_end":
                SceneManager.LoadScene(0);
                break;
            case "win_end":
                Application.Quit();
                break;
                
            default:
                Debug.LogWarning($"Unknown trigger: {triggerName}");
                break;
        }
    }
    
    #region Your Game Logic Methods
    
    // GUIDE: Add your triggered methods here
    // Example:
    // private void StartQuest()
    // {
    //     // Your quest starting logic
    // }
    // 
    // private void GiveItemToPlayer(string itemId)
    // {
    //     // Your item giving logic
    // }
    // 
    // private void LoadScene(string sceneName)
    // {
    //     // Your scene loading logic
    // }
    
    #endregion
}

