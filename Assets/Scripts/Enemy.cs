using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    Rigidbody2D rb;
    SpriteRenderer sr;

    public float speed;
    [HideInInspector] public int dir;
    public Transform leftWallCheck, rightWallCheck, leftFootCheck, rightFootCheck, enemyProbe;
    public LayerMask ground, enemy;
    private Enemy riding;

    private bool dead;
    public float deathRotateSpeed;
    public Collider2D[] colliders;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        dir = -1;
    }

    private void FixedUpdate()
    {
        if (!dead)
        {
            // The serialized mask also contains the Enemy layer for enemy stacking.
            // Exclude this enemy's layer here so its probes cannot hit its own body.
            int terrainMask = ground.value & ~(1 << gameObject.layer);
            bool wallOnLeft = Physics2D.Raycast(leftWallCheck.position, Vector2.left, 0.2f, terrainMask);
            bool wallOnRight = Physics2D.Raycast(rightWallCheck.position, Vector2.right, 0.2f, terrainMask);
            bool groundOnLeft = Physics2D.Raycast(leftFootCheck.position, Vector2.down, 0.2f, terrainMask);
            bool groundOnRight = Physics2D.Raycast(rightFootCheck.position, Vector2.down, 0.2f, terrainMask);

            // Turn only when the sensor in the current travel direction finds a wall
            // or reaches a real ledge. Requiring the trailing foot to remain supported
            // prevents both sensors from fighting while the enemy is airborne.
            if (dir < 0 && (wallOnLeft || (!groundOnLeft && groundOnRight))) SetDirection(1);
            else if (dir > 0 && (wallOnRight || (!groundOnRight && groundOnLeft))) SetDirection(-1);

            // Allows enemy to ride other enemies
            if (Physics2D.OverlapCircle(enemyProbe.position, 0.1f, enemy)) { riding = Physics2D.OverlapCircle(enemyProbe.position, 0.1f, enemy).GetComponent<Enemy>(); }
            else { riding = null; }
            if (riding) { dir = riding.dir; sr.flipX = false; if (dir > 0) { sr.flipX = true; } }
        }
        else
        {
            transform.Rotate(new(0, 0, deathRotateSpeed * Time.fixedDeltaTime));
        }

        rb.linearVelocity = new(speed * dir, rb.linearVelocity.y);
    }

    private void SetDirection(int newDirection)
    {
        dir = newDirection;
        sr.flipX = dir > 0;
    }

    public void Die()
    {
        dead = true;
        rb.linearVelocity = new(rb.linearVelocity.x, 8);
        foreach (Collider2D collider2D in colliders) { collider2D.enabled = false; }
    }
}
