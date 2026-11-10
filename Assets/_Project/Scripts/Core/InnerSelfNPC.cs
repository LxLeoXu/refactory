using UnityEngine;
using UnityEngine.InputSystem;

// Echo is a narrative representation of the player's inner voice.
// Replace the previous InnerSelfNPC.cs with this file (do not keep two copies).
[RequireComponent(typeof(SpriteRenderer))]
public class InnerSelfNPC : MonoBehaviour
{
    [Header("References (optional)")]
    public Transform player;
    public PlayerStats playerStats;

    [Header("Conversation")]
    public float interactionRange = 2.4f;
    public bool freezePlayerWhileTalking = true;

    [Header("Emotional tension (fictional game value)")]
    [Range(0f, 100f)] public float tension = 0f;
    public float tensionOnDeath = 25f;
    public float tensionPerSecondWhenWeak = 1f;

    class Choice
    {
        public readonly string playerLine;
        public readonly string echoReply;
        public readonly float tensionChange;

        public Choice(string playerLine, string echoReply, float tensionChange = 0f)
        {
            this.playerLine = playerLine;
            this.echoReply = echoReply;
            this.tensionChange = tensionChange;
        }
    }

    class Topic
    {
        public readonly string echoLine;
        public readonly Choice[] choices;

        public Topic(string echoLine, Choice a, Choice b, Choice c)
        {
            this.echoLine = echoLine;
            choices = new[] { a, b, c };
        }
    }

    enum ScreenState { Closed, Choices, Answer }

    SpriteRenderer npcSprite;
    PlayerController controller;
    Rigidbody2D playerBody;
    bool disabledController;
    ScreenState screen = ScreenState.Closed;
    Topic current;
    Choice selected;
    Topic[] story;
    Topic lowHealthTopic, hungerTopic, deathTopic, victoryTopic, tensionTopic, epilogueTopic;
    int storyPosition;
    int topicsInThisTalk;
    int deaths;
    int deathsAcknowledged;
    int victories;
    int victoriesAcknowledged;
    int lastWeakTopicAt = -1;
    int lastTensionTopicAt = -1;
    int selectedIndex;

    void Awake()
    {
        npcSprite = GetComponent<SpriteRenderer>();
        BuildDialogues();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (player != null)
        {
            controller = player.GetComponent<PlayerController>();
            playerBody = player.GetComponent<Rigidbody2D>();
            if (playerStats == null) playerStats = player.GetComponent<PlayerStats>();
        }

        if (playerStats != null) playerStats.OnDied += OnPlayerDied;

        if (npcSprite.sprite == null && player != null)
        {
            SpriteRenderer source = player.GetComponentInChildren<SpriteRenderer>();
            if (source != null) npcSprite.sprite = source.sprite;
        }
    }

    void OnDestroy()
    {
        if (playerStats != null) playerStats.OnDied -= OnPlayerDied;
        CloseDialogue();
    }

    void OnDisable()
    {
        CloseDialogue();
    }

    void OnPlayerDied()
    {
        deaths++;
        ChangeTension(tensionOnDeath);
        CloseDialogue();
    }

    // Optional hook: call this method from RunManager after beating the boss.
    public void OnDungeonCompleted()
    {
        victories++;
        ChangeTension(-15f);
    }

    void Update()
    {
        if (playerStats != null && !playerStats.IsDead)
        {
            if (playerStats.Health < playerStats.maxHealth * 0.25f ||
                playerStats.Hunger < playerStats.maxHunger * 0.2f)
                ChangeTension(tensionPerSecondWhenWeak * Time.deltaTime);
        }

        npcSprite.color = Color.Lerp(
            new Color(0.67f, 0.85f, 1f, 0.90f),
            new Color(0.93f, 0.51f, 0.56f, 0.86f),
            tension / 100f);

        if (player == null || Keyboard.current == null) return;
        bool near = Vector2.Distance(transform.position, player.position) <= interactionRange;
        if (!near || (playerStats != null && playerStats.IsDead))
        {
            if (screen != ScreenState.Closed) CloseDialogue();
            return;
        }

        // T, not E/F or 1-3: those are already used by machines, storage and items.
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (screen == ScreenState.Closed) OpenDialogue();
            else CloseDialogue();
            return;
        }

        if (screen == ScreenState.Closed) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseDialogue();
            return;
        }

        if (screen == ScreenState.Choices)
        {
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                selectedIndex = (selectedIndex + 1) % 3;
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                selectedIndex = (selectedIndex + 2) % 3;
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                Choose(selectedIndex);
        }
        else if (screen == ScreenState.Answer &&
                 (Keyboard.current.enterKey.wasPressedThisFrame ||
                  Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            NextTopic();
        }
    }

    void ChangeTension(float amount)
    {
        tension = Mathf.Clamp(tension + amount, 0f, 100f);
    }

    void OpenDialogue()
    {
        topicsInThisTalk = 0;
        disabledController = false;
        if (freezePlayerWhileTalking && controller != null && controller.enabled)
        {
            disabledController = true;
            controller.enabled = false;
            if (playerBody != null) playerBody.linearVelocity = Vector2.zero;
        }
        NextTopic();
    }

    void CloseDialogue()
    {
        screen = ScreenState.Closed;
        current = null;
        selected = null;
        if (disabledController && controller != null &&
            (playerStats == null || !playerStats.IsDead))
            controller.enabled = true;
        disabledController = false;
    }

    void NextTopic()
    {
        selected = null;
        selectedIndex = 0;
        topicsInThisTalk++;

        if (deathsAcknowledged < deaths)
        {
            deathsAcknowledged = deaths;
            current = deathTopic;
        }
        else if (victoriesAcknowledged < victories)
        {
            victoriesAcknowledged = victories;
            current = victoryTopic;
        }
        else if (playerStats != null &&
                 (playerStats.Health < playerStats.maxHealth * 0.35f ||
                  playerStats.Hunger < playerStats.maxHunger * 0.25f) &&
                 lastWeakTopicAt != storyPosition)
        {
            lastWeakTopicAt = storyPosition;
            current = playerStats.Hunger < playerStats.maxHunger * 0.25f
                ? hungerTopic : lowHealthTopic;
        }
        else if (tension >= 65f && storyPosition >= 3 &&
                 lastTensionTopicAt != storyPosition)
        {
            lastTensionTopicAt = storyPosition;
            current = tensionTopic;
        }
        else if (storyPosition < story.Length)
        {
            current = story[storyPosition];
            storyPosition++;
        }
        else
        {
            current = epilogueTopic;
        }

        screen = ScreenState.Choices;
    }

    void Choose(int index)
    {
        if (screen != ScreenState.Choices || current == null) return;
        selected = current.choices[Mathf.Clamp(index, 0, 2)];
        ChangeTension(selected.tensionChange);
        screen = ScreenState.Answer;
    }

    void OnGUI()
    {
        if (player == null) return;
        if (Vector2.Distance(transform.position, player.position) > interactionRange) return;
        if (playerStats != null && playerStats.IsDead) return;

        if (screen == ScreenState.Closed)
        {
            float promptWidth = Mathf.Min(290f, Screen.width - 20f);
            GUI.Box(new Rect((Screen.width - promptWidth) * 0.5f,
                Screen.height - 61f, promptWidth, 47f), "[T] Porozprávať sa s Ozvenou");
            return;
        }

        float width = Mathf.Min(650f, Screen.width - 24f);
        float height = Mathf.Min(365f, Screen.height - 24f);
        Rect frame = new Rect((Screen.width - width) * 0.5f,
            Screen.height - height - 12f, width, height);

        GUI.Box(frame, "");
        GUILayout.BeginArea(new Rect(frame.x + 14f, frame.y + 12f,
            frame.width - 28f, frame.height - 24f));

        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        GUIStyle line = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
        GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 14, wordWrap = true };

        GUILayout.Label("OZVENA", title);
        GUILayout.Space(4f);

        if (screen == ScreenState.Choices && current != null)
        {
            GUILayout.Label(current.echoLine, line, GUILayout.MinHeight(54f));
            GUILayout.Space(9f);
            for (int i = 0; i < current.choices.Length; i++)
            {
                string marker = i == selectedIndex ? "▶ " : "   ";
                if (GUILayout.Button(marker + "TY: " + current.choices[i].playerLine,
                    button, GUILayout.Height(51f)))
                    Choose(i);
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("↑/↓ vybrať · Enter potvrdiť · Esc/T zavrieť", line);
        }
        else if (selected != null)
        {
            GUILayout.Label("TY: " + selected.playerLine, line, GUILayout.MinHeight(48f));
            GUILayout.Space(8f);
            GUILayout.Label("OZVENA: " + selected.echoReply, line, GUILayout.MinHeight(76f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ďalšia téma  [Enter]", button, GUILayout.Height(42f)))
                NextTopic();
            if (GUILayout.Button("Ukončiť rozhovor  [T / Esc]", button, GUILayout.Height(37f)))
                CloseDialogue();
        }
        GUILayout.EndArea();
    }

    void BuildDialogues()
    {
        story = new[]
        {
            new Topic("Už si späť. Ten zvuk dverí by som spoznal kdekoľvek.",
                new Choice("Myslel som, že tu bude ticho.", "Ticho sa drží len chvíľu. Potom si začneš všímať vlastné myšlienky.", -2),
                new Choice("Čakal si na mňa?", "Povedzme, že som nemal kam odísť.", 2),
                new Choice("Ako sa voláš?", "Vyber mi meno. Ja som si na žiadne nespomenul.")),

            new Topic("V dielni to vonia po kove. Niekedy je jednoduchšie opraviť stroj než odpovedať na otázku.",
                new Choice("Stroje aspoň dávajú zmysel.", "Presne. Vložíš niečo dnu a vieš, čo má vyjsť von.", -1),
                new Choice("Na akú otázku?", "Čo sa stane, keď už nebude čo opravovať?", 2),
                new Choice("Môžeme niečo postaviť spolu?", "Ty postavíš. Ja ti budem pripomínať, prečo si začal.", -2)),

            new Topic("Keď si v dungeone, počujem každý výstrel. Aspoň mám ten pocit.",
                new Choice("Veď tam nikdy nie si.", "Nie? Potom prečo si pamätám presne to, čo ty?", 3),
                new Choice("To znie nemožne.", "Veľa vecí znie nemožne, kým ich nepocítiš.", 1),
                new Choice("Možno som ti o tom rozprával.", "Možno. Len si nepamätám, že by si otvoril ústa.", 2)),

            new Topic("Pamätáš si prvý kryštál, ktorý si priniesol? Držal si ho, akoby znamenal niečo viac.",
                new Choice("Bol to len materiál.", "Niekedy je aj obyčajná vec dôkazom, že sme niekam došli."),
                new Choice("Myslel som na ďalšiu výpravu.", "Ty stále myslíš na ďalšie dvere. A čo tieto, za tebou?", 2),
                new Choice("Bol som na seba hrdý.", "To je dobré. Tento pocit si pokojne nechaj.", -4)),

            new Topic("Zaujímavé je, že nikdy nestojíme pred zrkadlom naraz.",
                new Choice("Čo tým naznačuješ?", "Že odpoveď možno nie je v zrkadle, ale v tom, kto sa pozerá.", 2),
                new Choice("To je náhoda.", "Možno. Rád by som tomu veril."),
                new Choice("Nepotrebujem na to odpoveď.", "Nemusíš mať odpoveď dnes.", -3)),

            new Topic("Raz si sa vrátil bez jediného slova. Aj tak som vedel, ako si sa cítil.",
                new Choice("Prečo mi vieš čítať myšlienky?", "Možno ich nečítam. Možno v nich už dávno bývam.", 4),
                new Choice("Len si ma dobre poznal.", "To je pravda. Poznám ťa presnejšie, než viem vysvetliť.", 1),
                new Choice("Vtedy som potreboval pokoj.", "Tak ti ho doprajem. Nemusíme hovoriť stále.", -4)),

            new Topic("Všimol si si, že keď sa bojíš, aj môj hlas znie inak?",
                new Choice("Si ozvena mojich myšlienok?", "To je zatiaľ najbližšie k pravde, ako si sa dostal.", -1),
                new Choice("Prestaň hovoriť v hádankách.", "Dobre. Nie som tu preto, aby som ťa skúšal.", -2),
                new Choice("Chcem vedieť, kto si.", "Potom budeme musieť prestať predstierať, že sme cudzinci.", 3)),

            new Topic("Niekedy povieš 'my', keď rozprávaš len o sebe. Všimol si si to?",
                new Choice("Myslel som tým nás dvoch.", "A čo ak je to jeden príbeh rozprávaný dvoma hlasmi?", 3),
                new Choice("Možno som si na teba zvykol.", "Aj na vlastnú ozvenu si človek dokáže zvyknúť.", 1),
                new Choice("To nič nedokazuje.", "Nie. A nechcem ťa presviedčať nasilu.", -2)),

            new Topic("Nepamätám si, že by som niekedy otvoril tie dvere. Pamätám si iba, ako nimi prechádzaš ty.",
                new Choice("Prečo si nikdy neodišiel?", "Pretože moja cesta je presne taká dlhá ako tvoja.", 2),
                new Choice("Takže nie si skutočný?", "Som skutočný ako myšlienka. Nie však ako ďalší človek v miestnosti.", -2),
                new Choice("To by vysvetľovalo veľa vecí.", "Viem. Niekedy vysvetlenie vyzerá tichšie než záhada.", -3)),

            new Topic("Poviem to priamo: nie sme tu dvaja ľudia. Som hlas, ktorým si rozprávaš sám so sebou.",
                new Choice("Takže celý čas hovorím so sebou?", "Áno. A nie je na tom nič, za čo by si sa mal hanbiť.", -4),
                new Choice("Prečo si mi to nepovedal skôr?", "Možno si potreboval vlastné tempo, aby si tomu rozumel.", 1),
                new Choice("Znamená to, že zmizneš?", "Nie. Myšlienky sa menia, ale nemusíš ich hneď zahodiť.", -2)),

            new Topic("Keď dokončíš výpravu, prinesieš sem materiály. Ale aj niečo, čo sa nedá uložiť do skladu.",
                new Choice("Skúsenosti?", "Presne. A niektoré sú cennejšie než najvzácnejší kryštál.", -2),
                new Choice("Únavu?", "Aj tú. Preto je v poriadku si na chvíľu oddýchnuť.", -3),
                new Choice("Otázky.", "Otázky sú dobré. Nemusia mať okamžitú odpoveď.")),

            new Topic("Možno som len tvoja ozvena. Ale to, čo sa rozhodneš urobiť ďalej, patrí tebe.",
                new Choice("Chcem pokračovať.", "Tak choď, keď budeš pripravený. Nie preto, že musíš.", -2),
                new Choice("Chcem si najprv oddýchnuť.", "To je tiež rozhodnutie, na ktorom záleží.", -4),
                new Choice("Chcem ešte chvíľu hovoriť.", "Som tu ako myšlienka. A ty máš čas ju dopovedať.", -2))
        };

        lowHealthTopic = new Topic("Vyzeráš unavene. Nemusíš každú výpravu dokončiť za každú cenu.",
            new Choice("Som v poriadku.", "Dobre. Len si nezabudni skontrolovať vybavenie."),
            new Choice("Mám toho dosť.", "Potom má zmysel na chvíľu zostať v bezpečí.", -5),
            new Choice("Musím byť silnejší.", "Sila nie je len vo výdrži. Niekedy je v tom, že si dáš prestávku.", -2));

        hungerTopic = new Topic("Počujem ti škvŕkať v bruchu. S prázdnym žalúdkom sa ťažko premýšľa.",
            new Choice("Mám ešte zásoby?", "Pozri inventár. Jedlo býva užitočnejšie než ďalšia súčiastka.", -2),
            new Choice("V dungeone nebol čas jesť.", "Teraz si doma. Aspoň teraz sa môžeš postarať o seba.", -3),
            new Choice("To vydržím.", "Možno. Ale nemusíš skúšať svoje hranice zakaždým.", 1));

        deathTopic = new Topic("Vrátil si sa po náročnej výprave. Nevyšlo to, ale si zase v bezpečí.",
            new Choice("Stratil som polovicu lootu.", "To zamrzí. Nie všetko, čo si získal, sa však dá spočítať.", -2),
            new Choice("Mal som byť opatrnejší.", "Možno. Nabudúce môžeš skúsiť inú cestu alebo lepšie vybavenie.", -3),
            new Choice("Nechcem sa o tom rozprávať.", "Dobre. Nemusíme. Môžeme začať niečím iným.", -1));

        victoryTopic = new Topic("Tentoraz si sa vrátil po víťazstve. To je dôvod osláviť malý pokrok.",
            new Choice("Ten boss bol silný.", "A teraz už vieš, že sa s ním dá poradiť.", -3),
            new Choice("Prinesiem loot do skladu.", "Dobrý plán. Každé vylepšenie zjednoduší ďalšiu cestu.", -2),
            new Choice("Aj tak mám zvláštny pocit.", "Víťazstvo nemusí vyriešiť všetko naraz. Aj to je v poriadku.", -3));

        tensionTopic = new Topic("Dnes zniem ostrejšie než zvyčajne. Možno len opakujem tvoje obavy.",
            new Choice("Tak sa skúsme upokojiť.", "Môžeme. Jeden krok za druhým.", -8),
            new Choice("Prečo mi odporuješ?", "Pretože jedna myšlienka nemusí mať vždy pravdu.", 2),
            new Choice("Čo by mi teraz pomohlo?", "Skontrolovať zásoby. Rozhodnúť sa, čo je dôležité. A chvíľu zostať tu.", -6));

        epilogueTopic = new Topic("Zase sme sa stretli pri tých istých strojoch. Ale ty už nie si presne ten istý ako predtým.",
            new Choice("Mám ešte veľa práce.", "Tak si ju rozdeľ na malé časti. Nepotrebuješ zvládnuť všetko naraz.", -2),
            new Choice("Čo ak ďalšia výprava nevyjde?", "Potom sa môžeš poučiť, pripraviť a skúsiť to inak.", -2),
            new Choice("Som rád, že môžeme hovoriť.", "Je dobré dopriať vlastným myšlienkam priestor.", -4));
    }
}
