# Aktuálny stav po úplnej konsolidácii PNG

Všetkých 109 PNG je teraz v `Assets/_Project/Art/Textures/`. Zostávajúce 27 dynamicky načítavané sprity používa nový RefactorySpriteCatalog s priamymi referenciami; žiadne PNG nezostalo v Resources ani TextMesh Pro. Podrobnosti: `TEXTURE_CONSOLIDATION_REPORT.md`.

Nasleduje historický report prvej etapy. Jeho výnimky o ponechaní PNG v Resources/TextMesh Pro a nemenení runtime skriptov už po druhej etape neplatia.

---

# REFACTORY v0.2 — PROJECT CLEANUP REPORT

Dátum: 9. november 2026. Projekt: `C:/Users/homol/LxLeoXu/refactory`. Unity 6000.3.26f1.

## Výsledok

Uskutočnených 161 presunov cez Unity `AssetDatabase.MoveAsset`: 81 PNG, 58 C# skriptov a 22 prefabov. Všetkých 161 GUID a pôvodné nastavenia slicing, PPU, pivotov a filtrovania sú zachované. Po presune boli všetky meta súbory bajtovo identické; pri neskoršom importe Unity doplnilo do `demon_boss 1.png.meta` a `Logo 1.png.meta` predvolené Android nastavenia s `overridden: 0`. Ostatných 159 meta súborov zostalo bajtovo identických. Pri kolízii názvov sa použil stabilný suffix z GUID. Žiadne obsahové duplikáty neboli zmazané; odstránených bolo 32 prázdnych priečinkov vrátane pôvodných vnorených `Assets/Assets`, `Assets/Assets 1` a `Assets/scrips`.

Východiskový Git pracovný strom bol čistý, vetva `feature/cleaup`. Vytvorenie novej vetvy odmietol prístup k `.git/refs`; nezmenil som existujúcu vetvu ani nevytvoril commit. Pred úpravami bola vytvorená ZIP záloha Assets, Packages a ProjectSettings. Záloha obsahuje aj ignorované súbory v týchto priečinkoch; neobsahuje Library, Temp ani Git históriu.

## Konečná štruktúra

```text
Assets/
  _Project/
    Art/Textures/       # 81 PNG v jednom priečinku
    Prefabs/            # 22 prefabov
    Scripts/
      Core/
      Player/
      Enemies/
      Dungeon/
      Inventory/
      Crafting/
      Factory/
      UI/
      Audio/
  Editor/              # editorové nástroje a kontrola referencií
  Resources/           # 27 dynamicky načítavaných PNG, JSON, pôvodná hudba a prefaby
  Scenes/              # MainMenu, LevelScene, test, SampleScene
  GameJamExtras/       # zachovaný prefab používaný editorovým nástrojom
  REFACTORY_Patch011/   # zachovaný patch prefab a súvisiace súbory
  Settings/
  TextMesh Pro/        # nezmenené third-party dáta
  _Recovery/           # nezmenené záložné scény
```

Priečinky Audio/Materials/Scenes pod _Project nevznikli naprázdno. Hudba ostala na existujúcom mieste v Resources. Scény ostali na pôvodných cestách kvôli konfigurácii buildu. ProjectSettings aj Packages a všetky `.unity` súbory boli porovnané so zálohou a sú nezmenené. Do Library sa ručne nezasahovalo; Unity si pri importe aktualizovalo vlastnú cache.

## Opravené referencie a skripty

- 9 pickup prefabov: chýbajúci GUID `6f24d04f7bdb64c6f80d3fc078ced491` nahradený existujúcim GUID `0da6cdf23a590e343b82f0b6327c9d5d`. Identitu potvrdzuje `Assembly-CSharp::GameJamPickupVisual` v každom poškodenom komponente aj funkčné zodpovedajúce prefaby v Resources.
- 4 GeneratedFactory prefaby: neplatný odkaz na StatusLamp obnovený na existujúcu textúru z rovnakého GeneratedFactory balíka, GUID `f583de8f4be88a948970b47597f7a095`, pôvodný sliced sprite fileID `7268949169216187869`. Overené načítaním prefabov v Unity.
- `Assets/Editor/GameJamFactorySetup.cs`: generovanie smeruje na `_Project/Prefabs` a textúry v `_Project/Art/Textures`.
- `Assets/Editor/GameJamStorageAnimationSetup.cs`: centralizovaný priečinok generovaných textúr a opravené priečinky vyhľadávania strojov.
- `Assets/Editor/REFACTORYChestAudioSetup011.cs`: opravená skutočná cesta `Assets/Resources/GameJamMusic/Tracks/`.
- Nový `Assets/Editor/RefactoryCleanupTools.cs`: menu **Tools > REFACTORY > Cleanup > Validate references**. Kontroluje GUID, chýbajúce komponenty a objektové referencie, Resources a povinné build scény. Výsledok zapisuje do `PROJECT_CLEANUP_VALIDATION.txt`. Jednorazové automatické presuny už nie sú aktívne.

Obsah runtime skriptov sa nemenil; upravené boli iba tri editorové nástroje. Herná logika nebola prepisovaná.

## Validácia

- Unity úspešne skompilovalo presunuté runtime aj editorové skripty.
- Kontrola v Unity otvorila 44 prefabov a 7 scén (scény iba ako preview, bez uloženia).
- Pred úpravami: 79 nálezov. Po úpravách: 50 nálezov. Po normalizácii presunutých ciest žiadny nový nález. Počet nálezov nie je počet jedinečných chýb: jedna chýbajúca referencia sa môže objaviť ako neznámy GUID aj ako chýbajúci objekt.
- Všetkých 27 zachovaných dynamických spriteov sa načíta cez Resources.Load<Sprite>; items a recipes cez Resources.Load<TextAsset>.
- MainMenu aj LevelScene existujú a sú povolené v build konfigurácii.
- Žiadne duplicitné GUID ani názvy tried pri statickom skene; 27 skupín rovnakého obsahu zostalo zachovaných, keďže majú samostatné GUID a možné rozdiely importu/referencií.
- Nenašiel sa žiadny Sprite Atlas ani projektové NUnit/UnityTest testy v Assets.
- `git diff --check` prešiel.
- Krátky Play Mode smoke test LevelScene: zobrazila sa základňa, postava, stroje a HUD; log potvrdil vygenerovanie 10 miestností a bossa. Potom bol Play Mode ukončený. Toto nie je kompletný gameplay ani standalone build test.
- Pri Play Mode sa objavili upozornenia na sprite tiling bez Full Rect; importné nastavenia sa zámerne nemenili. Preview audit môže navyše vypísať upozornenia na globálne 2D svetlá pri súčasnom načítaní otvorenej a preview scény.

## Zostávajúce problémy

Všetky nasledujúce nálezy boli prítomné už pred upratovaním. Neboli opravené odhadom náhradných assetov:

- `LevelScene`: starý odkaz na BaseStorageChest prefab a neplatné zdrojové komponenty; Slime má chýbajúci loot prefab.
- `Dungeon.prefab`: chýbajú tri enemy prefaby a chest prefab.
- `tank2.prefab`: chýbajúca textúra a smerové sprity.
- `test.unity`: chýbajúci skript na player, viaceré prefaby/sprity; scéna nie je povolená v builde.
- `_Recovery`: staré chýbajúce prefab referencie; zálohy ponechané bez zásahov.
- `Settings/Renderer2D.asset`: šesť GUID, ktoré Unity nerozpoznalo. Nezamieňajú sa automaticky za runtime chybu renderera; vyžadujú kontrolu kompatibility nastavení URP.
- Súbor `.slime.png-autosave.kra` je pôvodný skrytý autosave bez meta; Unity ho neimportuje, zostal zachovaný.

Úplný zoznam nálezov je na konci tohto reportu a v `PROJECT_CLEANUP_VALIDATION.txt`.

## Čo ručne otestovať

1. Spustiť MainMenu a prechod do LevelScene; overiť pause/resume a návrat do menu.
2. Pohyb, streľbu, zásahy, zbrane, bežných nepriateľov aj bossov.
3. Prechod základňa–dungeon–základňa, generovanie miestností a loot z truhlíc/nepriateľov.
4. Inventár, ikony, sklad, crafting a výrobné stroje.
5. Hudbu a efekty, prípadné ukladanie a načítanie; vykonať standalone build.
6. Pri zostávajúcich referenciách vybrať správne pôvodné assety podľa autorovho zámeru, najmä dungeon enemies, loot a skladový prefab.

## Záloha a obnova

Záloha `refactory-before-cleanup.zip` je priložená k výsledku v Codexe. Pred obnovou zavrieť Unity a zachovať aj aktuálny stav. ZIP je určený na obnovu do samostatného priečinka projektu; neprekrývať ním bez rozmyslu reorganizovaný Assets, pretože by vznikli duplicitné GUID. Kompletná mapa presunov a validačné dôkazy sú v `CleanupEvidence/`.

## Výnimky z centrálneho priečinka textúr

27 PNG v DungeonTiles a ItemIcons ostalo kvôli dynamickým Resources.Load cestám. Jedna third-party textúra EmojiOne ostala v TextMesh Pro.

- `Assets/Resources/DungeonTiles/background_void.png`
- `Assets/Resources/DungeonTiles/chest_closed.png`
- `Assets/Resources/DungeonTiles/chest_open.png`
- `Assets/Resources/DungeonTiles/door_wood.png`
- `Assets/Resources/DungeonTiles/factory_floor.png`
- `Assets/Resources/DungeonTiles/floor_ambush.png`
- `Assets/Resources/DungeonTiles/floor_danger.png`
- `Assets/Resources/DungeonTiles/floor_moss.png`
- `Assets/Resources/DungeonTiles/floor_ruins.png`
- `Assets/Resources/DungeonTiles/floor_stone.png`
- `Assets/Resources/DungeonTiles/floor_supply.png`
- `Assets/Resources/DungeonTiles/floor_treasure.png`
- `Assets/Resources/DungeonTiles/prop_crate.png`
- `Assets/Resources/DungeonTiles/prop_lamp.png`
- `Assets/Resources/DungeonTiles/prop_pillar.png`
- `Assets/Resources/DungeonTiles/trap_spikes.png`
- `Assets/Resources/DungeonTiles/wall_stone.png`
- `Assets/Resources/ItemIcons/armor_plate.png`
- `Assets/Resources/ItemIcons/armor_upgrade.png`
- `Assets/Resources/ItemIcons/crystal.png`
- `Assets/Resources/ItemIcons/health_potion.png`
- `Assets/Resources/ItemIcons/iron.png`
- `Assets/Resources/ItemIcons/metal_plate.png`
- `Assets/Resources/ItemIcons/scrap.png`
- `Assets/Resources/ItemIcons/weapon_part.png`
- `Assets/Resources/ItemIcons/weapon_upgrade.png`
- `Assets/Resources/ItemIcons/wood.png`

## Opravené prefaby

- `Assets/_Project/Prefabs/Pickup_armor_plate.prefab`
- `Assets/_Project/Prefabs/Pickup_armor_upgrade.prefab`
- `Assets/_Project/Prefabs/Pickup_crystal.prefab`
- `Assets/_Project/Prefabs/Pickup_health_potion.prefab`
- `Assets/_Project/Prefabs/Pickup_iron.prefab`
- `Assets/_Project/Prefabs/Pickup_metal_plate.prefab`
- `Assets/_Project/Prefabs/Pickup_scrap.prefab`
- `Assets/_Project/Prefabs/Pickup_weapon_upgrade.prefab`
- `Assets/_Project/Prefabs/Pickup_wood.prefab`
- `Assets/Resources/GeneratedFactory/Prefabs/Factory_ArmorPress.prefab`
- `Assets/Resources/GeneratedFactory/Prefabs/Factory_Assembler.prefab`
- `Assets/Resources/GeneratedFactory/Prefabs/Factory_Furnace.prefab`
- `Assets/Resources/GeneratedFactory/Prefabs/Factory_UpgradeStation.prefab`

## Odstránené prázdne priečinky

- `Assets/scrips/GameJamFactory/Sprites`
- `Assets/Resources/GameJamBoss/Sprites`
- `Assets/Resources/GameJamEnemies/Sprites`
- `Assets/Resources/GeneratedBasePolish/Sprites`
- `Assets/Resources/GeneratedFactory/Sprites`
- `Assets/prefabs/GameJamExtras/Prefabs`
- `Assets/prefabs/GameJamExtras/Sprites`
- `Assets/GeneratedBasePolish/GameJamExtras 1/Sprites`
- `Assets/Assets/GameJamExtras/Sprites`
- `Assets/Assets/scripts/factory`
- `Assets/Assets/GameJamExtras`
- `Assets/Assets/scripts`
- `Assets/GeneratedBasePolish/GameJamExtras 1`
- `Assets/GeneratedBasePolish/Sprites`
- `Assets/prefabs/GameJamExtras`
- `Assets/Resources/GeneratedBasePolish`
- `Assets/Resources/GJPixelArt`
- `Assets/Resources/pixelartassets`
- `Assets/Resources/Sprites`
- `Assets/scrips/boss`
- `Assets/scrips/crafting`
- `Assets/scrips/factory`
- `Assets/scrips/GameJamFactory`
- `Assets/scrips/Player`
- `Assets/scrips/rougelike`
- `Assets/Assets`
- `Assets/GeneratedBasePolish`
- `Assets/pixelarts`
- `Assets/prefabs`
- `Assets/scrips`
- `Assets/Assets 1/Editor`
- `Assets/Assets 1`

## Mapa presunov

| Pôvodná cesta | Nová cesta |
|---|---|
| `Assets/Assets/GameJamExtras/Sprites/Gear.png` | `Assets/_Project/Art/Textures/Gear.png` |
| `Assets/Assets/GameJamExtras/Sprites/Steam.png` | `Assets/_Project/Art/Textures/Steam.png` |
| `Assets/Assets/GameJamExtras/Sprites/Storage_Chest.png` | `Assets/_Project/Art/Textures/Storage_Chest.png` |
| `Assets/Assets/GameJamExtras/Sprites/WorkGlow.png` | `Assets/_Project/Art/Textures/WorkGlow.png` |
| `Assets/GeneratedBasePolish/GameJamExtras 1/Sprites/Gear.png` | `Assets/_Project/Art/Textures/Gear_cb81c5e7.png` |
| `Assets/GeneratedBasePolish/GameJamExtras 1/Sprites/Steam.png` | `Assets/_Project/Art/Textures/Steam_858d1e96.png` |
| `Assets/GeneratedBasePolish/GameJamExtras 1/Sprites/Storage_Chest.png` | `Assets/_Project/Art/Textures/Storage_Chest_48197dbf.png` |
| `Assets/GeneratedBasePolish/GameJamExtras 1/Sprites/WorkGlow.png` | `Assets/_Project/Art/Textures/WorkGlow_03ee5b24.png` |
| `Assets/GeneratedBasePolish/Sprites/amber_lamp.png` | `Assets/_Project/Art/Textures/amber_lamp.png` |
| `Assets/GeneratedBasePolish/Sprites/factory_crate.png` | `Assets/_Project/Art/Textures/factory_crate.png` |
| `Assets/GeneratedBasePolish/Sprites/factory_floor.png` | `Assets/_Project/Art/Textures/factory_floor.png` |
| `Assets/GeneratedBasePolish/Sprites/machine_pad.png` | `Assets/_Project/Art/Textures/machine_pad.png` |
| `Assets/GeneratedBasePolish/Sprites/walkway.png` | `Assets/_Project/Art/Textures/walkway.png` |
| `Assets/GeneratedBasePolish/Sprites/wall_terminal.png` | `Assets/_Project/Art/Textures/wall_terminal.png` |
| `Assets/GeneratedBasePolish/Sprites/warning_stripes.png` | `Assets/_Project/Art/Textures/warning_stripes.png` |
| `Assets/pixelarts/arrow.png` | `Assets/_Project/Art/Textures/arrow.png` |
| `Assets/pixelarts/demon_boss 1.png` | `Assets/_Project/Art/Textures/demon_boss 1.png` |
| `Assets/pixelarts/Logo 1.png` | `Assets/_Project/Art/Textures/Logo 1.png` |
| `Assets/pixelarts/monster.png` | `Assets/_Project/Art/Textures/monster.png` |
| `Assets/pixelarts/player.png` | `Assets/_Project/Art/Textures/player.png` |
| `Assets/pixelarts/slime.png` | `Assets/_Project/Art/Textures/slime.png` |
| `Assets/pixelarts/tank.png` | `Assets/_Project/Art/Textures/tank.png` |
| `Assets/scrips/GameJamFactory/Sprites/Machine_ArmorPress.png` | `Assets/_Project/Art/Textures/Machine_ArmorPress.png` |
| `Assets/scrips/GameJamFactory/Sprites/Machine_Assembler.png` | `Assets/_Project/Art/Textures/Machine_Assembler.png` |
| `Assets/scrips/GameJamFactory/Sprites/Machine_Furnace.png` | `Assets/_Project/Art/Textures/Machine_Furnace.png` |
| `Assets/scrips/GameJamFactory/Sprites/Machine_StatusLamp.png` | `Assets/_Project/Art/Textures/Machine_StatusLamp.png` |
| `Assets/scrips/GameJamFactory/Sprites/Machine_UpgradeStation.png` | `Assets/_Project/Art/Textures/Machine_UpgradeStation.png` |
| `Assets/Resources/GameJamBoss/Sprites/forge_heart.png` | `Assets/_Project/Art/Textures/forge_heart.png` |
| `Assets/Resources/GameJamEnemies/Sprites/crystal_wisp.png` | `Assets/_Project/Art/Textures/crystal_wisp.png` |
| `Assets/Resources/GameJamEnemies/Sprites/mine_spider.png` | `Assets/_Project/Art/Textures/mine_spider.png` |
| `Assets/Resources/GeneratedBasePolish/Sprites/amber_lamp.png` | `Assets/_Project/Art/Textures/amber_lamp_6db55d42.png` |
| `Assets/Resources/GeneratedBasePolish/Sprites/factory_crate.png` | `Assets/_Project/Art/Textures/factory_crate_b4481863.png` |
| `Assets/Resources/GeneratedBasePolish/Sprites/machine_pad.png` | `Assets/_Project/Art/Textures/machine_pad_f11915f7.png` |
| `Assets/Resources/GeneratedBasePolish/Sprites/walkway.png` | `Assets/_Project/Art/Textures/walkway_e9bd996a.png` |
| `Assets/Resources/GeneratedBasePolish/Sprites/wall_terminal.png` | `Assets/_Project/Art/Textures/wall_terminal_36551952.png` |
| `Assets/Resources/GeneratedBasePolish/Sprites/warning_stripes.png` | `Assets/_Project/Art/Textures/warning_stripes_913ed74f.png` |
| `Assets/Resources/GeneratedFactory/Sprites/Machine_ArmorPress.png` | `Assets/_Project/Art/Textures/Machine_ArmorPress_054390c3.png` |
| `Assets/Resources/GeneratedFactory/Sprites/Machine_Assembler.png` | `Assets/_Project/Art/Textures/Machine_Assembler_c08d886d.png` |
| `Assets/Resources/GeneratedFactory/Sprites/Machine_Furnace.png` | `Assets/_Project/Art/Textures/Machine_Furnace_bcbb6861.png` |
| `Assets/Resources/GeneratedFactory/Sprites/Machine_StatusLamp.png` | `Assets/_Project/Art/Textures/Machine_StatusLamp_f583de8f.png` |
| `Assets/Resources/GeneratedFactory/Sprites/Machine_UpgradeStation.png` | `Assets/_Project/Art/Textures/Machine_UpgradeStation_a0751009.png` |
| `Assets/Resources/GJPixelArt/crystal_rifle.png` | `Assets/_Project/Art/Textures/crystal_rifle.png` |
| `Assets/Resources/GJPixelArt/forge_heart_idle.png` | `Assets/_Project/Art/Textures/forge_heart_idle.png` |
| `Assets/Resources/GJPixelArt/forge_heart_phase2.png` | `Assets/_Project/Art/Textures/forge_heart_phase2.png` |
| `Assets/Resources/GJPixelArt/furnace_cannon.png` | `Assets/_Project/Art/Textures/furnace_cannon.png` |
| `Assets/Resources/GJPixelArt/item_armor_upgrade.png` | `Assets/_Project/Art/Textures/item_armor_upgrade.png` |
| `Assets/Resources/GJPixelArt/item_crystal.png` | `Assets/_Project/Art/Textures/item_crystal.png` |
| `Assets/Resources/GJPixelArt/item_iron.png` | `Assets/_Project/Art/Textures/item_iron.png` |
| `Assets/Resources/GJPixelArt/item_potion.png` | `Assets/_Project/Art/Textures/item_potion.png` |
| `Assets/Resources/GJPixelArt/item_scrap.png` | `Assets/_Project/Art/Textures/item_scrap.png` |
| `Assets/Resources/GJPixelArt/item_weapon_upgrade.png` | `Assets/_Project/Art/Textures/item_weapon_upgrade.png` |
| `Assets/Resources/GJPixelArt/prop_barrel.png` | `Assets/_Project/Art/Textures/prop_barrel.png` |
| `Assets/Resources/GJPixelArt/prop_crate.png` | `Assets/_Project/Art/Textures/prop_crate.png` |
| `Assets/Resources/GJPixelArt/prop_crystal.png` | `Assets/_Project/Art/Textures/prop_crystal.png` |
| `Assets/Resources/GJPixelArt/scrap_shotgun.png` | `Assets/_Project/Art/Textures/scrap_shotgun.png` |
| `Assets/Resources/GJPixelArt/turret_bullet.png` | `Assets/_Project/Art/Textures/turret_bullet.png` |
| `Assets/Resources/GJPixelArt/turret_fire.png` | `Assets/_Project/Art/Textures/turret_fire.png` |
| `Assets/Resources/GJPixelArt/turret_idle.png` | `Assets/_Project/Art/Textures/turret_idle.png` |
| `Assets/Resources/pixelartassets/arrow.png` | `Assets/_Project/Art/Textures/arrow_d391ee40.png` |
| `Assets/Resources/pixelartassets/demon_boss.png` | `Assets/_Project/Art/Textures/demon_boss.png` |
| `Assets/Resources/pixelartassets/monster.png` | `Assets/_Project/Art/Textures/monster_7cbe743a.png` |
| `Assets/Resources/pixelartassets/player.png` | `Assets/_Project/Art/Textures/player_73d6101f.png` |
| `Assets/Resources/pixelartassets/slime.png` | `Assets/_Project/Art/Textures/slime_d1048a75.png` |
| `Assets/Resources/pixelartassets/tank.png` | `Assets/_Project/Art/Textures/tank_d6dda338.png` |
| `Assets/Resources/Sprites/amber_lamp.png` | `Assets/_Project/Art/Textures/amber_lamp_6a833bce.png` |
| `Assets/Resources/Sprites/chest_closed.png` | `Assets/_Project/Art/Textures/chest_closed.png` |
| `Assets/Resources/Sprites/chest_open.png` | `Assets/_Project/Art/Textures/chest_open.png` |
| `Assets/Resources/Sprites/factory_crate.png` | `Assets/_Project/Art/Textures/factory_crate_5f67151a.png` |
| `Assets/Resources/Sprites/factory_floor.png` | `Assets/_Project/Art/Textures/factory_floor_fa345c22.png` |
| `Assets/Resources/Sprites/forge_heart.png` | `Assets/_Project/Art/Textures/forge_heart_476fde9e.png` |
| `Assets/Resources/Sprites/Gear.png` | `Assets/_Project/Art/Textures/Gear_b6b26e9e.png` |
| `Assets/Resources/Sprites/machine_pad.png` | `Assets/_Project/Art/Textures/machine_pad_d71d74d3.png` |
| `Assets/Resources/Sprites/Neon Pixel Power-Up Icon Sheet.png` | `Assets/_Project/Art/Textures/Neon Pixel Power-Up Icon Sheet.png` |
| `Assets/Resources/Sprites/Pixel Potion Upgrade Collection.png` | `Assets/_Project/Art/Textures/Pixel Potion Upgrade Collection.png` |
| `Assets/Resources/Sprites/Steam.png` | `Assets/_Project/Art/Textures/Steam_4630b149.png` |
| `Assets/Resources/Sprites/Storage_Chest.png` | `Assets/_Project/Art/Textures/Storage_Chest_bc4af93c.png` |
| `Assets/Resources/Sprites/walkway.png` | `Assets/_Project/Art/Textures/walkway_7b5a72a4.png` |
| `Assets/Resources/Sprites/wall_stone.png` | `Assets/_Project/Art/Textures/wall_stone.png` |
| `Assets/Resources/Sprites/wall_terminal.png` | `Assets/_Project/Art/Textures/wall_terminal_db6517fe.png` |
| `Assets/Resources/Sprites/warning_stripes.png` | `Assets/_Project/Art/Textures/warning_stripes_8a31ba4a.png` |
| `Assets/Resources/Sprites/WorkGlow.png` | `Assets/_Project/Art/Textures/WorkGlow_cd895fa3.png` |
| `Assets/Assets 1/Editor/REFACTORYPauseUIFix.cs` | `Assets/Editor/REFACTORYPauseUIFix.cs` |
| `Assets/MainMenuControlsPanel.cs` | `Assets/_Project/Scripts/UI/MainMenuControlsPanel.cs` |
| `Assets/REFACTORYChestAudioSetup011.cs` | `Assets/Editor/REFACTORYChestAudioSetup011.cs` |
| `Assets/scrips/BaseStorageChest.cs` | `Assets/_Project/Scripts/Inventory/BaseStorageChest.cs` |
| `Assets/scrips/boss/GJForgeHeartBoss.cs` | `Assets/_Project/Scripts/Enemies/GJForgeHeartBoss.cs` |
| `Assets/scrips/boss/GJForgeHeartBullet.cs` | `Assets/_Project/Scripts/Enemies/GJForgeHeartBullet.cs` |
| `Assets/scrips/boss/GJGameMusic.cs` | `Assets/_Project/Scripts/Audio/GJGameMusic.cs` |
| `Assets/scrips/boss/GJRandomBossSelector.cs` | `Assets/_Project/Scripts/Enemies/GJRandomBossSelector.cs` |
| `Assets/scrips/crafting/CraftingSystem.cs` | `Assets/_Project/Scripts/Crafting/CraftingSystem.cs` |
| `Assets/scrips/crafting/CraftingUI.cs` | `Assets/_Project/Scripts/UI/CraftingUI.cs` |
| `Assets/scrips/crafting/GJFieldTurret.cs` | `Assets/_Project/Scripts/Crafting/GJFieldTurret.cs` |
| `Assets/scrips/crafting/GJFieldTurretBullet.cs` | `Assets/_Project/Scripts/Crafting/GJFieldTurretBullet.cs` |
| `Assets/scrips/crafting/GJFieldTurretDeployer.cs` | `Assets/_Project/Scripts/Crafting/GJFieldTurretDeployer.cs` |
| `Assets/scrips/DungeonBuffPickup.cs` | `Assets/_Project/Scripts/Inventory/DungeonBuffPickup.cs` |
| `Assets/scrips/DungeonBuffSpawner.cs` | `Assets/_Project/Scripts/Dungeon/DungeonBuffSpawner.cs` |
| `Assets/scrips/factory/BaseRoomBuilder.cs` | `Assets/_Project/Scripts/Factory/BaseRoomBuilder.cs` |
| `Assets/scrips/factory/FactoryMachine.cs` | `Assets/_Project/Scripts/Factory/FactoryMachine.cs` |
| `Assets/scrips/factory/FactoryMachineLight.cs` | `Assets/_Project/Scripts/Factory/FactoryMachineLight.cs` |
| `Assets/scrips/FactoryMachineAnimator.cs` | `Assets/_Project/Scripts/Factory/FactoryMachineAnimator.cs` |
| `Assets/scrips/FoodPickup.cs` | `Assets/_Project/Scripts/Inventory/FoodPickup.cs` |
| `Assets/scrips/GameJamFactorySetup.cs` | `Assets/Editor/GameJamFactorySetup.cs` |
| `Assets/scrips/GameJamPickupVisual.cs` | `Assets/_Project/Scripts/Inventory/GameJamPickupVisual.cs` |
| `Assets/scrips/GameJamStorageAnimationSetup.cs` | `Assets/Editor/GameJamStorageAnimationSetup.cs` |
| `Assets/scrips/IDamageable.cs` | `Assets/_Project/Scripts/Player/IDamageable.cs` |
| `Assets/scrips/InnerSelfNPC.cs` | `Assets/_Project/Scripts/Core/InnerSelfNPC.cs` |
| `Assets/scrips/MainMenu.cs` | `Assets/_Project/Scripts/UI/MainMenu.cs` |
| `Assets/scrips/PauseMenu.cs` | `Assets/_Project/Scripts/UI/PauseMenu.cs` |
| `Assets/scrips/Player/CameraFollow.cs` | `Assets/_Project/Scripts/Player/CameraFollow.cs` |
| `Assets/scrips/Player/DungeonBalanceAndDetails.cs` | `Assets/_Project/Scripts/Dungeon/DungeonBalanceAndDetails.cs` |
| `Assets/scrips/Player/HUD.cs` | `Assets/_Project/Scripts/UI/HUD.cs` |
| `Assets/scrips/Player/Inventory1.cs` | `Assets/_Project/Scripts/Inventory/Inventory1.cs` |
| `Assets/scrips/Player/ItemDatabase.cs` | `Assets/_Project/Scripts/Inventory/ItemDatabase.cs` |
| `Assets/scrips/Player/ItemPickup.cs` | `Assets/_Project/Scripts/Inventory/ItemPickup.cs` |
| `Assets/scrips/Player/LootChest.cs` | `Assets/_Project/Scripts/Dungeon/LootChest.cs` |
| `Assets/scrips/Player/PlayerController.cs` | `Assets/_Project/Scripts/Player/PlayerController.cs` |
| `Assets/scrips/Player/PlayerHUD.cs` | `Assets/_Project/Scripts/UI/PlayerHUD.cs` |
| `Assets/scrips/Player/PlayerItemUse.cs` | `Assets/_Project/Scripts/Inventory/PlayerItemUse.cs` |
| `Assets/scrips/Player/PlayerStats.cs` | `Assets/_Project/Scripts/Player/PlayerStats.cs` |
| `Assets/scrips/Player/Projectile.cs` | `Assets/_Project/Scripts/Player/Projectile.cs` |
| `Assets/scrips/PlayerBuffs.cs` | `Assets/_Project/Scripts/Player/PlayerBuffs.cs` |
| `Assets/scrips/RefactoryPauseMenuV2.cs` | `Assets/_Project/Scripts/UI/RefactoryPauseMenuV2.cs` |
| `Assets/scrips/rougelike/DungeonGenerator.cs` | `Assets/_Project/Scripts/Dungeon/DungeonGenerator.cs` |
| `Assets/scrips/rougelike/DungeonRoomExtras.cs` | `Assets/_Project/Scripts/Dungeon/DungeonRoomExtras.cs` |
| `Assets/scrips/rougelike/DungeonSpikeTrap.cs` | `Assets/_Project/Scripts/Dungeon/DungeonSpikeTrap.cs` |
| `Assets/scrips/rougelike/Enemy.cs` | `Assets/_Project/Scripts/Enemies/Enemy.cs` |
| `Assets/scrips/rougelike/EnemyBullet.cs` | `Assets/_Project/Scripts/Enemies/EnemyBullet.cs` |
| `Assets/scrips/rougelike/EnemyDirectionalSprite.cs` | `Assets/_Project/Scripts/Enemies/EnemyDirectionalSprite.cs` |
| `Assets/scrips/rougelike/EnemyLoot.cs` | `Assets/_Project/Scripts/Enemies/EnemyLoot.cs` |
| `Assets/scrips/rougelike/FactoryWeaponForge.cs` | `Assets/_Project/Scripts/Crafting/FactoryWeaponForge.cs` |
| `Assets/scrips/rougelike/FactoryWeaponHit.cs` | `Assets/_Project/Scripts/Crafting/FactoryWeaponHit.cs` |
| `Assets/scrips/rougelike/FactoryWeapons.cs` | `Assets/_Project/Scripts/Crafting/FactoryWeapons.cs` |
| `Assets/scrips/rougelike/GJCrystalWispAttack.cs` | `Assets/_Project/Scripts/Enemies/GJCrystalWispAttack.cs` |
| `Assets/scrips/rougelike/GJCrystalWispBolt.cs` | `Assets/_Project/Scripts/Enemies/GJCrystalWispBolt.cs` |
| `Assets/scrips/rougelike/GJMineSpiderAttack.cs` | `Assets/_Project/Scripts/Enemies/GJMineSpiderAttack.cs` |
| `Assets/scrips/rougelike/GJSpiderMine.cs` | `Assets/_Project/Scripts/Enemies/GJSpiderMine.cs` |
| `Assets/scrips/rougelike/Room.cs` | `Assets/_Project/Scripts/Dungeon/Room.cs` |
| `Assets/scrips/RunManager.cs` | `Assets/_Project/Scripts/Core/RunManager.cs` |
| `Assets/scrips/TextureDirectionSwitcher.cs` | `Assets/_Project/Scripts/Core/TextureDirectionSwitcher.cs` |
| `Assets/prefabs/Chest.prefab` | `Assets/_Project/Prefabs/Chest.prefab` |
| `Assets/prefabs/Enemy_Boss.prefab` | `Assets/_Project/Prefabs/Enemy_Boss.prefab` |
| `Assets/prefabs/GameJamExtras/Prefabs/BaseStorageChest.prefab` | `Assets/_Project/Prefabs/BaseStorageChest.prefab` |
| `Assets/prefabs/iron.prefab` | `Assets/_Project/Prefabs/iron.prefab` |
| `Assets/prefabs/Pickup_armor_plate.prefab` | `Assets/_Project/Prefabs/Pickup_armor_plate.prefab` |
| `Assets/prefabs/Pickup_armor_upgrade.prefab` | `Assets/_Project/Prefabs/Pickup_armor_upgrade.prefab` |
| `Assets/prefabs/Pickup_crystal.prefab` | `Assets/_Project/Prefabs/Pickup_crystal.prefab` |
| `Assets/prefabs/Pickup_health_potion.prefab` | `Assets/_Project/Prefabs/Pickup_health_potion.prefab` |
| `Assets/prefabs/Pickup_iron.prefab` | `Assets/_Project/Prefabs/Pickup_iron.prefab` |
| `Assets/prefabs/Pickup_metal_plate.prefab` | `Assets/_Project/Prefabs/Pickup_metal_plate.prefab` |
| `Assets/prefabs/Pickup_scrap.prefab` | `Assets/_Project/Prefabs/Pickup_scrap.prefab` |
| `Assets/prefabs/Pickup_weapon_part.prefab` | `Assets/_Project/Prefabs/Pickup_weapon_part.prefab` |
| `Assets/prefabs/Pickup_weapon_upgrade.prefab` | `Assets/_Project/Prefabs/Pickup_weapon_upgrade.prefab` |
| `Assets/prefabs/Pickup_wood.prefab` | `Assets/_Project/Prefabs/Pickup_wood.prefab` |
| `Assets/prefabs/player.prefab` | `Assets/_Project/Prefabs/player.prefab` |
| `Assets/prefabs/Projectile.prefab` | `Assets/_Project/Prefabs/Projectile.prefab` |
| `Assets/prefabs/runner.prefab` | `Assets/_Project/Prefabs/runner.prefab` |
| `Assets/prefabs/Slime.prefab` | `Assets/_Project/Prefabs/Slime.prefab` |
| `Assets/prefabs/tank.prefab` | `Assets/_Project/Prefabs/tank.prefab` |
| `Assets/prefabs/tank2.prefab` | `Assets/_Project/Prefabs/tank2.prefab` |
| `Assets/prefabs/wood.prefab` | `Assets/_Project/Prefabs/wood.prefab` |
| `Assets/Scenes/Dungeon.prefab` | `Assets/_Project/Prefabs/Dungeon.prefab` |

## Zostávajúce nálezy Unity

```text
Validated 44 prefabs and 7 scenes. Compilation succeeded for this editor assembly.
Unresolved GUID | Assets/Settings/Renderer2D.asset | e5c6678ed2aaa91408dd3df699057aae
Unresolved GUID | Assets/Settings/Renderer2D.asset | 03cfc4915c15d504a9ed85ecc404e607
Unresolved GUID | Assets/Settings/Renderer2D.asset | 53a11f4ebaebf4049b3638ef78dc9664
Unresolved GUID | Assets/Settings/Renderer2D.asset | 8f96cd657dc40064aa21efcc7e50a2e7
Unresolved GUID | Assets/Settings/Renderer2D.asset | 57d7c4c16e2765b47a4d2069b311bffe
Unresolved GUID | Assets/Settings/Renderer2D.asset | 24ec0e140fb444a44ab96ee80844e18e
Unresolved GUID | Assets/_Project/Prefabs/Dungeon.prefab | 340e8284bc39fca4c9b5ac825e158bdd
Unresolved GUID | Assets/_Project/Prefabs/Dungeon.prefab | 5d93c069aee345b4ebefe212f01c9886
Unresolved GUID | Assets/_Project/Prefabs/Dungeon.prefab | f501ba8cc1b12aa42b26f059ccfc36d1
Unresolved GUID | Assets/_Project/Prefabs/Dungeon.prefab | a06b8f19047219341aa6d73bb4d5cbb5
Missing object | Assets/_Project/Prefabs/Dungeon.prefab | Dungeon | DungeonGenerator.enemyPrefabs.Array.data[0]
Missing object | Assets/_Project/Prefabs/Dungeon.prefab | Dungeon | DungeonGenerator.enemyPrefabs.Array.data[1]
Missing object | Assets/_Project/Prefabs/Dungeon.prefab | Dungeon | DungeonGenerator.enemyPrefabs.Array.data[2]
Missing object | Assets/_Project/Prefabs/Dungeon.prefab | Dungeon | DungeonGenerator.chestPrefab
Unresolved GUID | Assets/_Project/Prefabs/Slime.prefab | 3eb6d84408471394d8d6f729571f86b8
Missing object | Assets/_Project/Prefabs/Slime.prefab | Slime | EnemyLoot.pickupPrefab
Unresolved GUID | Assets/_Project/Prefabs/tank2.prefab | 7a61583ce3d719048a375b86e93c6a4c
Unresolved GUID | Assets/_Project/Prefabs/tank2.prefab | 66846e7b070e27d2a8220b8acf6d78e1
Missing object | Assets/_Project/Prefabs/tank2.prefab | tank2 | SpriteRenderer.m_Sprite
Missing object | Assets/_Project/Prefabs/tank2.prefab | tank2 | SpriteDirectionSwitcher.directionalSprites.Array.data[0]
Missing object | Assets/_Project/Prefabs/tank2.prefab | tank2 | SpriteDirectionSwitcher.directionalSprites.Array.data[1]
Missing object | Assets/_Project/Prefabs/tank2.prefab | tank2 | SpriteDirectionSwitcher.directionalSprites.Array.data[2]
Missing object | Assets/_Project/Prefabs/tank2.prefab | tank2 | SpriteDirectionSwitcher.directionalSprites.Array.data[3]
Unresolved GUID | Assets/Scenes/LevelScene.unity | f7d8dadf6b4b0824ab7135ac49ae1b62
Missing object | Assets/Scenes/LevelScene.unity | BaseStorageChest | Transform.m_CorrespondingSourceObject
Missing object | Assets/Scenes/LevelScene.unity | BaseStorageChest | SpriteRenderer.m_CorrespondingSourceObject
Missing object | Assets/Scenes/LevelScene.unity | BaseStorageChest | BoxCollider2D.m_CorrespondingSourceObject
Missing object | Assets/Scenes/LevelScene.unity | BaseStorageChest | BaseStorageChest.m_CorrespondingSourceObject
Missing object | Assets/Scenes/LevelScene.unity | Slime | EnemyLoot.pickupPrefab
Missing object | Assets/Scenes/SampleScene.unity | Slime | EnemyLoot.pickupPrefab
Unresolved GUID | Assets/_Recovery/0.unity | 482c6f40f97c1694b96b210e7b22d116
Missing object | Assets/_Recovery/0.unity | Dungeon | DungeonGenerator.enemyPrefabs.Array.data[0]
Missing object | Assets/_Recovery/0.unity | Slime (Missing Prefab with guid: 482c6f40f97c1694b96b210e7b22d116) | Transform.m_CorrespondingSourceObject
Unresolved GUID | Assets/Scenes/test.unity | f7d8dadf6b4b0824ab7135ac49ae1b62
Unresolved GUID | Assets/Scenes/test.unity | 0c13aa492f70a854d9bb024e3c2ba5cd
Unresolved GUID | Assets/Scenes/test.unity | b1cd2f22dc0c14a6c856d8a916d74dd4
Missing object | Assets/Scenes/test.unity | Dungeon | DungeonGenerator.enemyPrefabs.Array.data[0]
Missing object | Assets/Scenes/test.unity | Dungeon | DungeonGenerator.enemyPrefabs.Array.data[1]
Missing object | Assets/Scenes/test.unity | Dungeon | DungeonGenerator.enemyPrefabs.Array.data[2]
Missing object | Assets/Scenes/test.unity | Dungeon | DungeonGenerator.chestPrefab
Missing object | Assets/Scenes/test.unity | Slime | EnemyLoot.pickupPrefab
Missing object | Assets/Scenes/test.unity | BaseStorageChest (Missing Prefab with guid: f7d8dadf6b4b0824ab7135ac49ae1b62) | Transform.m_CorrespondingSourceObject
Missing scripts | Assets/Scenes/test.unity | player | 1
Missing object | Assets/Scenes/test.unity | npc | SpriteRenderer.m_Sprite
Unresolved GUID | Assets/_Recovery/0 (1).unity | f7d8dadf6b4b0824ab7135ac49ae1b62
Missing object | Assets/_Recovery/0 (1).unity | BaseStorageChest | Transform.m_CorrespondingSourceObject
Missing object | Assets/_Recovery/0 (1).unity | BaseStorageChest | SpriteRenderer.m_CorrespondingSourceObject
Missing object | Assets/_Recovery/0 (1).unity | BaseStorageChest | BoxCollider2D.m_CorrespondingSourceObject
Missing object | Assets/_Recovery/0 (1).unity | BaseStorageChest | BaseStorageChest.m_CorrespondingSourceObject
Missing object | Assets/_Recovery/0 (1).unity | Slime | EnemyLoot.pickupPrefab
```
