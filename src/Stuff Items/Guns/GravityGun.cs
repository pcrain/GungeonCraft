using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Dungeonator;
using System.Reflection;
using System.Collections.ObjectModel;
using Alexandria.Misc;
using Alexandria.EnemyAPI;

namespace NevernamedsItems
{
    public class GravityGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gravity Gun", "gravitygun");
            Game.Items.Rename("outdated_gun_mods:gravity_gun", "nn:gravity_gun");
            var behav = gun.gameObject.AddComponent<GravityGun>();
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalReloadAudio = "Play_wpn_chargelaser_shot_01";
            gun.SetShortDescription("Not A Toy");
            gun.SetLongDescription("Picks up and throws objects, and weak enemies." + "\n\nOriginally developed for hazardous materials transport by an alien empire." + "\nUtilises negative mass and counter-resonant fluctuators to maintain a stable zero-point field.");

            gun.SetupSprite(null, "gravitygun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 25);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(562) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.usesContinuousFireAnimation = true;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            //GUN STATS
            gun.barrelOffset.transform.localPosition = new Vector3(1.31f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(10000);
            gun.ammo = 10000;
            gun.gunClass = GunClass.CHARGE;
            gun.doesScreenShake = false;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10000;
            gun.DefaultModule.angleVariance = 0f;
            gun.InfiniteAmmo = true;
            gun.reloadTime = 0f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.ammoCost = 0;
            gun.DefaultModule.projectiles[0] = null;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Y-Beam Laser";

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            GravityGunID = gun.PickupObjectId;

            ElectricImmunity = new DamageTypeModifier();
            ElectricImmunity.damageMultiplier = 0f;
            ElectricImmunity.damageType = CoreDamageTypes.Electric;
        }
        private static DamageTypeModifier ElectricImmunity;
        public static int GravityGunID;
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (ChestExplosionData == null)
            {
                ChestExplosionData = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData.CopyExplosionData();
                ChestExplosionData.breakSecretWalls = true;
                if (!ChestExplosionData.ignoreList.Contains(player.specRigidbody)) ChestExplosionData.ignoreList.Add(player.specRigidbody);
            }
            if (MajorBreakableImpactVFX == null)
            {
                MajorBreakableImpactVFX = (PickupObjectDatabase.GetById(37) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.overrideMidairDeathVFX;
            }
            base.OnPickedUpByPlayer(player);
        }
        public override void OnSwitchedAwayFromThisGun()
        {
            if (CurrentCaughtProjectile)
            {
                // AkSoundEngine.PostEvent("Stop_WPN_moonscraperLaser_loop_01", gun.GunPlayerOwner().gameObject);
                CurrentCaughtProjectile.DieInAir(false, true, true, false);
            }
            if (isUsingPropFly)
            {
                HandlePropFly(false);
            }
            if (PlayerIsElectricImmune && gun.GunPlayerOwner())
            {
                gun.GunPlayerOwner().healthHaver.damageTypeModifiers.Remove(ElectricImmunity);
                PlayerIsElectricImmune = false;
            }
            base.OnSwitchedAwayFromThisGun();
        }
        public GravityGun()
        {

        }
        private static ExplosionData ChestExplosionData;
        private static GameObject MajorBreakableImpactVFX;
        private bool PlayerIsElectricImmune = false;

        private Vector2 HoldPosition()
        {
            if (gun && gun.GunPlayerOwner() && gun.sprite)
            {
                PlayerController player = gun.GunPlayerOwner();
                Vector2 vector = player.CenterPosition;
                Vector2 normalized = (player.unadjustedAimPoint.XY() - vector).normalized;
                Vector2 pos = (gun.sprite.WorldCenter + normalized * 2f);
                return pos;
            }
            else return Vector2.zero;
        }
        private void CheckForGrab()
        {
            // UnityEngine.Object.Instantiate<GameObject>(TempStorage.PurpleRedLaserCircleVFX, HoldPosition(), Quaternion.identity);

            List<GameObject> ValidObjects = new List<GameObject>();
            List<MinorBreakable> allMinorBreakables = StaticReferenceManager.AllMinorBreakables;
            for (int k = allMinorBreakables.Count - 1; k >= 0; k--)
            {
                MinorBreakable minorBreakable = allMinorBreakables[k];
                if (minorBreakable && minorBreakable.specRigidbody)
                {
                    if (!minorBreakable.IsBroken && minorBreakable.sprite)
                    {
                        if (Vector2.Distance(HoldPosition(), minorBreakable.CenterPoint) < 1.5f)
                        {
                            ValidObjects.Add(minorBreakable.gameObject);
                        }
                    }
                }
            }
            List<MajorBreakable> allMajorBreakables = StaticReferenceManager.AllMajorBreakables;
            for (int k = allMajorBreakables.Count - 1; k >= 0; k--)
            {
                MajorBreakable majorBreakable = allMajorBreakables[k];
                if (majorBreakable && majorBreakable.specRigidbody)
                {
                    if (!majorBreakable.IsDestroyed && majorBreakable.sprite)
                    {
                        if (Vector2.Distance(HoldPosition(), majorBreakable.CenterPoint) < 1.5f)
                        {
                            bool flag = true;
                            if (majorBreakable.GetComponent<Chest>() && majorBreakable.GetComponent<Chest>().ChestIdentifier == Chest.SpecialChestIdentifier.RAT) flag = false;
                            if (majorBreakable.GetComponent<BashelliskBodyController>() != null) flag = false;
                            if (majorBreakable.GetComponent<Projectile>() != null) flag = false;
                            if (majorBreakable.name.ToLower().Contains("boss_reward_pedestal") || majorBreakable.name.ToLower().Contains("minecart_turret")) flag = false;
                            if (flag)
                            {
                                ValidObjects.Add(majorBreakable.gameObject);
                            }
                        }
                    }
                }
            }
            if (gun.GunPlayerOwner())
            {
                RoomHandler roomHandler = gun.GunPlayerOwner().CurrentRoom;
                if (roomHandler != null)
                {
                    List<AIActor> activeEnemies = roomHandler.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                    if (activeEnemies != null && activeEnemies.Count > 0)
                    {
                        for (int i = 0; i < activeEnemies.Count; i++)
                        {
                            AIActor aiactor = activeEnemies[i];
                            if (aiactor && aiactor.specRigidbody && aiactor.IsNormalEnemy && !aiactor.IsGone && aiactor.healthHaver && aiactor.healthHaver.IsVulnerable && !aiactor.healthHaver.IsBoss)
                            {
                                if (Vector2.Distance(HoldPosition(), aiactor.sprite.WorldCenter) < 1.5f)
                                {
                                    if (!aiactor.IsInMinecart())
                                    {
                                        if (aiactor.healthHaver.GetMaxHealth() <= HealthMaxValue() || GravityGunOverrideGrabGuids.Contains(aiactor.EnemyGuid))
                                        {
                                            ValidObjects.Add(aiactor.gameObject);
                                        }
                                        else if (gun.GunPlayerOwner().PlayerHasActiveSynergy("Xenobiology") && BlobGuids.Contains(aiactor.EnemyGuid))
                                        {
                                            ValidObjects.Add(aiactor.gameObject);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            if (ValidObjects.Count > 0)
            {
                GameObject closestThing = null;
                float dist = float.MaxValue;
                for (int k = ValidObjects.Count - 1; k >= 0; k--)
                {
                    float num3 = Vector2.Distance(HoldPosition(), ValidObjects[k].transform.position);
                    if (num3 < dist)
                    {
                        closestThing = ValidObjects[k];
                        dist = num3;
                    }
                }
                if (closestThing != null) CatchObject(closestThing);
            }
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            if (CurrentCaughtProjectile)
            {
                //AkSoundEngine.PostEvent("Stop_WPN_moonscraperLaser_loop_01", player.gameObject);
                CurrentCaughtProjectile.DieInAir(false, true, true, false);
            }
            if (isUsingPropFly)
            {
                HandlePropFly(false);
            }
            if (PlayerIsElectricImmune)
            {
                player.healthHaver.damageTypeModifiers.Remove(ElectricImmunity);
                PlayerIsElectricImmune = false;
            }
            base.OnPostDroppedByPlayer(player);
        }
        private bool isUsingPropFly = false;
        private void HandlePropFly(bool AddsPropFly)
        {
            if (gun && gun.GunPlayerOwner())
            {
                PlayerController player = gun.GunPlayerOwner();
                gun.RemoveStatFromGun(PlayerStats.StatType.MovementSpeed);
                if (!AddsPropFly)
                {
                    player.SetIsFlying(false, "GravityGunPropFly", true, false);
                    player.AdditionalCanDodgeRollWhileFlying.RemoveOverride("GravityGunPropFly");
                }
                if (AddsPropFly)
                {
                    player.SetIsFlying(true, "GravityGunPropFly", true, false);
                    player.AdditionalCanDodgeRollWhileFlying.AddOverride("GravityGunPropFly", null);
                    gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, 2, StatModifier.ModifyMethod.ADDITIVE);
                }
                gun.GunPlayerOwner().stats.RecalculateStats(gun.GunPlayerOwner());
            }
        }
        protected override void Update()
        {
            //Prop Fly
            if (gun.GunPlayerOwner())
            {
                if (gun.GunPlayerOwner().CharacterUsesRandomGuns)
                {
                    if (isUsingPropFly)
                    {
                        HandlePropFly(false);
                        isUsingPropFly = false;
                    }
                    gun.GunPlayerOwner().ChangeToRandomGun();
                }
                if (CurrentCaughtProjectile != null)
                {
                    if (gun.GunPlayerOwner().PlayerHasActiveSynergy("Prop Fly") && isUsingPropFly == false)
                    {
                        HandlePropFly(true);
                        isUsingPropFly = true;
                    }
                    else if (!gun.GunPlayerOwner().PlayerHasActiveSynergy("Prop Fly") && isUsingPropFly == true)
                    {
                        HandlePropFly(false);
                        isUsingPropFly = false;
                    }
                }
                else
                {
                    if (isUsingPropFly)
                    {
                        HandlePropFly(false);
                        isUsingPropFly = false;
                    }
                }
            }
            //Negative Matter
            if (gun.GunPlayerOwner())
            {
                if (gun.GunPlayerOwner().PlayerHasActiveSynergy("Negative Matter"))
                {
                    if (gun.GunPlayerOwner().CurrentGun.PickupObjectId == GravityGunID)
                    {
                        if (!PlayerIsElectricImmune)
                        {
                            gun.GunPlayerOwner().healthHaver.damageTypeModifiers.Add(ElectricImmunity);
                            PlayerIsElectricImmune = true;
                        }
                    }
                    else
                    {
                        if (PlayerIsElectricImmune)
                        {
                            gun.GunPlayerOwner().healthHaver.damageTypeModifiers.Remove(ElectricImmunity);
                            PlayerIsElectricImmune = false;

                        }
                    }
                }
                else
                {
                    if (PlayerIsElectricImmune)
                    {
                        gun.GunPlayerOwner().healthHaver.damageTypeModifiers.Remove(ElectricImmunity);
                        PlayerIsElectricImmune = false;
                    }
                }
            }
            //Object Handling
            if (gun.IsFiring && !CurrentCaughtProjectile)
            {
                CheckForGrab();
            }
            else if (gun.IsFiring && CurrentCaughtProjectile)
            {
                if (CurrentCaughtProjectile.specRigidbody == null) CurrentCaughtProjectile.DieInAir(false, true, true, false);
                CurrentCaughtProjectile.transform.position = HoldPosition() - ObjectCenterOffset(CurrentCaughtProjectile.gameObject);
                if (CurrentCaughtProjectile.specRigidbody != null)
                {
                    CurrentCaughtProjectile.specRigidbody.Position = new Position(HoldPosition() - ObjectCenterOffset(CurrentCaughtProjectile.gameObject));
                    CurrentCaughtProjectile.specRigidbody.UpdateColliderPositions();
                }
            }
            else if (!gun.IsFiring && CurrentCaughtProjectile)
            {
                //AkSoundEngine.PostEvent("Stop_WPN_moonscraperLaser_loop_01", gun.GunPlayerOwner().gameObject);
                AkSoundEngine.PostEvent("Play_wpn_chargelaser_shot_01", gun.GunPlayerOwner().gameObject);
                this.LaunchProjectile();
            }
            base.Update();
        }
        private void LaunchProjectile()
        {
            if (gun && gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                CurrentCaughtProjectile.SuppressHitEffects = true;
                CurrentCaughtProjectile.collidesWithProjectiles = true;
                CurrentCaughtProjectile.UpdateCollisionMask();
                CurrentCaughtProjectile.transform.parent = null;
                CurrentCaughtProjectile.specRigidbody.Position = new Position(HoldPosition() - ObjectCenterOffset(CurrentCaughtProjectile.gameObject));
                CurrentCaughtProjectile.baseData.speed = 30;
                if (gun.GunPlayerOwner().PlayerHasActiveSynergy("Negative Matter")) CurrentCaughtProjectile.baseData.speed *= 1.5f;
                CurrentCaughtProjectile.UpdateSpeed();
                Vector2 vector = gun.CurrentOwner.CenterPosition;
                Vector2 normalized = ((gun.CurrentOwner as PlayerController).unadjustedAimPoint.XY() - vector).normalized;
                CurrentCaughtProjectile.SendInDirection(normalized, true, false);
                PostLaunchProjectile(CurrentCaughtProjectile);
                CurrentCaughtProjectile = null;
            }
        }
        public Projectile CurrentCaughtProjectile;
        private Vector2 ObjectCenterOffset(GameObject thing)
        {
            Vector2 position = thing.transform.position;
            Vector2 offset = Vector2.zero;
            if (thing.GetComponent<tk2dSprite>()) offset = thing.GetComponent<tk2dSprite>().WorldCenter - position;
            else if (thing.GetComponent<SpeculativeRigidbody>()) offset = thing.GetComponent<SpeculativeRigidbody>().UnitCenter - position;
            else
            {
                Debug.LogError("NN: tk2dSprite AND SpeculativeRigidbody are null in the selected gameobject!");
            }
            return offset;
        }
        private void CatchObject(GameObject thingy)
        {
            AkSoundEngine.PostEvent("Play_WPN_RechargeGun_Recharge_01", gun.GunPlayerOwner().gameObject);

            if (gun.GunPlayerOwner())
            {
                IPlayerInteractable @interface = thingy.GetInterface<IPlayerInteractable>();
                if (@interface != null)
                {
                    RoomHandler roomFromPosition = GameManager.Instance.Dungeon.GetRoomFromPosition(thingy.transform.position.IntXY(VectorConversions.Round));
                    if (roomFromPosition.IsRegistered(@interface))
                    {
                        roomFromPosition.DeregisterInteractable(@interface);
                    }
                }

                if (thingy.GetComponent<MinorBreakable>())
                {
                    thingy.GetComponent<MinorBreakable>().OnlyBrokenByCode = true;
                    thingy.GetComponent<MinorBreakable>().isInvulnerableToGameActors = true;
                    thingy.GetComponent<MinorBreakable>().resistsExplosions = true;

                    thingy.transform.position = HoldPosition() - ObjectCenterOffset(thingy);
                    //if (thingy.GetComponent<SpeculativeRigidbody>()) thingy.GetComponent<SpeculativeRigidbody>().Position = new Position(HoldPosition() - ObjectCenterOffset(CurrentCaughtProjectile.gameObject));
                    thingy.transform.parent = gun.transform;
                    Projectile projectile = thingy.GetOrAddComponent<Projectile>();
                    projectile.Shooter = gun.GunPlayerOwner().specRigidbody;
                    projectile.Owner = gun.GunPlayerOwner().gameActor;
                    projectile.baseData.damage = 15;
                    projectile.baseData.range = 1000f;
                    projectile.baseData.speed = 0f;
                    projectile.collidesWithProjectiles = false;
                    projectile.baseData.force = 50f;
                    projectile.specRigidbody.CollideWithTileMap = true;
                    projectile.specRigidbody.Reinitialize();
                    projectile.specRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.Projectile;
                    projectile.Start();
                    projectile.projectileHitHealth = 20;
                    projectile.UpdateCollisionMask();
                    projectile.gameObject.AddComponent<GravityGunObjectDeathHandler>();
                    CurrentCaughtProjectile = projectile;
                }
                else if (thingy.GetComponent<MajorBreakable>())
                {
                    bool shouldBlockProjWhileHeld = false;
                    bool isTable = false;

                    thingy.GetComponent<MajorBreakable>().DamageReduction = 0.1f;
                    thingy.GetComponent<MajorBreakable>().IgnoreExplosions = true;

                    thingy.transform.position = HoldPosition() - ObjectCenterOffset(thingy);

                    if (thingy.GetComponentInParent<FlippableCover>())
                    {
                        isTable = true;
                        // ETGModConsole.Log("Found flippable");
                        MajorBreakable thingyBreakable = thingy.GetComponent<MajorBreakable>();
                        FlippableCover cover = thingy.GetComponentInParent<FlippableCover>();
                        SpeculativeRigidbody body = thingy.GetComponentInParent<SpeculativeRigidbody>();
                        //if (body) ETGModConsole.Log("Found body");
                        cover.shadowSprite.renderer.enabled = false;
                        if (cover.IsFlipped)
                        {
                            shouldBlockProjWhileHeld = true;
                        }
                        thingyBreakable.OnDamaged -= cover.Damaged;
                        thingyBreakable.OnBreak -= cover.DestroyCover;
                        for (int i = body.OnPostRigidbodyMovement.GetInvocationList().Count() - 1; i >= 0; i--)
                        {
                            Delegate Assignment = body.OnPostRigidbodyMovement.GetInvocationList()[i];
                            if (Assignment.Method.ToString().Contains("OnPostMovement"))
                            {
                                body.OnPostRigidbodyMovement = (Action<SpeculativeRigidbody, Vector2, IntVector2>)Delegate.Remove(body.OnPostRigidbodyMovement, Assignment);
                                //ETGModConsole.Log("Removed the Rigid Body Deligate");
                            }

                        }


                        UnityEngine.Object.Destroy(thingy.GetComponentInParent<FlippableCover>());
                    }

                    if (thingy.GetComponent<Chest>())
                    {
                        thingy.GetComponent<Chest>().pickedUp = true;
                        thingy.GetComponent<Chest>().contents = null;
                        thingy.GetComponent<Chest>().ForceKillFuse();
                        if (thingy.GetComponent<Chest>().ChestIdentifier == Chest.SpecialChestIdentifier.SECRET_RAINBOW) thingy.GetComponent<Chest>().RevealSecretRainbow();
                        thingy.GetComponent<Chest>().DeregisterChestOnMinimap();
                    }

                    thingy.transform.parent = gun.transform;

                    // ETGModConsole.Log(thingy.name);

                    Projectile projectile = thingy.GetOrAddComponent<Projectile>();
                    if (projectile.specRigidbody) projectile.specRigidbody.Initialize();
                    projectile.Shooter = gun.GunPlayerOwner().specRigidbody;
                    projectile.Owner = gun.GunPlayerOwner().gameActor;
                    projectile.baseData.damage = 30;
                    if (thingy.GetComponent<Chest>() && !thingy.GetComponent<Chest>().IsOpen)
                    {
                        if (thingy.GetComponent<Chest>().IsRainbowChest || thingy.GetComponent<Chest>().IsGlitched) projectile.baseData.damage *= 100000;
                        else projectile.baseData.damage *= 2;
                    }
                    if (isTable && gun && gun.GunPlayerOwner() && gun.GunPlayerOwner().PlayerHasActiveSynergy("Hidden Tech Nitro"))
                    {
                        ExplosiveModifier boom = projectile.gameObject.GetOrAddComponent<ExplosiveModifier>(); 
                        boom.explosionData = PickupObjectDatabase.GetById(398)?.GetComponent<TableFlipItem>().ProjectileExplosionData;
                        if (gun.GunPlayerOwner().HasActiveBonusSynergy(CustomSynergyType.ROCKET_POWERED_TABLES))
                        {
                            HomingModifier homingModifier = projectile.gameObject.AddComponent<HomingModifier>();
                            homingModifier.AssignProjectile(projectile);
                            homingModifier.HomingRadius = 20f;
                            homingModifier.AngularVelocity = 720f;
                            BounceProjModifier bounceProjModifier = projectile.gameObject.AddComponent<BounceProjModifier>();
                            bounceProjModifier.numberOfBounces = 4;
                            bounceProjModifier.onlyBounceOffTiles = true;
                        }
                    }
                    projectile.baseData.range = 1000f;
                    projectile.baseData.speed = 0f;
                    if (shouldBlockProjWhileHeld) projectile.collidesWithProjectiles = true;
                    else projectile.collidesWithProjectiles = false;
                    projectile.pierceMinorBreakables = true;
                    projectile.baseData.force = 50f;
                    projectile.specRigidbody.CollideWithTileMap = true;
                    projectile.specRigidbody.Reinitialize();

                    projectile.specRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.Projectile;
                    projectile.Start();
                    projectile.projectileHitHealth = 20;
                    projectile.UpdateCollisionMask();
                    thingy.AddComponent<GravityGunObjectDeathHandler>();

                    CurrentCaughtProjectile = projectile;

                }
                else if (thingy.GetComponent<AIActor>())
                {
                    SpeculativeRigidbody body = thingy.GetComponent<SpeculativeRigidbody>();
                    HealthHaver healthiness = thingy.GetComponent<HealthHaver>();
                    bool isNotMineFlayerBell = true;
                    if (thingy.GetComponent<BulletKingToadieController>())
                    {
                        for (int i = body.OnPreRigidbodyCollision.GetInvocationList().Count() - 1; i >= 0; i--)
                        {
                            Delegate Assignment = body.OnPreRigidbodyCollision.GetInvocationList()[i];
                            if (Assignment.Method.ToString().Contains("PreRigidbodyCollision"))
                            {
                                body.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Remove(body.OnPreRigidbodyCollision, Assignment);
                            }

                        }
                    }
                    if (thingy.GetComponent<AIActor>().EnemyGuid == "78a8ee40dff2477e9c2134f6990ef297") //Is Bell
                    {
                        if (thingy.GetComponent<AIActor>().IsSecretlyTheMineFlayer())
                        {
                            isNotMineFlayerBell = false;
                            thingy.GetComponent<HealthHaver>().ApplyDamage(2.14748365E+09f, Vector2.zero, "Begone Thot", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                        }
                    }
                    if (isNotMineFlayerBell)
                    {
                        if (thingy.GetComponent<BehaviorSpeculator>()) thingy.GetComponent<BehaviorSpeculator>().Stun(1000000000000, true);
                        thingy.GetComponent<AIActor>().FallingProhibited = true;
                        thingy.transform.position = HoldPosition() - ObjectCenterOffset(thingy);

                        thingy.transform.parent = gun.transform;
                        Projectile projectile = thingy.GetOrAddComponent<Projectile>();
                        projectile.Shooter = gun.GunPlayerOwner().specRigidbody;
                        projectile.DestroyMode = Projectile.ProjectileDestroyMode.DestroyComponent;
                        projectile.Owner = gun.GunPlayerOwner().gameActor;

                        if (thingy.GetComponent<HealthHaver>().GetMaxHealth() < 500) projectile.baseData.damage = thingy.GetComponent<HealthHaver>().GetMaxHealth();
                        else projectile.baseData.damage = 500;
                        projectile.baseData.range = 1000f;
                        projectile.baseData.speed = 0f;
                        projectile.baseData.force = 50f;
                        projectile.collidesWithProjectiles = false;
                        projectile.pierceMinorBreakables = true;
                        projectile.specRigidbody.CollideWithTileMap = true;
                        projectile.specRigidbody.CollideWithOthers = true;
                        projectile.collidesWithEnemies = true;
                        projectile.specRigidbody.Reinitialize();
                        projectile.specRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.Projectile;
                        projectile.Start();
                        projectile.projectileHitHealth = 20;
                        projectile.UpdateCollisionMask();
                        projectile.gameObject.AddComponent<GravityGunObjectDeathHandler>();
                        CurrentCaughtProjectile = projectile;
                    }
                }
            }
            //if (this.CurrentCaughtProjectile) ETGModConsole.Log(CurrentCaughtProjectile.name);
        }
        private float HealthMaxValue()
        {
            float start = 15;
            if (gun.GunPlayerOwner() && gun.GunPlayerOwner().PlayerHasActiveSynergy("Negative Matter")) start = 20;
            start *= AIActor.BaseLevelHealthModifier;
            return start;
        }
        private void PostLaunchProjectile(Projectile bullet)
        {
            if (bullet.ProjectilePlayerOwner())
            {
                PlayerController owner = bullet.ProjectilePlayerOwner();
                if (owner.PlayerHasActiveSynergy("Negative Matter")) bullet.damageTypes |= CoreDamageTypes.Electric;
                bullet.baseData.damage *= owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                bullet.baseData.speed *= owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                bullet.baseData.force *= owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                bullet.BossDamageMultiplier *= owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                //bullet.RuntimeUpdateScale(owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale));
                if (owner.PlayerHasActiveSynergy("Red Letter Day"))
                {
                    if (bullet.gameObject.GetComponent<MinorBreakable>() && (UnityEngine.Random.value <= 0.1f || bullet.gameObject.GetComponent<MinorBreakable>().name.ToLower().Contains("crate")))
                    {
                        int selection = UnityEngine.Random.Range(1, 5);
                        switch (selection)
                        {
                            case 1:
                                bullet.statusEffectsToApply.Add(StaticStatusEffects.irradiatedLeadEffect);
                                break;
                            case 2:
                                bullet.statusEffectsToApply.Add(StaticStatusEffects.hotLeadEffect);
                                break;
                            case 3:
                                ExplosiveModifier boom = bullet.gameObject.GetOrAddComponent<ExplosiveModifier>();
                                boom.explosionData = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
                                break;
                            case 4:
                                GravityGunObjectDeathHandler handler = bullet.gameObject.GetComponent<GravityGunObjectDeathHandler>();
                                if (handler) handler.AppliesGlitter = true;
                                break;
                        }
                    }
                }
                bullet.UpdateSpeed();
                owner.DoPostProcessProjectile(bullet);
                FakeVolleyModification(bullet, owner);
            }
        }

        private void FakeVolleyModification(Projectile bullet, PlayerController owner)
        {
            Dictionary<Projectile, float> CurrentVolley = new Dictionary<Projectile, float>();
            CurrentVolley.Add(bullet, bullet.Direction.ToAngle());
            foreach (PassiveItem item in owner.passiveItems)
            {
                if (item.PickupObjectId == 241) //Scattershot
                {
                    int volleyCount = CurrentVolley.Count;
                    for (int i = 0; i < volleyCount; i++)
                    {
                        float Angle = ProjSpawnHelper.GetAccuracyAngled(CurrentVolley[CurrentVolley.Keys.ElementAt(i)], 25, owner);
                        float Angle2 = ProjSpawnHelper.GetAccuracyAngled(CurrentVolley[CurrentVolley.Keys.ElementAt(i)], 25, owner);
                        Projectile newBullet = CloneProjectileForFakeVolley(CurrentVolley.Keys.ElementAt(i), Angle, owner);
                        Projectile newBullet2 = CloneProjectileForFakeVolley(CurrentVolley.Keys.ElementAt(i), Angle2, owner);
                        CurrentVolley.Add(newBullet, Angle);
                        CurrentVolley.Add(newBullet2, Angle2);
                    }
                }
                if (item.PickupObjectId == 287) //Backup Gun
                {
                    int volleyCount = CurrentVolley.Count;
                    for (int i = 0; i < volleyCount; i++)
                    {
                        float Angle = ProjSpawnHelper.GetAccuracyAngled(CurrentVolley[CurrentVolley.Keys.ElementAt(i)] + 180, 5, owner);
                        Projectile newBullet = CloneProjectileForFakeVolley(CurrentVolley.Keys.ElementAt(i), Angle, owner);
                        CurrentVolley.Add(newBullet, Angle);
                    }
                }
                if (item.PickupObjectId == CrossBullets.CrossBulletsID)
                {
                    if (item.GetComponent<CrossBullets>() && item.GetComponent<CrossBullets>().isActive)
                    {
                        int volleyCount = CurrentVolley.Count;
                        for (int i = 0; i < volleyCount; i++)
                        {
                            float Angle = ProjSpawnHelper.GetAccuracyAngled(CurrentVolley[CurrentVolley.Keys.ElementAt(i)] - 90, 10, owner);
                            float Angle2 = ProjSpawnHelper.GetAccuracyAngled(CurrentVolley[CurrentVolley.Keys.ElementAt(i)] + 90, 10, owner);
                            float Angle3 = ProjSpawnHelper.GetAccuracyAngled(CurrentVolley[CurrentVolley.Keys.ElementAt(i)] + 180, 10, owner);
                            Projectile newBullet = CloneProjectileForFakeVolley(CurrentVolley.Keys.ElementAt(i), Angle, owner);
                            Projectile newBullet2 = CloneProjectileForFakeVolley(CurrentVolley.Keys.ElementAt(i), Angle2, owner);
                            Projectile newBullet3 = CloneProjectileForFakeVolley(CurrentVolley.Keys.ElementAt(i), Angle3, owner);
                            CurrentVolley.Add(newBullet, Angle);
                            CurrentVolley.Add(newBullet2, Angle2);
                            CurrentVolley.Add(newBullet3, Angle3);
                        }
                    }
                }
            }
            foreach (PlayerItem active in owner.activeItems)
            {
                if (active.PickupObjectId == 168)
                {
                    if (active.IsCurrentlyActive)
                    {
                        int volleyCount = CurrentVolley.Count;
                        for (int i = 0; i < volleyCount; i++)
                        {
                            float Angle = ProjSpawnHelper.GetAccuracyAngled(CurrentVolley[CurrentVolley.Keys.ElementAt(i)], 20, owner);
                            Projectile newBullet = CloneProjectileForFakeVolley(CurrentVolley.Keys.ElementAt(i), Angle, owner);
                            CurrentVolley.Add(newBullet, Angle);
                        }
                    }
                }
            }
        }
        private Projectile CloneProjectileForFakeVolley(Projectile bullet, float angle, PlayerController owner)
        {
            GameObject gameObject2 = SpawnManager.SpawnProjectile(bullet.gameObject, bullet.sprite.WorldCenter, Quaternion.Euler(0f, 0f, angle));
            Projectile component2 = gameObject2.GetComponent<Projectile>();
            component2.specRigidbody.Reinitialize();
            component2.collidesWithPlayer = false;
            component2.Owner = owner;
            component2.Shooter = owner.specRigidbody;
            AIActor originalActor = bullet.GetComponent<AIActor>();
            AIActor actor = gameObject2.GetComponent<AIActor>();
            if (actor != null && originalActor != null)
            {
                actor.CustomLootTable = originalActor.CustomLootTable;
                actor.CustomLootTableMaxDrops = originalActor.CustomLootTableMaxDrops;
                actor.CustomLootTableMinDrops = originalActor.CustomLootTableMinDrops;
                //actor.CorpseObject = originalActor.CorpseObject;
                actor.CanDropCurrency = originalActor.CanDropCurrency;
                actor.AssignedCurrencyToDrop = originalActor.AssignedCurrencyToDrop;
                component2.DestroyMode = Projectile.ProjectileDestroyMode.DestroyComponent;
                actor.ParentRoom = bullet.GetAbsoluteRoom();
            }
            Chest originalChest = bullet.GetComponent<Chest>();
            Chest newChest = gameObject2.GetComponent<Chest>();
            if (originalChest != null && newChest != null)
            {
                if (originalChest.IsMimic)
                {
                    newChest.MaybeBecomeMimic();
                }
            }
            return component2;
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (CurrentCaughtProjectile)
            {
                CurrentCaughtProjectile.transform.parent = null;
                if (CurrentCaughtProjectile.specRigidbody != null) CurrentCaughtProjectile.specRigidbody.Position = new Position(HoldPosition() - ObjectCenterOffset(CurrentCaughtProjectile.gameObject));
                CurrentCaughtProjectile.DieInAir(false, true, true, false);
            }
            base.OnReloadPressed(player, gun, manualReload);
        }
        public static List<string> GravityGunOverrideGrabGuids = new List<string>()
        {
            "f38686671d524feda75261e469f30e0b", //Ammoconda Ball
            "b5e699a0abb94666bda567ab23bd91c4", //Chancellor
            "d4dd2b2bbda64cc9bcec534b4e920518", //Vengeful Chancellor
            "02a14dec58ab45fb8aacde7aacd25b01", //Old Chancellor
            "76bc43539fc24648bff4568c75c686d1", //Chicken
            "f155fd2759764f4a9217db29dd21b7eb", //Mountain Cube
            //DET GUIDS
            "ac986dabc5a24adab11d48a4bccf4cb1",
            "48d74b9c65f44b888a94f9e093554977",
            "c5a0fd2774b64287bf11127ca59dd8b4",
            "b67ffe82c66742d1985e5888fd8e6a03",
            "d9632631a18849539333a92332895ebd",
            "1898f6fe1ee0408e886aaf05c23cc216",
            "abd816b0bcbf4035b95837ca931169df",
            "07d06d2b23cc48fe9f95454c839cb361",
        };
        public static List<string> BlobGuids = new List<string>()
        {
            "0239c0680f9f467dbe5c4aab7dd1eca6", //Blobulon
            "042edb1dfb614dc385d5ad1b010f2ee3", //Blobuloid
            "42be66373a3d4d89b91a35c9ff8adfec", //Blobulin
            "e61cab252cfb435db9172adc96ded75f", //Poisbulon
            "fe3fe59d867347839824d5d9ae87f244", //Poisbuloid
            "b8103805af174924b578c98e95313074", //Poisbulin
            "022d7c822bc146b58fe3b0287568aaa2", //Blizzbulon
            "ccf6d241dad64d989cbcaca2a8477f01", //LeadBulon
            "062b9b64371e46e195de17b6f10e47c8", //Bloodbulon
            "116d09c26e624bca8cca09fc69c714b3", //Poopulon
            "864ea5a6a9324efc95a0dd2407f42810", //Cubulon
            "0b547ac6b6fc4d68876a241a88f5ca6a", //Cubulead
            "1bc2a07ef87741be90c37096910843ab", //Chancebulon
        };
        private static void HandleMimicOverrideBreak(PlayerController player, Chest mimic)
        {
            List<PickupObject> Payout = mimic.PredictContents(player);
            if (Payout.Count > 0)
            {
                if (GameStatsManager.Instance.IsRainbowRun)
                {
                    LootEngine.SpawnBowlerNote(GameManager.Instance.RewardManager.BowlerNoteMimic, mimic.sprite.WorldCenter, GameManager.Instance.Dungeon.GetRoomFromPosition(mimic.sprite.WorldCenter.ToIntVector2()), true);
                }
                else
                {
                    for (int i = Payout.Count - 1; i >= 0; i--)
                    {
                        LootEngine.SpawnItem(Payout[i].gameObject, mimic.sprite.WorldCenter, Vector2.zero, 1f, false, true, false);
                    }
                }
            }
        }
        private static void HandleChestOverrideBreak(PlayerController player, Chest chest)
        {
            GameManager.Instance.Dungeon.GeneratedMagnificence -= chest.GeneratedMagnificence;
            if (chest.spawnAnimName.StartsWith("wood_"))
            {
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.WOODEN_CHESTS_BROKEN, 1f);
            }
            if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && GameManager.Instance.NumberOfLivingPlayers == 1)
            {
                PlayerController deadPlayer = (!GameManager.Instance.PrimaryPlayer.healthHaver.IsDead) ? GameManager.Instance.SecondaryPlayer : GameManager.Instance.PrimaryPlayer;
                deadPlayer.specRigidbody.enabled = true;
                deadPlayer.gameObject.SetActive(true);
                deadPlayer.sprite.renderer.enabled = true;
                deadPlayer.ResurrectFromChest(chest.sprite.WorldBottomCenter);
            }
            else
            {
                List<PickupObject> Payout = new List<PickupObject>();

                bool flag = PassiveItem.IsFlagSetAtAll(typeof(ChestBrokenImprovementItem));
                bool flag2 = GameStatsManager.Instance.GetFlag(GungeonFlags.ITEMSPECIFIC_GOLD_JUNK);
                float num = GameManager.Instance.RewardManager.ChestDowngradeChance;
                float num2 = GameManager.Instance.RewardManager.ChestHalfHeartChance;
                float num3 = GameManager.Instance.RewardManager.ChestExplosionChance;
                float num4 = GameManager.Instance.RewardManager.ChestJunkChance;
                float num5 = (!flag2) ? 0f : 0.005f;
                bool flag3 = GameStatsManager.Instance.GetFlag(GungeonFlags.ITEMSPECIFIC_SER_JUNKAN_UNLOCKED);
                float num6 = (!flag3 || Chest.HasDroppedSerJunkanThisSession) ? 0f : GameManager.Instance.RewardManager.ChestJunkanUnlockedChance;
                if (GameManager.Instance.PrimaryPlayer && GameManager.Instance.PrimaryPlayer.carriedConsumables.KeyBullets > 0)
                {
                    num4 *= GameManager.Instance.RewardManager.HasKeyJunkMultiplier;
                }
                if (SackKnightController.HasJunkan())
                {
                    num4 *= GameManager.Instance.RewardManager.HasJunkanJunkMultiplier;
                    num5 *= 3f;
                }
                if (chest.IsTruthChest)
                {
                    num = 0f;
                    num2 = 0f;
                    num3 = 0f;
                    num4 = 1f;
                }
                num4 -= num5;
                float num7 = num5 + num + num2 + num3 + num4 + num6;
                float num8 = UnityEngine.Random.value * num7;
                bool isTryingToPayOutWithItem = false;
                if (flag2 && num8 < num5)
                {
                    int goldJunk = GlobalItemIds.GoldJunk;
                    Payout.Add(PickupObjectDatabase.GetById(goldJunk));
                }
                else if (num8 < num || flag)
                {
                    int tierShift = -4;
                    isTryingToPayOutWithItem = true;
                    bool didShitEarlier = false;
                    if (flag)
                    {
                        float value = UnityEngine.Random.value;
                        if (value < ChestBrokenImprovementItem.PickupQualChance)
                        {
                            didShitEarlier = true;
                            PickupObject pickupObject = null;
                            while (pickupObject == null)
                            {
                                GameObject gameObject = GameManager.Instance.RewardManager.CurrentRewardData.SingleItemRewardTable.SelectByWeight(false);
                                if (gameObject)
                                {
                                    pickupObject = gameObject.GetComponent<PickupObject>();
                                }
                            }
                            Payout.Add(pickupObject);
                        }
                        else if (value < ChestBrokenImprovementItem.PickupQualChance + ChestBrokenImprovementItem.MinusOneQualChance)
                        {
                            tierShift = -1;
                        }
                        else if (value < ChestBrokenImprovementItem.PickupQualChance + ChestBrokenImprovementItem.EqualQualChance + ChestBrokenImprovementItem.MinusOneQualChance)
                        {
                            tierShift = 0;
                        }
                        else
                        {
                            tierShift = 1;
                        }
                    }
                    if (!didShitEarlier)
                    {
                        if (chest.forceContentIds.Count > 0)
                        {
                            for (int i = 0; i < chest.forceContentIds.Count; i++)
                            {
                                Payout.Add(PickupObjectDatabase.GetById(chest.forceContentIds[i]));
                            }
                        }
                        if (Payout.Count == 0 && !flag)
                        {
                            FloorRewardManifest seededManifestForCurrentFloor = GameManager.Instance.RewardManager.GetSeededManifestForCurrentFloor();
                            if (seededManifestForCurrentFloor != null && seededManifestForCurrentFloor.PregeneratedChestContents.ContainsKey(chest))
                            {
                                Payout = seededManifestForCurrentFloor.PregeneratedChestContents[chest];
                            }
                            else
                            {
                                Payout = OverrideGenerateChestContents(player, chest, tierShift, new System.Random());
                            }
                        }
                    }
                }
                else if (num8 < num + num2) Payout.Add(GameManager.Instance.RewardManager.HalfHeartPrefab);
                else if (num8 < num + num2 + num4)
                {
                    bool flag5 = false;
                    if (!Chest.HasDroppedSerJunkanThisSession && !flag3 && UnityEngine.Random.value < 0.2f)
                    {
                        Chest.HasDroppedSerJunkanThisSession = true;
                        flag5 = true;
                    }
                    int id = (chest.overrideJunkId < 0) ? GlobalItemIds.Junk : chest.overrideJunkId;
                    if (flag5)
                    {
                        id = GlobalItemIds.SackKnightBoon;
                    }
                    Payout.Add(PickupObjectDatabase.GetById(id));
                }
                else if (num8 < num + num2 + num4 + num6)
                {
                    Chest.HasDroppedSerJunkanThisSession = true;
                    Payout.Add(PickupObjectDatabase.GetById(GlobalItemIds.SackKnightBoon));
                }
                else
                {
                    Exploder.DoDefaultExplosion(chest.sprite.WorldCenter, Vector2.zero, null, false, CoreDamageTypes.None, false);
                }
                if (Payout.Count > 0)
                {
                    if (isTryingToPayOutWithItem && GameStatsManager.Instance.IsRainbowRun)
                    {
                        LootEngine.SpawnBowlerNote(GameManager.Instance.RewardManager.BowlerNoteChest, chest.sprite.WorldCenter, GameManager.Instance.Dungeon.GetRoomFromPosition(chest.sprite.WorldCenter.ToIntVector2()), true);
                    }
                    else
                    {
                        for (int i = Payout.Count - 1; i >= 0; i--)
                        {
                            LootEngine.SpawnItem(Payout[i].gameObject, chest.sprite.WorldCenter, Vector2.zero, 1f, false, true, false);
                        }
                    }
                }
            }
            for (int k = 0; k < GameManager.Instance.AllPlayers.Length; k++)
            {
                if (GameManager.Instance.AllPlayers[k].OnChestBroken != null)
                {
                    GameManager.Instance.AllPlayers[k].OnChestBroken(GameManager.Instance.AllPlayers[k], chest);
                }
            }
        }
        private static List<PickupObject> OverrideGenerateChestContents(PlayerController player, Chest chest, int tierShift, System.Random safeRandom = null)
        {
            List<PickupObject> list = new List<PickupObject>();
            if (chest.lootTable.lootTable == null)
            {
                list.Add(GameManager.Instance.Dungeon.baseChestContents.SelectByWeight(false).GetComponent<PickupObject>());
            }
            else if (chest.lootTable != null)
            {
                if (tierShift <= -1)
                {
                    if (chest.breakertronLootTable.lootTable != null)
                    {
                        list = chest.breakertronLootTable.GetItemsForPlayer(player, -1, null, safeRandom);
                    }
                    else
                    {
                        list = chest.lootTable.GetItemsForPlayer(player, tierShift, null, safeRandom);
                    }
                }
                else
                {
                    list = chest.lootTable.GetItemsForPlayer(player, tierShift, null, safeRandom);
                    if (chest.lootTable.CompletesSynergy)
                    {
                        float num = Mathf.Clamp01(0.6f - 0.1f * (float)chest.lootTable.LastGenerationNumSynergiesCalculated);
                        num = Mathf.Clamp(num, 0.2f, 1f);
                        if (num > 0f)
                        {
                            float num2 = (safeRandom == null) ? UnityEngine.Random.value : ((float)safeRandom.NextDouble());
                            if (num2 < num)
                            {
                                chest.lootTable.CompletesSynergy = false;
                                list = chest.lootTable.GetItemsForPlayer(player, tierShift, null, safeRandom);
                                chest.lootTable.CompletesSynergy = true;
                            }
                        }
                    }
                }
            }
            return list;
        }
        public class GravityGunObjectDeathHandler : MonoBehaviour
        {
            public GravityGunObjectDeathHandler()
            {
            }
            private void Start()
            {
                this.m_projectile = base.GetComponent<Projectile>();
                this.m_projectile.wallDecals = new VFXPool() { type = VFXPoolType.None, effects = new VFXComplex[0] };
                this.m_projectile.damageTypes = CoreDamageTypes.None;
                this.m_projectile.damagesWalls = false;
                this.m_projectile.OnDestruction += this.Destruction;
                this.m_projectile.specRigidbody.OnCollision += this.ProjectileCollision;
                this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
                this.m_projectile.OnHitEnemy += this.OnHitEnemy;
                this.m_projectile.specRigidbody.CollideWithOthers = true;
                this.m_projectile.specRigidbody.CollideWithTileMap = true;
                this.m_projectile.UpdateCollisionMask();
                this.AppliesGlitter = false;
                if (this.m_projectile.GetComponent<AIActor>() != null)
                {
                    AIActor actor = this.m_projectile.GetComponent<AIActor>();
                    if (actor.specRigidbody)
                    {
                        actor.specRigidbody.CollideWithOthers = true;
                        actor.specRigidbody.CollideWithTileMap = true;
                    }
                }
            }
            private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
            {
                if (!fatal && AppliesGlitter)
                {
                    if (enemy && enemy.aiActor)
                    {
                        enemy.aiActor.ApplyGlitter();
                    }
                }
            }
            private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
            {
                if (otherRigidbody && otherRigidbody.projectile != null && otherRigidbody.projectile.ProjectilePlayerOwner())
                {
                    PhysicsEngine.SkipCollision = true;
                }
            }
            private void ProjectileCollision(CollisionData data)
            {
                if (data.OtherRigidbody && data.OtherRigidbody.GetComponent<MinorBreakable>() && !data.OtherRigidbody.GetComponent<Projectile>())
                {
                    if (data.MyRigidbody.GetComponent<MinorBreakable>())
                    {
                        data.OtherRigidbody.GetComponent<MinorBreakable>().Break();
                    }
                }
            }
            private void Destruction(Projectile bullet)
            {
                if (bullet.GetComponent<AIActorIsKevin>())
                {
                    if (bullet && bullet.ProjectilePlayerOwner())
                    {
                        bullet.ProjectilePlayerOwner().DoEasyBlank(bullet.specRigidbody.UnitCenter, EasyBlankType.FULL);
                    }
                }
                if (bullet.gameObject.GetComponent<MinorBreakable>())
                {
                    bullet.gameObject.GetComponent<MinorBreakable>().Break();
                    var type = typeof(MinorBreakable);
                    var func = type.GetMethod("OnBreakAnimationComplete", BindingFlags.Instance | BindingFlags.NonPublic);
                    var ret = func.Invoke(bullet.gameObject.GetComponent<MinorBreakable>(), null);
                }
                else if (bullet.gameObject.GetComponent<MajorBreakable>())
                {
                    UnityEngine.Object.Instantiate<GameObject>(GravityGun.MajorBreakableImpactVFX, bullet.sprite.WorldCenter, Quaternion.identity);

                    if (bullet.gameObject.GetComponentInChildren<Chest>() && !bullet.gameObject.GetComponentInChildren<Chest>().IsOpen)
                    {
                        if (bullet.gameObject.GetComponentInChildren<Chest>().IsMimic)
                        {
                            HandleMimicOverrideBreak(bullet.Owner as PlayerController, bullet.gameObject.GetComponentInChildren<Chest>());
                        }
                        else
                        {
                            HandleChestOverrideBreak(bullet.Owner as PlayerController, bullet.gameObject.GetComponentInChildren<Chest>());
                            Exploder.Explode(bullet.specRigidbody.UnitCenter, GravityGun.ChestExplosionData, Vector2.zero);
                        }
                    }
                    bullet.gameObject.GetComponent<MajorBreakable>().Break(Vector2.zero);
                }
                else if (bullet.gameObject.GetComponent<AIActor>() && bullet.gameObject.GetComponent<HealthHaver>())
                {
                    if (bullet.GetAbsoluteRoom() != bullet.gameObject.GetComponent<AIActor>().ParentRoom)
                    {
                        if (bullet.gameObject.GetComponent<SpawnEnemyOnDeath>() != null)
                        {
                            UnityEngine.Object.Destroy(bullet.gameObject.GetComponent<SpawnEnemyOnDeath>());
                        }
                    }
                    bullet.gameObject.GetComponent<HealthHaver>().ApplyDamage(2.14748365E+09f, Vector2.zero, "Yeet", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                    bullet.gameObject.AddComponent<AIActorConstantKill>();
                }

            }
            private Projectile m_projectile;
            public bool AppliesGlitter;
        }
        public class AIActorConstantKill : MonoBehaviour
        {
            public AIActorConstantKill()
            {

            }
            public void Start()
            {
                this.actor = base.GetComponent<AIActor>();

            }
            public void Update()
            {
                if (actor && actor.healthHaver && actor.healthHaver.IsAlive)
                {
                    actor.healthHaver.ApplyDamage(2.14748365E+09f, Vector2.zero, "ConstantKill", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                }
            }
            private AIActor actor;
        }

    }
}
