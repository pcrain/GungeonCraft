﻿// namespace CwaffingTheGungy;
//
//     public class HeadCannon : CwaffGun
//     {
//         public static string gunName          = "Head Cannon";
//         public static string spriteName       = "multiplicator";
//         public static string shortDescription = "Better Than One";
//         public static string longDescription  = "(launches you :D)";

//         public static void Add()
//         {
//             Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
//             var comp = gun.gameObject.AddComponent<HeadCannon>();

//             gun.DefaultModule.ammoCost            = 1;
//             gun.DefaultModule.shootStyle          = ShootStyle.Automatic;
//             gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Random;
//             gun.reloadTime                        = 1.1f;
//             gun.DefaultModule.angleVariance       = 15.0f;
//             gun.DefaultModule.cooldownTime        = 0.7f;
//             gun.DefaultModule.numberOfShotsInClip = 1000;
//             gun.quality                           = ItemQuality.D;
//             gun.SetBaseMaxAmmo(1000);
//             gun.SetAnimationFPS(gun.shootAnimation, 24);

//             Projectile projectile       = gun.InitFirstProjectile();
//             projectile.baseData.damage  = 20f;
//             projectile.baseData.speed   = 30.0f;

//             projectile.gameObject.AddComponent<HeadCannonBullets>();
//         }

//     }

//     public class HeadCannonBullets : MonoBehaviour
//     {
//         private Projectile m_projectile;
//         private PlayerController m_owner;

//         private static VFXPool vfx = null;

//         private void Start()
//         {
//             if (vfx == null)
//                 vfx = VFX.CreatePoolFromVFXGameObject((Items.MagicLamp.AsGun()).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);

//             this.m_projectile = base.GetComponent<Projectile>();
//             // this.m_projectile.BulletScriptSettings.surviveTileCollisions = true;
//             if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
//             {
//                 this.m_owner      = this.m_projectile.Owner as PlayerController;
//             }

//             SpeculativeRigidbody specRigidBody = this.m_projectile.specRigidbody;
//             // this.m_projectile.BulletScriptSettings.surviveTileCollisions = true;
//             specRigidBody.OnCollision += this.OnCollision;
//         }

//         private void OnCollision(CollisionData coll)
//         {

//             if (!coll.OtherRigidbody || !coll.OtherRigidbody.gameObject || !coll.OtherRigidbody.gameObject.GetComponent<AIActor>())
//                 return; //ignore collisions with enemies, we only care about walls

//             PhysicsEngine.PostSliceVelocity     = new Vector2?(default(Vector2));
//             SpeculativeRigidbody specRigidbody  = this.m_projectile.specRigidbody;
//             specRigidbody.OnCollision          -= this.OnCollision;

//             this.TeleportPlayerToPosition(this.m_owner, coll.PostCollisionUnitCenter);
//             this.m_projectile.DieInAir();
//         }

//         private void TeleportPlayerToPosition(PlayerController player, Vector2 position)
//         {
//             int num_steps = 100;

//             Vector2 playerPos   = player.transform.position;
//             Vector2 targetPos   = position;
//             Vector2 deltaPos    = (targetPos - playerPos)/((float)(num_steps));
//             Vector2 adjustedPos = Vector2.zero;

//             // magic code that slowly moves the player out of walls
//             for (int i = 0; i < num_steps; ++i)
//             {
//                 player.transform.position = playerPos + i * deltaPos;
//                 player.specRigidbody.Reinitialize();
//                 if (PhysicsEngine.Instance.OverlapCast(player.specRigidbody, null, true, false, null, null, false, null, null))
//                 {
//                     break;
//                 }
//                 adjustedPos = player.transform.position;
//             }

//             player.StartCoroutine(DoPlayerTeleport(player, playerPos, adjustedPos, 0.125f));
//         }

//         private IEnumerator DoPlayerTeleport(PlayerController player, Vector2 start, Vector2 end, float duration)
//         {
//             float timer = 0;

//             GameManager.Instance.MainCameraController.SetManualControl(true, true);
//             Vector2 startCamPos = GameManager.Instance.MainCameraController.GetIdealCameraPosition();
//             Vector2 endCamPos   = end;
//             Vector2 deltaPos    = (endCamPos - startCamPos);

//             player.transform.position = start;
//             player.specRigidbody.Reinitialize();
//             player.sprite.renderer.enabled = false;
//             player.specRigidbody.enabled = false;

//             player.gameObject.Play("Play_OBJ_chestwarp_use_01");
//             vfx.SpawnAtPosition(start.ToVector3ZisY(-1f), 0, null, null, null, -0.05f);

//             while (timer < duration)
//             {
//                 timer += BraveTime.DeltaTime;
//                 Vector2 interPos = startCamPos + (timer/duration)*deltaPos;
//                 GameManager.Instance.MainCameraController.OverridePosition = interPos;
//                 yield return null;
//             }

//             player.gameObject.Play("Play_OBJ_chestwarp_use_01");
//             vfx.SpawnAtPosition(end.ToVector3ZisY(-1f), 0, null, null, null, -0.05f);
//             this.m_owner.DoEasyBlank(end, EasyBlankType.MINI);

//             GameManager.Instance.MainCameraController.SetManualControl(false, true);
//             player.transform.position = end;
//             player.sprite.renderer.enabled = true;
//             player.specRigidbody.enabled = true;
//             player.specRigidbody.Reinitialize();

//             yield break;
//         }
//     }
