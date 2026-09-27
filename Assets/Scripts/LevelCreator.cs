using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelComponent
{
    public Color color;
    public GameObject myObject;
}

public class LevelCreator : MonoBehaviour
{
    public Texture2D image;
    public float disPerPixel;
    public LevelComponent[] components;
    public int maxObjects;
    public Transform player;
    private CompositeCollider2D groundComposite;

    private void Awake()
    {
        ConfigureCompositeGround();
        CreateLevel();
    }

    private void ConfigureCompositeGround()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) gameObject.layer = groundLayer;

        Rigidbody2D groundBody = GetComponent<Rigidbody2D>();
        if (groundBody == null) groundBody = gameObject.AddComponent<Rigidbody2D>();
        groundBody.bodyType = RigidbodyType2D.Static;

        groundComposite = GetComponent<CompositeCollider2D>();
        if (groundComposite == null) groundComposite = gameObject.AddComponent<CompositeCollider2D>();
        groundComposite.geometryType = CompositeCollider2D.GeometryType.Polygons;
    }

    public void CreateLevel()
    {
        for (int x = 0; x < image.width; x++)
        {
            for (int y = 0;  y < image.height; y++)
            {
                Color c = image.GetPixel(x, y);
                if (c == Color.white || c.a == 0) continue;

                GameObject closestObject = GetClosestComponent(x, y).myObject;

                maxObjects--;
                if (maxObjects < 0) { Debug.LogError("Ran out of objects. Please increase max objects in Level Creator object!"); return; }
                if (closestObject == null) { player.position = new Vector2(disPerPixel * x, disPerPixel * y); continue; }
                GameObject g = Instantiate(closestObject, new Vector2(disPerPixel * x, disPerPixel * y), Quaternion.identity, transform);
                if (g.layer == 6 && !g.CompareTag("Spike"))
                {
                    ConfigureGroundCollider(g);
                    ProbeGround(g.transform, x, y);
                }
            }
        }
    }

    // A solid box is much more reliable than four independent edges. Adjacent edge
    // colliders leave seams that can catch a moving body or let it tunnel between tiles.
    private void ConfigureGroundCollider(GameObject groundObject)
    {
        EdgeCollider2D[] edges = groundObject.GetComponents<EdgeCollider2D>();
        if (edges.Length == 0) return;

        PhysicsMaterial2D material = edges[0].sharedMaterial;
        foreach (EdgeCollider2D edge in edges) edge.enabled = false;

        BoxCollider2D solidCollider = groundObject.AddComponent<BoxCollider2D>();
        Vector3 scale = groundObject.transform.lossyScale;
        solidCollider.size = new Vector2(
            disPerPixel / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            disPerPixel / Mathf.Max(Mathf.Abs(scale.y), 0.0001f));
        solidCollider.sharedMaterial = material;
        solidCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    private LevelComponent GetClosestComponent(int x, int y)
    {
        if (x >= image.width || y >= image.height || x < 0 || y < 0) return null;
        Color c = image.GetPixel(x, y);
        if (c == Color.white || c.a == 0) return null;
        Color closestColor = Color.black;
        GameObject closestObject = null;
        float closestDiff = 99;
        for (int x2 = 0; x2 < components.Length; x2++)
        {
            Color paletteC = components[x2].color;
            float currentDiff = 0;
            currentDiff += Mathf.Abs(c.r - paletteC.r);
            currentDiff += Mathf.Abs(c.g - paletteC.g);
            currentDiff += Mathf.Abs(c.b - paletteC.b);
            if (currentDiff < closestDiff) { closestDiff = currentDiff; closestColor = paletteC; closestObject = components[x2].myObject; }
        }
        LevelComponent lc = new()
        {
            color = closestColor,
            myObject = closestObject
        };
        return lc;
    }

    private void ProbeGround(Transform ground, int x, int y)
    {
        if (GetClosestComponent(x, y+ 1 )?.color != Color.black) { ground.GetChild(0).gameObject.SetActive(true); }
        if (GetClosestComponent(x + 1, y)?.color != Color.black) { ground.GetChild(1).gameObject.SetActive(true); }
        if (GetClosestComponent(x, y - 1)?.color != Color.black) { ground.GetChild(2).gameObject.SetActive(true); }
        if (GetClosestComponent(x - 1, y)?.color != Color.black) { ground.GetChild(3).gameObject.SetActive(true); }
    }

}
