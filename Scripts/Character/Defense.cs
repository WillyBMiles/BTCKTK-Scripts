using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character))]
public class Defense : MonoBehaviour
{
    [SerializeField]
    string defenseLock;
    [SerializeField]
    float respawnTimer;
    [SerializeField]
    GameObject icon;
    [SerializeField]
    GameObject crackedIcon;

    [SerializeField]
    float gracePeriod;

    bool grace = false;

    float timer;
    Character character;
    public AudioClip defenseSound;


    // Start is called before the first frame update
    void Start()
    {
        character = GetComponent<Character>();
        character.HitSignal += Hit;
    }

    // Update is called once per frame
    void Update()
    {
        if (character.dead > 0f)
            timer = 0;
        if (timer > 0f || character.dead > 0f )
        {
            if (icon != null)
                icon.SetActive(false);
            if (crackedIcon != null)
                crackedIcon.SetActive(false);
            timer -= Time.deltaTime;
        }
        else
        {
            if (icon != null)
            {
                if (grace)
                {
                    crackedIcon.SetActive(true);
                    if (crackedIcon != null)
                        icon.SetActive(false);
                    
                }else
                {
                    icon.SetActive(true);
                    if (crackedIcon != null)
                        crackedIcon.SetActive(false);
                }
                
            }
                
            character.AddDefenseLock(defenseLock);
            
        }
            
    }

    void Hit()
    {
        if (defenseLock != null)
            AudioSource.PlayClipAtPoint(defenseSound, transform.position);
        if (timer <= 0f)
        {
            grace = true;
            if (gracePeriod == 0f)
            {
                ResetHit();
            }
            else
            {
                Invoke(nameof(ResetHit), gracePeriod);
            }
        }

    }

    void ResetHit()
    {
        timer = respawnTimer;
        character.RemoveDefenseLock(defenseLock);
        grace = false;
    }


}
