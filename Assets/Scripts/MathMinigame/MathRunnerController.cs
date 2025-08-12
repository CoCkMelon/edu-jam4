using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MathRunnerController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Game Settings")]
    [SerializeField] private float playerBaseSpeed = 300f;
    [SerializeField] private float playerMaxSpeed = 800f;
    [SerializeField] private float playerAcceleration = 2000f;
    [SerializeField] private float playerDrag = 0.95f;
    [SerializeField] private float gravity = 1500f;
    [SerializeField] private float jumpForce = 600f;
    [SerializeField] private float scrollSpeed = 200f;
    [SerializeField] private float numberLineSpacing = 600f;
    [SerializeField] private float gateCollisionRadius = 40f;
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
    private float playerVelocityY = 0f;
    private bool isJumping = false;
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
        public VisualElement gatesContainer;
        public float position;
        public List<NumberGate> gates;
        public bool passed;
    }

    private class NumberGate
    {
        public VisualElement element;
        public int value;
        public bool isCorrect;
        public float yPosition;
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
        playerVelocityY = 0f;
        isJumping = false;
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

        // Apply gravity
        playerVelocityY -= gravity * Time.deltaTime;

        // Apply drag
        playerVelocityY *= playerDrag;

        // Update player position
        playerY += playerVelocityY * Time.deltaTime;

        // Clamp player to screen bounds
        float screenHeight = gameWorld.resolvedStyle.height;
        playerY = Mathf.Clamp(playerY, 25f, screenHeight - 25f);

        // Update player UI position
        player.style.top = playerY - 25f;

        // Scroll number lines
        for (int i = activeLines.Count - 1; i >= 0; i--)
        {
            var line = activeLines[i];
            line.position -= scrollSpeed * Time.deltaTime;
            line.container.style.left = line.position;

            // Check collision with player
            if (!line.passed && line.position < 150f)
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

        // Create gates container (vertical stack)
        line.gatesContainer = new VisualElement();
        line.gatesContainer.AddToClassList("number-gate-container");

        // Determine number of gates (3-5)
        int numGates = Random.Range(3, 5);

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
            if (options[i] != currentTask.answer)
            {
                gate.element.AddToClassList("number-gate-wrong");
            }
            gate.value = options[i];
            gate.isCorrect = (gate.value == currentTask.answer);
            gate.yPosition = 100f + (i * 120f); // Vertical spacing

            // Position the gate in the container (will be positioned vertically by the container)
            gate.element.style.position = Position.Absolute;
            gate.element.style.top = 0; // Let flex layout handle positioning

            var numberText = new Label(gate.value.ToString());
            numberText.AddToClassList("number-text");
            gate.element.Add(numberText);

            line.gatesContainer.Add(gate.element);
            line.gates.Add(gate);
        }

        line.container.Add(line.gatesContainer);
        numberLinesContainer.Add(line.container);
        activeLines.Add(line);
    }

    void CheckCollision(NumberLine line)
    {
        float playerScreenY = playerY;
        bool hitCorrect = false;
        bool hitWrong = false;

        foreach (var gate in line.gates)
        {
            // Calculate distance between player and gate
            float distance = Mathf.Abs(playerScreenY - gate.yPosition);

            if (distance <= gateCollisionRadius)
            {
                if (gate.isCorrect)
                {
                    hitCorrect = true;
                }
                else
                {
                    hitWrong = true;
                    // Visual feedback for wrong answer
                    gate.element.style.backgroundColor = new Color(1f, 0.2f, 0.2f, 0.5f);
                }
            }
        }

        if (hitCorrect)
        {
            OnCorrectAnswer();
        }
        else if (hitWrong)
        {
            OnWrongAnswer();
        }
        else
        {
            OnMissed();
        }
    }

    void OnCorrectAnswer()
    {
        score += 10;
        UpdateScore();
        ShowFeedback("Correct!", new Color(0.4f, 1f, 0.4f));
        GenerateNewTask();
    }

    void OnWrongAnswer()
    {
        lives--;
        UpdateLives();
        ShowFeedback("Wrong!", new Color(1f, 0.4f, 0.4f));

        if (lives <= 0)
        {
            GameOver();
        }
    }

    void OnMissed()
    {
        lives--;
        UpdateLives();
        ShowFeedback("Missed!", new Color(1f, 1f, 0.4f));

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

    void ApplyJumpForce()
    {
        if (isGameOver) return;

        playerVelocityY = jumpForce;
        isJumping = true;

        // Add visual effect
        player.AddToClassList("player-physics");
        StartCoroutine(RemovePhysicsClass());
    }

    IEnumerator RemovePhysicsClass()
    {
        yield return new WaitForSeconds(0.2f);
        player.RemoveFromClassList("player-physics");
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        ApplyJumpForce();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        isJumping = false;
    }

    void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.W || evt.keyCode == KeyCode.Space)
        {
            ApplyJumpForce();
        }
    }

    void OnKeyUp(KeyUpEvent evt)
    {
        if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.W || evt.keyCode == KeyCode.Space)
        {
            isJumping = false;
        }
    }
}
