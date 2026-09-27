using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Probe : MonoBehaviour
{
    public LayerMask probe, ground;
    private void Start()
    {
        if (Physics2D.OverlapCircleAll(transform.position, 0.05f, probe).Count() > 1 && Physics2D.OverlapCircle(transform.position, 0.05f, ground)) { GetComponent<SpriteRenderer>().enabled = true; GetComponent<BoxCollider2D>().enabled = false; }
        else { gameObject.SetActive(false); }
    }
}
