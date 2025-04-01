using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DummySpawner : NetworkBehaviour
{
    [SerializeField]
    GameObject prefab;
    [SerializeField]
    float respawnTimer;
    GameObject myDummy;

    float timer = 0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!isServer)
            return;

        if (myDummy == null)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                RespawnDummy();
        }
    }

    void RespawnDummy()
    {
        if (!isServer)
            return;
        myDummy = Instantiate(prefab,transform.position, transform.rotation);
        NetworkServer.Spawn(myDummy);
        timer = respawnTimer;
    }
}
