using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Projectile))]
public class ChainProjectile : MonoBehaviour
{
    [SerializeField]
    GameObject prefab;
    Projectile projectile;
    // Start is called before the first frame update
    void Start()
    {
        projectile = GetComponent<Projectile>();
        projectile.PreDestroySignal += PreDestroy;
    }

    // Update is called once per frame
    void Update()
    {
    }

    void PreDestroy()
    {
        GameObject go = Instantiate(prefab, transform.position, transform.rotation);
        Projectile newProjectile = go.GetComponent<Projectile>();
        newProjectile.Initialize(projectile.id, projectile.abilityIndex, projectile.targetPosition, projectile.shape, projectile.origin);
    }
}
