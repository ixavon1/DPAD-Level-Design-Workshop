using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cobblestone : MonoBehaviour
{
    public float life, fallSpeed;
    public Sprite cracked;
    private bool dead;

    private void FixedUpdate()
    {
        if (dead)
        {
            transform.Rotate(0, 0, 720 * Time.fixedDeltaTime);
            transform.position += new Vector3(0, -fallSpeed * Time.fixedDeltaTime, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GetComponent<SpriteRenderer>().sprite = cracked;
            Invoke("Die", life);
        }
    }

    private void Die()
    {
        dead = true;
        gameObject.layer = 10;
        Destroy(gameObject, 5);
    }
}
