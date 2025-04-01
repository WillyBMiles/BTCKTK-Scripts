using Mirror;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyCharacter : Character
{
    public enum AlertLevel
    {
        Ready,
        OnGuard,
        Unaware,
        Asleep,
        NeverAlert
    }

    [Space(20)]
    [SyncVar]
    public StartingConditions startingConditions;
    

    AlertLevel alertLevel;
    public float visionRadius = .1f;
    [SerializeField]
    TextMeshProUGUI text;

    public float speed;

    readonly List<EnemyCharacter> neighbors = new();
    [SyncVar]
    public int targetID = -1;
    public Character target { get; set; }
    [HideInInspector]
    public TargetOffset targetOffset;

    public enum TargetOffset
    {
        Center,
        Left,
        Right,
    }

    [HideInInspector]
    [SyncVar]
    public Vector3 targetPosition;

    [System.Serializable]
    public struct StartingConditions
    {
        public AlertLevel alertLevel;
        public float facingDirection;
        public bool practicing;
    }

    static readonly Dictionary<AlertLevel, (string, float)> alertTimes = new() { { AlertLevel.Ready, ("!", .1f) }, 
        { AlertLevel.OnGuard, ("?", .75f) }, { AlertLevel.Unaware, ("...", 1.65f) }, { AlertLevel.Asleep, ("zz", 2.5f) }, {AlertLevel.NeverAlert, ("--", 9999f) } };


    float alertTime = 0f;
    public bool alerted { get; private set; } = false;

    int startUnaware = 30;

    public Vector3 movementTarget { get; set; }

    protected override void CharacterStart()
    {
        base.CharacterStart();

        ApplyStartingConditions(startingConditions);

        movementTarget = transform.position;

    }
    void FindNeighbors()
    {
        foreach (Character c in characters.Values)
        {
            if (c is EnemyCharacter ec && CanSeeTarget(c, out TargetOffset _, true, true))
                neighbors.Add(ec);
        }
    }

    protected override void CharacterPostIni()
    {
        base.CharacterPostIni();
        FindNeighbors();
    }

    //visual just applies the visual things
    public void ApplyStartingConditions(StartingConditions startingConditions, bool visual = false)
    {
        alertLevel = startingConditions.alertLevel;
        TurnModel(Quaternion.Euler(0f, 0f, startingConditions.facingDirection));
        (text.text, alertTime) = alertTimes[alertLevel];
        if (!visual && isServer)
        {
            this.startingConditions = startingConditions;
            RpcApplyStartingConditions(startingConditions);
        }

    }

    [ClientRpc]
    void RpcApplyStartingConditions(StartingConditions startingConditions)
    {
        if (!isServer)
            ApplyStartingConditions(startingConditions);
    }



    // Update is called once per frame
    float practiceDelay;
    protected override void CharacterUpdate()
    {
        base.CharacterUpdate();
        if (!isServer)
        {
            target = GetCharacter(targetID);
        }


        if (!alerted && !InInterest()) //too far to care, interest management
            return;

        if (target != null) //managing alertness
        {
            alerted = true;
            if (target.dead > 0)
                target = null;
        }

        //turning the model in the right direction
        if (!IsFrozen() && !IsStunned() && target != null && alertTime < 1f) 
        {
            TurnModel(Quaternion.Euler(0f, 0f, 180-Vector2.SignedAngle(transform.position - targetPosition, transform.right )));
        }


        text.color = alerted ? Color.red : Color.white;
        (text.text, _) = alertTimes[alertLevel];

        if (startUnaware > 0)
        {
            startUnaware--;
            return;
        }

        //chaining neighbors' alertness to me
        foreach (EnemyCharacter ec in neighbors) 
        {
            if (ec.alerted)
            {
                alerted = true;
                
                if (isServer)
                {
                    if (targetID == -1)
                    {
                        targetID = ec.targetID;
                        target = ec.target;
                        targetPosition = ec.targetPosition;
                    }
                        
                }
                else if (targetID == -1)
                    targetID = ec.targetID;
            }
        }

        //reset target, dead targets
        if (target != null && target.dead > 0f) 
        {
            target = null;
            if (isServer)
            {
                targetID = -1;
            }
        }

        //practicing
        
        if (IsAuthoritative && !alerted && startingConditions.practicing )
        {
            
            if (practiceDelay <= 0f)
            {
                runAbility.Activate(transform.position + model.transform.right, RunAbility.PRACTICE_ID);
                practiceDelay = runAbility.practiceAbility.cooldown * (1+ Random.value);
            }
                
            practiceDelay -= Time.deltaTime;
            
        }
        

        //chaining my alertness to my neighbors
        if (alerted)
        {
            foreach (EnemyCharacter ec in neighbors)
            {
                if (ec.startUnaware <= 0)
                {
                    ec.alerted = true;
                    if (isServer)
                    {
                        if (ec.targetID == -1)
                        {
                            ec.targetID = targetID;
                            ec.target = target;
                            ec.targetPosition = targetPosition;
                        }
                            
                    }
                    else if (ec.targetID == -1)
                        ec.targetID = targetID;
                }

            }
            alertTime -= Time.deltaTime;
            alertLevel = GetAlertLevel(alertTime);
            
        }


        offset = (movementTarget - transform.position).normalized * (Mathf.Min(Time.deltaTime * speed, (movementTarget - transform.position).magnitude));
    }


    public void SetAlerted(bool value)
    {
        alerted = value;
    }

    public bool AlertedAndReady()
    {
        return alerted && alertTime <= 0f;
    }

    #region Utilities
    readonly List<RaycastHit2D> hits = new();
    public bool CanSeeTarget(Character c, out TargetOffset targetOffset, bool useRaycast = false, bool ignoreAware = false)
    {

        targetOffset = TargetOffset.Center;

        if (startUnaware > 0 && !ignoreAware)
            return false;
        if (c == null)
            return false;
        if (c.dead > 0)
            return false;
        if (Mathf.Abs(c.transform.position.y - transform.position.y) > 15f)
            return false; //too far
        hits.Clear();
        int raycastNumber;

        if (c is PlayerCharacter pc)
        {
            if (pc.IsInvisible())
                return false;
        }
        Vector3 testLeft, testRight;
        (testLeft, testRight) = c.GetVisionExtents(transform.position);
        Vector3 extentLeft, extentRight;
        (extentLeft, extentRight) = GetVisionExtents(c.transform.position);

        if (useRaycast)
        {
            raycastNumber = Physics2D.Raycast(extentLeft, testLeft - transform.position, visionFilter, hits, Vector2.Distance(transform.position, c.transform.position));
            targetOffset = TargetOffset.Left;
            if (raycastNumber > 0)
            {
                raycastNumber = Physics2D.Raycast(extentRight, testRight - transform.position, visionFilter, hits, Vector2.Distance(transform.position, c.transform.position));
                targetOffset = TargetOffset.Right;
            }
                
        }
        else
        {
            raycastNumber = Physics2D.CircleCast(extentLeft, visionRadius, testLeft - transform.position, visionFilter, hits, Vector2.Distance(transform.position, c.transform.position));
            targetOffset = TargetOffset.Left;
            if (raycastNumber > 0)
            {
                raycastNumber = Physics2D.CircleCast(extentRight, visionRadius, testRight - transform.position, visionFilter, hits, Vector2.Distance(transform.position, c.transform.position));
                targetOffset = TargetOffset.Right;
            }
                
        }

        if (raycastNumber == 0)
        {
            targetOffset = TargetOffset.Center;
        }
        return raycastNumber == 0;
    }

    public bool CanSeeTarget()
    {
        return CanSeeTarget(target, out TargetOffset _, true);
    }

    public Vector3 AdjustTarget(Character input, TargetOffset offset)
    {
        if (input == null)
            return transform.position;
        Vector3 leftHit, rightHit;
        (leftHit, rightHit) = input.GetVisionExtents(transform.position, false);
        return offset switch
        {
            TargetOffset.Center => input.transform.position,
            TargetOffset.Left => leftHit,
            TargetOffset.Right => rightHit,
            _ => target.transform.position,
        };
    }
    #endregion

    #region oldAttacks
    /*
    [ClientRpc]
    void RpcAttack(int id)
    {
        OverrideAttack(id);
    }
    void LocalAttack()
    {
        if (target != null)
            createProjectile.Shoot(target.transform.position, Projectile.GenerateID(),  false);
    }
    void UseAbility(bool left)
    {
        if (target != null)
        {
            Vector3 leftHit, rightHit;
            (leftHit, rightHit) = target.GetVisionExtents(transform.position, false);
            Vector3 position = left ? rightHit : leftHit;

            runAbility.Activate(position, 0, target);
        }
    }

    int lastAttack;
    void OverrideAttack(int id)
    {
        if (id == lastAttack)
            return;
        lastAttack = id;

        if (target != null)
            createProjectile.Shoot(target.transform.position, id, false, true);
    }
    /*
    void HitScanAttack( bool left)
    {
        if (target != null)
        {
            hitScan.Fire(target, left, true);
        }
    }
    */
    #endregion



    static AlertLevel GetAlertLevel(float alertTime)
    {
        float bestTime = Mathf.Infinity;
        AlertLevel ret = AlertLevel.Ready;
        foreach (var (alertLevel, (_, time)) in alertTimes)
        {
            if (alertTime <= time && bestTime > alertTime)
            {
                bestTime = alertTime;
                ret = alertLevel;
            }
        }
        return ret;
    }

    public bool InInterest()
    {
        return Mathf.Abs(PlayerCharacter.highestPlayer - transform.position.y) < 20f;
    }



    #region AI stuff

    #endregion
}
