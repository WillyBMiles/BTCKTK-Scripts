using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Rendering.PostProcessing;

public class CameraControls : MonoBehaviour
{
    [SerializeField]
    float maxScroll;
    [SerializeField]
    float scrollSpeed;
    float currentScroll;
    [SerializeField]
    PostProcessVolume volume;

    public Vector3 startingPosition;
    // Start is called before the first frame update
    void Start()
    {
        startingPosition = transform.position;
    }

    bool lockedCamera = false;
    // Update is called once per frame
    void LateUpdate()
    {
//FOR TESTING PURPOSES
        if (Input.GetKeyDown(KeyCode.C))
        {
            lockedCamera = !lockedCamera;
        }
        if (lockedCamera)
        {
            transform.position = startingPosition;
            return;
        }
        //END FOR TESTING PURPOSES



        NetworkPlayer p = NetworkPlayer.GetLocalPlayer();
        if (p == null || p.myCharacters.Count == 0)
            return;

        //if (p.myCharacters.Count == 1) //single player
        //{
        PlayerCharacter pc = null;
        foreach (int i in p.myCharacters)
        {
            if (Character.GetCharacter(i) != null)
            {
                pc = Character.GetCharacter(i) as PlayerCharacter;
            }
        }
            if (pc != null)
            {
                transform.position = pc.transform.position;
            }

            if (HeadlessCommands.instance != null && NetworkPlayer.GetLocalPlayer() != null)
            {
                if (pc != null)
                {
                    if (pc.dead > 0f)
                    {
                        volume.enabled = true;
                    }
                    else
                        volume.enabled = false;
                }
            }
            else
            {
                volume.enabled = false;
            }

        //}
        //else //local multiplayer
        //{
        //    volume.enabled = false;
        //    Vector3 position = new();
        //    foreach (int id in p.myCharacters)
        //    {
        //        Character c = Character.GetCharacter(id);
        //        if (c != null)
        //        {
        //            position += c.transform.position;
        //        }
                
                
        //    }
        //    position /= p.myCharacters.Count;
        //    transform.position = position;

        //}
        if (InputPlayer.changingView.Count > 0)
        {
            currentScroll = Mathf.Clamp(currentScroll + scrollSpeed * Time.deltaTime, 0f, maxScroll);
        }
        else
        {
            currentScroll = Mathf.Clamp(currentScroll - scrollSpeed * Time.deltaTime, 0f, maxScroll);
        }

        transform.position += currentScroll * Vector3.up;


        transform.position = new Vector3(transform.position.x, transform.position.y, -10f); //should always be last line
    }
}
