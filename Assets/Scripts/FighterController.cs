using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FighterController : MonoBehaviour {
    Vector2 movementVector;
    Rigidbody2D rb;
    public float runSpeed = 9f;
    public float walkSpeed = 6f;
    Animator animator;

    //feels like there may end up being issues with how I handle flipping directions if it doesn't keep track of things properly
    private enum Direction { right, left };
    private Direction _spriteDirection = Direction.right;
    private Direction SpriteDirection {
        get {
            return _spriteDirection;
        }
        set {
            if (_spriteDirection != value) {
                //flip to the new direction
                gameObject.transform.localScale *= new Vector2(-1, 1);
            }
            _spriteDirection = value;
        }
    }

    [SerializeField] private bool _isMoving = false;
    private bool IsMoving {
        get {
            return _isMoving;
        }
        set {
            _isMoving = value;
            animator.SetBool(AnimationStrings.isMoving, _isMoving);
            if (_isMoving == false) {
                //make sure the corresponding bools are set to false
                animator.SetBool(AnimationStrings.isRunning, false);
                animator.SetBool(AnimationStrings.isWalking, false);
            }
        }
    }

    private enum GroundedMovementType { walking, running };
    private GroundedMovementType _groundedMovement = GroundedMovementType.walking;
    private GroundedMovementType GroundedMovement {
        get {
            return _groundedMovement;
        }
        set {
            _groundedMovement = value;
            if (_groundedMovement == GroundedMovementType.running) {
                animator.SetBool(AnimationStrings.isRunning, true);
                animator.SetBool(AnimationStrings.isWalking, false);
            } else {
                animator.SetBool(AnimationStrings.isRunning, false);
                animator.SetBool(AnimationStrings.isWalking, true);
            }
        }
    }

    [SerializeField] private bool _isGrounded = true;
    private bool IsGrounded {
        get {
            return _isGrounded;
        }
        set {
            _isGrounded = value;
            animator.SetBool(AnimationStrings.isGrounded, _isGrounded);
        }
    }

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void FixedUpdate() {
        MoveFighter();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnMove(InputAction.CallbackContext context) {
        movementVector = context.ReadValue<Vector2>();
    }

    private void MoveFighter() {
        //All movement of the character should be able to be handled by this function or child functions by checking if the character is grounded   
        if (IsGrounded) {
            IsMoving = movementVector.x != 0;
            if (IsMoving) {
                //Check control sticks vector.x to see if it should be walking or running. 
                if (Mathf.Abs(movementVector.x) < 0.9) {
                    GroundedMovement = GroundedMovementType.walking;
                    rb.velocity = new Vector2(movementVector.x * walkSpeed, 0);
                } else {
                    GroundedMovement = GroundedMovementType.running;
                    //make sure that there is only one running speed i.e. movementVector.x * groundedMoveSpeed
                    if (movementVector.x > 0) movementVector.x = 1;
                    else movementVector.x = -1;
                    rb.velocity = new Vector2(movementVector.x * runSpeed, 0); //the character is on the ground so velocity.y = 0
                }
                SetSpriteDirection(); 
            }
        } else {
            //TODO: Do air movement
        }
    }

    //Right now SetSpriteDirection should only be called when the Fighter is on the ground. 
    void SetSpriteDirection() {
        if (movementVector.x > 0 && SpriteDirection != Direction.right) {
            //flip it so it's facing right
            SpriteDirection = Direction.right;
        } else if (movementVector.x < 0 && SpriteDirection != Direction.left) {
            //flip so it's facing left
            SpriteDirection = Direction.left;
        }
    }
}
