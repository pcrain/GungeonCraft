using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.Misc;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class TackShooter : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Tack Shooter";
            string resourceName = "NevernamedsItems/Resources/NeoActiveSprites/tackshooter_generic";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<TackShooter>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Tacked On";
            string longDesc = "Places a radial tack-spraying turret."+"\n\nLegends tell of a tiny monkey hiding inside, operating the device.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 200);

            TackShooterObject = SetupTackShooter();

            item.consumable = false;
            item.quality = ItemQuality.D;
        }
        public static GameObject TackShooterObject;
        private static string[] RegTackShooterSprites = new string[]
        {
            "NevernamedsItems/Resources/PlaceableObjects/TackShooter/tackshooter_idle_001",
            "NevernamedsItems/Resources/PlaceableObjects/TackShooter/tackshooter_shoot_001",
            "NevernamedsItems/Resources/PlaceableObjects/TackShooter/tackshooter_shoot_002",
            "NevernamedsItems/Resources/PlaceableObjects/TackShooter/tackshooter_spawn_001",
            "NevernamedsItems/Resources/PlaceableObjects/TackShooter/tackshooter_spawn_002",
            "NevernamedsItems/Resources/PlaceableObjects/TackShooter/tackshooter_spawn_003",
            "NevernamedsItems/Resources/PlaceableObjects/TackShooter/tackshooter_spawn_004",
            "NevernamedsItems/Resources/PlaceableObjects/TackShooter/tackshooter_spawn_005"
        };
        private static GameObject SetupTackShooter()
        {
            //Make Tack
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(26) as Gun).DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 8f;
            projectile.baseData.range = 5f;
            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(28) as Gun).DefaultModule.projectiles[0].hitEffects.tileMapVertical.effects[0].effects[0].effect;

            //Setup the Tack Shooter Object
            GameObject prefab = CompanionBuilder.BuildPrefab("Tack Shooter", "Tack_Shooter_GUID", TackShooter.RegTackShooterSprites[0], new IntVector2(6, 2), new IntVector2(7, 14));//SpriteBuilder.SpriteFromResource("Planetside/Resources2/testturret/testturret_idle_001");
            prefab.name = "Tack Shooter";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(new IntVector2(6, 2), new IntVector2(7, 14));
            TackShooterBehaviour shooter = prefab.AddComponent<TackShooterBehaviour>();
            shooter.ProjectileToShoot = projectile;


            AIAnimator aiAnimator = prefab.GetOrAddComponent<AIAnimator>();

            aiAnimator.IdleAnimation = new DirectionalAnimation
            {
                Type = DirectionalAnimation.DirectionType.Single,
                Flipped = new DirectionalAnimation.FlipType[1],
                Prefix = "idle",
                AnimNames = new string[]
                { "idle" }
            };

            aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation> {
                    //Spawn
                    new AIAnimator.NamedDirectionalAnimation {
                        name = "appear",
                        anim = new DirectionalAnimation {
                            Prefix = "appear",
                            Type = DirectionalAnimation.DirectionType.Single,
                            Flipped = new DirectionalAnimation.FlipType[1],
                            AnimNames = new string[]
                            { "appear" }
                        }
                    },
                    //Shoot
                    new AIAnimator.NamedDirectionalAnimation {
                        name = "shoot",
                        anim = new DirectionalAnimation {
                            Prefix = "shoot",
                            Type = DirectionalAnimation.DirectionType.Single,
                            Flipped = new DirectionalAnimation.FlipType[1],
                            AnimNames = new string[]
                            { "shoot" }
                        }
                    },
                };
            tk2dSpriteAnimator spriteAnimator = prefab.GetOrAddComponent<tk2dSpriteAnimator>();
            tk2dSpriteCollectionData tackShooterCollection = SpriteBuilder.ConstructCollection(prefab, "TackShooterCollection");
            UnityEngine.Object.DontDestroyOnLoad(tackShooterCollection);
            for (int i = 0; i < RegTackShooterSprites.Length; i++)
            {
                SpriteBuilder.AddSpriteToCollection(RegTackShooterSprites[i], tackShooterCollection);
            }
            SpriteBuilder.AddAnimation(spriteAnimator, tackShooterCollection, new List<int> { 0 }, "idle", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 12;
            SpriteBuilder.AddAnimation(spriteAnimator, tackShooterCollection, new List<int> { 1, 2 }, "shoot", tk2dSpriteAnimationClip.WrapMode.Once).fps = 12;
            SpriteBuilder.AddAnimation(spriteAnimator, tackShooterCollection, new List<int> { 3, 4, 5, 6, 7 }, "appear", tk2dSpriteAnimationClip.WrapMode.Once).fps = 12;

            body.PixelColliders.Clear();
            body.PixelColliders.Add(new PixelCollider
            {
                ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                CollisionLayer = CollisionLayer.PlayerHitBox,
                IsTrigger = false,
                BagleUseFirstFrameOnly = false,
                SpecifyBagelFrame = string.Empty,
                BagelColliderNumber = 0,
                ManualOffsetX = 6,
                ManualOffsetY = 2,
                ManualWidth = 7,
                ManualHeight = 14,
                ManualDiameter = 0,
                ManualLeftX = 0,
                ManualLeftY = 0,
                ManualRightX = 0,
                ManualRightY = 0
            });
            body.CollideWithOthers = false;

            prefab.SetActive(false);
            FakePrefab.MarkAsFakePrefab(prefab);
            UnityEngine.Object.DontDestroyOnLoad(prefab);

            return prefab;
        }
        public override void DoEffect(PlayerController user)
        {
            Vector2 position = user.PositionInDistanceFromAimDir(1);
            GameObject tackShooter = UnityEngine.Object.Instantiate<GameObject>(TackShooterObject, position, Quaternion.identity);
            tackShooter.GetComponent<tk2dSprite>().PlaceAtLocalPositionByAnchor(position, tk2dBaseSprite.Anchor.MiddleCenter);
            TackShooterBehaviour shooterComp = tackShooter.GetComponent<TackShooterBehaviour>();
            if (shooterComp)
            {
                shooterComp.owner = user;
            }
        }
    }
    public class TackShooterBehaviour : MonoBehaviour
    {
        public TackShooterBehaviour()
        {
            timeExisted = 0;
            isActive = false;
            amountToFire = 8;
            cooldown = 0.8f;
            soundEvent = "Play_WPN_nailgun_shot_01";
        }
        private void Shoot()
        {
            if (isActive)
            {
                bodyAnimator.PlayUntilFinished("shoot", false, null, -1f, false);
                if (!string.IsNullOrEmpty(soundEvent))
                {
                    AkSoundEngine.PostEvent(soundEvent, base.gameObject);
                }
                int angle = 0;
                for (int i = 0; i < amountToFire; i++)
                {
                    GameObject gameObject = SpawnManager.SpawnProjectile(ProjectileToShoot.gameObject, base.gameObject.GetComponent<tk2dBaseSprite>().WorldCenter, Quaternion.Euler(0f, 0f, angle), true);
                    Projectile component = gameObject.GetComponent<Projectile>();
                    if (component)
                    {
                        component.Owner = owner;
                        component.Shooter = owner.specRigidbody;
                        component.TreatedAsNonProjectileForChallenge = true;

                        component.baseData.damage *= owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        component.baseData.speed *= owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                        component.baseData.force *= owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                        component.baseData.range *= owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                        component.BossDamageMultiplier *= owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                        component.AdditionalScaleMultiplier *= owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                        component.UpdateSpeed();
                        component.ApplyCompanionModifierToBullet(owner);
                    }
                    angle += (360 / amountToFire);
                }
            }
        }
        public void RemoveTackShooter()
        {
            isActive = false;
            LootEngine.DoDefaultItemPoof(base.GetComponent<tk2dBaseSprite>().WorldCenter);
            UnityEngine.Object.Destroy(base.gameObject);
        }
        private void Start()
        {
            bodyAnimator = base.GetComponent<AIAnimator>();
            timeTillNextShot = cooldown;
            isActive = true;
            bodyAnimator.PlayUntilFinished("appear", false, null, -1f, false);
        }
        private void Update()
        {
            if (isActive) timeExisted += BraveTime.DeltaTime;
            if (timeExisted > 5 && owner && !owner.IsInCombat)
            {
                RemoveTackShooter();
            }
            if (timeTillNextShot > 0)
            {
                timeTillNextShot -= BraveTime.DeltaTime;
            }
            else if (timeTillNextShot <= 0)
            {
                Shoot();
                timeTillNextShot = cooldown;
            }
        }
        public float timeExisted;
        public float timeTillNextShot;

        public bool isActive;

        private AIAnimator bodyAnimator;
        public PlayerController owner;
        public float cooldown;
        public string soundEvent;
        public Projectile ProjectileToShoot;
        public int amountToFire;
    }
}
