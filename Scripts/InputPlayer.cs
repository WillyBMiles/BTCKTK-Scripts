using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputPlayer : MonoBehaviour
{
    [HideInInspector]
    public NetworkPlayer player;
    [HideInInspector]
    public PlayerCharacter playerCharacter;
    PingManager pingManager;
    PlayerInput playerInput;

    bool dontPushControlsToCharacter = false; //set to true when cursor is unlocked
    bool unlockedCursor = false;

    [HideInInspector]
    public int newPrefab;

    [HideInInspector]
    public CharacterSelectionEntry cse = null;

    public static List<InputPlayer> changingView = new();

    // Start is called before the first frame update
    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        player = NetworkPlayer.GetLocalPlayer();
        RequestPlayerCreation();
    }

    void RequestPlayerCreation()
    {
        player.AddInputPlayerToStack(this);
    }

    int myPc = -1;
    public void AssignPlayerCharacter(int pc)
    {
        myPc = pc;
    }


    // Update is called once per frame
    void Update()
    {
        if (playerCharacter == null && Character.GetCharacter(myPc) != null)
        {
            PlayerCharacter pc = Character.GetCharacter(myPc) as PlayerCharacter;
            playerCharacter = pc;
            pc.myInputPlayer = this;
            pingManager = pc.GetComponent<PingManager>();
        }
        if (cse != null && cse.isActiveAndEnabled)
        {
            newPrefab = cse.currentPrefab;
        }

        dontPushControlsToCharacter = unlockedCursor;

        changingView.Remove(null);
    }

    private void OnDestroy()
    {
        changingView.Remove(this);
    }

    #region Wrappers
    public void OnMove(InputAction.CallbackContext context)
    {
        if (unlockedCursor)
        {
            pingManager.OnMovePingCursor(context);
        }

        if (dontPushControlsToCharacter)
            return;
        if (playerCharacter == null)
            return;
        playerCharacter.OnMove(context);
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        if (unlockedCursor)
        {
            pingManager.OnLookPingCursor(context);
        }

        if (dontPushControlsToCharacter)
            return;
        if (playerCharacter == null)
            return;
        playerCharacter.OnLook(context);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (dontPushControlsToCharacter)
            return;
        if (playerCharacter == null)
            return;
        playerCharacter.OnAttack(context, playerInput);

    }

    public void OnVoteToReset(InputAction.CallbackContext context)
    {
        if (playerCharacter == null) //vote to reset unaffected by dontPushControlsToPlayer
            return;
        playerCharacter.OnVoteToReset(context);

    }

    public void OnCharacterSelect(InputAction.CallbackContext context)
    {
        if (context.action.triggered && CharacterSelection.instance != null && !CharacterSelection.instance.showing)
        {
            CharacterSelection.instance.CmdShow();
        }
    }

    public void OnPing(InputAction.CallbackContext context)
    {
        if (pingManager == null)
            return;
        pingManager.OnPing(context, playerInput);
    }

    public void OnTimerPing(InputAction.CallbackContext context)
    {
        if (pingManager == null)
            return;
        pingManager.OnTimerPing(context, playerInput);
    }

    public void OnDisconnect(InputAction.CallbackContext context)
    {
        if (player == null)
            return;
        if (context.action.triggered)
        {
            Disconnect();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.action.triggered)
        {
            if (CharacterSelection.instance.showing && CharacterSelection.instance.CanConfirm())
            {
                CharacterSelection.instance.CmdConfirm();
            }
        }
        if (dontPushControlsToCharacter)
            return;
        playerCharacter.OnInteract(context);
    }

    public void OnUnlockCamera(InputAction.CallbackContext context)
    {
        if (context.action.triggered)
        {
            unlockedCursor = !unlockedCursor;
            if (unlockedCursor)
                pingManager.ShowPingCursor();
            else
                pingManager.HidePingCursor();
        }
    }

    public void OnChangeView(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (!changingView.Contains(this))
                changingView.Add(this);
        }
        if (context.canceled)
        {
            changingView.Remove(this);
        }
    }

    public void OnDeviceLost(PlayerInput playerInput)
    {
        Disconnect();
    }

    void Disconnect()
    {
        player.GetComponent<SuperPlayerController>().Destroy(this);
    }


    public void OnMenuLeft(InputAction.CallbackContext context)
    {
        if (context.action.triggered && cse != null && cse.isActiveAndEnabled)
        {
            cse.Move(-1);
        }
    }

    public void OnMenuRight(InputAction.CallbackContext context)
    {
        if (context.action.triggered && cse != null && cse.isActiveAndEnabled)
        {
            cse.Move(1);
        }
    }

    public void OnLookHold(InputAction.CallbackContext context)
    {
        playerCharacter.OnLookHold(context);
    }
    #endregion
}
