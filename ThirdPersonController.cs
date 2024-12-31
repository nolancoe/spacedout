using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System

public class ThirdPersonController : MonoBehaviour
{
    //Avoiding strings when setting animator triggers 
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Land = Animator.StringToHash("Land");
    private static readonly int Falling = Animator.StringToHash("Falling");
    private static readonly int Climb = Animator.StringToHash("Climb");
    private static readonly int ToBlendTree = Animator.StringToHash("ToBlendTree");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int StartHang = Animator.StringToHash("StartHang");

    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float sprintSpeedMultiplier = 2f; // Sprint multiplier
    public float jumpForce = 2f;
    public float gravity = -20f;
    public float turnSmoothTime = 0.1f;
    private Transform _cam;
    // Grounded delay variables
    private float _groundedTime;
    public float jumpDelay = 0.5f;
    private bool _isGrounded;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    
    [Header("Climbing/Hanging Settings")]
    public float hangTransitionSpeed = 5f; // Speed of snapping to hand position
    private bool _isHanging;
    private Transform _ledgeHandPosition;
    private float _targetLedgeRotationY;
    public float climbUpDistance = 2.0f; // Vertical distance to climb
    public float climbForwardDistance = 1.0f; // Forward distance to climb
    private bool _canClimb;
    public float climbDelayTime = 2f;
    public bool _isClimbing;
    private bool _canRehang = true;
    // Offsets for adjusting the player's position when hanging on a ledge
    private float _ledgeOffsetX, _ledgeOffsetY, _ledgeOffsetZ;
    [SerializeField] private GameObject ledgeFinderObject;
    
    
    [Header("Falling Settings")]
    private bool _wasGrounded; // To track grounded state from the previous frame
    private bool _isFalling; // To track if the player is currently falling
    
    
    
    

    // Controller stuff
    private CharacterController _controller;
    private Vector3 _velocity;
    
    private bool _jumpRequested;

    private float _turnSmoothVelocity;
    

    // New Input System
    private Vector2 _moveInput;
    private bool _jumpInput;
    private bool _isSprinting; // Tracks sprint input
    
    // Animation
    private Animator _animator;


    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        if (Camera.main != null)
        {
            _cam = Camera.main.transform;
        }

        _animator = GetComponent<Animator>();

        Cursor.lockState = CursorLockMode.Locked; // Lock cursor
        
        //Ledge Checker
        LedgeCollisionChecker.onLedgeCollision += HandleLedgeCollision;
        
    }

    private void OnDestroy()
    {
        LedgeCollisionChecker.onLedgeCollision -= HandleLedgeCollision;
    }

    private void Update()
    {
        if (!_isHanging && _ledgeHandPosition != null)
        {
            var distanceToLedge = Vector3.Distance(transform.position, _ledgeHandPosition.position);
            if (distanceToLedge > 5f) // Adjust threshold as needed
            {
                Debug.Log($"Clearing ledge data. Distance to ledge: {distanceToLedge}");
                _ledgeHandPosition = null;
                _targetLedgeRotationY = 0f;
            }
        }
        
        if (_isHanging && _ledgeHandPosition != null)
        {
            // Apply the offset to the hand position when setting the player's position
            var adjustedPosition = _ledgeHandPosition.position + new Vector3(_ledgeOffsetX, _ledgeOffsetY, _ledgeOffsetZ);
            transform.position = Vector3.Lerp(transform.position, adjustedPosition, hangTransitionSpeed * Time.deltaTime);
            //Method to handle climbing up from ledge
            HandleHangingInput();
        }
        else
        {
            _canClimb = false;
            // Existing movement logic...
            HandleGroundCheck();
            if (_controller.enabled)
            {
                HandleMovement();
                HandleGravity();
                HandleJump();
            }
        }
        
        
        // Get movement input (e.g., from a character controller or other input system)
        var horizontal = _moveInput.x; // Left/Right input
        var vertical = _moveInput.y;   // Forward/Backward input
        
        
        // Ensure sprinting animation stops when there's no movement input
        if (_isSprinting && _moveInput is { x: 0, y: 0 })
        {
            _isSprinting = false;
        }
        
        
        
        // Set Animator Parameters for the Blend Tree
        var speedValue = _isSprinting ? 2f : Mathf.Clamp01(Mathf.Abs(vertical));
        _animator.SetFloat(Speed, speedValue, 0.1f, Time.deltaTime); // Smooth Speed Transition
        _animator.SetFloat(Horizontal, horizontal); // For strafing or turning
    }
    
    
    private void HandleGroundCheck()
    {
        Debug.DrawRay(groundCheck.position, Vector3.down * groundDistance, Color.red);
        
        // Define the number of raycasts and their positions
        var rayCount = 5; // More rays for more coverage, but at a performance cost
        var radius = 0.5f; // Radius around the ground check point for additional rays

        // Check if the player is grounded
        _isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask);
    
        // Perform additional raycasts around the central point
        if (!_isGrounded)
        {
            for (var i = 0; i < rayCount; i++)
            {
                var angle = i * (360f / rayCount);
                var rayDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
                // Offset the raycast position slightly outward
                var offsetPosition = groundCheck.position + (rayDirection * radius);
                Debug.DrawRay(offsetPosition, Vector3.down * groundDistance, Color.blue); // Visual debugging

                if (Physics.Raycast(offsetPosition, Vector3.down, out _, groundDistance, groundMask))
                {
                    _isGrounded = true;
                    break; // If any ray hits the ground, we consider the player grounded
                }
            }
        }

        switch (_isGrounded)
        {
            // Transition from not grounded to grounded
            case true when !_wasGrounded:
            {
                if (_isFalling) // Only trigger "Land" if the player was falling
                {
                    _animator.SetTrigger(Land);
                    _isFalling = false; // Reset falling state
                }

                break;
            }
            // Transition from grounded to falling
            // Player starts falling
            case false when !_isFalling && _velocity.y < -10f:
                _isFalling = true;

                _animator.SetTrigger(Falling);
                break;
        }

        // Update the grounded state for the next frame
        _wasGrounded = _isGrounded;

        // Apply small downward force when grounded
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }
    
        // Allow jump when grounded
        if (_isGrounded)
        {
            _groundedTime += Time.deltaTime;
        }
        else
        {
            _groundedTime = 0f;
        }
    }



    
    private void HandleMovement()
    {
        var direction = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

        if (!(direction.magnitude >= 0.1f)) return;
        var targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _cam.eulerAngles.y;
        var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);

        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        // Adjust speed based on sprint
        var currentSpeed = moveSpeed * (_isSprinting ? sprintSpeedMultiplier : 1f);

        var moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        var deltaSpeed = currentSpeed * Time.deltaTime;
        _controller.Move(moveDir.normalized * deltaSpeed);
    }

    private void HandleGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
    
    
    private void HandleLedgeCollision(Vector3 position, Transform handTransform, float offsetX, float offsetY, float offsetZ, float ledgeRotationY)
    {
        // Save the hand position transform for snapping
        
        _ledgeHandPosition = !_isClimbing ? handTransform : null;


        // Set the per-ledge offsets
        _ledgeOffsetX = offsetX;
        _ledgeOffsetY = offsetY;
        _ledgeOffsetZ = offsetZ;

        // Store the target rotation for later use in StartHanging
        _targetLedgeRotationY = ledgeRotationY;
        Debug.Log($"Ledge Collision Detected at position {position}, HandTransform: {handTransform.name}, Rotation: {ledgeRotationY}");
    }

// Coroutine to smoothly rotate the player towards the ledge's Y rotation
    private IEnumerator SmoothRotateToLedge(float targetRotationY)
    {
        var currentRotationY = transform.eulerAngles.y;
        var angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentRotationY, targetRotationY));

        // Calculate duration dynamically based on the angle difference and speed
        var duration = angleDifference / hangTransitionSpeed; // hangTransitionSpeed now acts as rotation speed (degrees/second)
        var elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            var t = elapsedTime / duration;
            var newRotationY = Mathf.LerpAngle(currentRotationY, targetRotationY, t);
            transform.rotation = Quaternion.Euler(0f, newRotationY, 0f);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Final rotation to ensure precision
        transform.rotation = Quaternion.Euler(0f, targetRotationY, 0f);
    }


    // ReSharper disable Unity.PerformanceAnalysis
    private void HandleHangingInput()
    {
        if (!_isHanging || !_canClimb || _isClimbing || !(_moveInput.y > 0)) return;
        // Assuming positive Y is forward movement, like 'W'
        if (!_canClimb) return;
        _isClimbing = true;
        StartClimb();
    }

    private void StartClimb()
    {
        // Trigger climbing animation and disable movement
        if (!_canClimb) return;
        _animator.SetTrigger(Climb);
        _controller.enabled = false; // Disable movement during climbing
        _moveInput = Vector2.zero;
            
        if (ledgeFinderObject != null)
        {
            ledgeFinderObject.SetActive(false);
        }
        _isGrounded = false;
        _ledgeHandPosition = null;
            
        CompleteClimb();
    }

    private void CompleteClimb()
    {
        // Calculate the new position based on player orientation
        var upwardMovement = Vector3.up * climbUpDistance;
        var forwardMovement = transform.forward * climbForwardDistance;
    
        // First, apply the upward movement
        var intermediatePosition = transform.position + upwardMovement;
    
        // Then apply the forward movement after the upward movement
        var targetPosition = intermediatePosition + forwardMovement;
    
        // Move player to the new position
        StartCoroutine(SmoothMoveToPosition(targetPosition, 1f));

        Debug.Log("Climb complete. Player moved to: " + targetPosition);
    }
    
    private IEnumerator SmoothMoveToPosition(Vector3 targetPosition, float duration)
    {
        var startPosition = transform.position;
        var elapsedTime = 0f;
        var halfDuration = duration / 2f;

        // First, move upward
        var upwardTargetPosition = startPosition + Vector3.up * climbUpDistance;
        while (elapsedTime < halfDuration)
        {
            transform.position = Vector3.Lerp(startPosition, upwardTargetPosition, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // Ensure we reach the exact upward position before moving forward
        transform.position = upwardTargetPosition;

        // Reset elapsed time for the forward movement
        elapsedTime = 0f;
        var forwardTargetPosition = upwardTargetPosition + transform.forward * climbForwardDistance;

        // Now move forward
        while (elapsedTime < halfDuration)
        {
            transform.position = Vector3.Lerp(upwardTargetPosition, forwardTargetPosition, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // Ensure the final position is set accurately
        transform.position = forwardTargetPosition;

        StopHanging();
    
        if (ledgeFinderObject != null)
        {
            ledgeFinderObject.SetActive(true);
        }
        _isGrounded = true;
    }

    
    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
        if (_isHanging && !_isClimbing) // Check if the player is currently hanging
        {
            // Make the player jump off the ledge
            StopHanging();
            _animator.SetTrigger(Jump);
            _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            StartCoroutine(JumpToBlendTree());// Add jump force
        }
        else if (_ledgeHandPosition != null && _canRehang && !_isClimbing) // Check if re-hanging is allowed
        {
            StartHanging();
        }
        else if (_isGrounded && !_isClimbing) // Regular jump
        {
            _jumpRequested = true;
        }
    }

    private void StartHanging()
    {
        // Check if the player's rotation is within ±30 degrees of the ledge rotation
        var currentYRotation = transform.eulerAngles.y;
        var rotationDifference = Mathf.Abs(Mathf.DeltaAngle(currentYRotation, _targetLedgeRotationY));
        Debug.Log($"Start Hanging - Current Rotation: {currentYRotation}, Target Rotation: {_targetLedgeRotationY}, Difference: {rotationDifference}");
        
        if (rotationDifference > 30f)
        {
            Debug.Log("Player is not facing the ledge within ±30 degrees. Hang action canceled.");
            _jumpRequested = true;
            return; // Cancel hanging if the angle difference exceeds 30 degrees
            
        }

        StartCoroutine(ClimbDelay());
        _velocity = Vector3.zero; // Stop movement
        _controller.enabled = false; // Disable CharacterController
        _animator.SetTrigger(StartHang);
        _isHanging = true;
        Debug.Log($"Hanging Started. Adjusting to position {_ledgeHandPosition.position}, {_targetLedgeRotationY}");

        // Smoothly rotate towards the ledge when hanging starts
        StartCoroutine(SmoothRotateToLedge(_targetLedgeRotationY)); // Use the stored rotation value
        
    }

    private void StopHanging()
    {
        _isHanging = false;
        _controller.enabled = true; // Re-enable CharacterController
        _canClimb = false;
        
        
        Debug.Log("Hanging Stopped. Cooldown initiated.");
        // Temporarily disable hanging to prevent instant re-hang
        StartCoroutine(RehangCooldown());
    }
    
    private IEnumerator ClimbDelay()
    {
        yield return new WaitForSeconds(climbDelayTime); // Adjust delay as needed
        _canClimb = true;
    }
    // Cooldown coroutine for re-hanging
    private IEnumerator RehangCooldown()
    {
        _isClimbing = false;
        _canRehang = false;
        yield return new WaitForSeconds(1f); // Adjust delay as needed
        _canRehang = true;
        _animator.ResetTrigger("Climb");
        _canClimb = false;
        
    }
    

    private void HandleJump()
    {
        if (_jumpRequested) // Check if jump was requested
        {
            if (_isGrounded && _groundedTime >= jumpDelay) // Check jump conditions
            {
                // Trigger the Jump animation
                _animator.SetTrigger(Jump);

                // Start a coroutine to delay the actual jump force
                _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                StartCoroutine(JumpToBlendTree());
            }

            _jumpRequested = false; // Reset jump request immediately
        }
    }
    
    private IEnumerator JumpToBlendTree()
    {
        yield return new WaitForSeconds(1f);
        if (_isFalling) yield break;
        _animator.SetTrigger(ToBlendTree);
        yield return new WaitForSeconds(1f);
        _animator.ResetTrigger(nameof(ToBlendTree));

    }
    
    // Input System Methods
    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }
    
    public void OnSprint(InputValue value)
    {
        // Sprinting only happens when shift is pressed AND there's movement input
        _isSprinting = value.isPressed && (_moveInput.x != 0 || _moveInput.y != 0);
    }
    
}