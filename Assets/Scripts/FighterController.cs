using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class FighterController : MonoBehaviour {
    Vector2 inputMovementVector;
    Rigidbody2D rb;
    public ContactFilter2D castFilter;
    private const float RUN_SPEED = 9f;
    private const float WALK_SPEED = 6f;
    private const float MAX_AIR_SPEED = 5f;
    private const float AIR_ACCELERATION = 0.5f;
    private const float MAX_FALL_SPEED = -7f;
    private const float FAST_FALL_SPEED = -15f;
    private const int MAX_GROUNDED_JUMPS = 2;
    private const int MAX_AIR_JUMPS = 1;
    private const float FULL_HOP_POWER = 14f;
    private const float SHORT_HOP_POWER = 8f;
    private int numJumps = 2;
    private float jumpPower;
    private bool fastFall = false;
    private float jumpRiseTime = 0f;
    private const float JUMP_RISE_TIME_NO_ADDED_GRAV = 0.2f;
    private const float SHORT_HOP_RISE_TIME_NO_ADDED_GRAV = 0.08f;
    private const float GRAVITY_INCREASE = 1f;

    //animation states
    Animator animator;
    private string currentState;
    private const string IDLE = "pikachu_idle";
    private const string IDLE_WAIT_1 = "pikachu_idle_wait1";
    private const string IDLE_WAIT_2 = "pikachu_idle_wait2";
    private const string WALK = "pikachu_walk";
    private const string RUN = "pikachu_run";
    private const string FALL = "pikachu_fall";
    private const string JUMP_SQUAT = "pikachu_jumpSquat";
    private const string RISE = "pikachu_rise";


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
    [SerializeField] private bool isMoving = false;
    [SerializeField] private bool isGrounded = true;
    private bool isJump = false;
    private float idleTime = 0f;
    private CapsuleCollider2D baseCollider;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        baseCollider = GetComponent<CapsuleCollider2D>();
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
        inputMovementVector = context.ReadValue<Vector2>();
    }

    public void OnFullHop(InputAction.CallbackContext context) {
        if (context.performed) {
            if (numJumps > 0) {
                isJump = true;
                fastFall = false;
                numJumps--;
                jumpPower = FULL_HOP_POWER;
                ChangeAnimationState(JUMP_SQUAT);
            }
        }
    }

    public void OnShortHop(InputAction.CallbackContext context) {
        if (context.performed) {
            if (numJumps > 0) {
                isJump = true;
                fastFall = false;
                numJumps--;
                jumpPower = SHORT_HOP_POWER;
                ChangeAnimationState(JUMP_SQUAT);
            }
        }
    }

    private void MoveFighter() {  
        if (IsGrounded() && !isJump) {
            GroundedMovement();
        } else {
            //TODO: finish air movement
            AirMovement();
        }
    }

    private void GroundedMovement() {
        //TODO: Fix sliding
        numJumps = MAX_GROUNDED_JUMPS; //reset number of jumps
        fastFall = false; //make sure next time we go airborne we don't fast fall
        isMoving = inputMovementVector.x != 0;
        jumpRiseTime = 0f;
        if (isMoving) {
            idleTime = 0f;
            if (Mathf.Abs(inputMovementVector.x) < 0.9) {
                //fighter is walking
                rb.velocity = new Vector2(inputMovementVector.x * WALK_SPEED, 0);
                ChangeAnimationState(WALK);
            } else {
                //fighter is running
                //make sure that there is only one running speed i.e. inputMovementVector.x * groundedMoveSpeed
                if (inputMovementVector.x > 0) inputMovementVector.x = 1;
                else inputMovementVector.x = -1;
                rb.velocity = new Vector2(inputMovementVector.x * RUN_SPEED, 0); //the character is on the ground so velocity.y = 0
                ChangeAnimationState(RUN);
            }
            SetSpriteDirection();
        } else {
            if (idleTime < 3f) {
                if (!IsAnimationPlaying(animator, IDLE_WAIT_1) && !IsAnimationPlaying(animator, IDLE_WAIT_2)) {
                    ChangeAnimationState(IDLE);
                    idleTime += Time.deltaTime;
                }
            } else {
                if (!IsAnimationPlaying(animator, IDLE_WAIT_1) && !IsAnimationPlaying(animator, IDLE_WAIT_2)) {
                    int picker = Random.Range(1, 3);
                    if (picker == 1) ChangeAnimationState(IDLE_WAIT_1);
                    else ChangeAnimationState(IDLE_WAIT_2);
                    idleTime = 0f;
                }
            }
        }
    }

    private void AirMovement() { 
        isMoving = true;
        if (IsAnimationPlaying(animator, JUMP_SQUAT)) return; //if we're still in the jump squat then we don't do anything. This means we will freeze for 3 frames on jump in mid air
        if (isJump || rb.velocity.y > 0) AirMovementRising();
        else AirMovementFalling();
    }

    private void AirMovementRising() {
        if (isJump) { //if we get this far and it's a jump then we've finished the jumpsquat and it's time to rise. 
            isJump = false;
            jumpRiseTime = 0f;
            rb.velocity = new Vector2(inputMovementVector.x, jumpPower);
            ChangeAnimationState(RISE);
        } else {
            //rising
            //TODO: handle front flip as rising approaches 0. 
            //Gradually increase gravity after a certain time on the fullhop or shorthop to make it more snappy on the rise up.
            float yVelocityModifier = 0f;
            jumpRiseTime += Time.deltaTime;
            if (jumpPower == FULL_HOP_POWER) {
                if (jumpRiseTime >= JUMP_RISE_TIME_NO_ADDED_GRAV) {
                    yVelocityModifier += GRAVITY_INCREASE;
                }
            } else if (jumpPower == SHORT_HOP_POWER) {
                if (jumpRiseTime >= SHORT_HOP_RISE_TIME_NO_ADDED_GRAV) {
                    yVelocityModifier += GRAVITY_INCREASE;
                }
            }
            AirMovementVector(yVelocityModifier);
        }
    }

    private void AirMovementFalling() {
        jumpRiseTime = 0f;
        ChangeAnimationState(FALL);
        if (inputMovementVector.y < -0.8) fastFall = true;
        AirMovementVector();
    }

    private void AirMovementVector(float yVelocityRiseModifier = 0) {
        float newXVelocity = rb.velocity.x;
        float newYVelocity;

        //side to side acceleration in the air
        if (inputMovementVector.x > 0) {
            newXVelocity = (rb.velocity.x + AIR_ACCELERATION) < MAX_AIR_SPEED ? rb.velocity.x + AIR_ACCELERATION : MAX_AIR_SPEED;
        } else if (inputMovementVector.x < 0) {
            newXVelocity = (rb.velocity.x - AIR_ACCELERATION) > MAX_AIR_SPEED * -1 ? rb.velocity.x - AIR_ACCELERATION : MAX_AIR_SPEED * -1;
        }
        //fallspeed calculations
        if (fastFall) newYVelocity = FAST_FALL_SPEED;
        else if (yVelocityRiseModifier == 0) newYVelocity = MAX_FALL_SPEED > rb.velocity.y ? MAX_FALL_SPEED : rb.velocity.y;
        else newYVelocity = (rb.velocity.y - yVelocityRiseModifier) >= 0 ? rb.velocity.y - yVelocityRiseModifier : 0;

        rb.velocity = new Vector2(newXVelocity, newYVelocity);
    }

    //start playing a new animation
    private void ChangeAnimationState(string newState) {
        if (newState == currentState) return;

        animator.Play(newState);
        currentState = newState;
    }

    //Check if a specific animation is playing
    //parameter named "0" is the animation layer (should always be 0 I think for me right now) 
    bool IsAnimationPlaying(Animator animator, string stateName) {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName) && 
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f) { //this line is checking how far along the animation is between 0 and 1
            return true;
        } else {
            return false;
        }
    }

    bool IsGrounded() {
        RaycastHit2D[] groundHits = new RaycastHit2D[5];
        isGrounded = baseCollider.Cast(Vector2.down, castFilter, groundHits, 0.05f) > 0;
        return isGrounded;
    }

    //Right now SetSpriteDirection should only be called when the Fighter is on the ground. 
    void SetSpriteDirection() {
        if (inputMovementVector.x > 0 && SpriteDirection != Direction.right) {
            //flip it so it's facing right
            SpriteDirection = Direction.right;
        } else if (inputMovementVector.x < 0 && SpriteDirection != Direction.left) {
            //flip so it's facing left
            SpriteDirection = Direction.left;
        }
    }
}
