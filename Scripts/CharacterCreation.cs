using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CharacterCreation : MonoBehaviour
{
    public static CharacterCreation instance;
    public bool show = false;

    public List<GameObject> prefabs;
    public int currentPrefab;
    
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        currentPrefab = 0;
        //show = true;
    }

    // Update is called once per frame
    void Update()
    {

    }


    float H;
    private void OnGUI()
    {
        if (!show)
            return;

        float width = 250f;
        float height = 200f;
        Rect rect = new(Screen.width / 2f - width / 2f, Screen.height / 2f - height / 2f, width, height);
        GUILayout.BeginArea(rect);
        GUILayout.Box("Choose a class:");

        int i = 0;
        foreach (GameObject go in prefabs)
        {
            if (i == currentPrefab)
            {
                GUILayout.Box(go.name);
            }
            else
            {
                if (GUILayout.Button(go.name))
                {
                    currentPrefab = i;
                }
            }
            i++;
        }

        GUILayout.Box("Choose a color:");
        H = GUILayout.HorizontalSlider(H, 0f, 1f);
        GUIStyle style = new();
        style.normal.background = MakeBackgroundTexture(1, 1, GetCurrentColor());
        GUILayout.Box("", style);
        GUI.backgroundColor = Color.white;

        GUILayout.EndArea();
    }

    public Color GetCurrentColor()
    {
        return Color.HSVToRGB(H, .8f, .4f);
    }

    private Texture2D MakeBackgroundTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D backgroundTexture = new Texture2D(width, height);

        backgroundTexture.SetPixels(pixels);
        backgroundTexture.Apply();

        return backgroundTexture;
    }
}
