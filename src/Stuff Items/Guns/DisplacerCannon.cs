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

namespace NevernamedsItems
{

    public class DisplacerCannon : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Displacer Cannon", "displacercannon");
            Game.Items.Rename("outdated_gun_mods:displacer_cannon", "nn:displacer_cannon");

            var behav = gun.gameObject.AddComponent<DisplacerCannon>();
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalReloadAudio = "Play_OBJ_teleport_arrive_01";

            gun.SetShortDescription("Goodbye");
            gun.SetLongDescription("Displaces enemies through time and space to somewhere... else. "+"\nThose too weak to withstand the vortex will be lost among the border worlds of reality."+"\n\n\"We're gonna take our problems and DISPLACE them somewhere else!\"");

            gun.SetupSprite(null, "displacercannon_idle_001", 8);

            gun.SetAnimationFPS(gun.chargeAnimation, 11);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_WPN_warp_impact_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(228) as Gun).muzzleFlashEffects;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.4f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(3.06f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(40);
            gun.gunClass = GunClass.CHARGE;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 9;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 6f;
            projectile.baseData.speed *= 0.5f;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.SmoothLightBlueLaserCircleVFX;
            DisplaceEnemies displacement = projectile.gameObject.AddComponent<DisplaceEnemies>();

            projectile.SetProjectileSpriteRight("displacercannon_projectile", 17, 17, true, tk2dBaseSprite.Anchor.MiddleCenter, 15, 15);

            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 1f,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("DisplacerCannon Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/displacercannon_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/displacercannon_clipempty");

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (!everPickedUpByPlayer)
            {
                listOfDisplacedEnemies.DisplacedEnemies.Clear();
                GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
                PlayerResponsibleForDisplacement displacing = player.gameObject.GetOrAddComponent<PlayerResponsibleForDisplacement>();
            }
            storedPlayer = player;

            base.OnPickedUpByPlayer(player);
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            base.OnReloadPressed(player, gun, bSOMETHING);
            if ((gun.ClipCapacity == gun.ClipShotsRemaining) || (gun.CurrentAmmo == gun.ClipShotsRemaining))
            {
                if (player.PlayerHasActiveSynergy("Loot Vortex"))
                {
                    if (gun.CurrentAmmo >= 10)
                    {
                        gun.CurrentAmmo -= 10;
                        IntVector2 bestRewardLocation2 = player.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                        Vector3 convertedVector = bestRewardLocation2.ToVector3();
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(BraveUtility.RandomElement(lootIDlist)).gameObject, convertedVector, Vector2.zero, 1f, false, true, false);
                        AkSoundEngine.PostEvent("Play_OBJ_chestwarp_use_01", gameObject);
                        var tpvfx = (PickupObjectDatabase.GetById(573) as ChestTeleporterItem).TeleportVFX;
                        SpawnManager.SpawnVFX(tpvfx, convertedVector, Quaternion.identity, true);
                    }
                }
            }
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            if (player != null && player.PlayerHasActiveSynergy("Misfire Cannon"))
            {
                InstantTeleportToPlayerCursorBehav tp = projectile.gameObject.GetOrAddComponent<InstantTeleportToPlayerCursorBehav>();
            }
            base.PostProcessProjectile(projectile);
        }
        public static List<int> lootIDlist = new List<int>()
        {
            565, //Glass Guon Stone
            73, //Half Heart
            85, //Heart
            120, //Armor
            224, //Blank
            67, //Key
        };
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            if (!player.gameObject.GetComponent<PlayerResponsibleForDisplacement>())
            {
                player.CurrentGun.gameObject.AddComponent<PlayerResponsibleForDisplacement>();
            }
            base.OnPostFired(player, gun);
        }
        private void OnNewFloor()
        {
            if (storedPlayer)
            {
                PlayerResponsibleForDisplacement displacing = storedPlayer.gameObject.GetOrAddComponent<PlayerResponsibleForDisplacement>();
            }
        }
        public DisplacerCannon()
        {

        }
        PlayerController storedPlayer;
    }

    public static class listOfDisplacedEnemies
    {
        public static List<DisplacedEnemy> DisplacedEnemies = new List<DisplacedEnemy>();
    }
    public class PlayerResponsibleForDisplacement : MonoBehaviour
    {
        public PlayerResponsibleForDisplacement()
        {
        }
        private void Start()
        {
            this.m_player = base.GetComponent<PlayerController>();
            m_player.OnEnteredCombat += this.OnEnteredCombat;
        }
        private void OnEnteredCombat()
        {
            if (listOfDisplacedEnemies.DisplacedEnemies.Count > 0)
            {
                float chanceToSpawn = GetChanceToSpawn();
                //ETGModConsole.Log("Chance to spawn: " + chanceToSpawn);
                if ((chanceToSpawn > UnityEngine.Random.value))
                {
                    DoSpawn();
                    if ((chanceToSpawn - 1) > UnityEngine.Random.value)
                    {
                        DoSpawn();
                        if ((chanceToSpawn - 2) > UnityEngine.Random.value) DoSpawn();
                    }
                }
            }
        }
        private float GetChanceToSpawn()
        {
            float chanceToSpawn = 0.1f;
            chanceToSpawn *= listOfDisplacedEnemies.DisplacedEnemies.Count;
            return chanceToSpawn;
        }
        private void DoSpawn()
        {
            DisplacedEnemy enemy = BraveUtility.RandomElement(listOfDisplacedEnemies.DisplacedEnemies);

            var Enemy = EnemyDatabase.GetOrLoadByGuid(enemy.GUID);
            IntVector2? bestRewardLocation = m_player.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
            AIActor TargetActor = AIActor.Spawn(Enemy.aiActor, bestRewardLocation.Value, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(bestRewardLocation.Value), true, AIActor.AwakenAnimationType.Default, true);
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);
            if (TargetActor.IsBlackPhantom && !enemy.ISJAMMED) TargetActor.UnbecomeBlackPhantom();
            else if (!TargetActor.IsBlackPhantom && enemy.ISJAMMED) TargetActor.BecomeBlackPhantom();
            TargetActor.healthHaver.ForceSetCurrentHealth(enemy.HEALTH);


            listOfDisplacedEnemies.DisplacedEnemies.Remove(enemy);

            AkSoundEngine.PostEvent("Play_OBJ_chestwarp_use_01", gameObject);
            var tpvfx = (PickupObjectDatabase.GetById(573) as ChestTeleporterItem).TeleportVFX;
            SpawnManager.SpawnVFX(tpvfx, TargetActor.sprite.WorldCenter, Quaternion.identity, true);
        }
        private PlayerController m_player;

    }

    public class DisplacedEnemy
    {
        public string GUID;
        public float HEALTH;
        public bool ISJAMMED;
    }
    public class DisplaceEnemies : MonoBehaviour
    {
        public DisplaceEnemies()
        {
        }
        private void Start()
        {
            try
            {
                //ETGModConsole.Log("Start was called");
                this.m_projectile = base.GetComponent<Projectile>();
                if (m_projectile != null)
                {
                    m_projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;

                }
                else
                {
                    //ETGModConsole.Log("m_projectile was null");
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        public static List<string> DisplacerIgnoreList = new List<string>()
        {
            EnemyGuidDatabase.Entries["super_space_turtle_dummy"],
        };
        private void OnPreCollision(SpeculativeRigidbody myRigidBody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidBody, PixelCollider otherPixelCollider)
        {
            //ETGModConsole.Log("OnPreCollision was called");
            try
            {
                if (otherRigidBody != null && otherRigidBody.aiActor != null && otherRigidBody.healthHaver != null)
                {
                    if (!otherRigidBody.healthHaver.IsBoss && otherRigidBody.healthHaver.IsVulnerable && !otherRigidBody.aiActor.HasTag("mimic") && !DisplacerIgnoreList.Contains(otherRigidBody.aiActor.EnemyGuid) && otherRigidBody.GetComponent<CompanionController>() == null && otherRigidBody.aiActor.CompanionOwner == null)
                    {
                        //VFX
                        //ETGModConsole.Log("We made it past the initial checks");

                        AkSoundEngine.PostEvent("Play_OBJ_chestwarp_use_01", gameObject);
                        var tpvfx = (PickupObjectDatabase.GetById(573) as ChestTeleporterItem).TeleportVFX;
                        SpawnManager.SpawnVFX(tpvfx, otherRigidBody.sprite.WorldCenter, Quaternion.identity, true);

                        //ETGModConsole.Log("We made it past the VFX");

                        //FUNCTIONAL STUFF
                        float enemyHP = otherRigidBody.healthHaver.GetCurrentHealth();
                        float damageToDeal = 30;
                        float chanceToNotBuffer = 0;
                        if (m_projectile.Owner != null && m_projectile.Owner is PlayerController)
                        {
                            PlayerController projOwner = m_projectile.Owner as PlayerController;
                            damageToDeal *= projOwner.stats.GetStatValue(PlayerStats.StatType.Damage);
                            if (projOwner.PlayerHasActiveSynergy("Delete This")) chanceToNotBuffer = 0.25f;
                        }
                        if (enemyHP > damageToDeal)
                        {
                            if (chanceToNotBuffer < UnityEngine.Random.value)
                            {
                                listOfDisplacedEnemies.DisplacedEnemies.Add(new DisplacedEnemy { GUID = otherRigidBody.aiActor.EnemyGuid, HEALTH = (enemyHP - damageToDeal), ISJAMMED = otherRigidBody.aiActor.IsBlackPhantom });
                            }
                        }
                        if (otherRigidBody.gameObject.GetComponent<ExplodeOnDeath>())
                        {
                            Destroy(otherRigidBody.gameObject.GetComponent<ExplodeOnDeath>());
                        }
                        //ETGModConsole.Log("Count of Buffered Enemies: " + listOfDisplacedEnemies.DisplacedEnemies.Count);
                        otherRigidBody.aiActor.EraseFromExistenceWithRewards(true);
                        m_projectile.DieInAir();
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }

        }
        private Projectile m_projectile;
    }
}
