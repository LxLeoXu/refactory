using UnityEngine;

// Existing traps stay always-on unless timed is explicitly enabled.
public class DungeonSpikeTrap : MonoBehaviour
{
    [Min(0f)] public float damage = 7f;
    [Min(0.1f)] public float hitCooldown = 1.25f;
    public bool timed;
    public float safeTime=1.5f, warningTime=0.9f, activeTime=1.1f, phaseOffset;
    public Color warningColor=new Color(1f,0.75f,0.12f);
    public Room owner;
    public enum Phase { Safe, Warning, Active }
    public Phase CurrentPhase { get; private set; }
    float nextHitTime, startedAt;
    bool started;
    SpriteRenderer visual;
    public void SetVisual(SpriteRenderer value) { visual=value; }
    void Awake() { visual=GetComponent<SpriteRenderer>(); }
    public static Phase EvaluatePhase(float elapsed,float safe,float warning,float active)
    {
        safe=Mathf.Max(0.3f,safe); warning=Mathf.Max(0.4f,warning); active=Mathf.Max(0.2f,active);
        float time=Mathf.Repeat(elapsed,safe+warning+active);
        return time<safe ? Phase.Safe : time<safe+warning ? Phase.Warning : Phase.Active;
    }
    void Update()
    {
        if(!timed) { CurrentPhase=Phase.Active; return; }
        if(!started && (owner==null || owner.HasEntered)) { started=true; startedAt=Time.time; }
        CurrentPhase=started ? EvaluatePhase(Mathf.Max(0f,Time.time-startedAt-phaseOffset),safeTime,warningTime,activeTime) : Phase.Safe;
        if(visual!=null)
            visual.color=CurrentPhase==Phase.Active ? new Color(1f,0.24f,0.12f)
                : CurrentPhase==Phase.Warning ? warningColor : new Color(0.36f,0.42f,0.46f,0.65f);
    }
    void OnTriggerStay2D(Collider2D other)
    {
        if(timed && (!started || EvaluatePhase(Mathf.Max(0f,Time.time-startedAt-phaseOffset),safeTime,warningTime,activeTime)!=Phase.Active)) return;
        var stats=other.GetComponentInParent<PlayerStats>();
        if(stats==null || !stats.CompareTag("Player") || stats.IsDead || Time.time<nextHitTime) return;
        nextHitTime=Time.time+hitCooldown;
        Vector2 direction=(Vector2)(stats.transform.position-transform.position);
        stats.TakeDamage(damage,direction.normalized);
    }
}
