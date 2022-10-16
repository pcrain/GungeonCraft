using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections.Generic;
using Dungeonator;

namespace NevernamedsItems
{
    public class CrossBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Cross Bullets";
            string resourceName = "NevernamedsItems/Resources/crossbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<CrossBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Quad Shot";
            string longDesc = "Occasionally grants quad shot along the cardinal directions." + "\n\nTrademark ability of an ancient nordic gunslinger.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("bullet_modifier");
            CrossBulletsID = item.PickupObjectId;
        }
        public static int CrossBulletsID;
       public bool isActive;
        private void PostProcess(Projectile bullet, float th) { RecalculateVolley(); }

        private void RecalculateVolley()
        {
            bool shouldBeActive = (UnityEngine.Random.value <= 0.25);
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
            int count = volleyToModify.projectiles.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileModule projectileModule = volleyToModify.projectiles[i];
                float num = 3f * 10f * -1f / 2f;
                int angleIterator = 0;
                for (int j = 0; j < 3; j++)
                {
                    int sourceIndex = i;
                    if (projectileModule.CloneSourceIndex >= 0)
                    {
                        sourceIndex = projectileModule.CloneSourceIndex;
                    }
                    ProjectileModule projectileModule2 = ProjectileModule.CreateClone(projectileModule, false, sourceIndex);
                    float angleFromAim = num + 10f * (float)j;
                    projectileModule2.angleFromAim = angleFromAim;
                    projectileModule2.ignoredForReloadPurposes = true;
                    projectileModule2.ammoCost = 0;
                    if (angleIterator == 0) { projectileModule2.angleFromAim += 90; angleIterator++; }
                    else if (angleIterator == 1) { projectileModule2.angleFromAim += 180; angleIterator++; }
                    else if (angleIterator == 2) { projectileModule2.angleFromAim -= 90; angleIterator++; }
                    volleyToModify.projectiles.Add(projectileModule2);
                }
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

