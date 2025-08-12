using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MathRunnerController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Game Settings")]
    [SerializeField] private float playerSpeed = 5f;
    [SerializeField] private float scrollSpeed = 200f;
    [SerializeField] private float numberLineSpacing = 600f;
    [SerializeField] private int maxLives = 3;

    private VisualElement root;
    private VisualElement player;
    private VisualElement gameWorld;
    private VisualElement numberLinesContainer;
    private Label scoreLabel;
    private Label taskLabel;
    private Label livesLabel;
    private Label feedbackLabel;
    private VisualElement gameOverPanel;
    private Label finalScoreLabel;

    private float playerY = 300f;
    private float targetY = 300f;
    private int score = 0;
    private int lives;
    private bool isGameOver = false;
    private float nextLinePosition = 800f;

    private MathTask currentTask;
    private List<NumberLine> activeLines = new List<NumberLine>();

    private class MathTask
    {
        public int num1;
        public int num2;
        public string operation;
        public int answer;

        public string GetTaskText()
        {
            return $"{num1} {operation} {num2} = ?";
        }
    }

    private class NumberLine
    {
        public VisualElement container;
        public float position;
        public List<NumberGate> gates;
        public bool passed;
    }

    private class NumberGate
    {
        public VisualElement element;
        public int value;
        public bool isCorrect;
    }

    void Start()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        InitializeUI();
        StartNewGame();
    }

    void InitializeUI()
    {
        root = uiDocument.rootVisualElement;

        gameWorld = root.Q<VisualElement>("game-world");
        player = root.Q<VisualElement>("player");
        numberLinesContainer = root.Q<VisualElement>("number-lines-container");
        scoreLabel = root.Q<Label>("score-label");
        taskLabel = root.Q<Label>("task-label");
        livesLabel = root.Q<Label>("lives-label");
        feedbackLabel = root.Q<Label>("feedback-label");
        gameOverPanel = root.Q<VisualElement>("game-over-panel");
        finalScoreLabel = root.Q<Label>("final-score-label");

        var restartButton = root.Q<Button>("restart-button");
        restartButton.clicked += StartNewGame;

        // Register input callbacks
        root.RegisterCallback<PointerDownEvent>(OnPointerDown);
        root.RegisterCallback<PointerUpEvent>(OnPointerUp);
        root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        root.RegisterCallback<KeyUpEvent>(OnKeyUp);

        // Focus the root to receive keyboard events
        root.focusable = true;
        root.Focus();
    }

    void StartNewGame()
    {
        score = 0;
        lives = maxLives;
        isGameOver = false;
        playerY = 300f;
        targetY = 300f;
        nextLinePosition = 800f;

        // Clear existing lines
        foreach (var line in activeLines)
        {
            line.container.RemoveFromHierarchy();
        }
        activeLines.Clear();

        // Update UI
        UpdateScore();
        UpdateLives();
        gameOverPanel.style.display = DisplayStyle.None;

        // Generate first task
        GenerateNewTask();

        // Create first number line
        CreateNumberLine();
    }

    void Update()
    {
        if (isGameOver) return;

        // Smooth player movement
        playerY = Mathf.Lerp(playerY, targetY, Time.deltaTime * 10f);
        player.style.top = playerY;

        // Scroll number lines
        for (int i = activeLines.Count - 1; i >= 0; i--)
        {
            var line = activeLines[i];
            line.position -= scrollSpeed * Time.deltaTime;
            line.container.style.left = line.position;

            // Check collision with player
            if (!line.passed && line.position < 150f && line.position > 50f)
            {
                CheckCollision(line);
                line.passed = true;
            }

            // Remove lines that have scrolled off screen
            if (line.position < -200f)
            {
                line.container.RemoveFromHierarchy();
                activeLines.RemoveAt(i);
            }
        }

        // Create new lines
        if (activeLines.Count == 0 || activeLines[activeLines.Count - 1].position < nextLinePosition - numberLineSpacing)
        {
            CreateNumberLine();
        }
    }

    void GenerateNewTask()
    {
        currentTask = new MathTask();

        // Randomly choose operation
        int opType = Random.Range(0, 4);
        switch (opType)
        {
            case 0: // Addition
                currentTask.num1 = Random.Range(1, 20);
                currentTask.num2 = Random.Range(1, 20);
                currentTask.operation = "+";
                currentTask.answer = currentTask.num1 + currentTask.num2;
                break;
            case 1: // Subtraction
                currentTask.num1 = Random.Range(10, 30);
                currentTask.num2 = Random.Range(1, currentTask.num1);
                currentTask.operation = "-";
                currentTask.answer = currentTask.num1 - currentTask.num2;
                break;
            case 2: // Multiplication
                currentTask.num1 = Random.Range(2, 10);
                currentTask.num2 = Random.Range(2, 10);
                currentTask.operation = "×";
                currentTask.answer = currentTask.num1 * currentTask.num2;
                break;
            case 3: // Division
                currentTask.num2 = Random.Range(2, 10);
                currentTask.answer = Random.Range(2, 10);
                currentTask.num1 = currentTask.num2 * currentTask.answer;
                currentTask.operation = "÷";
                break;
        }

        taskLabel.text = currentTask.GetTaskText();
    }

    void CreateNumberLine()
    {
        var line = new NumberLine();
        line.container = new VisualElement();
        line.container.AddToClassList("number-line");
        line.position = nextLinePosition;
        line.container.style.left = line.position;
        line.gates = new List<NumberGate>();
        line.passed = false;

        // Determine number of gates and their positions
        int numGates = Random.Range(3, 5);
        float totalHeight = gameWorld.resolvedStyle.height;
        float gateSpacing = (totalHeight - 100f) / (numGates - 1);

        // Generate answer options
        List<int> options = new List<int>();
        options.Add(currentTask.answer); // Add correct answer

        // Add wrong answers
        for (int i = 1; i < numGates; i++)
        {
            int wrongAnswer;
            do
            {
                wrongAnswer = currentTask.answer + Random.Range(-10, 11);
            } while (options.Contains(wrongAnswer) || wrongAnswer < 0);
            options.Add(wrongAnswer);
        }

        // Shuffle options
        for (int i = 0; i < options.Count; i++)
        {
            int temp = options[i];
            int randomIndex = Random.Range(i, options.Count);
            options[i] = options[randomIndex];
            options[randomIndex] = temp;
        }

        // Create gates
        for (int i = 0; i < numGates; i++)
        {
            var gate = new NumberGate();
            gate.element = new VisualElement();
            gate.element.AddToClassList("number-gate");
            gate.value = options[i];
            gate.isCorrect = (gate.value == currentTask.answer);

            if (!gate.isCorrect)
            {
                gate.element.AddToClassList("number-gate-wrong");
            }

            gate.element.style.top = 50f + (i * gateSpacing);

            var numberText = new Label(gate.value.ToString());
            numberText.AddToClassList("number-text");
            gate.element.Add(numberText);

            line.container.Add(gate.element);
            line.gates.Add(gate);
        }

        numberLinesContainer.Add(line.container);
        activeLines.Add(line);
    }

    void CheckCollision(NumberLine line)
    {
        float playerCenter = playerY + 25f; // Player height/2
        NumberGate hitGate = null;

        foreach (var gate in line.gates)
        {
            float gateTop = gate.element.resolvedStyle.top;
            float gateBottom = gateTop + 70f; // Gate height

            if (playerCenter >= gateTop && playerCenter <= gateBottom)
            {
                hitGate = gate;
                break;
            }
        }

        if (hitGate != null)
        {
            if (hitGate.isCorrect)
            {
                OnCorrectAnswer(hitGate);
            }
            else
            {
                OnWrongAnswer(hitGate);
            }
        }
        else
        {
            // Missed all gates
            OnMissed();
        }
    }

    void OnCorrectAnswer(NumberGate gate)
    {
        score += 10;
        UpdateScore();
        ShowFeedback("Correct!", Color.green);
        gate.element.AddToClassList("pulse-animation");
        GenerateNewTask();
    }

    void OnWrongAnswer(NumberGate gate)
    {
        lives--;
        UpdateLives();
        ShowFeedback("Wrong!", Color.red);
        gate.element.style.backgroundColor = new Color(1f, 0.2f, 0.2f, 0.5f);

        if (lives <= 0)
        {
            GameOver();
        }
    }

    void OnMissed()
    {
        lives--;
        UpdateLives();
        ShowFeedback("Missed!", Color.yellow);

        if (lives <= 0)
        {
            GameOver();
        }
    }

    void ShowFeedback(string text, Color color)
    {
        feedbackLabel.text = text;
        feedbackLabel.style.color = color;
        feedbackLabel.style.display = DisplayStyle.Flex;

        // Hide feedback after 1 second
        StartCoroutine(HideFeedbackAfterDelay());
    }

    IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        feedbackLabel.style.display = DisplayStyle.None;
    }

    void UpdateScore()
    {
        scoreLabel.text = $"Score: {score}";
    }

    void UpdateLives()
    {
        livesLabel.text = $"Lives: {lives}";
        if (lives <= 1)
        {
            livesLabel.style.color = Color.red;
        }
    }

    void GameOver()
    {
        isGameOver = true;
        finalScoreLabel.text = $"Final Score: {score}";
        gameOverPanel.style.display = DisplayStyle.Flex;
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        if (!isGameOver)
        {
            targetY = Mathf.Max(0, targetY - 50f);
        }
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (!isGameOver)
        {
            targetY = Mathf.Min(gameWorld.resolvedStyle.height - 50f, targetY + 50f);
        }
    }

    void OnKeyDown(KeyDownEvent evt)
    {
        if (isGameOver) return;

        if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.W)
        {
            targetY = Mathf.Max(0, targetY - 50f);
        }
        else if (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.S)
        {
            targetY = Mathf.Min(gameWorld.resolvedStyle.height - 50f, targetY + 50f);
        }
    }

    void OnKeyUp(KeyUpEvent evt)
    {
        // Can be used for additional controls if needed
    }
}
