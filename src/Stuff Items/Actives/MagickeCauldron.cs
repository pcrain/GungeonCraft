using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text;
using System.Reflection;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class MagickeCauldron : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Alchemy Crucible";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/magickecauldron_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<MagickeCauldron>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Philosopher's Own";
            string longDesc = "Magically changes enemies into other enemies from the same floor." + "\nBest used on strong enemies, where the only way forwards is down." + "\n\nDon't get used to yourself. You're gonna have to change.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 150);
            //ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 150);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            //ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.C;
        }

        public override void DoEffect(PlayerController user)
        {
            try
            {
                List<string> guidsOnFloor = FloorAndGenerationToolbox.CurrentFloorEnemyPalette;

                List<AIActor> activeEnemies = user.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    enemiesToReRoll.Clear();
                    enemiesToPostMogModify.Clear();
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        AIActor aiactor = activeEnemies[i];
                        enemiesToReRoll.Add(aiactor);
                    }
                    foreach (AIActor enemy in enemiesToReRoll)
                    {
                        string guidToTransmogTo = "01972dee89fc4404a5c408d50007dad5";
                        if (guidsOnFloor.Count > 0) guidToTransmogTo = BraveUtility.RandomElement(guidsOnFloor);
                        if (user.PlayerHasActiveSynergy("I CAN DO ANYTHING"))
                        {
                            List<string> newChaosPalette = GenerateChaosPalette();
                            guidToTransmogTo = BraveUtility.RandomElement(newChaosPalette);
                        }

                        //Prevent Explodeys from Exploding
                        if (enemy.gameObject.GetComponent<ExplodeOnDeath>())
                        {
                            Destroy(enemy.gameObject.GetComponent<ExplodeOnDeath>());
                        }
                        if (!enemy.healthHaver.IsVulnerable) enemy.healthHaver.IsVulnerable = true;
                        //ACTUALLY DO THE TRANSMOGGING
                        enemy.Transmogrify(EnemyDatabase.GetOrLoadByGuid(guidToTransmogTo), (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
                    }
                    List<AIActor> activeEnemies2 = user.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                    if (activeEnemies2 != null)
                    {
                        for (int i = 0; i < activeEnemies2.Count; i++)
                        {
                            AIActor aiactor = activeEnemies2[i];
                            enemiesToPostMogModify.Add(aiactor);
                            if (aiactor.IsTransmogrified)
                            {
                                if (user.PlayerHasActiveSynergy("A Moste Accursed Brew"))
                                {
                                    aiactor.ApplyEffect(this.WeakenedDebuff, 1f, null);
                                }
                            }
                        }
                    }
                    foreach (AIActor enemy in enemiesToPostMogModify) HandlePostTransmogLootEnemies(enemy);

                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        public static List<string> GenerateChaosPalette()
        {
            List<string> ChaosPalette = new List<string>();
            ChaosPalette.AddRange(chaosEnemyPalette);
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ApacheThunder.etg.ExpandTheGungeon")) ChaosPalette.AddRange(ExpandTheGungeonChaosPalette);
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("kp.etg.frostandgunfire")) ChaosPalette.AddRange(FrostAndGunfireChaosPalette);
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("somebunny.etg.planetsideofgunymede")) ChaosPalette.AddRange(PlanetsideOfGunymedeChaosPalette);
            return ChaosPalette;
        }
        public void HandlePostTransmogLootEnemies(AIActor enemy)
        {
            try
            {
                if (enemy.EnemyGuid == EnemyGuidDatabase.Entries["spent"])
                {
                    if (enemy.GetComponent<SpawnEnemyOnDeath>()) Destroy(enemy.GetComponent<SpawnEnemyOnDeath>());
                }
                if (enemy.IsTransmogrified) enemy.IsTransmogrified = false;
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        public AIActorDebuffEffect WeakenedDebuff = new AIActorDebuffEffect
        {
            HealthMultiplier = 0.5f,
            CooldownMultiplier = 0.5f,
            SpeedMultiplier = 0.8f,
            KeepHealthPercentage = true,
            OverheadVFX = MagickeCauldron.overheadder,
            duration = 1000000f
        };
        public static GameObject overheadder = ResourceCache.Acquire("Global VFX/VFX_Debuff_Status") as GameObject;

        public static List<AIActor> enemiesToReRoll = new List<AIActor>();
        public static List<AIActor> enemiesToPostMogModify = new List<AIActor>();

        //All the older lists used in the past versions of the crucible
        //They're an awful mess, but I'm not deleting them in case I ever need them
        #region OLDLISTS
        /*  public List<string> FetchCurrentPalette(bool useChaos)
          {
              List<string> midCheckList = new List<string>();
              if (!useChaos)
              {
                  bool tilesetMatchesKnownTilesets = false;
                  switch (GameManager.Instance.Dungeon.tileIndices.tilesetId)
                  {
                      case GlobalDungeonData.ValidTilesets.CASTLEGEON:
                          midCheckList.AddRange(keepEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.SEWERGEON:
                          midCheckList.AddRange(oubEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.GUNGEON:
                          midCheckList.AddRange(gpEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.CATHEDRALGEON:
                          midCheckList.AddRange(abbeyEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.MINEGEON:
                          midCheckList.AddRange(minesEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.RATGEON:
                          midCheckList.AddRange(ratEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.CATACOMBGEON:
                          midCheckList.AddRange(hollowEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.OFFICEGEON:
                          midCheckList.AddRange(rngEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.FORGEGEON:
                          midCheckList.AddRange(forgeEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.HELLGEON:
                          midCheckList.AddRange(hellEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.JUNGLEGEON:
                          midCheckList.AddRange(jungleEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.BELLYGEON:
                          midCheckList.AddRange(bellyEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                      case GlobalDungeonData.ValidTilesets.PHOBOSGEON:
                          midCheckList.AddRange(keepEnemyPalette);
                          tilesetMatchesKnownTilesets = true;
                          break;
                  }
                  if (!tilesetMatchesKnownTilesets) midCheckList.AddRange(gpEnemyPalette);
              }
              else
              {
                  midCheckList.AddRange(chaosEnemyPalette);
              }
              return midCheckList;
          }
          public static List<string> keepEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["bullet_kin"],
              EnemyGuidDatabase.Entries["ak47_bullet_kin"],
              EnemyGuidDatabase.Entries["bandana_bullet_kin"],
              EnemyGuidDatabase.Entries["veteran_bullet_kin"],
              EnemyGuidDatabase.Entries["red_shotgun_kin"],
              EnemyGuidDatabase.Entries["blue_shotgun_kin"],
              EnemyGuidDatabase.Entries["hollowpoint"],
              EnemyGuidDatabase.Entries["rubber_kin"],
              EnemyGuidDatabase.Entries["grenade_kin"],
              EnemyGuidDatabase.Entries["blobulon"],
              EnemyGuidDatabase.Entries["blobuloid"],
              EnemyGuidDatabase.Entries["blobulin"],
              EnemyGuidDatabase.Entries["bookllet"],
              EnemyGuidDatabase.Entries["blue_bookllet"],
              EnemyGuidDatabase.Entries["green_bookllet"],
              EnemyGuidDatabase.Entries["gigi"],
              EnemyGuidDatabase.Entries["apprentice_gunjurer"],
              EnemyGuidDatabase.Entries["gunsinger"],
              EnemyGuidDatabase.Entries["bullat"],
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["grenat"],
              EnemyGuidDatabase.Entries["spirat"],
              EnemyGuidDatabase.Entries["gun_nut"],
              EnemyGuidDatabase.Entries["chain_gunner"],
          };
          public static List<string> oubEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["bullet_kin"],
              EnemyGuidDatabase.Entries["ak47_bullet_kin"],
              EnemyGuidDatabase.Entries["bandana_bullet_kin"],
              EnemyGuidDatabase.Entries["veteran_bullet_kin"],
              EnemyGuidDatabase.Entries["mutant_bullet_kin"],
              EnemyGuidDatabase.Entries["shroomer"],
              EnemyGuidDatabase.Entries["red_shotgun_kin"],
              EnemyGuidDatabase.Entries["blue_shotgun_kin"],
              EnemyGuidDatabase.Entries["veteran_shotgun_kin"],
              EnemyGuidDatabase.Entries["mutant_shotgun_kin"],
              EnemyGuidDatabase.Entries["sniper_shell"],
              EnemyGuidDatabase.Entries["professional"],
              EnemyGuidDatabase.Entries["hollowpoint"],
              EnemyGuidDatabase.Entries["king_bullat"],
              EnemyGuidDatabase.Entries["blobulon"],
              EnemyGuidDatabase.Entries["blobuloid"],
              EnemyGuidDatabase.Entries["blobulin"],
              EnemyGuidDatabase.Entries["poisbulon"],
              EnemyGuidDatabase.Entries["poisbuloid"],
              EnemyGuidDatabase.Entries["poisbulin"],
              EnemyGuidDatabase.Entries["poopulon"],
              EnemyGuidDatabase.Entries["gigi"],
              EnemyGuidDatabase.Entries["cubulon"],
              EnemyGuidDatabase.Entries["bullat"],
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["grenat"],
              EnemyGuidDatabase.Entries["spirat"],
              EnemyGuidDatabase.Entries["gat"],
              EnemyGuidDatabase.Entries["gunzookie"],
              EnemyGuidDatabase.Entries["gunzockie"],
              EnemyGuidDatabase.Entries["fungun"],
              EnemyGuidDatabase.Entries["spogre"],
          };
          public static List<string> gpEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["bullet_kin"], //yes
              EnemyGuidDatabase.Entries["ak47_bullet_kin"], //yes
              EnemyGuidDatabase.Entries["bandana_bullet_kin"], //yes
              EnemyGuidDatabase.Entries["veteran_bullet_kin"], //yes
              EnemyGuidDatabase.Entries["red_shotgun_kin"], //yes
              EnemyGuidDatabase.Entries["blue_shotgun_kin"], //yes
              EnemyGuidDatabase.Entries["veteran_shotgun_kin"], //yes
              EnemyGuidDatabase.Entries["sniper_shell"], //yes
              EnemyGuidDatabase.Entries["professional"], //yes
              EnemyGuidDatabase.Entries["hollowpoint"], //yes
              EnemyGuidDatabase.Entries["rubber_kin"], //yes
              EnemyGuidDatabase.Entries["tazie"], //yes
              EnemyGuidDatabase.Entries["king_bullat"], //yes
              EnemyGuidDatabase.Entries["grenade_kin"], //yes
              EnemyGuidDatabase.Entries["dynamite_kin"], //yes
              EnemyGuidDatabase.Entries["blobulon"],
              EnemyGuidDatabase.Entries["blobuloid"],
              EnemyGuidDatabase.Entries["blobulin"],
              EnemyGuidDatabase.Entries["bookllet"],
              EnemyGuidDatabase.Entries["blue_bookllet"],
              EnemyGuidDatabase.Entries["green_bookllet"],
              EnemyGuidDatabase.Entries["gigi"],
              EnemyGuidDatabase.Entries["muzzle_wisp"], //yes
              EnemyGuidDatabase.Entries["cubulon"], //yes
              EnemyGuidDatabase.Entries["chancebulon"], //yes
              EnemyGuidDatabase.Entries["apprentice_gunjurer"],
              EnemyGuidDatabase.Entries["gunsinger"],
              EnemyGuidDatabase.Entries["aged_gunsinger"],
              EnemyGuidDatabase.Entries["bullat"],
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["grenat"],
              EnemyGuidDatabase.Entries["spirat"],
              EnemyGuidDatabase.Entries["bullet_shark"],
              EnemyGuidDatabase.Entries["misfire_beast"],
              EnemyGuidDatabase.Entries["phaser_spider"],
              EnemyGuidDatabase.Entries["grip_master"],
              EnemyGuidDatabase.Entries["gun_nut"],
              EnemyGuidDatabase.Entries["lead_maiden"],
              EnemyGuidDatabase.Entries["beadie"],
              EnemyGuidDatabase.Entries["executioner"],
          };
          public static List<string> abbeyEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["bullet_kin"],
              EnemyGuidDatabase.Entries["veteran_bullet_kin"],
              EnemyGuidDatabase.Entries["cardinal"],
              EnemyGuidDatabase.Entries["red_shotgun_kin"],
              EnemyGuidDatabase.Entries["blue_shotgun_kin"],
              EnemyGuidDatabase.Entries["skullet"],
              EnemyGuidDatabase.Entries["hollowpoint"],
              EnemyGuidDatabase.Entries["king_bullat"],
              EnemyGuidDatabase.Entries["grenade_kin"],
              EnemyGuidDatabase.Entries["poopulon"],
              EnemyGuidDatabase.Entries["bloodbulon"],
              EnemyGuidDatabase.Entries["skusket"],
              EnemyGuidDatabase.Entries["bookllet"],
              EnemyGuidDatabase.Entries["blue_bookllet"],
              EnemyGuidDatabase.Entries["green_bookllet"],
              EnemyGuidDatabase.Entries["muzzle_wisp"],
              EnemyGuidDatabase.Entries["cubulon"],
              EnemyGuidDatabase.Entries["apprentice_gunjurer"],
              EnemyGuidDatabase.Entries["gunjurer"],
              EnemyGuidDatabase.Entries["aged_gunsinger"],
              EnemyGuidDatabase.Entries["wizbang"],
              EnemyGuidDatabase.Entries["bullat"],
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["grenat"],
              EnemyGuidDatabase.Entries["spirat"],
              EnemyGuidDatabase.Entries["bullet_shark"],
              EnemyGuidDatabase.Entries["gun_nut"],
              EnemyGuidDatabase.Entries["lead_maiden"],
              EnemyGuidDatabase.Entries["gun_cultist"],
          };
          public static List<string> minesEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["bullet_kin"],
              EnemyGuidDatabase.Entries["bandana_bullet_kin"],
              EnemyGuidDatabase.Entries["veteran_bullet_kin"],
              EnemyGuidDatabase.Entries["treadnaughts_bullet_kin"],
              EnemyGuidDatabase.Entries["minelet"],
              EnemyGuidDatabase.Entries["red_shotgun_kin"],
              EnemyGuidDatabase.Entries["blue_shotgun_kin"],
              EnemyGuidDatabase.Entries["veteran_shotgun_kin"],
              EnemyGuidDatabase.Entries["sniper_shell"],
              EnemyGuidDatabase.Entries["professional"],
              EnemyGuidDatabase.Entries["hollowpoint"],
              EnemyGuidDatabase.Entries["bombshee"],
              EnemyGuidDatabase.Entries["rubber_kin"],
              EnemyGuidDatabase.Entries["tazie"],
              EnemyGuidDatabase.Entries["king_bullat"],
              EnemyGuidDatabase.Entries["grenade_kin"],
              EnemyGuidDatabase.Entries["dynamite_kin"],
              EnemyGuidDatabase.Entries["blobulon"],
              EnemyGuidDatabase.Entries["blobuloid"],
              EnemyGuidDatabase.Entries["blobulin"],
              EnemyGuidDatabase.Entries["poisbulon"],
              EnemyGuidDatabase.Entries["poisbuloid"],
              EnemyGuidDatabase.Entries["poisbulin"],
              EnemyGuidDatabase.Entries["skusket"],
              EnemyGuidDatabase.Entries["gigi"],
              EnemyGuidDatabase.Entries["muzzle_wisp"],
              EnemyGuidDatabase.Entries["cubulon"],
              EnemyGuidDatabase.Entries["apprentice_gunjurer"],
              EnemyGuidDatabase.Entries["high_gunjurer"],
              EnemyGuidDatabase.Entries["gunsinger"],
              EnemyGuidDatabase.Entries["aged_gunsinger"],
              EnemyGuidDatabase.Entries["jammomancer"],
              EnemyGuidDatabase.Entries["jamerlengo"],
              EnemyGuidDatabase.Entries["bullat"],
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["grenat"],
              EnemyGuidDatabase.Entries["spirat"],
              EnemyGuidDatabase.Entries["coaler"],
              EnemyGuidDatabase.Entries["gat"],
              EnemyGuidDatabase.Entries["diagonal_det"],
              EnemyGuidDatabase.Entries["diagonal_x_det"],
              EnemyGuidDatabase.Entries["gunzookie"],
              EnemyGuidDatabase.Entries["gunzockie"],
              EnemyGuidDatabase.Entries["bullet_shark"],
              EnemyGuidDatabase.Entries["chancebulon"],
              EnemyGuidDatabase.Entries["misfire_beast"],
              EnemyGuidDatabase.Entries["gun_nut"],
              EnemyGuidDatabase.Entries["fungun"],
              EnemyGuidDatabase.Entries["spogre"],
              EnemyGuidDatabase.Entries["grip_master"],
              EnemyGuidDatabase.Entries["phaser_spider"],
              EnemyGuidDatabase.Entries["killithid"],
              EnemyGuidDatabase.Entries["tarnisher"],
              EnemyGuidDatabase.Entries["shambling_round"],
              EnemyGuidDatabase.Entries["shelleton"],
          };
          public static List<string> ratEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["chance_bullet_kin"],
              EnemyGuidDatabase.Entries["red_shotgun_kin"],
              EnemyGuidDatabase.Entries["blue_shotgun_kin"],
              EnemyGuidDatabase.Entries["veteran_shotgun_kin"],
              EnemyGuidDatabase.Entries["mutant_shotgun_kin"],
              EnemyGuidDatabase.Entries["ashen_shotgun_kin"],
              EnemyGuidDatabase.Entries["skullet"],
              EnemyGuidDatabase.Entries["creech"],
              EnemyGuidDatabase.Entries["grenade_kin"],
              EnemyGuidDatabase.Entries["cubulon"],
              EnemyGuidDatabase.Entries["chancebulon"],
              EnemyGuidDatabase.Entries["ammomancer"],
              EnemyGuidDatabase.Entries["jammomancer"],
              EnemyGuidDatabase.Entries["jamerlengo"],
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["lead_maiden"],
              EnemyGuidDatabase.Entries["misfire_beast"],
              EnemyGuidDatabase.Entries["phaser_spider"],
              EnemyGuidDatabase.Entries["killithid"],
              EnemyGuidDatabase.Entries["shelleton"],
              EnemyGuidDatabase.Entries["gunreaper"],
              EnemyGuidDatabase.Entries["mouser"],
              EnemyGuidDatabase.Entries["mouser"],
              EnemyGuidDatabase.Entries["mouser"],
              EnemyGuidDatabase.Entries["mouser"],
              EnemyGuidDatabase.Entries["rat"],
              EnemyGuidDatabase.Entries["rat_candle"],
          };
          public static List<string> hollowEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["bullet_kin"],
              EnemyGuidDatabase.Entries["ak47_bullet_kin"],
              EnemyGuidDatabase.Entries["bandana_bullet_kin"],
              EnemyGuidDatabase.Entries["veteran_bullet_kin"],
              EnemyGuidDatabase.Entries["cardinal"],
              EnemyGuidDatabase.Entries["red_shotgun_kin"],
              EnemyGuidDatabase.Entries["blue_shotgun_kin"],
              EnemyGuidDatabase.Entries["veteran_shotgun_kin"],
              EnemyGuidDatabase.Entries["sniper_shell"],
              EnemyGuidDatabase.Entries["professional"],
              EnemyGuidDatabase.Entries["gummy"],
              EnemyGuidDatabase.Entries["skullet"],
              EnemyGuidDatabase.Entries["skullmet"],
              EnemyGuidDatabase.Entries["hollowpoint"],
              EnemyGuidDatabase.Entries["king_bullat"],
              EnemyGuidDatabase.Entries["grenade_kin"],
              EnemyGuidDatabase.Entries["dynamite_kin"],
              EnemyGuidDatabase.Entries["blobulon"], //yes v
              EnemyGuidDatabase.Entries["blobuloid"],
              EnemyGuidDatabase.Entries["blobulin"],
              EnemyGuidDatabase.Entries["poisbulon"],
              EnemyGuidDatabase.Entries["poisbuloid"],
              EnemyGuidDatabase.Entries["poisbulin"], //yes ^
              EnemyGuidDatabase.Entries["blizzbulon"], //yes
              EnemyGuidDatabase.Entries["bloodbulon"], //yes
              EnemyGuidDatabase.Entries["skusket"], //yes
              EnemyGuidDatabase.Entries["cubulon"],
              EnemyGuidDatabase.Entries["chancebulon"],
              EnemyGuidDatabase.Entries["apprentice_gunjurer"], //yes
              EnemyGuidDatabase.Entries["gunjurer"], //yes
              EnemyGuidDatabase.Entries["high_gunjurer"], //yes
              EnemyGuidDatabase.Entries["lore_gunjurer"], //yes
              EnemyGuidDatabase.Entries["gunsinger"],
              EnemyGuidDatabase.Entries["aged_gunsinger"],
              EnemyGuidDatabase.Entries["ammomancer"],
              EnemyGuidDatabase.Entries["wizbang"],
              EnemyGuidDatabase.Entries["bullat"], //yes v
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["grenat"],
              EnemyGuidDatabase.Entries["spirat"], //yes ^
              EnemyGuidDatabase.Entries["mountain_cube"],
              EnemyGuidDatabase.Entries["gun_nut"],
              EnemyGuidDatabase.Entries["grip_master"],
              EnemyGuidDatabase.Entries["misfire_beast"],
              EnemyGuidDatabase.Entries["phaser_spider"],
              EnemyGuidDatabase.Entries["killithid"], //yes
              EnemyGuidDatabase.Entries["shelleton"], //yes
              EnemyGuidDatabase.Entries["agonizer"], //yes
              EnemyGuidDatabase.Entries["gunreaper"], //yes
          };
          public static List<string> rngEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["office_bullet_kin"],
              EnemyGuidDatabase.Entries["office_bullette_kin"],
              EnemyGuidDatabase.Entries["brollet"],
              EnemyGuidDatabase.Entries["western_bullet_kin"],
              EnemyGuidDatabase.Entries["pirate_bullet_kin"],
              EnemyGuidDatabase.Entries["armored_bullet_kin"],
              EnemyGuidDatabase.Entries["western_shotgun_kin"],
              EnemyGuidDatabase.Entries["pirate_shotgun_kin"],
              EnemyGuidDatabase.Entries["gargoyle"],
              EnemyGuidDatabase.Entries["necronomicon"],
              EnemyGuidDatabase.Entries["tablet_bookllett"],
              EnemyGuidDatabase.Entries["grey_cylinder"],
              EnemyGuidDatabase.Entries["red_cylinder"],
              EnemyGuidDatabase.Entries["bullet_mech"],
              EnemyGuidDatabase.Entries["m80_kin"],
              EnemyGuidDatabase.Entries["candle_kin"],
              EnemyGuidDatabase.Entries["western_cactus"],
              EnemyGuidDatabase.Entries["musketball"],
              EnemyGuidDatabase.Entries["bird_parrot"],
              EnemyGuidDatabase.Entries["western_snake"],
              EnemyGuidDatabase.Entries["kalibullet"],
              EnemyGuidDatabase.Entries["kbullet"],
              EnemyGuidDatabase.Entries["blue_fish_bullet_kin"],
              EnemyGuidDatabase.Entries["green_fish_bullet_kin"],
              EnemyGuidDatabase.Entries["fridge_maiden"],
              EnemyGuidDatabase.Entries["titan_bullet_kin"],
              EnemyGuidDatabase.Entries["titan_bullet_kin_boss"],
              EnemyGuidDatabase.Entries["titaness_bullet_kin_boss"],
          };
          public static List<string> forgeEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["bullet_kin"],
              EnemyGuidDatabase.Entries["ak47_bullet_kin"],
              EnemyGuidDatabase.Entries["bandana_bullet_kin"],
              EnemyGuidDatabase.Entries["veteran_bullet_kin"],
              EnemyGuidDatabase.Entries["shroomer"],
              EnemyGuidDatabase.Entries["ashen_bullet_kin"],
              EnemyGuidDatabase.Entries["red_shotgun_kin"],
              EnemyGuidDatabase.Entries["blue_shotgun_kin"],
              EnemyGuidDatabase.Entries["veteran_shotgun_kin"],
              EnemyGuidDatabase.Entries["sniper_shell"],
              EnemyGuidDatabase.Entries["professional"],
              EnemyGuidDatabase.Entries["hollowpoint"],
              EnemyGuidDatabase.Entries["king_bullat"],
              EnemyGuidDatabase.Entries["grenade_kin"],
              EnemyGuidDatabase.Entries["dynamite_kin"],
              EnemyGuidDatabase.Entries["blobulon"],
              EnemyGuidDatabase.Entries["blobuloid"],
              EnemyGuidDatabase.Entries["blobulin"],
              EnemyGuidDatabase.Entries["leadbulon"],
              EnemyGuidDatabase.Entries["muzzle_wisp"],
              EnemyGuidDatabase.Entries["muzzle_flare"],
              EnemyGuidDatabase.Entries["cubulon"],
              EnemyGuidDatabase.Entries["chancebulon"],
              EnemyGuidDatabase.Entries["cubulead"],
              EnemyGuidDatabase.Entries["apprentice_gunjurer"], //yes
              EnemyGuidDatabase.Entries["gunjurer"], //yes
              EnemyGuidDatabase.Entries["high_gunjurer"], //yes
              EnemyGuidDatabase.Entries["lore_gunjurer"], //yes
              EnemyGuidDatabase.Entries["gunsinger"],
              EnemyGuidDatabase.Entries["aged_gunsinger"],
              EnemyGuidDatabase.Entries["ammomancer"],
              EnemyGuidDatabase.Entries["bullat"], //yes v
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["grenat"],
              EnemyGuidDatabase.Entries["spirat"],
              EnemyGuidDatabase.Entries["wizbang"],
              EnemyGuidDatabase.Entries["phaser_spider"],
              EnemyGuidDatabase.Entries["tarnisher"],
              EnemyGuidDatabase.Entries["coaler"],
              EnemyGuidDatabase.Entries["great_bullet_shark"],
              EnemyGuidDatabase.Entries["gun_cultist"],
              EnemyGuidDatabase.Entries["grip_master"],
              EnemyGuidDatabase.Entries["gun_nut"],
              EnemyGuidDatabase.Entries["spectral_gun_nut"],
              EnemyGuidDatabase.Entries["lead_cube"],
              EnemyGuidDatabase.Entries["lead_maiden"],
              EnemyGuidDatabase.Entries["shelleton"],
              EnemyGuidDatabase.Entries["agonizer"],
              EnemyGuidDatabase.Entries["revolvenant"],
          };
          public static List<string> hellEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["bullet_kin"],
              EnemyGuidDatabase.Entries["ak47_bullet_kin"],
              EnemyGuidDatabase.Entries["bandana_bullet_kin"],
              EnemyGuidDatabase.Entries["veteran_bullet_kin"],
              EnemyGuidDatabase.Entries["cardinal"],
              EnemyGuidDatabase.Entries["shroomer"],
              EnemyGuidDatabase.Entries["ashen_bullet_kin"],
              EnemyGuidDatabase.Entries["mutant_bullet_kin"],
              EnemyGuidDatabase.Entries["fallen_bullet_kin"],
              EnemyGuidDatabase.Entries["red_shotgun_kin"],
              EnemyGuidDatabase.Entries["blue_shotgun_kin"],
              EnemyGuidDatabase.Entries["veteran_shotgun_kin"],
              EnemyGuidDatabase.Entries["ashen_shotgun_kin"],
              EnemyGuidDatabase.Entries["shotgrub"],
              EnemyGuidDatabase.Entries["sniper_shell"],
              EnemyGuidDatabase.Entries["professional"],
              EnemyGuidDatabase.Entries["gummy"],
              EnemyGuidDatabase.Entries["skullet"],
              EnemyGuidDatabase.Entries["skullmet"],
              EnemyGuidDatabase.Entries["creech"],
              EnemyGuidDatabase.Entries["hollowpoint"],
              EnemyGuidDatabase.Entries["king_bullat"],
              EnemyGuidDatabase.Entries["grenade_kin"],
              EnemyGuidDatabase.Entries["blobulon"], //yes v
              EnemyGuidDatabase.Entries["blobuloid"],
              EnemyGuidDatabase.Entries["blobulin"],
              EnemyGuidDatabase.Entries["poisbulon"],
              EnemyGuidDatabase.Entries["poisbuloid"],
              EnemyGuidDatabase.Entries["poisbulin"],
              EnemyGuidDatabase.Entries["leadbulon"],
              EnemyGuidDatabase.Entries["poopulon"],
              EnemyGuidDatabase.Entries["bloodbulon"],
              EnemyGuidDatabase.Entries["muzzle_wisp"],
              EnemyGuidDatabase.Entries["muzzle_flare"],
              EnemyGuidDatabase.Entries["cubulon"],
              EnemyGuidDatabase.Entries["cubulead"],
              EnemyGuidDatabase.Entries["apprentice_gunjurer"], //yes
              EnemyGuidDatabase.Entries["gunjurer"], //yes
              EnemyGuidDatabase.Entries["high_gunjurer"], //yes
              EnemyGuidDatabase.Entries["lore_gunjurer"], //yes
              EnemyGuidDatabase.Entries["gunsinger"],
              EnemyGuidDatabase.Entries["aged_gunsinger"],
              EnemyGuidDatabase.Entries["wizbang"],
              EnemyGuidDatabase.Entries["bullat"], //yes v
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["grenat"],
              EnemyGuidDatabase.Entries["spirat"],
              EnemyGuidDatabase.Entries["gunzookie"],
              EnemyGuidDatabase.Entries["gunzockie"],
              EnemyGuidDatabase.Entries["great_bullet_shark"],
              EnemyGuidDatabase.Entries["tombstoner"],
              EnemyGuidDatabase.Entries["gun_cultist"],
              EnemyGuidDatabase.Entries["gun_nut"],
              EnemyGuidDatabase.Entries["spectral_gun_nut"],
              EnemyGuidDatabase.Entries["flesh_cube"],
              EnemyGuidDatabase.Entries["lead_maiden"],
              EnemyGuidDatabase.Entries["shambling_round"],
              EnemyGuidDatabase.Entries["shelleton"],
              EnemyGuidDatabase.Entries["agonizer"],
              EnemyGuidDatabase.Entries["revolvenant"],
              EnemyGuidDatabase.Entries["gunreaper"],
          };

          //APACHE'S FLOORS
          public static List<string> jungleEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["western_snake"],
              EnemyGuidDatabase.Entries["chameleon"],
              EnemyGuidDatabase.Entries["gigi"],
              EnemyGuidDatabase.Entries["bird_parrot"],
              EnemyGuidDatabase.Entries["phaser_spider"],
              EnemyGuidDatabase.Entries["misfire_beast"],
              EnemyGuidDatabase.Entries["gunzookie"],
              EnemyGuidDatabase.Entries["bullat"], //yes v
              EnemyGuidDatabase.Entries["shotgat"],
              EnemyGuidDatabase.Entries["grenat"],
              EnemyGuidDatabase.Entries["spirat"],
              EnemyGuidDatabase.Entries["blue_fish_bullet_kin"],
              EnemyGuidDatabase.Entries["green_fish_bullet_kin"],
              EnemyGuidDatabase.Entries["skullet"],
              EnemyGuidDatabase.Entries["arrow_head"],
              EnemyGuidDatabase.Entries["shambling_round"],
              EnemyGuidDatabase.Entries["hooded_bullet"],
              EnemyGuidDatabase.Entries["gummy"],
              EnemyGuidDatabase.Entries["hollowpoint"],
              EnemyGuidDatabase.Entries["gunsinger"],
              EnemyGuidDatabase.Entries["bandana_bullet_kin"],
              EnemyGuidDatabase.Entries["veteran_bullet_kin"],
              EnemyGuidDatabase.Entries["veteran_shotgun_kin"],
              EnemyGuidDatabase.Entries["treadnaughts_bullet_kin"],
              EnemyGuidDatabase.Entries["sniper_shell"],
              EnemyGuidDatabase.Entries["professional"],
              EnemyGuidDatabase.Entries["fungun"],
              EnemyGuidDatabase.Entries["spogre"],
              EnemyGuidDatabase.Entries["pot_fairy"],
          };
          public static List<string> bellyEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["blue_fish_bullet_kin"],
              EnemyGuidDatabase.Entries["green_fish_bullet_kin"],
              EnemyGuidDatabase.Entries["pirate_bullet_kin"],
              EnemyGuidDatabase.Entries["pirate_shotgun_kin"],
              EnemyGuidDatabase.Entries["musketball"],
              EnemyGuidDatabase.Entries["dynamite_kin"],
              EnemyGuidDatabase.Entries["tarnisher"],
              EnemyGuidDatabase.Entries["bullet_shark"],
              EnemyGuidDatabase.Entries["great_bullet_shark"],
              EnemyGuidDatabase.Entries["flesh_cube"],
              EnemyGuidDatabase.Entries["shotgrub"],
              EnemyGuidDatabase.Entries["kbullet"],
              EnemyGuidDatabase.Entries["kalibullet"],
              EnemyGuidDatabase.Entries["creech"],
              EnemyGuidDatabase.Entries["blobulon"], //yes v
              EnemyGuidDatabase.Entries["blobuloid"],
              EnemyGuidDatabase.Entries["blobulin"],
              EnemyGuidDatabase.Entries["poisbulon"],
              EnemyGuidDatabase.Entries["poisbuloid"],
              EnemyGuidDatabase.Entries["poisbulin"],
              EnemyGuidDatabase.Entries["poopulon"],
              EnemyGuidDatabase.Entries["bloodbulon"],
              EnemyGuidDatabase.Entries["cubulon"],
              EnemyGuidDatabase.Entries["beadie"],
              EnemyGuidDatabase.Entries["mutant_bullet_kin"],
              EnemyGuidDatabase.Entries["gummy_spent"],
              EnemyGuidDatabase.Entries["shelleton"],
              EnemyGuidDatabase.Entries["skusket"],
              EnemyGuidDatabase.Entries["skusket_head"],
              EnemyGuidDatabase.Entries["skullmet"],
              EnemyGuidDatabase.Entries["spectral_gun_nut"],
              ModdedGUIDDatabase.ExpandTheGungeonGUIDs["agressive_cronenberg"],
          };
          public static List<string> westEnemyPalette = new List<string>()
          {
              EnemyGuidDatabase.Entries["blobulon"],
          };*/
        #endregion 

        public static List<string> chaosEnemyPalette = new List<string>()
        {
            EnemyGuidDatabase.Entries["bullet_kin"],
            EnemyGuidDatabase.Entries["ak47_bullet_kin"],
            EnemyGuidDatabase.Entries["bandana_bullet_kin"],
            EnemyGuidDatabase.Entries["veteran_bullet_kin"],
            EnemyGuidDatabase.Entries["treadnaughts_bullet_kin"],
            EnemyGuidDatabase.Entries["minelet"],
            EnemyGuidDatabase.Entries["cardinal"],
            EnemyGuidDatabase.Entries["shroomer"],
            EnemyGuidDatabase.Entries["ashen_bullet_kin"],
            EnemyGuidDatabase.Entries["mutant_bullet_kin"],
            EnemyGuidDatabase.Entries["fallen_bullet_kin"],
            EnemyGuidDatabase.Entries["chance_bullet_kin"],
            EnemyGuidDatabase.Entries["key_bullet_kin"],
            EnemyGuidDatabase.Entries["hooded_bullet"],
            EnemyGuidDatabase.Entries["red_shotgun_kin"],
            EnemyGuidDatabase.Entries["blue_shotgun_kin"],
            EnemyGuidDatabase.Entries["veteran_shotgun_kin"],
            EnemyGuidDatabase.Entries["mutant_shotgun_kin"],
            EnemyGuidDatabase.Entries["executioner"],
            EnemyGuidDatabase.Entries["ashen_shotgun_kin"],
            EnemyGuidDatabase.Entries["shotgrub"],
            EnemyGuidDatabase.Entries["sniper_shell"],
            EnemyGuidDatabase.Entries["professional"],
            EnemyGuidDatabase.Entries["gummy"],
            EnemyGuidDatabase.Entries["skullet"],
            EnemyGuidDatabase.Entries["skullmet"],
            EnemyGuidDatabase.Entries["creech"],
            EnemyGuidDatabase.Entries["hollowpoint"],
            EnemyGuidDatabase.Entries["bombshee"],
            EnemyGuidDatabase.Entries["rubber_kin"],
            EnemyGuidDatabase.Entries["tazie"],
            EnemyGuidDatabase.Entries["king_bullat"],
            EnemyGuidDatabase.Entries["grenade_kin"],
            EnemyGuidDatabase.Entries["dynamite_kin"],
            EnemyGuidDatabase.Entries["arrow_head"],
            EnemyGuidDatabase.Entries["blobulon"], //yes v
            EnemyGuidDatabase.Entries["blobuloid"],
            EnemyGuidDatabase.Entries["blobulin"],
            EnemyGuidDatabase.Entries["poisbulon"],
            EnemyGuidDatabase.Entries["poisbuloid"],
            EnemyGuidDatabase.Entries["poisbulin"],
            EnemyGuidDatabase.Entries["blizzbulon"],
            EnemyGuidDatabase.Entries["leadbulon"],
            EnemyGuidDatabase.Entries["poopulon"],
            EnemyGuidDatabase.Entries["bloodbulon"],
            EnemyGuidDatabase.Entries["skusket"],
            EnemyGuidDatabase.Entries["bookllet"],
            EnemyGuidDatabase.Entries["blue_bookllet"],
            EnemyGuidDatabase.Entries["green_bookllet"],
            EnemyGuidDatabase.Entries["gigi"],
            EnemyGuidDatabase.Entries["muzzle_wisp"],
            EnemyGuidDatabase.Entries["muzzle_flare"],
            EnemyGuidDatabase.Entries["cubulon"],
            EnemyGuidDatabase.Entries["chancebulon"],
            EnemyGuidDatabase.Entries["cubulead"],
            EnemyGuidDatabase.Entries["apprentice_gunjurer"], //yes
            EnemyGuidDatabase.Entries["gunjurer"], //yes
            EnemyGuidDatabase.Entries["high_gunjurer"], //yes
            EnemyGuidDatabase.Entries["lore_gunjurer"], //yes
            EnemyGuidDatabase.Entries["gunsinger"],
            EnemyGuidDatabase.Entries["aged_gunsinger"],
            EnemyGuidDatabase.Entries["ammomancer"],
            EnemyGuidDatabase.Entries["jammomancer"],
            EnemyGuidDatabase.Entries["jamerlengo"],
            EnemyGuidDatabase.Entries["wizbang"],
            EnemyGuidDatabase.Entries["pot_fairy"],
            EnemyGuidDatabase.Entries["bullat"], //yes v
            EnemyGuidDatabase.Entries["shotgat"],
            EnemyGuidDatabase.Entries["grenat"],
            EnemyGuidDatabase.Entries["spirat"],
            EnemyGuidDatabase.Entries["coaler"],
            EnemyGuidDatabase.Entries["gat"],
            EnemyGuidDatabase.Entries["diagonal_det"],
            EnemyGuidDatabase.Entries["diagonal_x_det"],
            EnemyGuidDatabase.Entries["gunzookie"],
            EnemyGuidDatabase.Entries["gunzockie"],
            EnemyGuidDatabase.Entries["bullet_shark"],
            EnemyGuidDatabase.Entries["great_bullet_shark"],
            EnemyGuidDatabase.Entries["tombstoner"],
            EnemyGuidDatabase.Entries["gun_cultist"],
            EnemyGuidDatabase.Entries["beadie"],
            EnemyGuidDatabase.Entries["gun_nut"],
            EnemyGuidDatabase.Entries["spectral_gun_nut"],
            EnemyGuidDatabase.Entries["chain_gunner"],
            EnemyGuidDatabase.Entries["fungun"],
            EnemyGuidDatabase.Entries["spogre"],
            EnemyGuidDatabase.Entries["mountain_cube"],
            EnemyGuidDatabase.Entries["flesh_cube"],
            EnemyGuidDatabase.Entries["lead_cube"],
            EnemyGuidDatabase.Entries["lead_maiden"],
            EnemyGuidDatabase.Entries["misfire_beast"],
            EnemyGuidDatabase.Entries["phaser_spider"],
            EnemyGuidDatabase.Entries["killithid"],
           // EnemyGuidDatabase.Entries["grip_master"],
            EnemyGuidDatabase.Entries["tarnisher"],
            EnemyGuidDatabase.Entries["shambling_round"],
            EnemyGuidDatabase.Entries["shelleton"],
            EnemyGuidDatabase.Entries["agonizer"],
            EnemyGuidDatabase.Entries["revolvenant"],
            EnemyGuidDatabase.Entries["gunreaper"],
            EnemyGuidDatabase.Entries["mine_flayers_bell"],
            EnemyGuidDatabase.Entries["mine_flayers_claymore"],
            EnemyGuidDatabase.Entries["fusebot"],
            EnemyGuidDatabase.Entries["ammoconda_ball"],
            EnemyGuidDatabase.Entries["bullet_kings_toadie"],
            EnemyGuidDatabase.Entries["bullet_kings_toadie_revenge"],
            EnemyGuidDatabase.Entries["old_kings_toadie"],
            EnemyGuidDatabase.Entries["draguns_knife"],
            EnemyGuidDatabase.Entries["dragun_knife_advanced"],
            EnemyGuidDatabase.Entries["gummy_spent"],
            EnemyGuidDatabase.Entries["candle_guy"],
            EnemyGuidDatabase.Entries["convicts_past_soldier"],
            EnemyGuidDatabase.Entries["robots_past_terminator"],
            EnemyGuidDatabase.Entries["marines_past_imp"],
            EnemyGuidDatabase.Entries["chicken"],
            EnemyGuidDatabase.Entries["rat"],
            EnemyGuidDatabase.Entries["dragun_egg_slimeguy"],
            EnemyGuidDatabase.Entries["poopulons_corn"],
            EnemyGuidDatabase.Entries["rat_candle"],
            EnemyGuidDatabase.Entries["tiny_blobulord"],
            EnemyGuidDatabase.Entries["office_bullet_kin"],
            EnemyGuidDatabase.Entries["office_bullette_kin"],
            EnemyGuidDatabase.Entries["brollet"],
            EnemyGuidDatabase.Entries["western_bullet_kin"],
            EnemyGuidDatabase.Entries["pirate_bullet_kin"],
            EnemyGuidDatabase.Entries["armored_bullet_kin"],
            EnemyGuidDatabase.Entries["western_shotgun_kin"],
            EnemyGuidDatabase.Entries["pirate_shotgun_kin"],
            EnemyGuidDatabase.Entries["gargoyle"],
            EnemyGuidDatabase.Entries["necronomicon"],
            EnemyGuidDatabase.Entries["tablet_bookllett"],
            EnemyGuidDatabase.Entries["grey_cylinder"],
            EnemyGuidDatabase.Entries["red_cylinder"],
            EnemyGuidDatabase.Entries["bullet_mech"],
            EnemyGuidDatabase.Entries["m80_kin"],
            EnemyGuidDatabase.Entries["candle_kin"],
            EnemyGuidDatabase.Entries["western_cactus"],
            EnemyGuidDatabase.Entries["musketball"],
            EnemyGuidDatabase.Entries["bird_parrot"],
            EnemyGuidDatabase.Entries["western_snake"],
            EnemyGuidDatabase.Entries["kalibullet"],
            EnemyGuidDatabase.Entries["kbullet"],
            EnemyGuidDatabase.Entries["blue_fish_bullet_kin"],
            EnemyGuidDatabase.Entries["green_fish_bullet_kin"],
            EnemyGuidDatabase.Entries["fridge_maiden"],
            EnemyGuidDatabase.Entries["titan_bullet_kin"],
            EnemyGuidDatabase.Entries["titan_bullet_kin_boss"],
            EnemyGuidDatabase.Entries["titaness_bullet_kin_boss"],
            EnemyGuidDatabase.Entries["robots_past_critter_3"],
            EnemyGuidDatabase.Entries["robots_past_critter_2"],
            EnemyGuidDatabase.Entries["robots_past_critter_1"],
            EnemyGuidDatabase.Entries["snake"],
            EnemyGuidDatabase.Entries["spent"],
        };
        public override bool CanBeUsed(PlayerController user)
        {
            List<AIActor> activeEnemies = user.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies != null) return true;
            else return false;
        }

        //MODDED ENEMY PALETTES
        public static List<string> ExpandTheGungeonChaosPalette = new List<string>()
        {
            ModdedGUIDDatabase.ExpandTheGungeonGUIDs["bootleg_bullat"],
            ModdedGUIDDatabase.ExpandTheGungeonGUIDs["bootleg_bullet_kin"],
            ModdedGUIDDatabase.ExpandTheGungeonGUIDs["bootleg_bandana_bullet_kin"],
            ModdedGUIDDatabase.ExpandTheGungeonGUIDs["bootleg_red_shotgun_kin"],
            ModdedGUIDDatabase.ExpandTheGungeonGUIDs["bootleg_blue_shotgun_kin"],
            ModdedGUIDDatabase.ExpandTheGungeonGUIDs["grenade_rat"],
            ModdedGUIDDatabase.ExpandTheGungeonGUIDs["cronenberg"],
            ModdedGUIDDatabase.ExpandTheGungeonGUIDs["agressive_cronenberg"],
            ModdedGUIDDatabase.ExpandTheGungeonGUIDs["explodey_boy"],
        };
        public static List<string> FrostAndGunfireChaosPalette = new List<string>()
        {
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["humphrey"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["milton"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["gunzooka"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["spitfire"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["firefly"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["ophaim"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["mushboom"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["salamander"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["skell"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["suppressor"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["cannon_kin"],
        };
        public static List<string> PlanetsideOfGunymedeChaosPalette = new List<string>()
        {
             ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["fodder"],
             ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["skullvenant"],
             ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["wailer"],
             ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["arch_gunjurer"],
             ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["cursebulon"],
             ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["glockulus"],
             ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["barretina"],
            //Bullets
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["an3s_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["apache_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["blazey_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["bleak_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["bot_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["bunny_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["cel_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["glaurung_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["hunter_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["king_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["kyle_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["neighborino_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["nevernamed_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["panda_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["retrash_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["skilotar_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["spapi_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["spcreat_bullet"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["wow_bullet"],
        };
    }
}
