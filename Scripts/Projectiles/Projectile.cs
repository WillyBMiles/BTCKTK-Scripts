using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

public class Projectile : SerializedMonoBehaviour
{
    public static Dictionary<int, Projectile> projectiles = new();
    public Character origin { get; private set; }

    public int id = 0;
    [Tooltip("Set for traps.")]
    public int editorId = -1;
    public delegate void VoidNoParams();
    public VoidNoParams PreDestroySignal;
    [HideInInspector]
    public int abilityIndex = -1;


    [HideInInspector]
    public bool HasAuthority = false;

    public List<Character.Faction> canHit = new();

   
    public ContactFilter2D filter2D;

    [Header("Options")]

    [SerializeField]
    float speed = 10f;
    [SerializeField]
    bool destroyOnHit = true;
    [SerializeField]
    float duration = Mathf.Infinity;
    [SerializeField]
    float distance = Mathf.Infinity;
    [SerializeField]
    float radius = .1f;

    [HideInInspector]
    public bool respawn = false;


    Vector3 startingPosition;
    public Vector3 targetPosition { get; private set; }

    public AudioClip createSound;
    public AudioClip hitSound;

    public bool destroyMidair = false;

    [Header("Only set on traps")]
    public Shape shape;

    public bool dontApplyEffect = false;

    [Tooltip("Must have vision from caster to hit enemy")]
    public bool mustHaveVision = false;

    // Start is called before the first frame update
    void Start()
    {
        if (createSound != null)
            AudioSource.PlayClipAtPoint(createSound, transform.position);
    }


    public static int GenerateID()
    {
        return Mathf.Abs(1000 * (int)Time.time + Random.Range(0, 999));
    }

    public void Initialize(int id, int abilityIndex, Vector3 target, Shape shape, Character origin = null)
    {
        this.id = id;
        this.abilityIndex = abilityIndex;
        projectiles[id] = this;
        HasAuthority = origin == null ? HeadlessCommands.instance.isServer : origin.IsAuthoritative;

        this.shape = shape;
        lastPosition = transform.position;
        startingPosition = transform.position;
        if (float.IsFinite(distance))
            targetPosition = startingPosition + (target - startingPosition).normalized * distance;
        else
            targetPosition = startingPosition + (target - startingPosition).normalized * 1000f;

        this.origin = origin;
    }

    private void OnDestroy()
    {
        projectiles.Remove(id);
    }

    Vector3 lastPosition;

    readonly List<RaycastHit2D> raycastHits = new();
    readonly List<RaycastHit2D> raycastHits2 = new();
    readonly List<RaycastHit2D> raycastHitsTemp = new();
    readonly List<Collider2D> collider2Ds = new();
    [HideInInspector]
    public List<Character> charactersHit = new();

    bool pause = true;
    // Update is called once per frame
    void Update()
    {
        if (id < 1 && editorId > 0 && HeadlessCommands.instance != null)
        {
            Initialize(editorId, abilityIndex, transform.position, shape);
        }

        

        raycastHits.Clear();
        collider2Ds.Clear();
        if (speed == 0f || pause)
        {
            Physics2D.OverlapCircle(lastPosition, radius, filter2D, collider2Ds);
            collider2Ds.Sort((Collider2D c1, Collider2D c2) => { return Vector3.Distance(c1.transform.position, transform.position).CompareTo(Vector3.Distance(c2.transform.position, transform.position)); });
            foreach (Collider2D c2D in collider2Ds)
            {
                if (Hit(c2D))
                {
                    if (destroyOnHit)
                    {
                        DestroyThis();
                    }
                    return;
                }

            }
            pause = false;
        }
        else
        {

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            Physics2D.CircleCast(lastPosition, radius, transform.position - lastPosition, filter2D, raycastHits, Vector3.Distance(transform.position, lastPosition));

            
            raycastHits.Sort((RaycastHit2D hit1, RaycastHit2D hit2) => { return Vector3.Distance(lastPosition, hit1.point).CompareTo(Vector3.Distance(lastPosition, hit2.point)); });

            foreach (RaycastHit2D hit in raycastHits)
            {
                if (Hit(hit.collider))
                {
                    transform.position = lastPosition + transform.right * (hit.distance + radius);
                    DestroyThis();
                    return;
                }

            }
        }
            


        lastPosition = transform.position;

        duration -= Time.deltaTime;
        if (duration <= 0f)
        {
            DestroyThis();
        }
        if (transform.position == targetPosition && speed > 0f)
        {
            DestroyThis();
        }

        if (destroyMidair && (origin == null || origin.dead > 0))
            DestroyThis();
    }

    static bool ContainsCollider(List<RaycastHit2D> raycastHit2Ds, Collider2D c2D)
    {
        foreach (RaycastHit2D r2D in raycastHit2Ds)
        {
            if (r2D.collider == c2D)
                return true;
        }
        return false;
    }
    static bool MatchesLayerMask(LayerMask layerMask, GameObject gameObject)
    {
        return ((1 << gameObject.layer) | layerMask) != 0;
    }

    void DestroyThis(bool sendCommand = true)
    {
        PreDestroySignal?.Invoke();
        if (respawn)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
        
        if (HasAuthority && sendCommand)
        {
            SignalDestroy(transform.position);
        }
    }

    List<RaycastHit2D> tempHits = new();
    bool Hit(Collider2D collider)
    {
        
        if (!NetworkController.instance.hitboxes.TryGetValue(collider.transform.gameObject, out HitBox hb))
        {
            if (hitSound != null)
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            return true;
        }
        
        if (mustHaveVision && Physics2D.Raycast(origin.transform.position, hb.transform.position - origin.transform.position, origin.visionFilter, tempHits, Vector2.Distance(hb.transform.position, origin.transform.position)) > 0){
            return false;
        }

        if (!canHit.Contains(hb.character.faction) || //can't hit characters out of faction
            (hb.character.dead > 0 && hb.character.dead != id) || //can't hit dead characters usually
            charactersHit.Contains(hb.character)) //can't hit same character multiple times
            return false;
        charactersHit.Add(hb.character);
        if ((HasAuthority && hb.character is not PlayerCharacter) //if not player character authority matters
            || PlayerCharacter.IsLocalPlayerCharacter(hb.character as PlayerCharacter)) //if player character only the local player character decides
        {
            if (!dontApplyEffect)
                shape.ApplyShape(id, abilityIndex, origin, transform.position, hb.character);
        }
            

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        return destroyOnHit;
    }

    void SignalDestroy(Vector3 position)
    {
        HeadlessCommands.instance.DestroyProjectile(id, position);
    }

    public void OverrideDestroy(Vector3 position)
    {
        transform.position = position;
        DestroyThis(false);
    }
}
