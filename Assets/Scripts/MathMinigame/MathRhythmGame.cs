using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class MathRhythmGameController : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float mathGravity = 800f;
    [SerializeField] private float mathJumpForce = 350f;
    [SerializeField] private float mathMaxVelocity = 500f;
    [SerializeField] private float mathScrollSpeed = 200f;
    [SerializeField] private int mathMinNumber = 0;
    [SerializeField] private int mathMaxNumber = 20;

    private UIDocument mathUIDocument;
    private VisualElement mathRoot;
    private VisualElement mathPlayerUFO;
    private VisualElement mathNumberRuler;
    private VisualElement mathGameArea;
    private VisualElement mathMovingElements;
    private Label mathTaskLabel;
    private Label mathScoreLabel;

    private float mathPlayerY;
    private float mathPlayerVelocity;
    private float mathGameAreaHeight;
    private bool mathIsFlying;

    private List<MathNumberElement> mathNumberElements = new List<MathNumberElement>();
    private MathTask mathCurrentTask;
    private int mathScore;
    private float mathAnswerCheckCooldown;

    private List<MathMovingObject> mathMovingObjects = new List<MathMovingObject>();
    private float mathNextSpawnTime;

    private class MathNumberElement
    {
        public Label label;
        public int value;
        public float yPosition;
        public bool isHighlighted;
    }

    private class MathTask
    {
        public int operand1;
        public int operand2;
        public string operation;
        public int answer;

        public string GetTaskText()
        {
            return $"{operand1} {operation} {operand2} = ?";
        }
    }

    private class MathMovingObject
    {
        public VisualElement element;
        public float xPosition;
        public float yPosition;
        public bool isObstacle;
    }

    void Start()
    {
        mathUIDocument = GetComponent<UIDocument>();
        if (mathUIDocument == null)
        {
            mathUIDocument = gameObject.AddComponent<UIDocument>();
        }

        InitializeMathUI();
        GenerateNewMathTask();
    }

    void InitializeMathUI()
    {
        mathRoot = mathUIDocument.rootVisualElement;
        mathPlayerUFO = mathRoot.Q<VisualElement>("MathPlayerUFO");
        mathNumberRuler = mathRoot.Q<VisualElement>("MathNumberRuler");
        mathGameArea = mathRoot.Q<VisualElement>("MathGameArea");
        mathMovingElements = mathRoot.Q<VisualElement>("MathMovingElements");
        mathTaskLabel = mathRoot.Q<Label>("MathTaskLabel");
        mathScoreLabel = mathRoot.Q<Label>("MathScoreLabel");

        // Set up input handlers
        mathRoot.RegisterCallback<PointerDownEvent>(OnMathPointerDown);
        mathRoot.RegisterCallback<PointerUpEvent>(OnMathPointerUp);

        // Initialize player position
        mathRoot.RegisterCallback<GeometryChangedEvent>(OnMathGeometryChanged);
    }

    void OnMathGeometryChanged(GeometryChangedEvent evt)
    {
        mathGameAreaHeight = mathGameArea.resolvedStyle.height;
        mathPlayerY = mathGameAreaHeight / 2f;

        // Generate number ruler
        GenerateMathNumberRuler();

        // Unregister to avoid repeated calls
        mathRoot.UnregisterCallback<GeometryChangedEvent>(OnMathGeometryChanged);
    }

    void GenerateMathNumberRuler()
    {
        mathNumberRuler.Clear();
        mathNumberElements.Clear();

        int numberCount = mathMaxNumber - mathMinNumber + 1;
        float spacing = mathGameAreaHeight / (numberCount + 1);

        for (int i = 0; i <= mathMaxNumber - mathMinNumber; i++)
        {
            int value = mathMaxNumber - i;
            float yPos = spacing * (i + 1);

            var numberLabel = new Label(value.ToString());
            numberLabel.AddToClassList("math-number-label");
            numberLabel.style.top = yPos - 15;

            mathNumberRuler.Add(numberLabel);

            mathNumberElements.Add(new MathNumberElement
            {
                label = numberLabel,
                value = value,
                yPosition = yPos,
                isHighlighted = false
            });
        }
    }

    void Update()
    {
        HandleMathInput();
        UpdateMathPlayerPhysics();
        UpdateMathNumberHighlight();
        UpdateMathMovingObjects();
        // CheckMathAnswer(); // REMOVED: Answer is now checked on input release, not every frame.
        SpawnMathObjects();

        // ADDED: The cooldown timer still needs to be ticked down in Update.
        if (mathAnswerCheckCooldown > 0)
        {
            mathAnswerCheckCooldown -= Time.deltaTime;
        }
    }

    void HandleMathInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            mathIsFlying = true;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            mathIsFlying = false;
            CheckMathAnswer(); // ADDED: Check answer on key release
        }
    }

    void OnMathPointerDown(PointerDownEvent evt)
    {
        mathIsFlying = true;
    }

    void OnMathPointerUp(PointerUpEvent evt)
    {
        mathIsFlying = false;
        CheckMathAnswer(); // ADDED: Check answer on pointer release
    }

    void UpdateMathPlayerPhysics()
    {
        // Apply force or gravity
        if (mathIsFlying)
        {
            mathPlayerVelocity += mathJumpForce * Time.deltaTime;
        }
        else
        {
            mathPlayerVelocity -= mathGravity * Time.deltaTime;
        }

        // Clamp velocity
        mathPlayerVelocity = Mathf.Clamp(mathPlayerVelocity, -mathMaxVelocity, mathMaxVelocity);

        // Update position
        mathPlayerY += mathPlayerVelocity * Time.deltaTime;

        // --- CHANGED: Boundary check and velocity reset ---
        // Clamp position to game area and reset velocity if boundaries are hit
        float minY = 20f;
        float maxY = mathGameAreaHeight - 20f;

        if (mathPlayerY <= minY)
        {
            mathPlayerY = minY;
            mathPlayerVelocity = 0f; // Reset velocity at the bottom
        }
        else if (mathPlayerY >= maxY)
        {
            mathPlayerY = maxY;
            mathPlayerVelocity = 0f; // Reset velocity at the top
        }
        // --- End of change ---

        // Apply position to UFO
        if (mathPlayerUFO != null)
        {
            mathPlayerUFO.style.top = mathPlayerY - 20;
        }
    }

    void UpdateMathNumberHighlight()
    {
        float closestDistance = float.MaxValue;
        MathNumberElement closestNumber = null;

        foreach (var numberElement in mathNumberElements)
        {
            float distance = Mathf.Abs(numberElement.yPosition - mathPlayerY);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNumber = numberElement;
            }

            // Reset highlight
            if (numberElement.isHighlighted)
            {
                numberElement.label.RemoveFromClassList("math-number-label-highlighted");
                numberElement.isHighlighted = false;
            }
        }

        // Highlight closest number
        if (closestNumber != null && closestDistance < 40)
        {
            closestNumber.label.AddToClassList("math-number-label-highlighted");
            closestNumber.isHighlighted = true;
        }
    }

    void CheckMathAnswer()
    {
        if (mathAnswerCheckCooldown > 0)
        {
            return;
        }

        var highlightedNumber = mathNumberElements.FirstOrDefault(n => n.isHighlighted);

        // CHANGED: Removed the '&& Input.GetKeyDown(KeyCode.Return)' to check answer on input release
        if (highlightedNumber != null)
        {
            if (highlightedNumber.value == mathCurrentTask.answer)
            {
                mathScore += 100;
                mathScoreLabel.text = $"Score: {mathScore}";
                GenerateNewMathTask();
            }
            else
            {
                mathScore = Mathf.Max(0, mathScore - 50);
                mathScoreLabel.text = $"Score: {mathScore}";
            }

            mathAnswerCheckCooldown = 1f; // Cooldown to prevent immediate re-submission
        }
    }

    void GenerateNewMathTask()
    {
        mathCurrentTask = new MathTask();

        string[] operations = { "+", "-", "*" };
        mathCurrentTask.operation = operations[Random.Range(0, operations.Length)];

        switch (mathCurrentTask.operation)
        {
            case "+":
                mathCurrentTask.operand1 = Random.Range(1, 11);
                mathCurrentTask.operand2 = Random.Range(1, 11);
                mathCurrentTask.answer = mathCurrentTask.operand1 + mathCurrentTask.operand2;
                break;
            case "-":
                mathCurrentTask.operand1 = Random.Range(5, 15);
                mathCurrentTask.operand2 = Random.Range(1, mathCurrentTask.operand1);
                mathCurrentTask.answer = mathCurrentTask.operand1 - mathCurrentTask.operand2;
                break;
            case "*":
                mathCurrentTask.operand1 = Random.Range(1, 6);
                mathCurrentTask.operand2 = Random.Range(1, 5);
                mathCurrentTask.answer = mathCurrentTask.operand1 * mathCurrentTask.operand2;
                break;
        }

        mathCurrentTask.answer = Mathf.Clamp(mathCurrentTask.answer, mathMinNumber, mathMaxNumber);
        mathTaskLabel.text = mathCurrentTask.GetTaskText();
    }

    void SpawnMathObjects()
    {
        if (Time.time < mathNextSpawnTime) return;

        var newObject = new MathMovingObject();
        newObject.xPosition = mathGameArea.resolvedStyle.width;
        newObject.yPosition = Random.Range(50f, mathGameAreaHeight - 50f);
        newObject.isObstacle = Random.Range(0f, 1f) > 0.5f;

        var element = new VisualElement();
        element.AddToClassList(newObject.isObstacle ? "math-obstacle" : "math-collectible");
        element.style.left = newObject.xPosition;
        element.style.top = newObject.yPosition;

        mathMovingElements.Add(element);
        newObject.element = element;
        mathMovingObjects.Add(newObject);

        mathNextSpawnTime = Time.time + Random.Range(1f, 3f);
    }

    void UpdateMathMovingObjects()
    {
        for (int i = mathMovingObjects.Count - 1; i >= 0; i--)
        {
            var obj = mathMovingObjects[i];
            obj.xPosition -= mathScrollSpeed * Time.deltaTime;
            obj.element.style.left = obj.xPosition;

            // Remove if off-screen
            if (obj.xPosition < -100)
            {
                obj.element.RemoveFromHierarchy();
                mathMovingObjects.RemoveAt(i);
            }
        }
    }
}
