using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using System.Linq;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar]
    public int id = -1;
    static int nextID = 0;

    public readonly SyncList<int> myCharacters = new();
    readonly List<PlayerCharacter> localPlayerCharacters = new();

    public List<InputPlayer> currentInputPlayers = new();

    public List<InputPlayer> InputPlayerStack = new();
    List<int> PlayerCharacterStack = new();

    [SerializeField]
    PlayerInputManager playerInputManager;
    [SerializeField]
    SuperPlayerController superPlayerController;

    public List<InputPlayer> inputPlayerAssignments = new();
    public List<int> availableAssignments = new();

    // Start is called before the first frame update
    void Start()
    {
        NetworkController.instance.players.Add(this);

        DontDestroyOnLoad(gameObject);

        if (isServer)
        {
            id = nextID;
            nextID++;
        }
            
        if (isOwned)
        {
            playerInputManager.enabled = true;
            superPlayerController.enabled = true;
        }
    }

    public void RecreateCharacters()
    {
        foreach (InputPlayer ip in currentInputPlayers)
        {
            inputPlayerAssignments.Add(ip);
            CmdSpawnPreabPlayer(ip.newPrefab);
        }
    }

    private void OnDestroy()
    {
        NetworkController.instance.players.Remove(this);
    }


    const int maxPlayerCount = 4;
    // Update is called once per frame
    void Update()
    {
        if (myCharacters.Count != localPlayerCharacters.Count)
        {
            localPlayerCharacters.Clear();
            foreach (int i in myCharacters)
            {
                localPlayerCharacters.Add(Character.GetCharacter(i) as PlayerCharacter);
            }
        }

        if (!isOwned)
            return;

        while (Mathf.Min(InputPlayerStack.Count, PlayerCharacterStack.Count) > 0)
        {
            InputPlayer ip = InputPlayerStack[0];
            currentInputPlayers.Add(ip);
            ip.AssignPlayerCharacter(PlayerCharacterStack[0]);
            PlayerCharacterStack.RemoveAt(0);
            InputPlayerStack.RemoveAt(0);
        }

        for (int i =0; i < inputPlayerAssignments.Count; i++)
        {
            InputPlayer ip = inputPlayerAssignments[i];
            for (int j =0; j < availableAssignments.Count; j++)
            {
                PlayerCharacter c = Character.GetCharacter(availableAssignments[j]) as PlayerCharacter;
                if (c!= null && c.prefabID == ip.newPrefab)
                {
                    ip.AssignPlayerCharacter(availableAssignments[j]);
                    availableAssignments.RemoveAt(j);
                    inputPlayerAssignments.RemoveAt(i);
                    i--;
                    break;
                }
            }
        }
        if (NetworkController.instance.playerCharacters.Count >= maxPlayerCount)
            playerInputManager.DisableJoining();
        else
            playerInputManager.EnableJoining();

    }

    public void AddInputPlayerToStack(InputPlayer inputPlayer)
    {
        InputPlayerStack.Add(inputPlayer);
        CmdSpawnPlayer();
    }

    public static NetworkConnection GetLocalConnection()
    {
        foreach (NetworkPlayer np in NetworkController.instance.players)
        {
            if (np.isOwned)
                return np.connectionToServer;
        }
        return null;
    }
    public static NetworkPlayer GetLocalPlayer()
    {
        foreach (NetworkPlayer np in NetworkController.instance.players)
        {
            if (np.isOwned)
                return np;
        }
        return null;
    }

    [Command]
    public void CmdSpawnPlayer()
    {
        int prefab = 0;
        for (int i =0; i < 4; i++)
        {
            bool found = false;
            foreach (PlayerCharacter pc in NetworkController.instance.playerCharacters)
            {
                if (pc.prefabID == i)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                prefab = i;
                break;
            }
        }

        RpcAddPlayerToStack(connectionToClient, NetworkController.instance.SpawnAndAssign(prefab, connectionToClient, this));
    }

    [TargetRpc]
    void RpcAddPlayerToStack(NetworkConnectionToClient target, int id)
    {
        PlayerCharacterStack.Add(id);
    }

    [Command]
    public void CmdSpawnPreabPlayer(int index)
    {
        RpcAddPlayerToRecreationStack(connectionToClient, NetworkController.instance.SpawnAndAssign(index, connectionToClient, this));
    }

    [TargetRpc]
    void RpcAddPlayerToRecreationStack(NetworkConnectionToClient target, int id)
    {
        availableAssignments.Add(id);
    }

    [Command]
    public void CmdRemovePlayer(int id)
    {
        PlayerCharacter pc = Character.GetCharacter(id) as PlayerCharacter;
        if (pc != null)
        {
            NetworkServer.Destroy(pc.gameObject);
        }
        

        myCharacters.Remove(id);

    }
}
