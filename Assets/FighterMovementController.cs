using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.InputSystem;

public class FighterMovementController : MonoBehaviour {
    #region GroundedMovementModifiers
    public const float IDLE_GROUND_FRICTION = 0.8f;
    public const float MAX_WALK_SPEED = 6f; //actual max walk speed is this value * 0.9 bc of how I am handling input vectors
    public const float RUN_SPEED = 9f;
    public const float SKID_SPEED = 0.2f;
    #endregion

    #region JumpModifiers
    public const int NUM_AIR_JUMPS = 1;
    public const float SHORT_HOP_IMPULSE = 9.5f;
    public const float FULL_HOP_IMPULSE = 13f;
    #endregion

    #region AirMovementStateModifiers

    #endregion

    #region InputTypeStrings
    private const string MOVE = "move";
    private const string JUMP = "jump";
    #endregion

    #region InputVectorParameters
    private const float RUN_INPUT = 0.9f;
    private const float TURN_AROUND_INPUT = 0.7f;
    #endregion

    #region AnimationStates
    private enum AnimationStates {
        gIdle,
        walk, 
        run,
        skid, 
        spotDodge, 
        roll, 
        shield,
        jumpSquat,
        rise,
        fall,
        airDodge,
        nAirDodge
    }
    AnimationStates state = AnimationStates.gIdle;
    #endregion

    private AnimationManager ac;
    private CapsuleCollider2D groundCollider;
    private Rigidbody2D rb;
    private InputObject input;

    
    #region UnityEngineLoop

    void Awake() {
        groundCollider = GetComponent<CapsuleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        ac = GetComponent<AnimationManager>();
        input = GetComponent<InputObject>();
    }
    void Start()
    {
     
    }

    void Update()
    {
        
    }

    void FixedUpdate() {
        if (IsGrounded()) {
            GroundedStateDecider();
        } else {
            AirStateDecider();
        }
    }
    #endregion

    #region GroundedStates
    //This function decides which grounded movement function to enter based on which state is playing.  
    private void GroundedStateDecider() {
        currAirJumps = NUM_AIR_JUMPS; //find a better place for this if possible. 
        if (state == AnimationStates.gIdle) {
            GroundedIdle();
        } else if (state == AnimationStates.walk) {
            Walk();
        } else if (state == AnimationStates.run) {
            Run();
        } else if (state == AnimationStates.skid) {
            Skid();
        } else if (state == AnimationStates.spotDodge) {
            //Do Spot Dodge Animation things
        } else if (state == AnimationStates.roll) {
            //Do Roll Animation things
        } else if (state == AnimationStates.jumpSquat) {
            JumpSquat();
        } else {
            if (state != AnimationStates.rise) GroundedIdle();
        }
    }

    #region GroundedIdleStateAndTransition
    private float groundIdleTime = 0;
    private void GroundedIdle() {
        //for some reason we slide after moving unless we do this. 
        if (Mathf.Abs(rb.velocity.x) > 0) {
            float newXValue = Mathf.MoveTowards(rb.velocity.x, 0, IDLE_GROUND_FRICTION);
            rb.velocity = new Vector2(newXValue, 0);
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

        GroundedIdleTransistion();
    }

    private void GroundedIdleTransistion() {
        switch (input.Type) {
            case MOVE:
                //either walk, run, turn around, or remain idle
                if (input.Vector.x == 0) {
                    //remain idle
                    return;
                } else if (Mathf.Abs(input.Vector.x) > RUN_INPUT) {
                    //transition to run state
                    ac.ChangeAnimationState(PikachuAnimationStates.RUN);
                    state = AnimationStates.run;
                    break;
                } else if (Mathf.Abs(input.Vector.y) > TURN_AROUND_INPUT) {
                    //flip directions if necessary
                    SetSpriteDirection();
                    break;
                } else {
                    //transition to walk state
                    ac.ChangeAnimationState(PikachuAnimationStates.WALK);
                    state = AnimationStates.walk;
                    break;
                }
            default:
                break;
        }
        groundIdleTime = 0; //Make sure Idle Wait animations happen at the right frequency
    }
    #endregion

    #region WalkStateAndTransition
    private void Walk() {
        if (!WalkTransition()) {
            SetSpriteDirection();
            //no need to do MAX_WALK_SPEED * -1 on Direction.left because inputMovementVector.x is already positive or negative
            rb.velocity = new Vector2(input.Vector.x * MAX_WALK_SPEED, 0);
            ac.ChangeAnimationState(PikachuAnimationStates.WALK);
        }
    }

    private bool WalkTransition() {
        switch(input.Type) {
            case MOVE:
                if (input.Vector.x == 0) {
                    ac.ChangeAnimationState(PikachuAnimationStates.IDLE);
                    state = AnimationStates.gIdle;
                    return true;
                } else if (Mathf.Abs(input.Vector.x) > RUN_INPUT) {
                    ac.ChangeAnimationState(PikachuAnimationStates.RUN);
                    state = AnimationStates.run;
                    return true;
                } else return false;
            default:
                return false;
        }
    }

    #endregion

    #region RunStateAndTransition
    private void Run() {
        if (!RunTransition()) {
            //need to do RUN_SPEED * -1 on Direction.left because we're not using inputMovementVector (RUN_SPEED isn't modifiable)
            if (SetSpriteDirection() == Direction.right) rb.velocity = new Vector2(RUN_SPEED, 0);
            else rb.velocity = new Vector2(RUN_SPEED * -1, 0);
            ac.ChangeAnimationState(PikachuAnimationStates.RUN);
        }
    }

    private bool RunTransition() {
        switch (input.Type) {
            case MOVE:
                if (input.Vector.x == 0) {
                    ac.ChangeAnimationState(PikachuAnimationStates.IDLE);
                    state = AnimationStates.gIdle;
                    return true;
                } else if ((input.Vector.x > 0 && rb.velocity.x < -1 * MAX_WALK_SPEED) || (input.Vector.x < 0 && rb.velocity.x > MAX_WALK_SPEED)) {
                    ac.ChangeAnimationState(PikachuAnimationStates.SKID);
                    state = AnimationStates.skid;
                    return true;
                } else return false;
            default:
                return false;
        }
    }
    #endregion

    #region SkidStateAndTransition
    //This state can only be accessed by holding the stick in the opposite direction of movement while in the run state
    private void Skid() {
        if (!SkidTransition()) {
            //lower speed
            float newXValue = Mathf.MoveTowards(rb.velocity.x, 0, SKID_SPEED);
            rb.velocity = new Vector2(newXValue, 0);
            SetSpriteDirection();
            ac.ChangeAnimationState(PikachuAnimationStates.SKID);
        }
    }

    private bool SkidTransition() {
        switch (input.Type) {
            case MOVE:
                if (input.Vector.x == 0) {
                    ac.ChangeAnimationState(PikachuAnimationStates.IDLE);
                    state = AnimationStates.gIdle;
                    return true;
                } else if ((input.Vector.x > 0 && rb.velocity.x < 0) || (input.Vector.x < 0 && rb.velocity.x > 0)) {
                    return false;
                } else {
                    ac.ChangeAnimationState(PikachuAnimationStates.RUN);
                    state = AnimationStates.run;
                    return true;
                }
            default:
                return true;
        }
    }
    #endregion

    #endregion

    #region JumpStates
    private int currAirJumps = NUM_AIR_JUMPS; //for some reason this can't be initialized with numAirJumps
    private void JumpSquat() {
        if (!IsGrounded() && currAirJumps < 1) return;
        else if (!IsGrounded() && currAirJumps > 0) currAirJumps--;

        ac.ChangeAnimationState(PikachuAnimationStates.JUMP_SQUAT);
    }

    private float jumpPower = FULL_HOP_IMPULSE;
    private void JumpImpulse() {
        //I'm going to want to turn off gravity here as well once I get air movement working in general. 
        //Then over time gradually reintroduce gravity as the fighter rises to simulate a very rapid deceleration
        if (!IsGrounded()) {
            //always full hop in the air
            jumpPower = FULL_HOP_IMPULSE;
        }
        rb.velocity = new Vector2(input.Vector.x, jumpPower);
        state = AnimationStates.rise;
    }


    #endregion

    #region AirStates
    private void AirStateDecider() {
        if (state == AnimationStates.rise) {
            Rise(); 
        } else if (state == AnimationStates.fall) {
            Fall();
        } else if (state == AnimationStates.jumpSquat) {
            JumpSquat();
        } else if (state == AnimationStates.airDodge) {
            //Do Air Dodge Animation Things
        } else {
            ac.ChangeAnimationState(PikachuAnimationStates.FALL);
        }
    }

    #region RiseStateAndTransition
    private void Rise() {
        if(!RiseTransition()) {
            ac.ChangeAnimationState(PikachuAnimationStates.RISE);
        }
    }

    private bool RiseTransition() {
        if (rb.velocity.y > 0) return false;
        else {
            state = AnimationStates.fall;
            return true;
        }
    }
    #endregion

    #region FallStatesAndTransition
    private void Fall() {
        if (!FallTransition()) {
            ac.ChangeAnimationState(PikachuAnimationStates.FALL);
        }
    }

    private bool FallTransition() {
        if (input.Type == JUMP) return true;
        else if (rb.velocity.y > 0) return true;
        else return false;
    }

    #endregion

    #endregion

    #region MiscFunctions
    //Ground Checker
    private ContactFilter2D castFilter;
    private bool IsGrounded() {
        RaycastHit2D[] groundHits = new RaycastHit2D[5];
        return groundCollider.Cast(Vector2.down, castFilter, groundHits, 0.05f) > 0;
    }

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
        if (input.Vector.x > 0 && SpriteDirection != Direction.right) {
            //flip it so it's facing right
            SpriteDirection = Direction.right;
        } else if (input.Vector.x < 0 && SpriteDirection != Direction.left) {
            //flip so it's facing left
            SpriteDirection = Direction.left;
        }
        return SpriteDirection;
    }
    #endregion

    #region Inputs
    public void OnMove(InputAction.CallbackContext context) {
        input.Type = MOVE;
        input.Vector = context.ReadValue<Vector2>();
    }

    public void OnFullHop(InputAction.CallbackContext context) {
        if (context.performed) {
            input.Type = JUMP;
            state = AnimationStates.jumpSquat;
            jumpPower = FULL_HOP_IMPULSE;
        }
    }

    public void OnShortHop(InputAction.CallbackContext context) {
        if (context.performed) {
            input.Type = JUMP;
            state = AnimationStates.jumpSquat;
            jumpPower = SHORT_HOP_IMPULSE;
        }
    }
    #endregion
}
