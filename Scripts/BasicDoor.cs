using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BasicDoor : NetworkBehaviour
{
    public bool onlyOpenInteraction;

    [SyncVar]
    bool serverOpen = false;
    bool open = false;

    public AudioClip openSound;
    SpriteRenderer sr;
    Collider2D c2D;
    Interactable interactable;
    public bool exitDoor;
    public bool locked = false;

    public GameObject nearbyShow;
    int nearby = 0;

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        c2D = GetComponent<Collider2D>();
        if (onlyOpenInteraction)
        {
            interactable = GetComponentInChildren<Interactable>();
            interactable.interactSignal += TryOpen;
            interactable.hoverSignal += Stay;
        }

        NetworkController.instance.respawnSignal += ServerClose;
    }

    // Update is called once per frame
    void Update()
    {
        sr.enabled = !open;
        c2D.enabled = !open;
        if (serverOpen && !open)
        {
            open = true;
            if (openSound != null)
                AudioSource.PlayClipAtPoint(openSound, transform.position);
        }

        if (exitDoor && NetworkController.instance.anyAlertedEnemies)
        {
            open = false;
            locked = true;
            if (isServer)
                serverOpen = false;
        }
        else if (exitDoor)
        {
            locked = false;
        }

        if (nearbyShow != null)
        {
            if (nearby > 0 && !open)
                nearbyShow.SetActive(true);
            else
                nearbyShow.SetActive(false);
        }


        nearby--;
    }

    public void Stay(PlayerCharacter playerCharacter)
    {
        nearby = 1;
    }


    private void OnTriggerStay2D(Collider2D collision)
    {
        if (onlyOpenInteraction)
            return;
        TryOpen(collision);
    }

    public void TryOpen(Collider2D collision)
    {
        if (locked)
        {
            return;
        }
        PlayerCharacter pc = PlayerCharacter.GetPlayerCharacter(collision.gameObject);
        TryOpen(pc);
    }

    void TryOpen(PlayerCharacter opener)
    {
        if (opener != null && !opener.IsInvisible() && !open)
        {
            if (openSound != null)
                AudioSource.PlayClipAtPoint(openSound, transform.position);
            open = true;
            CmdOpen();
        }
    }

    [Command(requiresAuthority = false)]
    void CmdOpen()
    {
        serverOpen = true;
    }

    void ServerClose()
    {
        if (isServer)
            CmdClose();
    }

    [Command(requiresAuthority = false)]
    void CmdClose()
    {
        serverOpen = false;
        RpcClose();
    }

    [ClientRpc]
    void RpcClose()
    {
        open = false;
    }
}
