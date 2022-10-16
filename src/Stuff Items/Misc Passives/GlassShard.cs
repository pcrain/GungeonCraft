using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class GlassShard : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Glass Shard";
            string resourceName = "NevernamedsItems/Resources/glassshard_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<GlassShard>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Walking on Broken Glass";
            string longDesc = "Makes Glass Guon Stones fire at enemies."+"\n\nCarries the soul of a vengeful gungeoneer."+"\nSome say if you gaze into the depths of the shard, you can see him gazing back out at you.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.A;

            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.speed *= 1.2f;
            projectile2.baseData.range *= 1f;
            projectile2.SetProjectileSpriteRight("glasster_projectile", 4, 4, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            GlassShardProjectile = projectile2;

            GlassShardID = item.PickupObjectId;
        }
        public static Projectile GlassShardProjectile;
        public static int GlassShardID;
        bool onCooldown = false;
        public override void Update()
        {
            if (Owner != null && Owner.IsInCombat)
            {
                if (!onCooldown)
                {
                    foreach (var orbital in Owner.orbitals)
                    {
                        var o = (PlayerOrbital)orbital;
                        if (o.name == "IounStone_Glass(Clone)")
                        {
                            if (Owner.CurrentGun && Owner.CurrentGun.PickupObjectId != Glasster.GlassterID)
                            {

                                if (Owner.PlayerHasActiveSynergy("Shattershot"))
                                {
                                    FireBullet(orbital.GetTransform(), 0, 10);
                                    FireBullet(orbital.GetTransform(), 14, 10);
                                    FireBullet(orbital.GetTransform(), -14, 10);
                                }
                                else
                                {
                                    FireBullet(orbital.GetTransform(), 0, 5);
                                }
                            }
                        }
                        //else ETGModConsole.Log("Guon name didn't match the glass guon.");
                    }
                    onCooldown = true;
                    float cooldownTime = 1.5f;
                    if (Owner.PlayerHasActiveSynergy("Break Fast!")) cooldownTime = 0.9f;
                    Invoke("ResetCooldown", cooldownTime);
                }
            }
            base.Update();
        }
        private void FireBullet(Transform pos, float angleOffset, float anglevariance)
        {        
            GameObject gameObject = GlassShardProjectile.InstantiateAndFireTowardsPosition(pos.position, ((Vector2)pos.position).GetPositionOfNearestEnemy(ActorCenter.SPRITE), angleOffset, anglevariance);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component != null)
            {
                component.Owner = Owner;
                component.TreatedAsNonProjectileForChallenge = true;
                component.Shooter = Owner.specRigidbody;
                component.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                component.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                component.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                component.AdditionalScaleMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                component.UpdateSpeed();
                Owner.DoPostProcessProjectile(component);
            }
        }
        private void ResetCooldown()
        {
            onCooldown = false;
        }
        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
            {
                for (int i = 0; i < 3; i++)
                {
                    player.AcquirePassiveItemPrefabDirectly(PickupObjectDatabase.GetById(565) as PassiveItem);

                }
            }
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
            base.Pickup(player);
        }
        private void OnNewFloor()
        {
            if (Owner)
            {
                Owner.AcquirePassiveItemPrefabDirectly(PickupObjectDatabase.GetById(565) as PassiveItem);
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            base.OnDestroy();
        }
    }

}