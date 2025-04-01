using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleDetach : MonoBehaviour
{

    ParticleSystem particle;
    Projectile projectile;

    // Start is called before the first frame update
    void Start()
    {
        particle = GetComponent<ParticleSystem>();
        projectile = GetComponentInParent<Projectile>();
        projectile.PreDestroySignal += PreDestroy;
    }

    void PreDestroy()
    {
        transform.parent = null;
        particle.Stop();
        ParticleSystem.MainModule main = particle.main;
        main.stopAction = ParticleSystemStopAction.Destroy;
    }

}
