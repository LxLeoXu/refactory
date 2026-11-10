using UnityEngine;
using System.Reflection;

// Doplnkova animacia. NEMENI FactoryMachine.cs ani FactoryMachineLight.cs.
// Zaroven nehybe samotnym objektom stroja (teda ani jeho colliderom).
[RequireComponent(typeof(FactoryMachine))]
public class FactoryMachineAnimator : MonoBehaviour
{
    [Header("Referencie vyplni editorovy setup")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer gearRenderer;
    public SpriteRenderer steamRenderer;
    public SpriteRenderer glowRenderer;

    [Header("Animacia")]
    public float gearDegreesPerSecond = 240f;
    public float animationSpeed = 3.5f;

    FactoryMachine machine;
    Vector3 steamStart;

    // Cita existujuce privatne polia. Nepotrebuje upravy FactoryMachine.cs.
    static readonly FieldInfo ProducingField = typeof(FactoryMachine).GetField(
        "isProducing", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo ReadyField = typeof(FactoryMachine).GetField(
        "isReady", BindingFlags.Instance | BindingFlags.NonPublic);

    void Awake()
    {
        machine = GetComponent<FactoryMachine>();
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
        if (steamRenderer != null) steamStart = steamRenderer.transform.localPosition;
    }

    void Update()
    {
        if (machine == null) return;
        bool running = ProducingField != null && (bool)ProducingField.GetValue(machine);
        bool ready = ReadyField != null && (bool)ReadyField.GetValue(machine);
        float beat = (Mathf.Sin(Time.time * animationSpeed * 2.5f) + 1f) * 0.5f;

        // Ozubene koliesko sa kruti iba pri vyrobe.
        if (gearRenderer != null)
        {
            gearRenderer.enabled = running;
            if (running)
                gearRenderer.transform.Rotate(0f, 0f, -gearDegreesPerSecond * Time.deltaTime);
        }

        // Jemny oblak pary nad pracovnym strojom.
        if (steamRenderer != null)
        {
            steamRenderer.enabled = running;
            if (running)
            {
                float wave = Mathf.Repeat(Time.time * 0.7f, 1f);
                steamRenderer.transform.localPosition = steamStart + new Vector3(0.06f * Mathf.Sin(Time.time * 2f), 0.35f * wave, 0f);
                Color smoke = Color.white;
                smoke.a = 0.55f * (1f - wave);
                steamRenderer.color = smoke;
            }
        }

        // Oranzove pulzovanie pri vyrobe, zelene po dokončení.
        if (glowRenderer != null)
        {
            glowRenderer.enabled = running || ready;
            if (running) glowRenderer.color = new Color(1f, 0.52f, 0.12f, Mathf.Lerp(0.22f, 0.7f, beat));
            else if (ready) glowRenderer.color = new Color(0.20f, 1f, 0.48f, Mathf.Lerp(0.20f, 0.52f, beat));
        }

        if (bodyRenderer != null)
        {
            if (running) bodyRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.86f, 0.70f), beat * 0.23f);
            else if (ready) bodyRenderer.color = Color.Lerp(Color.white, new Color(0.77f, 1f, 0.85f), beat * 0.12f);
            else bodyRenderer.color = Color.white;
        }
    }

    void OnDisable()
    {
        if (bodyRenderer != null) bodyRenderer.color = Color.white;
        if (gearRenderer != null) gearRenderer.enabled = false;
        if (steamRenderer != null) { steamRenderer.enabled = false; steamRenderer.transform.localPosition = steamStart; }
        if (glowRenderer != null) glowRenderer.enabled = false;
    }
}
