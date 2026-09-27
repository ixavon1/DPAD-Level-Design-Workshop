using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
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
    public float wallJumpHorizontalForce;
    public float wallJumpVerticalForce;
    public float slideGravity;
    private bool sliding, touchingLeftWall, touchingRightWall;
    public float minDisFromGroundForJump;
    private float disFromGround;
    private float slideDir;
    private float slideExitBuffer;

    [Header("Misc (do not touch)")]
    public LayerMask ground;
    public Transform foot1, foot2, leftWallCheck, rightWallCheck;
    private Transform lastCheckpoint;
    public Sprite flagRaised, flagUnraised;
    public float deathRotateSpeed;
    public Collider2D[] collidersToDisable;
    private bool dead;
    public GameObject deathObject;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = gravity;

        currentSprintMod = 1;

        Vector2 checkpoint = new(PlayerPrefs.GetFloat("CheckpointX"), PlayerPrefs.GetFloat("CheckpointY"));
        if (checkpoint != Vector2.zero) { transform.position = checkpoint; }

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

        // Wall jump input
        if (jumpInput && (sliding || slideExitBuffer > 0))
        {
            rb.linearVelocity = new(wallJumpHorizontalForce * -slideDir, wallJumpVerticalForce);
            sliding = false;
            sr.flipX = !sr.flipX;
        }

        // Jump input
        if ((grounded || currentJumps < maxJumps - 1 || coyoteBuffer > 0) && (jumpInput || earlyInputBuffer > 0) && !sliding)
        {
            coyoteBuffer = 0;
            earlyInputBuffer = 0;

            if (!grounded && coyoteBuffer <= 0) { currentJumps++; }
            rb.linearVelocity = new(rb.linearVelocity.x, jumpForce);
            transform.Translate(new(0, 0.3f, 0));
            grounded = false;
        }

        // Handles early input; if the user tries to jump just before hitting the ground, this "stores" the input and lets them jump anyway
        if (jumpInput && !grounded && currentJumps >= maxJumps - 1 && coyoteBuffer <= 0 && useInputBuffers && !sliding)
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
        if (slideExitBuffer > 0) slideExitBuffer -= Time.fixedDeltaTime;

        // Handles horizontal input by directly modifying rb.velocity, moving the x velocity towards the target speed
        float goal = inputX * speed * currentSprintMod;
        rb.linearVelocity = new(Mathf.MoveTowards(rb.linearVelocity.x, goal, acceleration * Time.fixedDeltaTime), rb.linearVelocity.y);

        // Checks if the player is currently touching the ground using an OverlapCircle
        grounded = Physics2D.OverlapCircle(foot1.position, 0.1f, ground) || Physics2D.OverlapCircle(foot2.position, 0.1f, ground);
        if (grounded) 
        {
            currentJumps = 0; 
            if (useInputBuffers) coyoteBuffer = 0.12f;
        }

        // Gets distance from ground for walljumping purposes (only start sliding a certain dis above ground)
        RaycastHit2D testGround = Physics2D.Raycast(transform.position, -transform.up, 10, ground);
        if (testGround.collider != null) { disFromGround = transform.position.y - testGround.point.y; }
        else { disFromGround = 10; }

        // Checks if the player is currently touching left or right walls using OverlapCircles
        touchingLeftWall = Physics2D.OverlapCircle(leftWallCheck.position, 0.1f, ground);
        touchingRightWall = Physics2D.OverlapCircle(rightWallCheck.position, 0.1f, ground);

        // Decides if the player should be sliding or not
        if (((inputX < 0  && touchingLeftWall) || (inputX > 0 && touchingRightWall) || (inputX == 0 && sliding)) && rb.linearVelocity.y < 0 && !grounded && (sliding || disFromGround > minDisFromGroundForJump) && (touchingLeftWall || touchingRightWall)) { if (!sliding) StartSliding(); }
        else sliding = false;
        if (Mathf.RoundToInt(slideDir) == Mathf.RoundToInt(-inputX)) sliding = false;
        // Changes rb gravity based on sliding
        if (sliding) { rb.gravityScale = slideGravity; if (useInputBuffers) { slideExitBuffer = 0.12f; } }
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

    private void StartSliding()
    {
        sliding = true;
        rb.linearVelocity = Vector2.zero;
        slideDir = inputX;
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
            PlayerPrefs.SetFloat("CheckpointX", lastCheckpoint.position.x);
            PlayerPrefs.SetFloat("CheckpointY", lastCheckpoint.position.y);
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
}
