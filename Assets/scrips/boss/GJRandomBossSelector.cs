using UnityEngine;

// Room.cs sleduje tento ROOT v alive. Preto ho ponechavame, kym boss nezomrie!
public class GJRandomBossSelector : MonoBehaviour
{
    public GameObject originalBossPrefab;
    public GameObject forgeHeartBossPrefab;
    [Tooltip("Zapni na prefabe len pri testovani noveho bossa.")]
    public bool forceForgeHeartForTesting = false;

    GameObject activeBoss;
    bool created;

    void Start()
    {
        GameObject selected = forgeHeartBossPrefab;
        if (!forceForgeHeartForTesting && originalBossPrefab != null && forgeHeartBossPrefab != null)
            selected = Random.value < 0.5f ? originalBossPrefab : forgeHeartBossPrefab;
        else if (selected == null)
            selected = originalBossPrefab;
        if (selected == null || selected == gameObject)
        {
            Debug.LogError("GJRandomBossSelector: chyba boss prefab!", this);
            Destroy(gameObject);
            return;
        }
        activeBoss = Instantiate(selected, transform);
        activeBoss.transform.localPosition = Vector3.zero;
        created = true;
    }

    void Update()
    {
        if (created && activeBoss == null) Destroy(gameObject);
    }
}
