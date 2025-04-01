using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoteToReset : NetworkBehaviour
{
    [SerializeField]
    TMPro.TextMeshProUGUI text;


    // Update is called once per frame
    void Update()
    {
        int total = NetworkController.instance.playerCharacters.Count;
        int count = 0;
        foreach (PlayerCharacter pc in NetworkController.instance.playerCharacters)
        {
            if (pc.votedToReset)
                count++;
        }

        if (isServer && total > 0 && (float)count / (float) total >= .7f && count > 0)
        {
            foreach (PlayerCharacter pc in NetworkController.instance.playerCharacters)
            {
                pc.votedToReset = false;
            }
            NetworkController.instance.ServerRespawn();
        }
        

        if (count == 0)
            text.enabled = false;
        else
        {
            text.enabled = true;
            
            if (NetworkPlayer.GetLocalPlayer() != null && false) //TODO: Multiple characters per player
            {
                text.text = "Waiting for other players to Reset " + count + "/" + total;
            }
            else
            {
                text.text = "Vote to Reset [R]/[Y]/[Triangle] " + count + "/" + total;
            }
            
        }
    }

}
