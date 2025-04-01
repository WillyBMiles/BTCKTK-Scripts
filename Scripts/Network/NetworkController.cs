using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkController : MonoBehaviour
{
    public static NetworkController instance;

    public List<NetworkPlayer> players = new();
    public List<PlayerCharacter> playerCharacters { get; set; } = new();
    public Dictionary<GameObject, HitBox> hitboxes = new();
    public List<EnemyRespawner> respawners = new();

    [System.Serializable]
    public struct DeathEffects {
        public GameObject spawn;
        public AudioClip sound;
    }
    public List<DeathEffects> deathEffects = new();

    public delegate void VoidNoParams();
    public VoidNoParams respawnSignal;

    int nextCharacterID = 1;
    public bool anyAlertedEnemies = false;

    // Start is called before the first frame update
    void Start()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }
            

        DontDestroyOnLoad(gameObject);
        instance = this;
        Application.targetFrameRate = 60;
        PlayerCharacter.highestPlayer = 0f;
    }

    bool respawning = false;
    // Update is called once per frame
    void Update()
    {
        PlayerCharacter.highestPlayer = 0;
        foreach (PlayerCharacter pc in playerCharacters)
        {
            if (pc.dead == 0f && pc.transform.position.y > PlayerCharacter.highestPlayer)
            {
                PlayerCharacter.highestPlayer = pc.transform.position.y;
            }
        }

        anyAlertedEnemies = false;
        foreach (Character c in Character.characters.Values)
        {
            if (c is EnemyCharacter ec)
            {
                if (ec.alerted)
                {
                    anyAlertedEnemies = true;
                    break;
                }
                    
            }
        }
    }
    
    public int SpawnAndAssign(int prefabID, NetworkConnection conn, NetworkPlayer player)
    {
        GameObject prefab = CharacterCreation.instance.prefabs[prefabID];
        GameObject go = Instantiate(prefab, Checkpoint.GetCheckpoint(), Quaternion.identity);

        PlayerCharacter c = go.GetComponent<PlayerCharacter>();
        c.PreInitialize();
        c.prefabID = prefabID;
        player.myCharacters.Add( c.id);

        NetworkServer.Spawn(go, conn);

        return c.id;

    }

    public static int GetNextCharacterID()
    {
        return instance.nextCharacterID++;
    }

    List<EnemyRespawner> tempRespawners = new();
    public void ServerRespawn()
    {
        if (respawning)
            return;
        foreach (PlayerCharacter pc in playerCharacters)
        {
            Vector3 pos = Checkpoint.GetCheckpoint();
            pc.transform.position = pos;
            pc.RpcSetTransform(pos, Quaternion.identity);
            pc.RpcRevive();
        }
        HeadlessCommands.instance.RpcRespawn();
        StartRespawn();
        

        Invoke(nameof(RespawnEnemies), .3f);
    }

    void StartRespawn()
    {
        tempRespawners.Clear();
        tempRespawners.AddRange(respawners);
        foreach (var er in tempRespawners)
        {
            er.StartRespawn();
        }
        respawning = true;
        
    }

    void RespawnEnemies()
    {
        tempRespawners.Clear();
        tempRespawners.AddRange(respawners);
        foreach (var er in tempRespawners)
        {
            er.EndRespawn();
        }
        respawning = false;
    }


    public void PlayDeathEffect(int number, Vector3 position, bool sendCommmand)
    {
        if (deathEffects.Count <= number || number < 0)
            return;
        DeathEffects d = deathEffects[number];

        Instantiate(d.spawn, position, Quaternion.identity);
        if (d.sound != null)
            AudioSource.PlayClipAtPoint(d.sound, transform.position);

        if (sendCommmand && HeadlessCommands.instance != null)
            HeadlessCommands.instance.CmdDeathEffect(number, NetworkPlayer.GetLocalPlayer().id, position);
    }

}
