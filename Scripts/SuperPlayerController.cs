using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperPlayerController : MonoBehaviour
{
    NetworkPlayer player;
    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<NetworkPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Destroy(InputPlayer inputPlayer)
    {
        if (inputPlayer.player != null && inputPlayer.playerCharacter != null)
        {
            inputPlayer.player.CmdRemovePlayer(inputPlayer.playerCharacter.id);
        }
        player.InputPlayerStack.Remove(inputPlayer);
        player.currentInputPlayers.Remove(inputPlayer);
        Destroy(inputPlayer.gameObject);
        
    }
}
