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
            if (Physics2D.OverlapCircleAll(leftWallCheck.position, 0.1f, ground).Length > 1) { dir = 1; sr.flipX = true; }
            else if (Physics2D.OverlapCircleAll(rightWallCheck.position, 0.1f, ground).Length > 1) { dir = -1; sr.flipX = false; }
            if (Physics2D.OverlapCircleAll(leftFootCheck.position, 0.1f, ground).Length <= 1) { dir = 1; sr.flipX = true; }
            else if (Physics2D.OverlapCircleAll(rightFootCheck.position, 0.1f, ground).Length <= 1) { dir = -1; sr.flipX = false; }

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

    public void Die()
    {
        dead = true;
        rb.linearVelocity = new(rb.linearVelocity.x, 8);
        foreach (Collider2D collider2D in colliders) { collider2D.enabled = false; }
    }
}
