using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class AutoHost : MonoBehaviour
{
    public float delay = 0f;
    // Start is called before the first frame update
    void Start()
    {
        Invoke(nameof(InvokeStart), delay);
    }

    void InvokeStart()
    {
        GetComponent<NetworkManager>().StartHost();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
