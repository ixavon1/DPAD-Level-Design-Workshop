using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spike : MonoBehaviour
{
    public Transform checkUp, checkRight, checkDown, checkLeft;
    public LayerMask ground;
    public Sprite full;

    private void Start()
    {
        if (Physics2D.OverlapCircle(checkDown.position, 0.1f, ground)) { return; }
        else if (Physics2D.OverlapCircle(checkRight.position, 0.1f, ground)) { transform.eulerAngles = new(0, 0, 90); return; }
        else if (Physics2D.OverlapCircle(checkUp.position, 0.1f, ground)) { transform.eulerAngles = new(0, 0, 180); return; }
        else if (Physics2D.OverlapCircle(checkLeft.position, 0.1f, ground)) { transform.eulerAngles = new(0, 0, 270); return; }
        GetComponent<SpriteRenderer>().sprite = full;
        GetComponent<BoxCollider2D>().offset = Vector2.zero;
        GetComponent<BoxCollider2D>().size = new(0.8f, 0.8f);
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).gameObject.SetActive(true);
    }
}
