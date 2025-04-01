using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerCharacter : Character
{
    [HideInInspector]
    [SyncVar]
    public int prefabID;

    [SyncVar]
    public bool votedToReset = false;

    public static float highestPlayer { get; set; }

    public Color myColor;

    [SerializeField]
    float speed;

    Collider2D c2D;

    [SerializeField]
    SpriteRenderer[] renderers;

    [SerializeField]
    SpriteRenderer[] recolor;

    [SerializeField]
    GameObject[] deadDisable;

    [SerializeField]
    GameObject[] deadEnable;

    NetworkTransform netTransform;

    float ghostStunTimer = 0f;
    const float maxGhostStunTimer = 2f;
    const float stunTime = 1f;
    bool lookHold = false;

    readonly List<string> invisibleLocks = new();

    readonly Dictionary<SpriteRenderer, Color> defaultColors = new();

    [HideInInspector]
    public InputPlayer myInputPlayer;



    // Start is called before the first frame update
    protected override void CharacterStart()
    {
        staticPCs.Add(gameObject, this);
        c2D = GetComponent<Collider2D>();
        foreach (SpriteRenderer r in renderers)
        {
            defaultColors[r] = r.color;
        }

        if (isServer)
            dead = 1;
        else
        {
            CmdRequestSync();
        }
        NetworkController.instance.playerCharacters.Add(this);
        netTransform = GetComponent<NetworkTransform>();

        foreach (SpriteRenderer r in recolor)
        {
            defaultColors[r] = myColor;
            r.color = myColor;
        }


    }
    [Command(requiresAuthority = false)]
    void CmdRequestSync()
    {
        RpcSyncDeath(dead);
    }

    [ClientRpc]
    void RpcSyncDeath(int dead)
    {
        this.dead = dead;
    }

    protected override void CharacterDestroy()
    {
        base.CharacterDestroy();
        NetworkController.instance.playerCharacters.Remove(this);

    }

    Vector2 moveInput;
    Vector2 lookInput;
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        if (lookInput.magnitude > .001f && offset.magnitude == 0f)
        {
            TurnModel(Quaternion.Euler(0f, 0f, -Vector2.SignedAngle(lookInput, transform.right)));
        }
    }

    public void OnVoteToReset(InputAction.CallbackContext context)
    {
        if (context.action.triggered)
        {
            CmdVoteToReset();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.action.triggered)
        {
            Interactable.TriggerCurrentInteractable(this);
        }
    }

    public void OnLookHold(InputAction.CallbackContext context)
    {
        if (context.performed) // the key has been pressed
        {
            lookHold = true;
        }
        if (context.canceled) //the key has been released
        {
            lookHold = false;
        }
    }

    [Command]
    void CmdVoteToReset()
    {
        votedToReset = true;
    }

    public void OnAttack(InputAction.CallbackContext context, PlayerInput input)
    {
        if (context.action.triggered)
        {
            Vector2 point;
            if (input.currentControlScheme == "KeyboardAndMouse")
                point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            else
            {
                if (lookInput.magnitude > 0f)
                {
                    point = transform.position + AutoAim(lookInput);
                }
                else
                {
                    point = transform.position + AutoAim(model.transform.right);
                }

            }
            runAbility.Activate(point, 0);
        }

    }

    List<EnemyCharacter> candidates = new();
    List<RaycastHit2D> hitArray = new List<RaycastHit2D>();
    const float MAX_AUTO_AIM = 30f; //in degrees
    Vector3 AutoAim(Vector3 direction)
    {
        candidates.Clear();
        foreach (Character c in Character.characters.Values)
        {
            if (c is EnemyCharacter ec)
            {
                if (ec.alerted)
                {
                    if (Physics2D.Raycast(transform.position, ec.transform.position - transform.position, visionFilter, hitArray, Vector3.Distance(transform.position, ec.transform.position)) == 0)
                    {
                        candidates.Add(ec);
                    }
                }
            }
        }

        float bestAngle = MAX_AUTO_AIM;
        foreach (EnemyCharacter candidate in candidates)
        {
            float thisAngle = Vector2.Angle(candidate.transform.position - transform.position, direction);
            if (thisAngle < bestAngle)
            {
                direction = candidate.transform.position - transform.position;
                bestAngle = thisAngle;
            }
        }

        return direction.normalized;

    }

    // Update is called once per frame
    protected override void CharacterUpdate()
    {
        DeadUpdate();

        if (!isOwned)
            return;

        offset = moveInput.x * Vector2.right +
            moveInput.y * Vector2.up;
        if (offset.sqrMagnitude > 1f)
            offset = offset.normalized;
        offset *= speed * Time.deltaTime;

        if (lookHold)
        {
            var point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Quaternion quat = Quaternion.Euler(0f, 0f, -Vector2.SignedAngle(point - transform.position, Vector3.right));
            TurnModel(quat);

        }
    }

    public bool IsInvisible()
    {
        return invisibleLocks.Count > 0;
    }

    public void AddInvisibleLock(string iLock)
    {
        if (!invisibleLocks.Contains(iLock))
            invisibleLocks.Add(iLock);
    }
    public void RemoveInvisibleLock(string iLock)
    {
        invisibleLocks.Remove(iLock);
    }


    void DeadUpdate()
    {
        if (dead > 0)
        {
            c2D.enabled = false;


            foreach (GameObject go in deadDisable)
                go.SetActive(false);
            foreach (GameObject go in deadEnable)
                go.SetActive(true);

            if (ghostStunTimer <= 0f)
            {
                Collider2D c2D = Physics2D.OverlapCircle(transform.position, .4f, LayerMask.GetMask("Character"));
                if (c2D != null)
                {
                    EnemyCharacter ec = c2D.GetComponent<EnemyCharacter>();
                    if (ec != null && ec.alerted)
                    {
                        ec.Stun(stunTime);
                        ghostStunTimer = maxGhostStunTimer;
                    }
                }

            }
            ghostStunTimer -= Time.deltaTime;
        }

        else
        {
            c2D.enabled = true;


            foreach (GameObject go in deadDisable)
                go.SetActive(true);
            foreach (GameObject go in deadEnable)
                go.SetActive(false);
        }

        if (IsInvisible())
        {
            foreach (var (sr, c) in defaultColors)
            {
                sr.color = new Color(c.r, c.g, c.b, .2f);
            }
        }
        else
        {
            foreach (var (sr, c) in defaultColors)
            {
                sr.color = c;
            }
        }
    }



    public static bool IsLocalPlayerCharacter(PlayerCharacter pc)
    {
        if (pc == null)
            return false;
        NetworkPlayer player = NetworkPlayer.GetLocalPlayer();
        if (player == null)
            return false;
        return player.myCharacters.Contains(pc.id);
    }

    [ClientRpc]
    public void RpcSetTransform(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        if (isOwned)
            netTransform.CmdTeleport(position, rotation);
    }

    private void OnDestroy()
    {
        NetworkController.instance.playerCharacters.Remove(this);
        if (isServer && NetworkPlayer.GetLocalPlayer() != null)
            NetworkPlayer.GetLocalPlayer().myCharacters.Remove(id);
        staticPCs.Remove(gameObject);
    }

    #region statics
    static Dictionary<GameObject, PlayerCharacter> staticPCs = new();

    public static PlayerCharacter GetPlayerCharacter(GameObject go)
    {
        if (staticPCs.ContainsKey(go))
        {
            return staticPCs[go];
        }
        return null;
        
    }

    #endregion
}
