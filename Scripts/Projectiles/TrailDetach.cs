using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class TrailDetach : MonoBehaviour
{

    TrailRenderer trail;
    Projectile projectile;
    // Start is called before the first frame update
    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        projectile = GetComponentInParent<Projectile>();
        projectile.PreDestroySignal += PreDestroy;
    }

    void PreDestroy()
    {
        transform.parent = null;
        trail.autodestruct = true;
        Invoke(nameof(Destroy), 2f);
    }

    void Destroy()
    {
        Destroy(gameObject);
    }

}
