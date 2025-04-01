using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.UI.VirtualMouseInput;

[RequireComponent(typeof(PlayerCharacter))]
public class PingManager : NetworkBehaviour
{
    [SerializeField]
    GameObject pingPrefab;
    [SerializeField]
    GameObject timerPingPrefab;
    [SerializeField]
    float cursorSpeed;

    PlayerCharacter playerCharacter;

    public SpriteRenderer pingCursor;


    // Start is called before the first frame update
    void Start()
    {
        playerCharacter = GetComponent<PlayerCharacter>();
    }

    float timerDelay = 4f;
    float currentTimer = 0f;
    // Update is called once per frame
    void Update()
    {
        if (!isOwned)
            return;
        currentTimer -= Time.deltaTime;

        if (playerCharacter != null)
            pingCursor.color = playerCharacter.myColor;
        Vector3 cursor = cursorMove.magnitude > cursorLook.magnitude ? cursorMove : cursorLook;
        pingCursor.transform.position += cursorSpeed * Time.deltaTime * cursor;

        Vector3 topLeftCorner = Camera.main.ViewportToWorldPoint(new Vector3(0f,0f));
        Vector3 bottomRightCorner = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f));
        pingCursor.transform.position = new Vector3(Mathf.Clamp(pingCursor.transform.position.x, topLeftCorner.x, bottomRightCorner.x), Mathf.Clamp(pingCursor.transform.position.y, topLeftCorner.y, bottomRightCorner.y));


    }

    public void ShowPingCursor()
    {
        pingCursor.enabled = true;
        pingCursor.transform.position = transform.position;
    }
    public void HidePingCursor()
    {
        pingCursor.enabled = false;
    }

    public void OnPing(InputAction.CallbackContext context, PlayerInput input)
    {
        if (!context.action.triggered)
            return;
        Vector2 point;
        if (input.currentControlScheme == "KeyboardAndMouse")
            point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        else
        {
            if (pingCursor.enabled)
            {
                point = pingCursor.transform.position;
            }
            else
            {
                point = transform.position;
            }
        }
             

        ShowPing(point, playerCharacter.myColor, true);
    }

    public void OnTimerPing(InputAction.CallbackContext context, PlayerInput input)
    {
        if (!context.action.triggered)
            return;
        if (currentTimer > 0f)
            return;
        Vector2 point;
        if (input.currentControlScheme == "KeyboardAndMouse")
            point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        else
        {
            if (pingCursor.enabled)
            {
                point = pingCursor.transform.position;
            }
            else
            {
                point = transform.position;
            }
        }

        ShowTimerPing(point, true);

        currentTimer = timerDelay;
    }

    void ShowPing(Vector3 position, Color color, bool sendCommand = false)
    {
        GameObject go = Instantiate(pingPrefab, position, Quaternion.identity);
        go.GetComponent<Ping>().color = color;
        if (sendCommand)
            CmdPing(position);
    }
    void ShowTimerPing(Vector3 position, bool sendCommand = false)
    {
        Instantiate(timerPingPrefab, position, Quaternion.identity);
        if (sendCommand)
            CmdTimerPing(position);
    }

    [Command(channel = Channels.Unreliable)]
    void CmdPing(Vector3 position)
    {
        RpcPing(position);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    void RpcPing(Vector3 position)
    {
        if (isOwned)
            return;
        ShowPing(position, playerCharacter.myColor);
    }

    [Command(channel = Channels.Unreliable)]
    void CmdTimerPing(Vector3 position)
    {
        RpcTimerPing(position);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    void RpcTimerPing(Vector3 position)
    {
        if (isOwned)
            return;
        ShowTimerPing(position);
    }

    Vector2 cursorMove;
    Vector2 cursorLook;
    public void OnMovePingCursor(InputAction.CallbackContext context)
    {
        cursorMove = context.ReadValue<Vector2>();
        
    }

    public void OnLookPingCursor(InputAction.CallbackContext context)
    {
        cursorLook = context.ReadValue<Vector2>();

    }
}
