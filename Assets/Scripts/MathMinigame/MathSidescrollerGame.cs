// Assets/Scripts/MathRhythmController.cs
using UnityEngine;
using UnityEngine.UIElements;

public class MathRhythmController : MonoBehaviour
{
    private VisualElement uxRoot;
    private VisualElement ufoPlayer;
    private VisualElement answerLine;
    private Label mathTaskLabel;
    private Label feedbackLabel;

    private float ufoVerticalPosition = 0.5f; // 0 = bottom, 1 = top (normalized)
    private float ufoVelocity = 0f;
    public float gravity = 0.001f;
    public float thrust = 0.0005f;

    private float lineProgress = 0f;
    public float lineSpeed = 0.003f;

    private int correctAnswer;
    private bool answerRecorded = false;
    private const float TOLERANCE = 0.05f; // How close to correct Y

    private float rulerMin = -10f;
    private float rulerMax = 10f;

    private void Start()
    {
        uxRoot = GetComponent<UIDocument>().rootVisualElement;

        // Load UI elements
        ufoPlayer = uxRoot.Q<VisualElement>("ufo-player");
        answerLine = uxRoot.Q<VisualElement>("answer-line");
        mathTaskLabel = uxRoot.Q<Label>("math-task");
        feedbackLabel = uxRoot.Q<Label>("feedback");

        GenerateNewTask();
    }

    private void Update()
    {
        HandlePlayerInput();
        UpdateUfoPosition();
        UpdateAnswerLine();

        if (!answerRecorded && lineProgress >= 0.5f)
        {
            RecordAnswer();
            answerRecorded = true;
        }
    }

    private void HandlePlayerInput()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            ufoVelocity -= thrust;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            ufoVelocity += thrust;
        }

        ufoVelocity += gravity; // Simulate gravity pulling down
        ufoVelocity *= 0.98f; // Damping
    }

    private void UpdateUfoPosition()
    {
        ufoVerticalPosition += ufoVelocity;

        // Clamp within screen
        ufoVerticalPosition = Mathf.Clamp(ufoVerticalPosition, 0.05f, 0.95f);

        // Convert normalized position to pixel Y
        float playerY = ufoVerticalPosition * Screen.height;
        ufoPlayer.style.top = new Length(playerY, LengthUnit.Pixel);
    }

    private void UpdateAnswerLine()
    {
        lineProgress += lineSpeed;
        if (lineProgress > 1.0f)
        {
            if (!answerRecorded)
                RecordAnswer();

            ResetRound();
        }
        else
        {
            answerLine.style.left = new Length(lineProgress * 100, LengthUnit.Percent);
        }
    }

    private void GenerateNewTask()
    {
        int a = Random.Range(1, 10);
        int b = Random.Range(1, 10);
        correctAnswer = a + b;

        // Ensure answer fits ruler
        while (correctAnswer > rulerMax || correctAnswer < rulerMin)
        {
            a = Random.Range(1, 10);
            b = Random.Range(1, 10);
            correctAnswer = a + b;
        }

        mathTaskLabel.text = $"Solve: {a} + {b} = ?";
        feedbackLabel.text = "";
        answerRecorded = false;
    }

    private void RecordAnswer()
    {
        float normalizedY = ufoVerticalPosition;
        float mappedValue = Mathf.Lerp(rulerMax, rulerMin, normalizedY); // top=1 → min, bottom=0 → max

        int playerAnswer = Mathf.RoundToInt(mappedValue);

        float correctNormalizedY = 1 - ((correctAnswer - rulerMin) / (rulerMax - rulerMin));
        float diff = Mathf.Abs(ufoVerticalPosition - correctNormalizedY);

        if (diff <= TOLERANCE)
        {
            feedbackLabel.text = $"Correct! ({playerAnswer})";
            feedbackLabel.style.color = Color.green;
        }
        else
        {
            feedbackLabel.text = $"Wrong! You picked {playerAnswer}, answer was {correctAnswer}";
            feedbackLabel.style.color = Color.red;
        }
    }

    private void ResetRound()
    {
        lineProgress = 0f;
        answerLine.style.left = new Length(0, LengthUnit.Percent);
        Invoke("GenerateNewTask", 2f);
    }
}
