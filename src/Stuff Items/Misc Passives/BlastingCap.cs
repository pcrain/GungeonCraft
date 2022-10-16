using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Gungeon;
using System.Collections.Generic;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class BlastingCap : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Blasting Cap";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/blastingcap_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BlastingCap>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Dire Spore";
            string longDesc = "Empowers explosions with damaging spores."+"\n\nThe fruiting caps of this fantastic fungus is known to contain a small gunpowder payload- allowing it to violently detonate in order to spread it's spores.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");


            item.quality = PickupObject.ItemQuality.C;
            ID = item.PickupObjectId;
        }

        public static int ID;
        public override void Pickup(PlayerController player)
        {
            CustomActions.OnExplosionComplex += Explosion;
            base.Pickup(player);
        }
        public void Explosion(Vector3 position, ExplosionData data, Vector2 dir, Action onbegin, bool ignoreQueues, CoreDamageTypes damagetypes, bool ignoreDamageCaps)
        {
            if (Owner)
            {
                AkSoundEngine.PostEvent("Play_ENM_mushroom_cloud_01", base.gameObject);
                int numToSpawn = (data.damageRadius > 2) ? Mathf.CeilToInt(data.damageRadius * 10) : Mathf.CeilToInt(data.damageRadius * 5);

                for (int i = 0; i < numToSpawn; i++)
                {
                    GameObject toFire = (PickupObjectDatabase.GetById(FungoCannon.FungoCannonID) as Gun).DefaultModule.chargeProjectiles[0].Projectile.gameObject;
                    GameObject obj = UnityEngine.Object.Instantiate<GameObject>(toFire, position, Quaternion.Euler(new Vector3(0f, 0f, UnityEngine.Random.Range(0, 360))));
                    Projectile proj = obj.GetComponent<Projectile>();
                    if (proj)
                    {
                        proj.Owner = Owner;
                        proj.Shooter = Owner.specRigidbody;
                        proj.gameObject.AddComponent<BounceProjModifier>();
                        proj.gameObject.AddComponent<PierceProjModifier>();
                        proj.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        proj.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                        proj.baseData.range *= Owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                        proj.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                        proj.BossDamageMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                        proj.UpdateSpeed();
                        if (Owner.PlayerHasActiveSynergy("Screamosynthesis") && UnityEngine.Random.value <= 0.07f)
                        {
                            BlankProjModifier projModifier = proj.gameObject.AddComponent<BlankProjModifier>();
                        }
                        Owner.DoPostProcessProjectile(proj);
                    }
                }
            }
        }
        public override void DisableEffect(PlayerController player)
        {
            CustomActions.OnExplosionComplex -= Explosion;
            base.DisableEffect(player);
        }
        
    }
}