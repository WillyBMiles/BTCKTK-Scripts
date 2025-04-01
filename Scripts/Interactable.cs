using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    static Dictionary<PlayerCharacter, Interactable> currentInteractions = new();

    List<PlayerCharacter> currentInteraction = new();
    public delegate void Interact(PlayerCharacter trigger);
    [Tooltip("Subscribe to this.")]
    public Interact interactSignal;
    public Interact hoverSignal;

    private void Update()
    {
        foreach (PlayerCharacter pc in currentInteraction)
        {
            TriggerStayOverride(pc);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerCharacter pc = PlayerCharacter.GetPlayerCharacter(collision.gameObject);
        if (pc != null)
        {
            currentInteraction.Add(pc);
            currentInteractions[pc] = this;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerCharacter pc = PlayerCharacter.GetPlayerCharacter(collision.gameObject);
        if (currentInteraction.Contains(pc))
        {
            currentInteraction.Remove(pc);
            if (currentInteractions.ContainsKey(pc))
            {
                currentInteractions.Remove(pc);
            }
        }
    }

    public static void TriggerCurrentInteractable(PlayerCharacter pc)
    {
        if (currentInteractions.ContainsKey(pc))
        {
            currentInteractions[pc].interactSignal?.Invoke(pc);
        }
    }

    void TriggerStayOverride(PlayerCharacter pc)
    {
        hoverSignal?.Invoke(pc);
    }
}
