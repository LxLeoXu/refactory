using UnityEngine;

// Farba kontrolky: cervena = caka, oranzova = vyraba, zelena = hotovo.
[RequireComponent(typeof(FactoryMachine))]
public class FactoryMachineLight : MonoBehaviour
{
    public SpriteRenderer statusLamp;
    FactoryMachine machine;
    void Awake() { machine = GetComponent<FactoryMachine>(); }
    void Update()
    {
        if (statusLamp == null) return;
        if (machine.IsReady) statusLamp.color = new Color(0.35f, 1f, 0.5f);
        else if (machine.IsProducing)
        {
            float blink = Mathf.PingPong(Time.time * 4f, 1f);
            statusLamp.color = Color.Lerp(new Color(0.45f, 0.24f, 0.07f), new Color(1f, 0.68f, 0.15f), blink);
        }
        else statusLamp.color = new Color(0.55f, 0.18f, 0.13f);
    }
}
