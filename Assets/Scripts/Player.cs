using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum WallJumpMode
{
    Disabled,
    Regular,
    Climbing
}

public class Player : MonoBehaviour
{
    private static bool hasRunCheckpoint;
    private static Vector2 runCheckpoint;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    [Header("Horizontal Movement")]
    public float speed;
    public float acceleration;
    public float sprintMod;
    private float inputX, currentSprintMod;

    [Header("Vertical Movement")]
    public float jumpForce;
    public float gravity;
    private float modGravity;
    public int maxJumps;
    private int currentJumps;
    private bool grounded;
    public bool useInputBuffers;
    private float earlyInputBuffer, coyoteBuffer;

    [Header("Wall Jumping")]
    [Tooltip("Disabled: no wall actions. Regular: jump away from walls. Climbing: repeatedly jump up the same wall.")]
    public WallJumpMode wallJumpMode = WallJumpMode.Regular;
    public float wallJumpHorizontalForce;
    [Min(0)] public float climbingWallJumpHorizontalForce = 2.5f;
    public float wallJumpVerticalForce;
    public float slideGravity;
    private bool sliding, touchingLeftWall, touchingRightWall;
    public float minDisFromGroundForJump;
    private float disFromGround;
    [Min(0)] public float wallSlideSpeed = 3f;
    [Min(0)] public float wallCoyoteTime = 0.12f;
    [Min(0)] public float wallJumpControlLock = 0.15f;
    private int wallSide;
    private float wallCoyoteBuffer;
    private float horizontalControlLock;
    private float groundCheckDisableTimer;

    [Header("Misc (do not touch)")]
    public LayerMask ground;
    public Transform foot1, foot2, leftWallCheck, rightWallCheck;
    private Transform lastCheckpoint;
    public Sprite flagRaised, flagUnraised;
    public float deathRotateSpeed;
    public Collider2D[] collidersToDisable;
    private bool dead;
    public GameObject deathObject;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRunCheckpoint()
    {
        hasRunCheckpoint = false;
        runCheckpoint = Vector2.zero;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = gravity;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        currentSprintMod = 1;

        if (hasRunCheckpoint) transform.position = runCheckpoint;

        transform.GetChild(0).parent = null;
    }

    // Input is handled in Update() because it runs once every *user* frame
    private void Update()
    {
        // Horizontal input is handled in Update() function to ensure that input is checked every *user* frame
        // rather than every *engine* frame as in FixedUpdate()
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { inputX = 1; sr.flipX = false; }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { inputX = -1; sr.flipX = true; }
        else { inputX = 0; }

        bool jumpInput = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space);

        // Sprint input
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) { currentSprintMod = sprintMod; }
        else { currentSprintMod = 1; }

        bool wallJumped = false;

        // Wall jumps work from direct contact or shortly after leaving a wall. The
        // launch always goes away from the wall, regardless of the held direction.
        if (wallJumpMode != WallJumpMode.Disabled && jumpInput && !grounded && wallCoyoteBuffer > 0 && wallSide != 0)
        {
            bool climbingJump = wallJumpMode == WallJumpMode.Climbing;
            float horizontalForce = -wallSide * (climbingJump ? climbingWallJumpHorizontalForce : wallJumpHorizontalForce);
            rb.linearVelocity = new Vector2(horizontalForce, wallJumpVerticalForce);
            sliding = false;
            wallJumped = true;
            wallCoyoteBuffer = 0;
            earlyInputBuffer = 0;
            horizontalControlLock = climbingJump ? 0 : wallJumpControlLock;
            groundCheckDisableTimer = 0.08f;
            sr.flipX = climbingJump ? wallSide < 0 : wallSide > 0;
        }

        // Jump input
        if (!wallJumped && (grounded || currentJumps < maxJumps - 1 || coyoteBuffer > 0) && (jumpInput || earlyInputBuffer > 0))
        {
            bool usedCoyoteTime = !grounded && coyoteBuffer > 0;
            coyoteBuffer = 0;
            earlyInputBuffer = 0;

            if (!grounded && !usedCoyoteTime) { currentJumps++; }
            rb.linearVelocity = new(rb.linearVelocity.x, jumpForce);
            grounded = false;
            groundCheckDisableTimer = 0.08f;
        }

        // Handles early input; if the user tries to jump just before hitting the ground, this "stores" the input and lets them jump anyway
        if (jumpInput && !wallJumped && !grounded && currentJumps >= maxJumps - 1 && coyoteBuffer <= 0 && useInputBuffers)
        {
            earlyInputBuffer = 0.12f;
        }

        // Adjusts gravity based on if player is holding jump
        modGravity = gravity;
        if (!(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow)) && rb.linearVelocity.y > 0) { modGravity = gravity * 1.6f; }
    }

    // Everything else is handled in FixedUpdate() because it runs once every *engine* frame
    private void FixedUpdate()
    {
        // Decreases timers
        if (earlyInputBuffer > 0) earlyInputBuffer-= Time.fixedDeltaTime;
        if (coyoteBuffer > 0) coyoteBuffer -= Time.fixedDeltaTime;
        if (wallCoyoteBuffer > 0) wallCoyoteBuffer -= Time.fixedDeltaTime;
        if (horizontalControlLock > 0) horizontalControlLock -= Time.fixedDeltaTime;
        if (groundCheckDisableTimer > 0) groundCheckDisableTimer -= Time.fixedDeltaTime;

        // Handles horizontal input by directly modifying rb.velocity, moving the x velocity towards the target speed
        if (horizontalControlLock <= 0)
        {
            float goal = inputX * speed * currentSprintMod;
            rb.linearVelocity = new(Mathf.MoveTowards(rb.linearVelocity.x, goal, acceleration * Time.fixedDeltaTime), rb.linearVelocity.y);
        }

        // Checks if the player is currently touching the ground using an OverlapCircle
        grounded = groundCheckDisableTimer <= 0 &&
            (Physics2D.OverlapCircle(foot1.position, 0.1f, ground) || Physics2D.OverlapCircle(foot2.position, 0.1f, ground));
        if (grounded)
        {
            currentJumps = 0;
            wallCoyoteBuffer = 0;
            wallSide = 0;
            if (useInputBuffers) coyoteBuffer = 0.12f;
        }

        // Gets distance from ground for walljumping purposes (only start sliding a certain dis above ground)
        RaycastHit2D testGround = Physics2D.Raycast(transform.position, -transform.up, 10, ground);
        if (testGround.collider != null) { disFromGround = transform.position.y - testGround.point.y; }
        else { disFromGround = 10; }

        // Checks if the player is currently touching left or right walls using OverlapCircles
        touchingLeftWall = Physics2D.OverlapCircle(leftWallCheck.position, 0.1f, ground);
        touchingRightWall = Physics2D.OverlapCircle(rightWallCheck.position, 0.1f, ground);

        if (wallJumpMode != WallJumpMode.Disabled && !grounded && (touchingLeftWall || touchingRightWall))
        {
            // If both checks overlap (for example in a narrow gap), favor the side
            // the player is moving toward so the jump direction stays predictable.
            if (touchingLeftWall && touchingRightWall) wallSide = inputX < 0 ? -1 : 1;
            else wallSide = touchingLeftWall ? -1 : 1;
            wallCoyoteBuffer = wallCoyoteTime;
        }
        else if (wallJumpMode == WallJumpMode.Disabled)
        {
            wallCoyoteBuffer = 0;
            wallSide = 0;
        }

        // Sliding is automatic while falling against a wall. It no longer gates the
        // ability to wall jump, and the capped fall speed gives consistent feedback.
        sliding = wallJumpMode != WallJumpMode.Disabled && !grounded && rb.linearVelocity.y < 0 && disFromGround > 0.1f &&
            (touchingLeftWall || touchingRightWall);
        if (sliding)
        {
            rb.gravityScale = slideGravity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }
        else rb.gravityScale = modGravity;

        if (dead)
        {
            transform.Rotate(new(0, 0, deathRotateSpeed * Time.fixedDeltaTime));
            rb.linearVelocity = new(speed/2, rb.linearVelocity.y);
        }

        // Sets animator parameters
        anim.SetBool("Walking", inputX != 0);
        anim.SetBool("Grounded", grounded);
        anim.SetFloat("Speed", currentSprintMod);
        anim.SetBool("Sliding", sliding);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Colects coins
        if (collision.CompareTag("Coin"))
        {
            Destroy(collision.gameObject);
        }

        // Kills enemy
        if (collision.CompareTag("Enemy Hurtbox"))
        {
            collision.transform.GetComponentInParent<Enemy>().Die();

            // Bumps player up
            float force = jumpForce / 1.3f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Space)) force = jumpForce;
            rb.linearVelocity = new(rb.linearVelocity.x, force);
        }

        // Handles checkpoints
        if (collision.CompareTag("Flag"))
        {
            if (lastCheckpoint) { lastCheckpoint.GetComponent<SpriteRenderer>().sprite = flagUnraised; }
            collision.GetComponent<SpriteRenderer>().sprite = flagRaised;
            lastCheckpoint = collision.transform;
            runCheckpoint = lastCheckpoint.position;
            hasRunCheckpoint = true;
        }

        // Handles hitting spikes/enemies
        if (collision.CompareTag("Enemy Hitbox") && !dead)
        {
            dead = true;
            foreach (Collider2D c in collidersToDisable) { c.enabled = false; }
            rb.linearVelocity = new(rb.linearVelocity.x, jumpForce);
            transform.GetChild(0).parent = null;
            sr.sortingOrder = 6;
            deathObject.SetActive(true);
            Invoke("ResetScene", 1.5f);
        }

        if (collision.CompareTag("Final")) { Debug.Log("You win!"); }
    }

    private void ResetScene() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    private void OnApplicationQuit() { ResetRunCheckpoint(); }
}
