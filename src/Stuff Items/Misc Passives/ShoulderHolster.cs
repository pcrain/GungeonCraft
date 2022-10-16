using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections.Generic;
using Dungeonator;
using System;

namespace NevernamedsItems
{
    public class ShoulderHolster : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Shoulder Holster";
            string resourceName = "NevernamedsItems/Resources/shoulderholster_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ShoulderHolster>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Bonus Bullets";
            string longDesc = "Chance for random bonus shots." + "\n\nA more than awkward shooting style, that's for certain.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.C;

            ShoulderHolsterID = item.PickupObjectId;

            hipHolsterShootHook = new Hook(
            typeof(FireOnReloadItem).GetMethod("HandleHipHolsterProcessing", BindingFlags.Instance | BindingFlags.NonPublic),
            typeof(ShoulderHolster).GetMethod("HipHolsterHook", BindingFlags.Instance | BindingFlags.Public),
            typeof(FireOnReloadItem)
            );
        }
        public void HipHolsterHook(Action<FireOnReloadItem, Projectile> orig, FireOnReloadItem self, Projectile bullet)
        {
            if (self.Owner && self.Owner.PlayerHasActiveSynergy("Heads, Shoulders, Knees, and Toes"))
            {
                bullet.baseData.damage *= 2;
            }
            orig(self, bullet);
        }
        public static int ShoulderHolsterID;
        public bool isActive;
        private static Hook hipHolsterShootHook;
        private void PostProcess(Projectile bullet, float th) { RecalculateVolley(); }

        private void RecalculateVolley()
        {
            bool shouldBeActive = (UnityEngine.Random.value <= 0.33f);
            if ((shouldBeActive && isActive) || (!shouldBeActive && !isActive)) return;
            if (shouldBeActive && !isActive)
            {
                Owner.stats.AdditionalVolleyModifiers += this.ModifyVolley;
                Owner.stats.RecalculateStats(Owner, false, false);
                isActive = true;
            }
            else if (!shouldBeActive && isActive)
            {
                Owner.stats.AdditionalVolleyModifiers -= this.ModifyVolley;
                Owner.stats.RecalculateStats(Owner, false, false);
                isActive = false;
            }
        }
        private void PostProcessBeam(BeamController beam) { RecalculateVolley(); }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessBeam += this.PostProcessBeam;
            player.PostProcessProjectile += this.PostProcess;
            base.Pickup(player);
        }
        public void ModifyVolley(ProjectileVolleyData volleyToModify)
        {

            float randoAngle = UnityEngine.Random.Range(1, 360);
            int count = volleyToModify.projectiles.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileModule projectileModule = volleyToModify.projectiles[i];
                int sourceIndex = i;
                if (projectileModule.CloneSourceIndex >= 0)
                {
                    sourceIndex = projectileModule.CloneSourceIndex;
                }
                ProjectileModule projectileModule2 = ProjectileModule.CreateClone(projectileModule, false, sourceIndex);
                projectileModule2.angleFromAim = randoAngle;
                projectileModule2.ignoredForReloadPurposes = true;
                projectileModule2.ammoCost = 0;
                if (Owner && Owner.PlayerHasActiveSynergy("Heads, Shoulders, Knees, and Toes"))
                {
                    for (int r = 0; r < projectileModule.projectiles.Count; r++)
                    {
                        Projectile startProj = projectileModule.projectiles[r];
                        if (startProj)
                        {
                            Projectile clonedProj = FakePrefab.Clone(startProj.gameObject).GetComponent<Projectile>();
                            if (clonedProj)
                            {
                                clonedProj.baseData.damage *= 2;
                                projectileModule2.projectiles[r] = clonedProj;
                            }
                        }
                    }
                }
                volleyToModify.projectiles.Add(projectileModule2);
            }
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessBeam -= this.PostProcessBeam;
            player.PostProcessProjectile -= this.PostProcess;
            player.stats.AdditionalVolleyModifiers -= this.ModifyVolley;
            player.stats.RecalculateStats(player, false, false);
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessBeam -= this.PostProcessBeam;
                Owner.PostProcessProjectile -= this.PostProcess;
                Owner.stats.AdditionalVolleyModifiers -= this.ModifyVolley;
                Owner.stats.RecalculateStats(Owner, false, false);
            }
            base.OnDestroy();
        }
    }

}