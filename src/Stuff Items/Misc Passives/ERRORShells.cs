using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class ERRORShells : PassiveItem
    {
        public static void Init()
        {
            string itemName = "ERROR Shells";
            string resourceName = "NevernamedsItems/Resources/errorshells_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ERRORShells>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "What do you mean 74 errors!?";
            string longDesc = "Picks a random selection of enemies to become highly efficient against.\n\n" + "These bullets were moulded by the numerous errors that went into making them, thanks to their incompetent smith.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_ERRORSHELLS, true);
            item.AddItemToDougMetaShop(30);

            ERRORShellsDummyEffect = new GameActorDecorationEffect()
            {
                AffectsEnemies = true,
                OverheadVFX = EasyVFXDatabase.ERRORShellsOverheadVFX,
                AffectsPlayers = false,
                AppliesTint = false,
                AppliesDeathTint = false,
                AppliesOutlineTint = false,
                duration = float.MaxValue,
                effectIdentifier = "ERROR Shells Overheader",
                resistanceType = EffectResistanceType.None,
                PlaysVFXOnActor = false,
                stackMode = GameActorEffect.EffectStackingMode.Ignore,
            };
        }
        public static GameActorDecorationEffect ERRORShellsDummyEffect;

        private void PreSpawn(AIActor aIActor)
        {
            if (badMenGoByeBye.Contains(aIActor.EnemyGuid))
            {
                aIActor.ApplyEffect(ERRORShellsDummyEffect);
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            PickEnemies();
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeam += this.PostProcessBeam;
            ETGMod.AIActor.OnPreStart += PreSpawn;
            TextBubble.DoAmbientTalk(player.transform, new Vector3(1, 2, 0), PickedEnemiesDialogue, 5f); ;
        }

        bool hasPicked = false;
        public string PickedEnemiesDialogue;
        private void PickEnemies()
        {
            int timesIterated = 0;
            if (hasPicked) return;
            string pickedEnemiesMessage = "";
            Dictionary<string, string> MidCheckGUIDS = new Dictionary<string, string>();
            foreach (string key in EnemyGuidDatabase.Entries.Keys)
            {
                if (!badMenToNotGoByeBye.Contains(EnemyGuidDatabase.Entries[key]))
                {
                    MidCheckGUIDS.Add(key, EnemyGuidDatabase.Entries[key]);
                }
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("somebunny.etg.planetsideofgunymede"))
            {
                foreach (string key in ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs.Keys)
                {
                    if (!badMenToNotGoByeBye.Contains(ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs[key]))
                    {
                        MidCheckGUIDS.Add(key, ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs[key]);
                    }
                }
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("frostandgunfireplaceholder"))
            {
                foreach (string key in ModdedGUIDDatabase.FrostAndGunfireGUIDs.Keys)
                {
                    if (!badMenToNotGoByeBye.Contains(ModdedGUIDDatabase.FrostAndGunfireGUIDs[key]))
                    {
                        MidCheckGUIDS.Add(key, ModdedGUIDDatabase.FrostAndGunfireGUIDs[key]);
                    }
                }
            }
            //add modded enemies if they exist
            for (int i = 0; i < 10; i++)
            {
                timesIterated++;
                int index = UnityEngine.Random.Range(0, MidCheckGUIDS.Count);
                string nameToAdd;
                if (timesIterated >= 10) nameToAdd = "and " + MidCheckGUIDS.Keys.ElementAt(index) + ".";
                else nameToAdd = MidCheckGUIDS.Keys.ElementAt(index) + ", \n";
                nameToAdd = nameToAdd.Replace("_", " ");
                ETGModConsole.Log("Enemy Chosen for Death: " + MidCheckGUIDS.Keys.ElementAt(index));
                pickedEnemiesMessage += nameToAdd;
                badMenGoByeBye.Add(MidCheckGUIDS.Values.ElementAt(index));
                MidCheckGUIDS.Remove(MidCheckGUIDS.Keys.ElementAt(index));
            }
            hasPicked = true;
            PickedEnemiesDialogue = pickedEnemiesMessage;
        }
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            ProjectileInstakillBehaviour instakill = sourceProjectile.gameObject.GetOrAddComponent<ProjectileInstakillBehaviour>();
            instakill.enemyGUIDsToKill.AddRange(badMenGoByeBye);
        }
        private void PostProcessBeam(BeamController sourceBeam)
        {
            if (sourceBeam.projectile)
            {
                this.PostProcessProjectile(sourceBeam.projectile, 1);
            }
        }

        public List<string> badMenGoByeBye = new List<string>()
        {
        };
        public List<string> badMenToNotGoByeBye = new List<string>()
        {
            EnemyGuidDatabase.Entries["gummy_spent"],
            EnemyGuidDatabase.Entries["western_shotgun_kin"],
            EnemyGuidDatabase.Entries["pirate_shotgun_kin"],
            EnemyGuidDatabase.Entries["blobuloid"],
            EnemyGuidDatabase.Entries["blobulin"],
            EnemyGuidDatabase.Entries["poisbuloid"],
            EnemyGuidDatabase.Entries["poisbulin"],
            EnemyGuidDatabase.Entries["black_skusket"],
            EnemyGuidDatabase.Entries["skusket_head"],
            EnemyGuidDatabase.Entries["shotgat"],
            EnemyGuidDatabase.Entries["necronomicon"],
            EnemyGuidDatabase.Entries["tablet_bookllett"],
            EnemyGuidDatabase.Entries["pot_fairy"],
            EnemyGuidDatabase.Entries["fridge_maiden"],
            EnemyGuidDatabase.Entries["bullet_king"],
            EnemyGuidDatabase.Entries["blobulord"],
            EnemyGuidDatabase.Entries["old_king"],
            EnemyGuidDatabase.Entries["lich"],
            EnemyGuidDatabase.Entries["megalich"],
            EnemyGuidDatabase.Entries["infinilich"],
            EnemyGuidDatabase.Entries["tiny_blobulord"],
            EnemyGuidDatabase.Entries["cannonbalrog"],
            EnemyGuidDatabase.Entries["brown_chest_mimic"],
            EnemyGuidDatabase.Entries["blue_chest_mimic"],
            EnemyGuidDatabase.Entries["green_chest_mimic"],
            EnemyGuidDatabase.Entries["red_chest_mimic"],
            EnemyGuidDatabase.Entries["black_chest_mimic"],
            EnemyGuidDatabase.Entries["rat_chest_mimic"],
            EnemyGuidDatabase.Entries["pedestal_mimic"],
            EnemyGuidDatabase.Entries["wall_mimic"],
            EnemyGuidDatabase.Entries["door_lord"],
            EnemyGuidDatabase.Entries["brollet"],
            EnemyGuidDatabase.Entries["grenat"],
            EnemyGuidDatabase.Entries["det"],
            EnemyGuidDatabase.Entries["x_det"],
            EnemyGuidDatabase.Entries["diagonal_x_det"],
            EnemyGuidDatabase.Entries["vertical_det"],
            EnemyGuidDatabase.Entries["horizontal_det"],
            EnemyGuidDatabase.Entries["diagonal_det"],
            EnemyGuidDatabase.Entries["vertical_x_det"],
            EnemyGuidDatabase.Entries["horizontal_x_det"],
            EnemyGuidDatabase.Entries["m80_kin"],
            EnemyGuidDatabase.Entries["mine_flayers_claymore"],
            EnemyGuidDatabase.Entries["office_bullet_kin"],
            EnemyGuidDatabase.Entries["office_bullette_kin"],
            EnemyGuidDatabase.Entries["western_bullet_kin"],
            EnemyGuidDatabase.Entries["pirate_bullet_kin"],
            EnemyGuidDatabase.Entries["armored_bullet_kin"],
            EnemyGuidDatabase.Entries["spectre"],
            EnemyGuidDatabase.Entries["bullat"],
            EnemyGuidDatabase.Entries["spirat"],
            EnemyGuidDatabase.Entries["gargoyle"],
            EnemyGuidDatabase.Entries["grey_cylinder"],
            EnemyGuidDatabase.Entries["red_cylinder"],
            EnemyGuidDatabase.Entries["bullet_mech"],
            EnemyGuidDatabase.Entries["arrow_head"],
            EnemyGuidDatabase.Entries["musketball"],
            EnemyGuidDatabase.Entries["western_cactus"],
            EnemyGuidDatabase.Entries["candle_kin"],
            EnemyGuidDatabase.Entries["chameleon"],
            EnemyGuidDatabase.Entries["bird_parrot"],
            EnemyGuidDatabase.Entries["western_snake"],
            EnemyGuidDatabase.Entries["kalibullet"],
            EnemyGuidDatabase.Entries["kbullet"],
            EnemyGuidDatabase.Entries["blue_fish_bullet_kin"],
            EnemyGuidDatabase.Entries["green_fish_bullet_kin"],
            EnemyGuidDatabase.Entries["ammoconda_ball"],
            EnemyGuidDatabase.Entries["summoned_treadnaughts_bullet_kin"],
            EnemyGuidDatabase.Entries["mine_flayers_bell"],
            EnemyGuidDatabase.Entries["titan_bullet_kin"],
            EnemyGuidDatabase.Entries["grip_master"],
            EnemyGuidDatabase.Entries["titan_bullet_kin_boss"],
            EnemyGuidDatabase.Entries["titaness_bullet_kin_boss"],
            EnemyGuidDatabase.Entries["fusebot"],
            EnemyGuidDatabase.Entries["candle_guy"],
            EnemyGuidDatabase.Entries["draguns_knife"],
            EnemyGuidDatabase.Entries["dragun_knife_advanced"],
            EnemyGuidDatabase.Entries["marines_past_imp"],
            EnemyGuidDatabase.Entries["convicts_past_soldier"],
            EnemyGuidDatabase.Entries["robots_past_terminator"],
            EnemyGuidDatabase.Entries["bullet_kings_toadie"],
            EnemyGuidDatabase.Entries["bullet_kings_toadie_revenge"],
            EnemyGuidDatabase.Entries["old_kings_toadie"],
            EnemyGuidDatabase.Entries["chicken"],
            EnemyGuidDatabase.Entries["rat"],
            EnemyGuidDatabase.Entries["dragun_egg_slimeguy"],
            EnemyGuidDatabase.Entries["poopulons_corn"],
            EnemyGuidDatabase.Entries["rat_candle"],
            EnemyGuidDatabase.Entries["robots_past_critter_3"],
            EnemyGuidDatabase.Entries["robots_past_critter_2"],
            EnemyGuidDatabase.Entries["robots_past_critter_1"],
            EnemyGuidDatabase.Entries["snake"],
            EnemyGuidDatabase.Entries["beholster"],
            EnemyGuidDatabase.Entries["treadnaught"],
            EnemyGuidDatabase.Entries["cannonbalrog"],
            EnemyGuidDatabase.Entries["gorgun"],
            EnemyGuidDatabase.Entries["gatling_gull"],
            EnemyGuidDatabase.Entries["ammoconda"],
            EnemyGuidDatabase.Entries["dragun"],
            EnemyGuidDatabase.Entries["dragun_advanced"],
            EnemyGuidDatabase.Entries["helicopter_agunim"],
            EnemyGuidDatabase.Entries["super_space_turtle_dummy"],
            EnemyGuidDatabase.Entries["cop_android"],
            EnemyGuidDatabase.Entries["portable_turret"],
            EnemyGuidDatabase.Entries["friendly_gatling_gull"],
            EnemyGuidDatabase.Entries["cucco"],
            EnemyGuidDatabase.Entries["cop"],
            EnemyGuidDatabase.Entries["blockner"],
            //Expand The Gungeon
            #region FrostAndGunfire
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["humphrey"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["milton"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["roomimic"],
            ModdedGUIDDatabase.FrostAndGunfireGUIDs["mushboom"],
            #endregion
            #region PlanetsideOfGunymede
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["shellrax"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["bullet_banker"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["jammed_guardian"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["deturret_left"],
            ModdedGUIDDatabase.PlanetsideOfGunymedeGUIDs["deturret_right"],
            //Gunymede Bullet Lads
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
#endregion
        };
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeam -= this.PostProcessBeam;
            ETGMod.AIActor.OnPreStart -= PreSpawn;

            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
                Owner.PostProcessBeam -= this.PostProcessBeam;
            }
            ETGMod.AIActor.OnPreStart -= PreSpawn;
            base.OnDestroy();
        }
    }
}

