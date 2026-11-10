# PNG konsolidácia — REFACTORY

Všetkých **109 PNG** je teraz priamo v `Assets/_Project/Art/Textures/`, bez ďalších podpriečinkov. Presunutých bolo posledných 28 PNG vrátane EmojiOne používaného TextMesh Pro. Obsah týchto PNG a ich `.meta` súborov zostal pri kontrole bajtovo identický; GUID, slicing, pivoty a PPU sa zachovali.

## Načítavanie spriteov

`Assets/Resources/RefactorySpriteCatalog.asset` obsahuje 27 priamych serializovaných odkazov na herné sprity. V Resources zostal iba tento malý katalóg spolu s pôvodnými JSON, audio a prefab assetmi; nie sú tam žiadne PNG. Unity zahrnie referencované textúry do buildu cez závislosti katalógu.

Nový `RefactorySpriteCatalog.Load(key)` používa pôvodné logické kľúče (napríklad `DungeonTiles/chest_closed`), ktoré už nie sú cestami k súborom. Katalóg sa načíta raz a lookup sa cachuje. Cache sa resetuje pri každom spustení hry, aj keď je vypnutý domain reload. Neznámy kľúč vracia null ako pôvodné Resources.Load<Sprite>.

Pri pridaní novej ikony alebo dlaždice umiestni PNG do centrálneho priečinka, nastav import na Sprite a pridaj zodpovedajúci kľúč a Sprite do zoznamu Entries na assete RefactorySpriteCatalog. Pri ikone použi `ItemIcons/<itemId>`, pri dlaždici `DungeonTiles/<názov>`. Pri presune alebo premenovaní PNG sa odkaz zachová cez GUID. Ak textúra obsahuje viac spriteov, vyber konkrétny sub-sprite.

## Upravené skripty

- `Assets/_Project/Scripts/Dungeon/DungeonBalanceAndDetails.cs`
- `Assets/_Project/Scripts/Dungeon/DungeonGenerator.cs`
- `Assets/_Project/Scripts/Dungeon/DungeonRoomExtras.cs`
- `Assets/_Project/Scripts/Dungeon/LootChest.cs`
- `Assets/_Project/Scripts/Factory/BaseRoomBuilder.cs`
- `Assets/_Project/Scripts/Inventory/GameJamPickupVisual.cs`

- Nový `Assets/_Project/Scripts/Core/RefactorySpriteCatalog.cs`.
- `Assets/Editor/REFACTORYChestAudioSetup011.cs`: nové cesty ku closed/open chest PNG.
- `Assets/Editor/RefactoryCleanupTools.cs`: validácia katalógu namiesto starých adresárov.

Jednorazový migračný editorový skript bol po overení odstránený z Assets a archivovaný v CleanupEvidence. Presuny vykonal AssetDatabase.MoveAsset. TextMesh Pro sprite asset používa zachovanú GUID referenciu na EmojiOne; nebol prepisovaný.

## Kontroly

- Počet PNG mimo centrálneho priečinka: **0**.
- Všetkých 27 nových lookupov vrátilo presne ten istý Sprite objekt ako pôvodné Resources.Load pred presunom.
- Všetkých 28 presunutých PNG aj `.meta` súborov sa zhoduje so zálohou.
- Unity kompilácia prešla; pôvodné upozornenie na nepoužité pole PauseMenu.mainMenuSceneName ostalo.
- Kontrola 44 prefabov a 7 scén: 50 pôvodných nálezov, **0 nových**.
- Scény, ProjectSettings a Packages sú nezmenené.
- Play Mode smoke test LevelScene prešiel: základňa, postava, stroje, HUD a vygenerovanie 10 miestností. V úseku logu tohto testu neboli výnimky ani chyby katalógu/chýbajúcich ikon. Pôvodné upozornenia Sprite Tiling/Full Rect zostali.
- Samostatný build a kompletný gameplay test neboli vykonané.

## Presuny

| Pôvodná cesta | Nová cesta | Kľúč katalógu |
|---|---|---|
| `Assets/TextMesh Pro/Sprites/EmojiOne.png` | `Assets/_Project/Art/Textures/EmojiOne.png` | priama TMP referencia |
| `Assets/Resources/DungeonTiles/background_void.png` | `Assets/_Project/Art/Textures/background_void.png` | `DungeonTiles/background_void` |
| `Assets/Resources/DungeonTiles/chest_closed.png` | `Assets/_Project/Art/Textures/chest_closed_86b9f351.png` | `DungeonTiles/chest_closed` |
| `Assets/Resources/DungeonTiles/chest_open.png` | `Assets/_Project/Art/Textures/chest_open_f134034b.png` | `DungeonTiles/chest_open` |
| `Assets/Resources/DungeonTiles/door_wood.png` | `Assets/_Project/Art/Textures/door_wood.png` | `DungeonTiles/door_wood` |
| `Assets/Resources/DungeonTiles/factory_floor.png` | `Assets/_Project/Art/Textures/factory_floor_fd21a7a6.png` | `DungeonTiles/factory_floor` |
| `Assets/Resources/DungeonTiles/floor_ambush.png` | `Assets/_Project/Art/Textures/floor_ambush.png` | `DungeonTiles/floor_ambush` |
| `Assets/Resources/DungeonTiles/floor_danger.png` | `Assets/_Project/Art/Textures/floor_danger.png` | `DungeonTiles/floor_danger` |
| `Assets/Resources/DungeonTiles/floor_moss.png` | `Assets/_Project/Art/Textures/floor_moss.png` | `DungeonTiles/floor_moss` |
| `Assets/Resources/DungeonTiles/floor_ruins.png` | `Assets/_Project/Art/Textures/floor_ruins.png` | `DungeonTiles/floor_ruins` |
| `Assets/Resources/DungeonTiles/floor_stone.png` | `Assets/_Project/Art/Textures/floor_stone.png` | `DungeonTiles/floor_stone` |
| `Assets/Resources/DungeonTiles/floor_supply.png` | `Assets/_Project/Art/Textures/floor_supply.png` | `DungeonTiles/floor_supply` |
| `Assets/Resources/DungeonTiles/floor_treasure.png` | `Assets/_Project/Art/Textures/floor_treasure.png` | `DungeonTiles/floor_treasure` |
| `Assets/Resources/DungeonTiles/prop_crate.png` | `Assets/_Project/Art/Textures/prop_crate_a04af61d.png` | `DungeonTiles/prop_crate` |
| `Assets/Resources/DungeonTiles/prop_lamp.png` | `Assets/_Project/Art/Textures/prop_lamp.png` | `DungeonTiles/prop_lamp` |
| `Assets/Resources/DungeonTiles/prop_pillar.png` | `Assets/_Project/Art/Textures/prop_pillar.png` | `DungeonTiles/prop_pillar` |
| `Assets/Resources/DungeonTiles/trap_spikes.png` | `Assets/_Project/Art/Textures/trap_spikes.png` | `DungeonTiles/trap_spikes` |
| `Assets/Resources/DungeonTiles/wall_stone.png` | `Assets/_Project/Art/Textures/wall_stone_f3465059.png` | `DungeonTiles/wall_stone` |
| `Assets/Resources/ItemIcons/armor_plate.png` | `Assets/_Project/Art/Textures/armor_plate.png` | `ItemIcons/armor_plate` |
| `Assets/Resources/ItemIcons/armor_upgrade.png` | `Assets/_Project/Art/Textures/armor_upgrade.png` | `ItemIcons/armor_upgrade` |
| `Assets/Resources/ItemIcons/crystal.png` | `Assets/_Project/Art/Textures/crystal.png` | `ItemIcons/crystal` |
| `Assets/Resources/ItemIcons/health_potion.png` | `Assets/_Project/Art/Textures/health_potion.png` | `ItemIcons/health_potion` |
| `Assets/Resources/ItemIcons/iron.png` | `Assets/_Project/Art/Textures/iron.png` | `ItemIcons/iron` |
| `Assets/Resources/ItemIcons/metal_plate.png` | `Assets/_Project/Art/Textures/metal_plate.png` | `ItemIcons/metal_plate` |
| `Assets/Resources/ItemIcons/scrap.png` | `Assets/_Project/Art/Textures/scrap.png` | `ItemIcons/scrap` |
| `Assets/Resources/ItemIcons/weapon_part.png` | `Assets/_Project/Art/Textures/weapon_part.png` | `ItemIcons/weapon_part` |
| `Assets/Resources/ItemIcons/weapon_upgrade.png` | `Assets/_Project/Art/Textures/weapon_upgrade.png` | `ItemIcons/weapon_upgrade` |
| `Assets/Resources/ItemIcons/wood.png` | `Assets/_Project/Art/Textures/wood.png` | `ItemIcons/wood` |
