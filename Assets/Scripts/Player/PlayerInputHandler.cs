using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerController2D playerController;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        playerController = GetComponent<PlayerController2D>();
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }
        
        // Ensure we have the Input Actions asset
        var inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
        if (inputActions != null)
        {
            playerInput.actions = inputActions;
        }
    }
    
    private void OnEnable()
    {
        if (playerInput.actions != null)
        {
            // Movement
            playerInput.actions["Move"].performed += OnMove;
            playerInput.actions["Move"].canceled += OnMove;
            
            // Jump
            playerInput.actions["Jump"].performed += OnJump;
            playerInput.actions["Jump"].canceled += OnJumpCanceled;
            
            // Dash
            playerInput.actions["Dash"].performed += OnDash;
            
            // Throw
            playerInput.actions["Fire"].performed += OnThrow;
        }
    }
    
    private void OnDisable()
    {
        if (playerInput.actions != null)
        {
            playerInput.actions["Move"].performed -= OnMove;
            playerInput.actions["Move"].canceled -= OnMove;
            playerInput.actions["Jump"].performed -= OnJump;
            playerInput.actions["Jump"].canceled -= OnJumpCanceled;
            playerInput.actions["Dash"].performed -= OnDash;
            playerInput.actions["Fire"].performed -= OnThrow;
        }
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        
        // Use reflection to set the moveInput field
        var moveInputField = typeof(PlayerController2D).GetField("moveInput", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (moveInputField != null)
        {
            moveInputField.SetValue(playerController, input);
        }
    }
    
    private void OnJump(InputAction.CallbackContext context)
    {
        var jumpPressedField = typeof(PlayerController2D).GetField("jumpPressed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (jumpPressedField != null)
        {
            jumpPressedField.SetValue(playerController, true);
        }
    }
    
    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        var jumpHeldField = typeof(PlayerController2D).GetField("jumpHeld", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (jumpHeldField != null)
        {
            jumpHeldField.SetValue(playerController, false);
        }
    }
    
    private void OnDash(InputAction.CallbackContext context)
    {
        var dashPressedField = typeof(PlayerController2D).GetField("dashPressed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dashPressedField != null)
        {
            dashPressedField.SetValue(playerController, true);
        }
    }
    
    private void OnThrow(InputAction.CallbackContext context)
    {
        var throwPressedField = typeof(PlayerController2D).GetField("throwPressed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (throwPressedField != null)
        {
            throwPressedField.SetValue(playerController, true);
        }
    }
}
