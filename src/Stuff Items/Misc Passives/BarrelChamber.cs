using System;
using System.Collections.Generic;
using System.Linq;
using Gungeon;
using System.Text;

using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class BarrelChamber : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Barrel Chamber";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/barrelchamber_icon";

            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BarrelChamber>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Wooden Shield";
            string longDesc = "A hilariously pathetic example of the forgotten art of doliumancy." + "\n\nCreates a weak, albeit free defence upon reloading an empty clip.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.D;
            ID = item.PickupObjectId;
        }
        public static int ID;
        bool canFire = true;
        private void HandleGunReloaded(PlayerController player, Gun playerGun)
        {
            if (playerGun.ClipShotsRemaining == 0)
            {
                StartCoroutine(SpawnBarrels(player));
            }
        }
        private IEnumerator BreakBarrel (MinorBreakable breakable)
        {
            yield return new WaitForSeconds(15f);
            breakable.Break();
            yield break;
        }
        private IEnumerator SpawnBarrels(PlayerController player)
        {
            if (canFire)
            {
                canFire = false;

                Vector2 playerPosition = player.sprite.WorldBottomCenter;
                List<Vector2> listToUse = offsetsForBarrels;
                if (player.PlayerHasActiveSynergy("Double Barrelled")) listToUse = offsetsForBarrelsSynergy;
                foreach (Vector2 offset in listToUse)
                {
                    Vector2 barrelPos = playerPosition + offset;
                    CellData cell = GameManager.Instance.Dungeon.data.cellData[(int)barrelPos.x][(int)barrelPos.y];
                    if (cell.type == CellType.FLOOR)
                    {
                        // EasyPlaceableObjects.GenericBarrelDungeonPlaceable.InstantiateObject(player.CurrentRoom, barrelPos.ToIntVector2(), )
                        GameObject barrel = UnityEngine.Object.Instantiate<GameObject>(EasyPlaceableObjects.GenericBarrel, barrelPos, Quaternion.identity);
                        barrel.GetComponentInChildren<MinorBreakable>().OnlyBrokenByCode = true;
                        barrel.GetComponentInChildren<SpeculativeRigidbody>().OnPreRigidbodyCollision += HandlePreCollision;
                        barrel.GetComponentInChildren<SpeculativeRigidbody>().PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;
                        barrel.GetComponentInChildren<tk2dSprite>().PlaceAtPositionByAnchor(barrelPos, tk2dSprite.Anchor.LowerCenter);
                        StartCoroutine(BreakBarrel(barrel.GetComponentInChildren<MinorBreakable>()));
                    }
                }

                Invoke("HandleCooldown", 0.5f);
            }
            yield break;
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            try
            {
                if (otherRigidbody)
                {
                    if (otherRigidbody.GetComponent<GameActor>())
                    {
                        PhysicsEngine.SkipCollision = true;
                    }
                    if (otherRigidbody.GetComponent<Projectile>())
                    {
                        if (otherRigidbody.GetComponent<Projectile>().ProjectilePlayerOwner())
                        {
                            PhysicsEngine.SkipCollision = true;
                        }
                        else
                        {
                            myRigidbody.GetComponentInChildren<MinorBreakable>().Break();
                            //otherRigidbody.GetComponent<Projectile>().DieInAir();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        public static List<Vector2> offsetsForBarrelsSynergy = new List<Vector2>()
        {
            //Right Wall
            new Vector2(2f, 1f),
            new Vector2(2f, 0f),
            new Vector2(2f, -1f),
            new Vector2(3f, 1f),
            new Vector2(3f, 0f),
            new Vector2(3f, -1f),
            //Left Wall
            new Vector2(-2f, -1f),
            new Vector2(-2f, 0f),
            new Vector2(-2f, 1f),
            new Vector2(-3f, -1f),
            new Vector2(-3f, 0f),
            new Vector2(-3f, 1f),
            //Top Wall
            new Vector2(0, 2),
            new Vector2(1, 2),
            new Vector2(-1, 2),
            new Vector2(0, 3),
            new Vector2(1, 3),
            new Vector2(-1, 3),
            //Bottom Wall
            new Vector2(0, -2),
            new Vector2(1, -2),
            new Vector2(-1, -2),
            new Vector2(0, -3),
            new Vector2(1, -3),
            new Vector2(-1, -3),
            //Corners
            new Vector2(2f, 2f),
            new Vector2(2f, -2f),
            new Vector2(-2f, 2f),
            new Vector2(-2f, -2f),

    };
        public static List<Vector2> offsetsForBarrels = new List<Vector2>()
        {
            //Right Wall
            new Vector2(2f, 1f),
            new Vector2(2f, 0f),
            new Vector2(2f, -1f),
            //Left Wall
            new Vector2(-2f, -1f),
            new Vector2(-2f, 0f),
            new Vector2(-2f, 1f),
            //Top Wall
            new Vector2(0, 2),
            new Vector2(1, 2),
            new Vector2(-1, 2),
            //Bottom Wall
            new Vector2(0, -2),
            new Vector2(1, -2),
            new Vector2(-1, -2),

    };
        private void HandleCooldown() { canFire = true; }
        private void PostProcessProj(Projectile proj, float g)
        {
            proj.pierceMinorBreakables = true;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProj;
            player.OnReloadedGun += this.HandleGunReloaded;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProj;
            player.OnReloadedGun -= this.HandleGunReloaded;

            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnReloadedGun -= this.HandleGunReloaded;
                Owner.PostProcessProjectile -= this.PostProcessProj;
            }

            base.OnDestroy();
        }
    }

}
