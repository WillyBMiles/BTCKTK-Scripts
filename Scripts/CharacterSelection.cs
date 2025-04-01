using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelection : NetworkBehaviour
{
    public static CharacterSelection instance;
    public bool showing = false;

    public Transform parent;
    public GameObject prefab;

    public GameObject confirmText;
    public GameObject warningText;

    readonly List<CharacterSelectionEntry> entries = new();

    int currentNumberOfPlayers;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (showing)
        {
            confirmText.SetActive(CanConfirm());
            warningText.SetActive(!CanConfirm());

            if (currentNumberOfPlayers != NetworkController.instance.playerCharacters.Count)
            {
                CmdReopen();
            }
        }
            
    }

    void Show()
    {
        parent.gameObject.SetActive(true);
        DestoyEntries();

        int i = 0;
        NetworkController.instance.playerCharacters.Sort((PlayerCharacter pc1, PlayerCharacter pc2) => pc1.id.CompareTo(pc2.id));
        foreach (PlayerCharacter pc in NetworkController.instance.playerCharacters)
        {
            GameObject go = Instantiate(prefab, parent);
            CharacterSelectionEntry cse = go.GetComponent<CharacterSelectionEntry>();
            cse.currentPrefab = pc.prefabID;
            cse.pc = pc;
            cse.level = i;
            i++;
            entries.Add(cse);
        }
        showing = true;
        currentNumberOfPlayers = NetworkController.instance.playerCharacters.Count;
    }

    [Command(requiresAuthority = false)]
    public void CmdShow()
    {
        if (!showing)
        {
            foreach (PlayerCharacter pc in NetworkController.instance.playerCharacters)
            {
                pc.votedToReset = false;
            }
            RpcShow();
        }
        
    }

    [Command(requiresAuthority = false)]
    public void CmdReopen()
    {

        RpcShow();

    }

    [ClientRpc]
    public void RpcShow()
    {
        Show();
    }

    public bool CanConfirm()
    {
        foreach (var entry in entries)
        {
            foreach (var entry2 in entries)
            {
                if (entry.currentPrefab == entry2.currentPrefab && entry != entry2)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void Hide()
    {
        DestoyEntries();
        parent.gameObject.SetActive(false);

        showing = false;
        //respawn characters
    }

    void DestoyEntries()
    {
        while (entries.Count > 0)
        {
            Destroy(entries[0].gameObject);
            entries.RemoveAt(0);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdMove(int level, int newPrefab)
    {
        RpcMove(level, newPrefab);
    }

    [ClientRpc]
    void RpcMove(int level, int newPrefab)
    {
        if (entries.Count <= level || level < 0)
            return;
        entries[level].currentPrefab = newPrefab;
    }

    [Command(requiresAuthority = false)]
    public void CmdConfirm()
    {
        if (showing && CanConfirm())
        {
            showing = false;
            RpcConfirm();
            while (NetworkController.instance.playerCharacters.Count > 0)
            {
                NetworkServer.Destroy(NetworkController.instance.playerCharacters[0].gameObject); //destroy code already removes them
            }
        }
        
    }

    [ClientRpc]
    void RpcConfirm()
    {
        Hide();

        NetworkPlayer.GetLocalPlayer().RecreateCharacters();
    }
}
