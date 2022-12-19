using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

public class MovementController : MonoBehaviour
{
    private AnimationManager ac;
    private const float RUN_INPUT = 0.9f;
    private const float ROLL_INPUT = 0.5f;
    private const float DODGE_INPUT = 0.7f;

    /*------------------------------------------------------------------------------------------------------------------------------------------------------------*/
    /* UNITY ENGINE GAME LOOP FUNCTIONS */
    //---------------------------------------------------------------------------------
    private void Awake() {
        groundCollider = GetComponent<CapsuleCollider2D>();
        fighter = GetComponent<Rigidbody2D>();
        ac = GetComponent<AnimationManager>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate() {
        if (IsGrounded()) {
            Debug.Log("Ground Movement");
            GroundedMovement();
        } else {
            Debug.Log("Air Movement");
            AirMovement();
        }
    }

    /*------------------------------------------------------------------------------------------------------------------------------------------------------------*/
    /* INPUT EVENT RESPONDERS */
    //---------------------------------------------------------------------------------
    public void OnMove(InputAction.CallbackContext context) {
        inputMovementVector = context.ReadValue<Vector2>();
    }

    //---------------------------------------------------------------------------------
    public void OnFullHop(InputAction.CallbackContext context) {
        Debug.Log("On Full Hop Entered");
        if (!isMovementLocked) {
            Debug.Log("On Full Hop is not movement locked");
            if (context.performed) {
                Debug.Log("On Full Hop Context Performed");
                isMovementLocked = true;
                IsJumpSquat = true;
                jumpType = Jump.fullHop;
            }
        } else Debug.Log("On Full Hop Is Movement Locked");
    }

    //---------------------------------------------------------------------------------
    public void OnShortHop(InputAction.CallbackContext context) {
        Debug.Log("On Short Hop Entered");
        if (!isMovementLocked) {
            Debug.Log("On Short Hop Is Not Movement Locked");
            if (context.performed) {
                Debug.Log("On Short Hop Context Performed");
                isMovementLocked = true;
                IsJumpSquat = true;
                jumpType = Jump.shortHop;
            }
        } else Debug.Log("On Short Hop is movement locked");
    }

    //---------------------------------------------------------------------------------
    public void OnDodge(InputAction.CallbackContext context) {
        if (!isMovementLocked) {
            if (context.performed) {
                dodgeInput = context.ReadValue<Vector2>();
                if (dodgeInput.x > 0) rollDirection = Direction.right;
                else rollDirection = Direction.left;
                isMovementLocked = true;
            }
        }
    }

    //---------------------------------------------------------------------------------
    public void OnShield() {

    }

    /*------------------------------------------------------------------------------------------------------------------------------------------------------------*/
    /* GENERAL MOVEMENT */
    //---------------------------------------------------------------------------------
    //Variables used in movement calcs
    Vector2 inputMovementVector;
    Vector2 dodgeInput;
    Rigidbody2D fighter;
    bool isMovementLocked = false;
    bool _isMoving = false;
    bool IsMoving {
        get {
            return _isMoving;
        }
        set {
            _isMoving = value;
            if (_isMoving == false) {
                isRunning = false;
            }
        }
    }

    //---------------------------------------------------------------------------------
    //Ground Checking
    private ContactFilter2D castFilter;
    private CapsuleCollider2D groundCollider;
    private bool IsGrounded() {
        RaycastHit2D[] groundHits = new RaycastHit2D[5];
        return groundCollider.Cast(Vector2.down, castFilter, groundHits, 0.05f) > 0;
    }

    //---------------------------------------------------------------------------------
    //Sprite Direction
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
    private Direction SetSpriteDirection() {
        if (inputMovementVector.x > 0 && SpriteDirection != Direction.right) {
            //flip it so it's facing right
            SpriteDirection = Direction.right;
        } else if (inputMovementVector.x < 0 && SpriteDirection != Direction.left) {
            //flip so it's facing left
            SpriteDirection = Direction.left;
        }
        return SpriteDirection;
    }

    //---------------------------------------------------------------------------------
    //unlock movement after animations end. Usually called as an animation event in the gui
    public void UnlockMovement() {
        isMovementLocked = false;
    }

    //---------------------------------------------------------------------------------
    //Stop movement. Useful for various things. Also used in animator gui
    private void StopMovement() {
        fighter.velocity = new Vector2(0, 0);
    }

    /*------------------------------------------------------------------------------------------------------------------------------------------------------------*/
    /* GROUNDED MOVEMENT */
    //---------------------------------------------------------------------------------
    //Grounded Movement Decider
    private void GroundedMovement() {
        fastFall = false;
        if (!isMovementLocked) {
            Debug.Log("Grounded Movement is not locked");
            IsMoving = inputMovementVector.x != 0;
            if (!IsMoving) {
                Debug.Log("Grounded Idle");
                GroundedIdle();
            } else {
                //reset our idle timer
                groundIdleTime = 0; 
                if (Mathf.Abs(inputMovementVector.x) < RUN_INPUT && !isRunning) {
                    Debug.Log("Grounded Walk");
                    Walk();
                } else {
                    if (((fighter.velocity.x > 0 && inputMovementVector.x < 0) || (fighter.velocity.x < 0 && inputMovementVector.x > 0)) && isRunning) {
                        //We're turning around
                        Debug.Log("Grounded Skid");
                        TurnAroundSkid();
                    } else Run(); Debug.Log("Grounded Run");
                }
            }
        } else {
            Debug.Log("Grounded Movement is locked");
            if (IsJumpSquat) {
                Debug.Log("Grounded Jump");
                JumpSquat();
            } else {
                Debug.Log("Grounded Dodge");
                if (Mathf.Abs(dodgeInput.x) > ROLL_INPUT || ac.IsAnimationPlaying(PikachuAnimationStates.ROLL)) {
                    Roll();
                } else if (Mathf.Abs(dodgeInput.y) > DODGE_INPUT || ac.IsAnimationPlaying(PikachuAnimationStates.SPOT_DODGE)) {
                    SpotDodge();
                } else {
                    //TODO: shield
                }
            }
        }
    }

    //---------------------------------------------------------------------------------
    //Grounded Idle
    private float groundIdleTime = 0;
    public float idleGroundFriction = 0.8f;
    private void GroundedIdle() {
        //for some reason we slide after moving unless we do this. 
        if (Mathf.Abs(fighter.velocity.x) > 0) {
            float newXValue = Mathf.MoveTowards(fighter.velocity.x, 0, idleGroundFriction);
            fighter.velocity = new Vector2(newXValue, 0);
        }

        //cycle through idle animations, playing idle wait animations randomly at certain intervals
        if (groundIdleTime < 3f) {
            if (!ac.IsAnimationPlaying(PikachuAnimationStates.IDLE_WAIT_1) && !ac.IsAnimationPlaying(PikachuAnimationStates.IDLE_WAIT_2)) {
                ac.ChangeAnimationState(PikachuAnimationStates.IDLE);
                groundIdleTime += Time.deltaTime;
            }
        } else {
            if (!ac.IsAnimationPlaying(PikachuAnimationStates.IDLE_WAIT_1) && !ac.IsAnimationPlaying(PikachuAnimationStates.IDLE_WAIT_2)) {
                int picker = Random.Range(1, 3);
                if (picker == 1) ac.ChangeAnimationState(PikachuAnimationStates.IDLE_WAIT_1);
                else ac.ChangeAnimationState(PikachuAnimationStates.IDLE_WAIT_2);
                groundIdleTime = 0f;
            }
        }
    }

    //---------------------------------------------------------------------------------
    //Walking
    public float maxWalkSpeed = 6f; //actual max walk speed is this value * 0.9
    private void Walk() {
        isRunning = false;
        SetSpriteDirection();
        //no need to do MAX_WALK_SPEED * -1 on Direction.left because inputMovementVector.x is already positive or negative
        fighter.velocity = new Vector2(inputMovementVector.x * maxWalkSpeed, 0);
        ac.ChangeAnimationState(PikachuAnimationStates.WALK);
    }

    //---------------------------------------------------------------------------------
    //Running
    private bool isRunning = false;
    public float runSpeed = 9f;
    private void Run() {
        isRunning = true;
        //need to do RUN_SPEED * -1 on Direction.left because we're not using inputMovementVector (RUN_SPEED isn't modifiable)
        if (SetSpriteDirection() == Direction.right) fighter.velocity = new Vector2(runSpeed, 0);
        else fighter.velocity = new Vector2(runSpeed * -1, 0);
        ac.ChangeAnimationState(PikachuAnimationStates.RUN);
    }

    //---------------------------------------------------------------------------------
    //Turn Around Skid
    //This only happens if you turn around to try and keep running the opposite direction
    public float skidSpeed = 0.2f;
    private void TurnAroundSkid() {
        //lower speed
        float newXValue = Mathf.MoveTowards(fighter.velocity.x, 0, skidSpeed);
        fighter.velocity = new Vector2(newXValue, 0);
        ac.ChangeAnimationState(PikachuAnimationStates.SKID);
    }

    //---------------------------------------------------------------------------------
    //Roll
    public EdgeDetection frontEdgeDetector;
    public EdgeDetection rearEdgeDetector;
    private void Roll() {
        if (frontEdgeDetector.colliders.Count == 0 || rearEdgeDetector.colliders.Count == 0) {
            //we're at an edge
            StopMovement();
        }
        ac.ChangeAnimationState(PikachuAnimationStates.ROLL);
    }

    //This function is called as an event in the animator gui
    public float rollSpeed = 8f;
    private Direction rollDirection = Direction.right;
    private void RollStartMovement() {
        if (rollDirection == Direction.right) fighter.velocity = new Vector2(rollSpeed, 0);
        else fighter.velocity = new Vector2(-rollSpeed, 0);
    }

    private void GroundedDodgeStopMovement() {
        StopMovement();
        dodgeInput = new Vector2(0, 0);
    }

    //---------------------------------------------------------------------------------
    //Spot Dodge
    private void SpotDodge() {
        ac.ChangeAnimationState(PikachuAnimationStates.SPOT_DODGE);
        dodgeInput = new Vector2(0, 0);
    }

    /*------------------------------------------------------------------------------------------------------------------------------------------------------------*/
    /* HYBRID MOVEMENT */
    //---------------------------------------------------------------------------------
    //Jumping
    public float jumpImpulse = FULL_HOP_IMPULSE;
    public float currAirJumps = 1;
    private bool _isJumpSquat = false;
    private bool IsJumpSquat {
        get {
            return _isJumpSquat;
        } set {
            _isJumpSquat = value;
            if (_isJumpSquat) fastFall = false;
        }
    }
    private enum Jump { fullHop, shortHop, airHop }
    private Jump jumpType = Jump.fullHop;
    private const float FULL_HOP_IMPULSE = 13f;
    private const float SHORT_HOP_IMPULSE = 9.5f;
    private const float AIR_HOP_IMPULSE = 13f;
    private const int MAX_AIR_HOPS = 1;

    private void JumpSquat() {
        if (!IsGrounded()) {
            if (currAirJumps > 0) {
                ac.ChangeAnimationState(PikachuAnimationStates.JUMP_SQUAT);
                --currAirJumps;
                Debug.Log("current air jumps: " + currAirJumps);
            }
        } else {
            ac.ChangeAnimationState(PikachuAnimationStates.JUMP_SQUAT);
            currAirJumps = MAX_AIR_HOPS;
            Debug.Log("current air jumps: " + currAirJumps);
        }
        
    }

    private void JumpImpulse() {
        Debug.Log("Jump Impulse entered");
        if (jumpType == Jump.fullHop) jumpImpulse = FULL_HOP_IMPULSE;
        else if (jumpType == Jump.shortHop) jumpImpulse = SHORT_HOP_IMPULSE;
        else jumpImpulse = AIR_HOP_IMPULSE;

        fighter.velocity = new Vector2(fighter.velocity.x, jumpImpulse);
        IsJumpSquat = false;
        //UnlockMovement(); //Will need to move this to airmovement rising, as it being here allows for the last frame of jump to cancel into run
    }

    /*------------------------------------------------------------------------------------------------------------------------------------------------------------*/
    /* AIR MOVEMENT */
    //---------------------------------------------------------------------------------
    public float maxAirDrift = 12f;
    public float airAcceleration = 0.5f;
    public float maxSlowFallSpeed = -7f;
    public float maxFastFallSpeed = -15f;
    public float gravityModifier = 1;
    private bool fastFall = false;
    
    private void AirMovement() {
        UnlockMovement(); //This is only ground movement. Default is unlocked. 
        //handle horizontal movement (this is the same for rising or falling)
        Drift();
        //handle vertical movement. realistically this is always falling unless there's been a jump
        if (IsJumpSquat) {
            JumpSquat();
        } else {
            if (fighter.velocity.y > 0) {
                Rising();
            } else {
                Falling();
            }
        }
        
        //handle jumps

        //handle air dodges
    }
    
    private void Drift() {
        float horizontalDriftVelocity = fighter.velocity.x;
        if (inputMovementVector.x > 0) {
            horizontalDriftVelocity = (fighter.velocity.x + airAcceleration) < maxAirDrift ? fighter.velocity.x + airAcceleration : maxAirDrift;
        } else if (inputMovementVector.x < 0) {
            horizontalDriftVelocity = (fighter.velocity.x - airAcceleration) > maxAirDrift * -1 ? fighter.velocity.x - airAcceleration : maxAirDrift * -1;
        }

        fighter.velocity = new Vector2(horizontalDriftVelocity, fighter.velocity.y);
    }
    private void Rising() {

    }

    private const float FAST_FALL_INPUT = -0.8f;
    private void Falling() {
        if (inputMovementVector.y < FAST_FALL_INPUT) {
            fastFall = true;
        }

        float fallingVelocity = 0;
        if (fastFall) {
            fallingVelocity = maxFastFallSpeed;
        } else {
            fallingVelocity = maxSlowFallSpeed > fighter.velocity.y ? maxSlowFallSpeed : fighter.velocity.y;
        }

        fighter.velocity = new Vector2(fighter.velocity.x, fallingVelocity);
    }

    
}
