using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    //pass

    public static CustomNetworkManager instance { get {
            if (_instance == null)
                _instance = FindAnyObjectByType<CustomNetworkManager>();
            return _instance;
        } }
    static CustomNetworkManager _instance;

}
