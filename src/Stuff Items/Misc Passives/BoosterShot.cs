using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;
using static Alexandria.Misc.PlayerUtility;

namespace NevernamedsItems
{
    class BoosterShot : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Booster Shot";
            string resourceName = "NevernamedsItems/Resources/boostershot_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BoosterShot>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Have you had your shots?";
            string longDesc = "5% chance to fully heal upon taking damage." + "\n\nThe mad wizard Alben Smallbore theorised that if one could train the body's immune system to fight pathogens, it may also be possible to vaccinate a Gungeoneer against bullets." + "\n\nAlben Smallbore did not have a medical license.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
        }
        private void Boost(PlayerController user)
        {
            if (UnityEngine.Random.value <= 0.05f)
            {
                if (!user.ForceZeroHealthState)
                {
                    user.healthHaver.ApplyHealing(10000);
                }
                else
                {
                    if (user.healthHaver.Armor < 6)
                    {
                        int amtNeeded = 6 - (int)user.healthHaver.Armor;
                        for (int i = 0; i < amtNeeded; i++)
                        {
                            LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                        }
                    }
                }
            }
        }
        public override void Update()
        {
            if (Owner)
            {
                if (Owner.PlayerHasActiveSynergy("Spun") != spunSynergyLastChecked)
                {
                    AlterItemStats.RemoveStatFromPassive(this, PlayerStats.StatType.Damage);
                    Owner.stats.RecalculateStats(Owner, false, false);

                    if (Owner.PlayerHasActiveSynergy("Spun"))
                    {
                        AlterItemStats.AddStatToPassive(this, PlayerStats.StatType.Damage, 2, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        Owner.stats.RecalculateStats(Owner, false, false);
                    }


                    spunSynergyLastChecked = Owner.PlayerHasActiveSynergy("Spun");
                }
            }
            base.Update();
        }
        private bool spunSynergyLastChecked;
        private void ModifyDamage(HealthHaver player, HealthHaver.ModifyDamageEventArgs args)
        {
            if (player && player.gameActor && player.gameActor is PlayerController)
            {
                PlayerController playercont = player.gameActor as PlayerController;
                if (playercont.characterIdentity == OMITBChars.Shade)
                {
                    if (UnityEngine.Random.value <= 0.1f)
                    {
                        args.ModifiedDamage = 0;
                        playercont.DoEasyBlank(playercont.specRigidbody.UnitCenter, EasyBlankType.MINI);
                    }
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            if (!m_pickedUpThisRun)
            {
                IntVector2 rewardPos = player.CurrentRoom.GetBestRewardLocation(IntVector2.One * 2, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                Vector3 convertedVector = rewardPos.ToVector3();
                LootEngine.SpawnItem(PickupObjectDatabase.GetById(85).gameObject, convertedVector, Vector2.zero, 1f, false, true, false);
            }
            player.healthHaver.ModifyDamage += ModifyDamage;
            player.OnReceivedDamage += this.Boost;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReceivedDamage -= this.Boost;
            player.healthHaver.ModifyDamage -= ModifyDamage;

            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.healthHaver.ModifyDamage -= ModifyDamage;

                Owner.OnReceivedDamage -= this.Boost;
            }
            base.OnDestroy();
        }
    }
}
