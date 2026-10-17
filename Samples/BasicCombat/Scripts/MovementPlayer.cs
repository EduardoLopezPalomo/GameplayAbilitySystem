using UnityEngine;
using UnityEngine.InputSystem;


public class MovementPlayer : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    
    private Vector2 movementInput;
    private float currentSpeedX;
    private float currentSpeedZ;
    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        float targetSpeedX = movementInput.x * movementSpeed;
        float targetSpeedZ = movementInput.y * movementSpeed;

        currentSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeedX, acceleration * Time.deltaTime);
        
        currentSpeedZ = Mathf.MoveTowards(currentSpeedZ, targetSpeedZ, acceleration * Time.deltaTime);
        
        Vector3 movement = new Vector3(currentSpeedX, 0f, currentSpeedZ);

        characterController.Move(movement * Time.deltaTime);
    }
    
}
