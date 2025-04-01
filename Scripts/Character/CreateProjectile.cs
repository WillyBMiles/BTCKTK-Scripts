using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character))]
public class CreateProjectile : NetworkBehaviour
{
    public GameObject prefab;
    public float offset;

    Character character;

    public float cooldown;
    public float freezeTime;

    public string freezeLock = "SHOOT";

    public AnimationClip clip;
    float currentFreezeTime;
    public float currentCooldown { get; set; }


    Animation animationComponent;

    

    // Start is called before the first frame update
    void Start()
    {
        character = GetComponent<Character>();
        animationComponent = GetComponent<Animation>();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentFreezeTime > 0f)
        {
            currentFreezeTime -= Time.deltaTime;
            if (currentFreezeTime <= 0f)
                character.RemoveFreezeLock(freezeLock);
        }
            
        if (currentCooldown > 0f)
            currentCooldown -= Time.deltaTime;
    }

    public void Shoot(Vector3 point, int id, bool sendCmd = true, bool ignoreLimitations = false)
    {
        if (!CanShoot(ignoreLimitations))
            return;
        if (sendCmd && !(isOwned || (isServer && character is not PlayerCharacter)))
        {
            return;
        }

       
        Vector3 direction = ((Vector2) point - (Vector2) transform.position).normalized;

        Vector3 origin = transform.position + direction * offset;
        
        Shoot(origin, direction, id);
        if (sendCmd)
        {
            if (isServer)
                RpcShoot(origin, direction, id);
            else
                CmdShoot(origin, direction, id);
        }


        
        
    }

    void Shoot(Vector3 origin, Vector3 direction, int id)
    {
        Quaternion quat = Quaternion.Euler(0f, 0f, -Vector2.SignedAngle(direction, Vector3.right));
        GameObject go = Instantiate(prefab, origin, quat);
        Projectile projectile = go.GetComponent<Projectile>();
        projectile.Initialize(id, -1, origin + direction,null, character);

        character.TurnModel(quat);

        if (animationComponent != null && clip != null)
        {
            animationComponent.clip = clip;
            animationComponent.Stop();
            animationComponent.Play();
        }
        
        if (freezeTime > 0f)
        {
            character.AddFreezeLock(freezeLock);
            currentFreezeTime = freezeTime;
        }
            
        if (cooldown > 0f)
            currentCooldown = cooldown;

        character.BreakingActionSignal?.Invoke();
    }

    [Command(channel = Channels.Unreliable)]
    void CmdShoot(Vector3 origin, Vector3 direction, int id)
    {
        RpcShoot(origin, direction, id);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    void RpcShoot(Vector3 origin, Vector3 direction, int id)
    {
        if (isOwned || (isServer && character is not PlayerCharacter))
            return;
        Shoot(origin, direction, id);
    }
    
    public bool CanShoot(bool ignoreLimitations = false)
    {
        return !(character.dead > 0f || (!ignoreLimitations && (currentCooldown > 0f || character.IsFrozen()) || character.IsStunned()));
    }

}
