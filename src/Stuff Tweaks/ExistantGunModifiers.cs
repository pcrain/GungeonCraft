using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.ItemAPI;
using UnityEngine;
using Alexandria.Misc;

namespace NevernamedsItems
{
    class ExistantGunModifiers
    {
        public static List<Projectile> Planets;
        public static void Init()
        {
            (PickupObjectDatabase.GetById(748) as Gun).gameObject.AddComponent<SunlightJavelinModifiers>();
            (PickupObjectDatabase.GetById(539) as Gun).gameObject.AddComponent<BoxingGloveModifiers>();
            (PickupObjectDatabase.GetById(506) as Gun).gameObject.AddComponent<ReallySpecialLuteModifiers>();
            (PickupObjectDatabase.GetById(93) as Gun).gameObject.AddComponent<OldGoldieModifiers>();
            (PickupObjectDatabase.GetById(32) as Gun).gameObject.AddComponent<VoidMarshalModifiers>();
            (PickupObjectDatabase.GetById(184) as Gun).gameObject.AddComponent<JudgeModifiers>();
            (PickupObjectDatabase.GetById(562) as Gun).gameObject.AddComponent<FatLineModifiers>();
            (PickupObjectDatabase.GetById(50) as Gun).gameObject.AddComponent<SAAModifiers>();
            (PickupObjectDatabase.GetById(197) as Gun).gameObject.AddComponent<PeashooterModifiers>();
            (PickupObjectDatabase.GetById(476) as Gun).gameObject.AddComponent<MTXGunModifiers>();
            (PickupObjectDatabase.GetById(576) as Gun).gameObject.AddComponent<RobotsLeftHandModifiers>();
            (PickupObjectDatabase.GetById(149) as Gun).gameObject.AddComponent<FaceMelterModifiers>();
            (PickupObjectDatabase.GetById(275) as Gun).gameObject.AddComponent<FlareGunModifiers>();
            (PickupObjectDatabase.GetById(481) as Gun).gameObject.AddComponent<CameraModifiers>();
            (PickupObjectDatabase.GetById(402) as Gun).gameObject.AddComponent<SnowballerModifiers>();
            (PickupObjectDatabase.GetById(33) as Gun).gameObject.AddComponent<TearJerkerModifiers>();
            (PickupObjectDatabase.GetById(596) as Gun).gameObject.AddComponent<TeapotModifiers>();

            Planets = new List<Projectile>()
        {
            (PickupObjectDatabase.GetById(597) as Gun).DefaultModule.projectiles[0], //Mercury
            (PickupObjectDatabase.GetById(597) as Gun).DefaultModule.projectiles[1], //Venus
            (PickupObjectDatabase.GetById(597) as Gun).DefaultModule.projectiles[2], //Earty
            (PickupObjectDatabase.GetById(597) as Gun).DefaultModule.projectiles[3],
            (PickupObjectDatabase.GetById(597) as Gun).DefaultModule.projectiles[4],
            (PickupObjectDatabase.GetById(597) as Gun).DefaultModule.projectiles[5],
            (PickupObjectDatabase.GetById(597) as Gun).DefaultModule.projectiles[6],
            (PickupObjectDatabase.GetById(597) as Gun).DefaultModule.projectiles[7],
        };
        }
    }
    public class TeapotModifiers : MonoBehaviour
    {
        private Gun self;
        private void Start()
        {
            self = base.GetComponent<Gun>();
            self.OnPreFireProjectileModifier += OnPreFireProjectileModifier;
        }
        private Projectile OnPreFireProjectileModifier(Gun sourceGun, Projectile sourceProjectile, ProjectileModule sourceModule)
        {
            if (sourceGun && sourceGun.GunPlayerOwner() && sourceGun.GunPlayerOwner().PlayerHasActiveSynergy("Russel's Teapot"))
            {
                if (UnityEngine.Random.value <= 0.15f)
                {
                    return BraveUtility.RandomElement(ExistantGunModifiers.Planets);
                }
            }
            return sourceProjectile;
        }
    }
    public class CameraModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.ProjectilePlayerOwner())
            {
                PlayerController player = projectile.ProjectilePlayerOwner();
                if (player.PlayerHasActiveSynergy("Infrared Camera"))
                {
                    foreach (PassiveItem item in player.passiveItems)
                    {
                        if (item is InfraredGuonStone)
                        {
                            GameObject orbital = (item as InfraredGuonStone).GimmeOrbital();
                            if (orbital != null)
                            {
                                float angle = 0;
                                for (int i = 0; i < 8; i++)
                                {
                                    BeamAPI.FreeFireBeamFromAnywhere(InfraredGuonStone.InfraredBeam, player, orbital, Vector2.zero, angle, 1);
                                    angle += 45;
                                }
                            }
                        }
                    }

                }
            }
            base.PostProcessProjectile(projectile);
        }
    }
    public class FlareGunModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (this.gun && this.gun.GunPlayerOwner())
            {
                if (this.gun.GunPlayerOwner().PlayerHasActiveSynergy("Mixed Signals"))
                {
                    projectile.baseData.speed *= 2f;
                    projectile.baseData.force += 40;
                    BounceProjModifier bonce = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                    bonce.numberOfBounces++;
                    projectile.UpdateSpeed();
                }
                base.PostProcessProjectile(projectile);
            }
        }
    }
    public class FaceMelterModifiers : GunBehaviour
    {
        public override void Update()
        {
            if (movementMod == null)
            {
                movementMod = new StatModifier()
                {
                    statToBoost = PlayerStats.StatType.MovementSpeed,
                    amount = 1.45f,
                    modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
                };
            }
            if (damageMod == null)
            {
                damageMod = new StatModifier()
                {
                    statToBoost = PlayerStats.StatType.Damage,
                    amount = 1.1f,
                    modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
                };
            }
            if (dodgeMod == null)
            {
                dodgeMod = new StatModifier()
                {
                    statToBoost = PlayerStats.StatType.DodgeRollSpeedMultiplier,
                    amount = 1.45f,
                    modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
                };
            }
            if (this.gun.GunPlayerOwner())
            {
                if (this.gun.CurrentOwner.CurrentGun == this.gun)
                {
                    if ((this.gun.CurrentOwner as PlayerController).PlayerHasActiveSynergy("Underground") && !synergyBoostActive)
                    {
                        GiveSynergyBoost();
                        synergyBoostActive = true;
                    }
                    else if (!(this.gun.CurrentOwner as PlayerController).PlayerHasActiveSynergy("Underground") && synergyBoostActive)
                    {
                        RemoveSynergyBoost();
                        synergyBoostActive = false;
                    }
                }
                else
                {
                    if (synergyBoostActive)
                    {
                        RemoveSynergyBoost();
                        synergyBoostActive = false;
                    }
                }
            }
        }
        public override void OnDropped()
        {
            if (synergyBoostActive)
            {
                RemoveSynergyBoost();
                synergyBoostActive = false;
            }
            base.OnDropped();
        }
        bool synergyBoostActive;
        StatModifier movementMod;
        StatModifier dodgeMod;
        StatModifier damageMod;
        private void GiveSynergyBoost()
        {
            ETGModConsole.Log("Gave boost");
            PlayerController playa = (this.gun.CurrentOwner as PlayerController);
            playa.baseFlatColorOverride = Color.blue.WithAlpha(1);
            playa.ownerlessStatModifiers.Add(movementMod);
            playa.ownerlessStatModifiers.Add(dodgeMod);
            playa.ownerlessStatModifiers.Add(damageMod);
            playa.stats.RecalculateStats(playa, true, false);

        }
        private void RemoveSynergyBoost()
        {
            PlayerController playa = (this.gun.CurrentOwner as PlayerController);
            playa.baseFlatColorOverride = Color.blue.WithAlpha(0);
            playa.ownerlessStatModifiers.Remove(movementMod);
            playa.ownerlessStatModifiers.Remove(dodgeMod);
            playa.ownerlessStatModifiers.Remove(damageMod);
            playa.stats.RecalculateStats(playa, true, false);
        }
    }
    public class JudgeModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (this.gun.CurrentOwner is PlayerController)
            {
                PlayerController playerController = this.gun.CurrentOwner as PlayerController;
                base.PostProcessProjectile(projectile);
                if (playerController.PlayerHasActiveSynergy("Court Marshal"))
                {
                    projectile.baseData.damage *= 1.2f;
                    projectile.baseData.speed *= 1.2f;
                    projectile.UpdateSpeed();
                }
            }
        }
    }
    public class VoidMarshalModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (this.gun.CurrentOwner is PlayerController)
            {
                PlayerController playerController = this.gun.CurrentOwner as PlayerController;
                base.PostProcessProjectile(projectile);
                if (playerController.PlayerHasActiveSynergy("Court Marshal"))
                {
                    projectile.baseData.damage *= 1.2f;
                    projectile.baseData.speed *= 1.2f;
                    projectile.UpdateSpeed();
                }
            }
        }
    }
    public class OldGoldieModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (this.gun.CurrentOwner is PlayerController)
            {
                PlayerController playerController = this.gun.CurrentOwner as PlayerController;
                base.PostProcessProjectile(projectile);
                if (playerController.PlayerHasActiveSynergy("The Classics"))
                {
                    projectile.baseData.range *= 2;
                }
            }
        }
    }
    public class ReallySpecialLuteModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController playerController = this.gun.CurrentOwner as PlayerController;
            base.PostProcessProjectile(projectile);
            if (playerController.PlayerHasActiveSynergy("Eternal Prose"))
            {
                projectile.baseData.range *= 10;
                projectile.baseData.speed *= 2;
            }
        }
    }
    public class BoxingGloveModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController playerController = this.gun.CurrentOwner as PlayerController;
            base.PostProcessProjectile(projectile);
            if (playerController.PlayerHasActiveSynergy("Gun Punch Man")) projectile.baseData.damage *= 2;
            if (playerController.PlayerHasActiveSynergy("Punch Line")) projectile.statusEffectsToApply.Add(StaticStatusEffects.charmingRoundsEffect);
            if (playerController.PlayerHasActiveSynergy("North Star")) projectile.statusEffectsToApply.Add(StaticStatusEffects.chaosBulletsFreeze);
        }
    }
    public class SunlightJavelinModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController playerController = this.gun.CurrentOwner as PlayerController;
            if (playerController.PlayerHasActiveSynergy("Grease Lightning")) projectile.baseData.damage *= 2;
            if (playerController.PlayerHasActiveSynergy("Gunderbolts and Lightning")) projectile.OnHitEnemy += this.AddFearEffect;
            base.PostProcessProjectile(projectile);
        }
        private FleePlayerData fleeData;
        private void AddFearEffect(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
        {
            PlayerController playerController = arg1.Owner as PlayerController;
            if (arg2 != null && arg2.aiActor != null && playerController != null && arg2 != null && arg2.healthHaver.IsAlive)
            {
                if (arg2.aiActor.EnemyGuid != "465da2bb086a4a88a803f79fe3a27677" && arg2.aiActor.EnemyGuid != "05b8afe0b6cc4fffa9dc6036fa24c8ec")
                {
                    StartCoroutine(HandleFear(playerController, arg2));
                }
            }
        }
        private IEnumerator HandleFear(PlayerController user, SpeculativeRigidbody enemy)
        {
            if (this.fleeData == null || this.fleeData.Player != user)
            {
                this.fleeData = new FleePlayerData();
                this.fleeData.Player = user;
                this.fleeData.StartDistance *= 2f;
            }
            if (enemy.aiActor.behaviorSpeculator != null)
            {
                enemy.aiActor.behaviorSpeculator.FleePlayerData = this.fleeData;
                FleePlayerData fleePlayerData = new FleePlayerData();
                yield return new WaitForSeconds(10f);
                enemy.aiActor.behaviorSpeculator.FleePlayerData.Player = null;
            }
        }
    }
    public class RobotsLeftHandModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Lefty Loosey"))
            {
                if (projectile.ProjectilePlayerOwner().SpriteFlipped)
                {
                    projectile.baseData.speed *= 1.5f;
                    projectile.UpdateSpeed();
                    PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                    pierce.penetration += 2;
                }
            }
        }
    }
    public class MTXGunModifiers : GunBehaviour
    {
        private void OnKilledEnemy(PlayerController player, HealthHaver enemy)
        {
            if (gun && player && enemy && player.PlayerHasActiveSynergy("Fully Funded"))
            {
                if (player.CurrentGun.PickupObjectId == 476 && enemy.GetMaxHealth() >= 10)
                {
                    //ETGModConsole.Log("Spawned bonus");
                    LootEngine.SpawnCurrency(enemy.specRigidbody.UnitCenter, 1, false);
                }
            }
        }
        public override void Update()
        {
            if (this && this.gun)
            {
                if (this.gun.GunPlayerOwner() != null && lastPlayerOwner == null)
                {
                    this.gun.GunPlayerOwner().OnKilledEnemyContext += this.OnKilledEnemy;

                    lastPlayerOwner = this.gun.GunPlayerOwner();
                }
                else if (this.gun.GunPlayerOwner() == null && lastPlayerOwner != null)
                {
                    lastPlayerOwner.OnKilledEnemyContext -= this.OnKilledEnemy;

                    lastPlayerOwner = null;
                }
            }
        }
        private PlayerController lastPlayerOwner;
        public override void OnDestroy()
        {
            if (this.gun.GunPlayerOwner() || lastPlayerOwner != null)
            {
                if (this.gun.GunPlayerOwner()) this.gun.GunPlayerOwner().OnKilledEnemyContext -= this.OnKilledEnemy;
                else if (lastPlayerOwner != null) lastPlayerOwner.OnKilledEnemyContext -= this.OnKilledEnemy;
            }
        }
    }
    public class SAAModifiers : GunBehaviour
    {
        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (gun.IsReloading)
            {
                if (!isReloading && player.PlayerHasActiveSynergy("Gunvana")) player.StartCoroutine(this.IncorporealityOnHit(player, 1));
                isReloading = true;
            }
            base.OnReloadPressed(player, gun, manualReload);
        }
        bool isReloading = false;
        public override void Update()
        {
            if (!gun.IsReloading && isReloading) isReloading = false;
        }

        private IEnumerator IncorporealityOnHit(PlayerController player, float incorporealityTime)
        {
            int enemyMask = CollisionMask.LayerToMask(CollisionLayer.EnemyCollider, CollisionLayer.EnemyHitBox, CollisionLayer.Projectile);
            player.specRigidbody.AddCollisionLayerIgnoreOverride(enemyMask);
            player.healthHaver.IsVulnerable = false;
            yield return null;
            float timer = 0f;
            float subtimer = 0f;
            while (timer < incorporealityTime)
            {
                while (timer < incorporealityTime)
                {
                    timer += BraveTime.DeltaTime;
                    subtimer += BraveTime.DeltaTime;
                    if (subtimer > 0.12f)
                    {
                        player.IsVisible = false;
                        subtimer -= 0.12f;
                        break;
                    }
                    yield return null;
                }
                while (timer < incorporealityTime)
                {
                    timer += BraveTime.DeltaTime;
                    subtimer += BraveTime.DeltaTime;
                    if (subtimer > 0.12f)
                    {
                        player.IsVisible = true;
                        subtimer -= 0.12f;
                        break;
                    }
                    yield return null;
                }
            }
            this.EndIncorporealityOnHit(player);
            yield break;
        }
        private void EndIncorporealityOnHit(PlayerController player)
        {
            int mask = CollisionMask.LayerToMask(CollisionLayer.EnemyCollider, CollisionLayer.EnemyHitBox, CollisionLayer.Projectile);
            player.IsVisible = true;
            player.healthHaver.IsVulnerable = true;
            player.specRigidbody.RemoveCollisionLayerIgnoreOverride(mask);
        }
    }
    public class FatLineModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            projectile.OnHitEnemy += this.OnHitEnemy;
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (bullet.Owner is PlayerController && (bullet.Owner as PlayerController).PlayerHasActiveSynergy("Parallel Lines"))
            {
                if (UnityEngine.Random.value <= 0.1f)
                {

                    if (UnityEngine.Random.value <= 0.5f)
                    {
                        GameObject silencerVFX = (GameObject)ResourceCache.Acquire("Global VFX/BlankVFX_Ghost");
                        AkSoundEngine.PostEvent("Play_OBJ_silenceblank_small_01", base.gameObject);
                        GameObject gameObject = new GameObject("silencer");
                        SilencerInstance silencerInstance = gameObject.AddComponent<SilencerInstance>();
                        float additionalTimeAtMaxRadius = 0.25f;
                        silencerInstance.TriggerSilencer(bullet.specRigidbody.UnitCenter, 25f, 5f, silencerVFX, 0f, 3f, 3f, 3f, 250f, 5f, additionalTimeAtMaxRadius, bullet.Owner as PlayerController, false, false);
                    }
                    else
                    {
                        ExplosionData data = DataCloners.CopyExplosionData(StaticExplosionDatas.explosiveRoundsExplosion);
                        data.ignoreList.Add(bullet.ProjectilePlayerOwner().specRigidbody);
                        Exploder.Explode(bullet.specRigidbody.UnitCenter, data, Vector2.zero);
                    }
                }
            }
        }
    }
    public class PeashooterModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.Owner && projectile.Owner is PlayerController && (projectile.Owner as PlayerController).PlayerHasActiveSynergy("Repeater"))
            {
                projectile.StartCoroutine(this.DoShadowDelayed(projectile));
            }
        }
        private IEnumerator DoShadowDelayed(Projectile projectile)
        {
            yield return null;
            projectile.SpawnChainedShadowBullets(1, 0.05f);
            yield break;
        }
    }
    public class SnowballerModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.ProjectilePlayerOwner() != null && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Frost-Key The Snowman"))
            {
                int multiplier = Mathf.Min(5, projectile.ProjectilePlayerOwner().carriedConsumables.KeyBullets);
                projectile.baseData.damage *= multiplier;
            }
        }
    }
    public class TearJerkerModifiers : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.ProjectilePlayerOwner() != null && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Cow, on a Trash Farm"))
            {
                if (UnityEngine.Random.value <= 0.25f)
                {
                    projectile.AdjustPlayerProjectileTint(ExtendedColours.poisonGreen, 1);
                    projectile.statusEffectsToApply.Add(StaticStatusEffects.irradiatedLeadEffect);
                }
            }
        }
    }
}
