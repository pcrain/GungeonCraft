namespace CwaffingTheGungy;

// public class Maestro : CwaffGun
// {
//     public static string ItemName         = "Maestro";
//     public static string ShortDescription = "TBD";
//     public static string LongDescription  = "TBD";
//     public static string Lore             = "TBD";

//     public static void Add()
//     {
//         Gun gun = Lazy.SetupGun<Maestro>(ItemName, ShortDescription, LongDescription, Lore);
//             gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.CHARM, reloadTime: 1.0f, ammo: 500, shootFps: 2);

//         gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.25f, angleVariance: 15.0f,
//           shootStyle: ShootStyle.Automatic, damage: 3.0f, speed: 60.0f, spawnSound: "tomislav_shoot",
//           sprite: "maestro_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter));
//     }

//     public override void OnPlayerPickup(PlayerController player)
//     {
//         StaticReferenceManager.ProjectileAdded -= CheckForMaestroHost;
//         StaticReferenceManager.ProjectileAdded += CheckForMaestroHost;
//         CwaffEvents.OnBankBulletOwnerAssigned -= CheckForMaestroHost;
//         CwaffEvents.OnBankBulletOwnerAssigned += CheckForMaestroHost;
//         base.OnPlayerPickup(player);
//     }

//     private void SetHost(AIActor newHost)
//     {
//         newHost.GetOrAddComponent<MaestroHost>();
//         foreach (Projectile p in StaticReferenceManager.AllProjectiles)
//             if (p.Owner == newHost)
//                 RegisterProjectileHost(p);
//     }

//     private void RedirectProjectiles(AIActor targetEnemy)
//     {
//         const float REFLECT_SPEED = 60f;
//         const float SPREAD = 10f;

//         if (this.PlayerOwner is not PlayerController pc)
//             return;

//         AkSoundEngine.PostEvent("Play_OBJ_metalskin_deflect_01", GameManager.Instance.gameObject);
//         float delay = 0.0f;
//         float baseDelay = 0.1f;
//         foreach (Projectile p in StaticReferenceManager.AllProjectiles)
//         {
//             if (!p || !p.isActiveAndEnabled)
//                 continue;
//             if (p.Owner == targetEnemy || p.Owner is PlayerController)
//                 continue;
//             if (!p.GetComponent<CapturedMaestroProjectile>()) //NOTE: checking it this way allows us to redirect projectiles with dead owners
//                 continue;

//             p.RemoveBulletScriptControl();
//             p.Direction = (targetEnemy.CenterPosition - p.specRigidbody.UnitCenter).normalized;
//             p.Direction = p.Direction.Rotate(UnityEngine.Random.Range(-SPREAD, SPREAD));
//             if (p.Owner && p.Owner.specRigidbody)
//                 p.specRigidbody.DeregisterSpecificCollisionException(p.Owner.specRigidbody);
//             p.Owner = pc;
//             p.SetNewShooter(pc.specRigidbody);
//             p.allowSelfShooting = false;
//             p.collidesWithPlayer = false;
//             p.collidesWithEnemies = true;
//             p.specRigidbody.CollideWithTileMap = false;
//             p.baseData.damage = Mathf.Max(p.baseData.damage, ProjectileData.FixedFallbackDamageToEnemies, 15f);
//             p.UpdateCollisionMask();
//             p.ResetDistance();
//             p.Reflected();

//             if (p.sprite)
//                 p.sprite.MakeHolographic(green: true);

//             delay += baseDelay;
//             baseDelay = Mathf.Max(C.FRAME, baseDelay - 0.005f);
//             p.FreezeAndLaunchWithDelay(delay, REFLECT_SPEED, sound: "knife_gun_launch");
//         }
//     }

//     private static void CheckForMaestroHost(Projectile p)
//     {
//         if (p && p.Owner is AIActor enemy && enemy.GetComponent<MaestroHost>())
//             RegisterProjectileHost(p);
//     }

//     public override void PostProcessProjectile(Projectile projectile)
//     {
//         base.PostProcessProjectile(projectile);
//         projectile.OnHitEnemy += this.OnHitEnemy;
//     }

//     private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
//     {
//         if (!this)
//             return;
//         if (enemy.GetComponent<AIActor>() is not AIActor target)
//             return;
//         if (!target.healthHaver || target.healthHaver.IsDead)
//             return;

//         RedirectProjectiles(target);
//         SetHost(target);
//     }

//     private static void RegisterProjectileHost(Projectile p)
//     {
//         if (!p.GetComponent<CapturedMaestroProjectile>())
//             p.AddComponent<CapturedMaestroProjectile>().Setup();
//     }

//     private static void DeregisterProjectileHost(Projectile p)
//     {
//         if (p.GetComponent<CapturedMaestroProjectile>() is CapturedMaestroProjectile mp)
//             mp.Teardown();
//     }
// }

// public class CapturedMaestroProjectile : MonoBehaviour
// {
//     private Projectile _projectile  = null;
//     private PlayerController _owner = null;
//     private Shader _oldShader = null;

//     public void Setup()
//     {
//         this._projectile = base.GetComponent<Projectile>();
//         this._owner = this._projectile.Owner as PlayerController;
//         this._projectile.OnDestruction += this.OnDestruction;

//         if (this._projectile.sprite)
//             this._oldShader = this._projectile.sprite.renderer.material.shader;
//     }

//     public void OnDestruction(Projectile p)
//     {
//         this._projectile.OnDestruction -= this.OnDestruction;
//         Teardown();
//     }

//     public void Teardown()
//     {
//         if (this._oldShader && this._projectile && this._projectile.sprite)
//             this._projectile.sprite.renderer.material.shader = this._oldShader;
//         UnityEngine.Object.Destroy(this);
//     }
// }

// public class MaestroHost : MonoBehaviour { }
