using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CharacterSelectionEntry : MonoBehaviour
{
    public int level = 0;
    public int currentPrefab =  0;

    public PlayerCharacter pc;
    public InputPlayer inputPlayer;

    public List<float> prefabOffsets = new();
    public List<float> levelOffsets = new();

    TextMeshProUGUI text;

    RectTransform rect;
    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        rect.anchoredPosition = new Vector2(prefabOffsets[currentPrefab], levelOffsets[level]);
        if (pc != null && pc.myInputPlayer != null)
        {
            pc.myInputPlayer.cse = this;
        }
        int playerNumber = level + 1;
        text.text = "Player " + playerNumber;
    }

    public void Move(int offset)
    {
        CharacterSelection.instance.CmdMove(level, Mathf.Clamp(currentPrefab + offset, 0, 3));
    }
}
