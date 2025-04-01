using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : NetworkBehaviour
{
    public static Dictionary<int, Character> characters = new();

    public static Rect bounds = new(-20f, -10f, 40f, 500f);

    [SyncVar]
    public int id = -1;

    public bool IsAuthoritative { get { return (this is PlayerCharacter) ? isOwned : isServer; } }
    public Action HitSignal;
    public Action BreakingActionSignal;
    public bool invincible;

    public ContactFilter2D visionFilter;
    public enum Faction
    {
        Players,
        Enemies
    }

    public Faction faction;

    [SerializeField]
    protected GameObject model;

    [SerializeField]
    GameObject stunSpiral;

    public bool DestroyOnDeath;

    public int deathEffect = 0;
    public int dead = 0; // dead > 0 is dead, dead === 0 is alive
    int lastHit = -1;
    float stunned = 0f;

    [SerializeField]
    float radius = .5f;

    [SerializeField]
    ContactFilter2D contactFilter2D;

    [Header("Use for enemies")]
    public bool fullWallPush = false;

    // Start is called before the first frame update
    void Start()
    {
        if (model != null)
            currentQuat = model.transform.rotation;
        if (isServer)
            PreInitialize();
        StartGetComponents();
        CharacterStart();
        lastPosition = transform.position;

    }

    [Tooltip("ONLY RUN ON SERVER, there is a bug where I can't check if you're on the server in this function")]
    public void PreInitialize()
    {

        if (id <= 0)
        {
            id = NetworkController.GetNextCharacterID();
        }
    }
    protected virtual void CharacterStart()
    {
        //pass
    }

    protected virtual void CharacterDestroy()
    {
        //pass
    }

    protected virtual void CharacterPostIni()
    {
        //pass
    }

    // Update is called once per frame
    void Update()
    {
        if (!characters.ContainsKey(id) && id != -1)
        {
            characters[id] = this;
            CharacterPostIni();
        }
            

        CharacterUpdate();
        TurnUpdate();
        PhysicsUpdate();

        transform.position = new(Mathf.Clamp(transform.position.x, bounds.xMin, bounds.xMax), Mathf.Clamp(transform.position.y, bounds.yMin, bounds.yMax), transform.position.z);

        if (stunSpiral != null)
        {
            if (stunned > 0f)
                stunSpiral.SetActive(true);
            else
                stunSpiral.SetActive(false);
        }
        if (stunned >= 0f)
            stunned -= Time.deltaTime;


    }

    Vector3 lastPosition;
    void TurnUpdate()
    {
        if (IsFrozen())
            return;

        if (transform.position != lastPosition)
        {
            Quaternion targetQuat = Quaternion.Euler(0f, 0f, -Vector2.SignedAngle(transform.position - lastPosition, Vector3.right));
            currentQuat = Quaternion.RotateTowards(currentQuat, targetQuat, 720f * Time.deltaTime);
            if (model != null)
                model.transform.rotation = currentQuat;
        }


        lastPosition = transform.position;
    }


    private void OnDisable()
    {
        characters.Remove(id);
        CharacterDestroy();
    }
    protected virtual void CharacterUpdate()
    {
        //pass
    }

    public void Hit(int hitBy)
    {
        
        if (lastHit == hitBy || invincible)
            return;


      
        VisualHit(hitBy);
        CmdVisualHit(hitBy);

        if (!isDefended())
            Die(hitBy);
    }
    public void VisualHit(int hitBy)
    {
        if (lastHit == hitBy)
            return;
        lastHit = hitBy;
        HitSignal?.Invoke();
    }

    public void Die(int hitBy, bool playAnimation = true)
    {
        if (playAnimation)
            OnDeath(hitBy, true);
        if (DestroyOnDeath)
            Destroy(gameObject);
        CmdDie(hitBy);
    }

    public void OnDeath(int hitBy, bool sendCommand)
    {
        dead = hitBy;
        NetworkController.instance.PlayDeathEffect(deathEffect, transform.position, DestroyOnDeath
            && sendCommand);
        
    }

    [Command(requiresAuthority = false)]
    void CmdDie(int hitBy)
    {
        if (DestroyOnDeath)
            NetworkServer.Destroy(gameObject);
        else
            RpcDie(hitBy);
    }

    [ClientRpc]
    void RpcDie(int hitBy)
    {
        if (dead == 0f)
            OnDeath(hitBy, false);
    }

    [ClientRpc]
    public void RpcRevive() {
        dead = 0;
        lastHit = 0;
    }

    [Command(requiresAuthority = false, channel = Channels.Unreliable)]
    void CmdVisualHit(int hitBy)
    {
        RpcVisualHit(hitBy);
    }
    [ClientRpc(channel = Channels.Unreliable)]
    void RpcVisualHit(int hitBy)
    {
        VisualHit(hitBy);
    }


    public static Character GetCharacter(int id)
    {
        if (characters.ContainsKey(id))
            return characters[id];
        return null;
    }
    public static int GetID(Character character)
    {
        if (character == null)
            return -1;
        return character.id;
    }

    List<string> freezeLocks = new();
    public bool IsFrozen()
    {
        return freezeLocks.Count > 0;
    }

    public void AddFreezeLock(string freezeLock)
    {
        if (!freezeLocks.Contains(freezeLock))
            freezeLocks.Add(freezeLock);
    }
    public void RemoveFreezeLock(string freezeLock)
    {
        freezeLocks.Remove(freezeLock);
    }

    List<string> defenseLocks = new();
    public bool isDefended()
    {
        return defenseLocks.Count > 0;
    }

    public void AddDefenseLock(string defenseLock)
    {
        if (!defenseLocks.Contains(defenseLock))
            defenseLocks.Add(defenseLock);
    }
    public void RemoveDefenseLock(string defenseLock)
    {
        defenseLocks.Remove(defenseLock);
    }



    protected Quaternion currentQuat;

    public void TurnModel(Quaternion quat)
    {
        if (model != null)
        {
            model.transform.rotation = quat;
            currentQuat = quat;
        }
    }


    protected Vector3 offset = new();
    void PhysicsUpdate()
    {


        if (IsFrozen() || IsStunned())
            return;
        if (!isServer && !(this is PlayerCharacter && isOwned))
            return;

        if (dead == 0)
        {
            RaycastHit2D hit2D = new();
            Vector3 middleOffset = OffsetToEdge(offset, ref hit2D);

            Vector3 finalOffset;

            if (middleOffset == offset)
                finalOffset = middleOffset;
            else
            {
                Vector3 perp = Vector3.Cross(hit2D.normal, Vector3.forward).normalized;
                Vector3 normalizedOffset = offset.normalized;
                float dot = Vector3.Dot(perp, normalizedOffset);
                if (dot > 0f)
                {
                    perp += (Vector3)hit2D.normal * .05f; //fixing long wall walking
                }
                else if (dot < 0f)
                {
                    perp -= (Vector3)hit2D.normal * .05f;//fixing long wall walking
                }

                finalOffset = middleOffset + 
                    (fullWallPush ? Mathf.Sign(dot) : dot) * offset.magnitude * perp;

                finalOffset = OffsetToEdge(finalOffset, ref hit2D);
            }
            transform.position += finalOffset;

        }
        else
        {
            transform.position += offset;
        }



    }

    readonly List<RaycastHit2D> hits = new();
    protected Vector2 OffsetToEdge(Vector3 offset, ref RaycastHit2D hit2D)
    {
        hits.Clear();
        if (Physics2D.CircleCast(transform.position, radius, offset, contactFilter2D, hits, offset.magnitude) > 0)
        {

            for (int i = 0; i < hits.Count; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.transform != transform)
                {

                    if (Vector3.Dot(hit.normal, offset) < 0 && offset.magnitude > hit.distance)
                    {
                        hit2D = hit;
                        offset = offset.normalized * hit.distance;
                    }
                }

            }
        }
        return offset;
    }


    public (Vector3, Vector3) GetVisionExtents(Vector3 origin, bool addTowards = true)
    {
        Vector3 right = Vector2.Perpendicular(transform.position - origin).normalized;
        Vector3 towards = (origin - transform.position).normalized;

        Vector3 add = addTowards ? towards * radius / 2f : new Vector3();
        return (transform.position + right * radius +  add, transform.position - right * radius + add);
    }

    public void Stun(float time)
    {
        BreakingActionSignal?.Invoke();
        stunned = Mathf.Max(time, stunned);
    }

    public bool IsStunned()
    {
        return stunned > 0f;
    }


    public Animation animationComponent { get; private set; }
    public RunAbility runAbility { get; private set; }

    void StartGetComponents()
    {
        animationComponent = GetComponent<Animation>();
        runAbility = GetComponent<RunAbility>();
    }

   
}
