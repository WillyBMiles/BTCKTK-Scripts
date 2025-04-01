using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    public Character character { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        character = GetComponentInParent<Character>();
        NetworkController.instance.hitboxes.Add(gameObject, this);
    }

    private void OnDestroy()
    {
        NetworkController.instance.hitboxes.Remove(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Hit(int hitBy)
    {
        character.Hit(hitBy);
    }
}
