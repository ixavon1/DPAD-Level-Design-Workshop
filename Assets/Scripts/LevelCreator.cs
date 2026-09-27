using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
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

    private void Awake()
    {
        CreateLevel();

        if (ComputeHash("Assets/level.png") != PlayerPrefs.GetString("PrevHash")) { PlayerPrefs.SetFloat("CheckpointX", 0); PlayerPrefs.SetFloat("CheckpointY", 0); }
        PlayerPrefs.SetString("PrevHash", ComputeHash("Assets/level.png"));
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
                if (g.layer == 6 && !g.CompareTag("Spike")) ProbeGround(g.transform, x, y);
            }
        }
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

    private string ComputeHash(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
