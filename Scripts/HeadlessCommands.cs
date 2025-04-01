using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadlessCommands : NetworkBehaviour
{
    public static HeadlessCommands instance;

    public List<Shape> headlessShapes = new();

    [HideInInspector]
    public RunAbility runAbility;
    private void Start()
    {
        instance = this;
        runAbility = GetComponent<RunAbility>();
    }

    public static int RegisterShape(Shape shape)
    {
        if (instance.headlessShapes.Contains(shape))
        {
            return instance.headlessShapes.IndexOf(shape);
        }
        instance.headlessShapes.Add(shape);
        return instance.headlessShapes.IndexOf(shape);

    }

    public void DestroyProjectile(int id, Vector3 position)
    {
        if (isServer)
            RpcDestroyProjectile(id, position);
        else
            CmdDestroyProjectile(id, position);
    }

    [Command(channel = Channels.Unreliable, requiresAuthority = false)]
    void CmdDestroyProjectile(int id, Vector3 position)
    {
        RpcDestroyProjectile(id, position);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    public void RpcDestroyProjectile(int id, Vector3 position)
    {
        if (Projectile.projectiles.ContainsKey(id))
        {
            if (Projectile.projectiles[id].isActiveAndEnabled)
                Projectile.projectiles[id].OverrideDestroy(position);
        }
    }

    readonly List<Projectile> tempProjectiles = new();
    public void DestroyAllProjectiles()
    {
        tempProjectiles.AddRange(Projectile.projectiles.Values);
        foreach (Projectile p in tempProjectiles)
        {
            if (p != null)
                p.OverrideDestroy(p.transform.position);
        }
        
    }

    [ClientRpc]
    public void RpcRespawn()
    {
        DestroyAllProjectiles();
        NetworkController.instance.respawnSignal?.Invoke();
        

    }


    [Command(requiresAuthority = false, channel = Channels.Unreliable)]
    public void CmdDeathEffect(int number, int myPlayer, Vector3 position)
    {
        RpcDeathEffect(number, myPlayer, position);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    void RpcDeathEffect(int number, int playerID, Vector3 position)
    {
        if (NetworkPlayer.GetLocalPlayer() == null || NetworkPlayer.GetLocalPlayer().id == playerID)
            return;
        NetworkController.instance.PlayDeathEffect(number, position, false);
    }
}
