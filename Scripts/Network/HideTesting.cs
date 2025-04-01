using UnityEngine;
using Mirror;

public class HideTesting : MonoBehaviour
{
    NetworkManagerHUD hud;

    private void Awake()
    {
        hud = GetComponent<NetworkManagerHUD>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            hud.enabled = !hud.enabled;
        }
    }
}
