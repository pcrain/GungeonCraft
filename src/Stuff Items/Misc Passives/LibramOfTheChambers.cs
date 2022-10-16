using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using Alexandria.Misc;

namespace NevernamedsItems
{
    class LibramOfTheChambers : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Libram of The Chambers";
            string resourceName = "NevernamedsItems/Resources/libramofthechambers_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<LibramOfTheChambers>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Behold the Boss Eater!";
            string longDesc = "Converts Master Rounds from vitality upgrades to bullet damage instead." + "\n\nAn ancient tome of unreadable scripts and texts with regards to the many Chambers of the Gungeon.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
        }
        public static void LateInit()
        {
            GameObject YellowChamberOrbital = GuonToolbox.MakeAnimatedOrbital("Yellow Chamber LibramOrbital",
                 3f, //Orbital radius
                 120f, //Orbital degrees per second
                 0, //Orbital Tier
                 PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                 0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                 new List<string>()
                 {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_yellow_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_yellow_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_yellow_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_yellow_004",
                 },
                 6, //FPS
                 new Vector2(9, 9), //Collider Dimensions
                 new Vector2(1, 2), //Collider Offsets
                 tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                 tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController YellowController = YellowChamberOrbital.AddComponent<LibramOrbitalController>();
            YellowController.shoots = true;
            YellowController.gunIDToShoot = LovePistol.LovePistolID;

            GameObject SixthChamberOrbital = GuonToolbox.MakeAnimatedOrbital("Sixth Chamber LibramOrbital",
                3f, //Orbital radius
                120f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_sixth_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_sixth_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_sixth_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_sixth_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController SixthController = SixthChamberOrbital.AddComponent<LibramOrbitalController>();
            SixthController.shoots = true;
            SixthController.shootDelay = 5;
            SixthController.curseShot = true;
            SixthController.gunIDToShoot = 45;

            GameObject OiledCylinderOrbital = GuonToolbox.MakeAnimatedOrbital("Oiled Cylinder LibramOrbital",
                3.5f, //Orbital radius
                240f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_oiled_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_oiled_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_oiled_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_oiled_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);

            GameObject NitroglycylinderOrbital = GuonToolbox.MakeAnimatedOrbital("Nitroglycylinder LibramOrbital",
                3f, //Orbital radius
                120f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_nitro_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_nitro_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_nitro_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_nitro_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController NitroController = NitroglycylinderOrbital.AddComponent<LibramOrbitalController>();
            NitroController.shoots = true;
            NitroController.gunIDToShoot = 81;
            NitroController.shootDelay = 8f;

            GameObject GlassChamberOrbital = GuonToolbox.MakeAnimatedOrbital("Glass Chamber LibramOrbital",
                3f, //Orbital radius
                120f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_glass_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_glass_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_glass_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_glass_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController GlassController = GlassChamberOrbital.AddComponent<LibramOrbitalController>();
            GlassController.shoots = true;
            GlassController.gunIDToShoot = Glasster.GlassterID;
            GlassController.isGlass = true;

            GameObject FlamechamberOrbital = GuonToolbox.MakeAnimatedOrbital("Flamechamber LibramOrbital",
                3f, //Orbital radius
                120f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_flame_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_flame_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_flame_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_flame_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController FlameController = FlamechamberOrbital.AddComponent<LibramOrbitalController>();
            FlameController.shoots = true;
            FlameController.gunIDToShoot = 336;

            GameObject SpringloadedChamberOrbital = GuonToolbox.MakeAnimatedOrbital("Springloaded Chamber LibramOrbital",
                3f, //Orbital radius
                120f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_spring_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_spring_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_spring_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_spring_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController SpringloadedController = SpringloadedChamberOrbital.AddComponent<LibramOrbitalController>();
            SpringloadedController.shoots = true;
            SpringloadedController.shootDelay = 1f;
            SpringloadedController.gunIDToShoot = 50;

            GameObject WitheringChamberOrbital = GuonToolbox.MakeAnimatedOrbital("Withering Chamber LibramOrbital",
                3f, //Orbital radius
                120f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_withered_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_withered_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_withered_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_withered_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController WitheringController = WitheringChamberOrbital.AddComponent<LibramOrbitalController>();
            WitheringController.shoots = true;
            WitheringController.gunIDToShoot = Redhawk.ID;

            GameObject HeavyChamberOrbital = GuonToolbox.MakeAnimatedOrbital("Heavy Chamber LibramOrbital",
                1.7f, //Orbital radius
                60f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                5, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_heavy_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_heavy_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_heavy_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_heavy_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);

            GameObject CyclopeanChamberOrbital = GuonToolbox.MakeAnimatedOrbital("Cyclopean Chamber LibramOrbital",
                3f, //Orbital radius
                120f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_cyclop_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_cyclop_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_cyclop_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_cyclop_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController CyclopeanController = CyclopeanChamberOrbital.AddComponent<LibramOrbitalController>();
            CyclopeanController.shoots = true;
            CyclopeanController.shootDelay = 6;
            CyclopeanController.gunIDToShoot = 404;
            CyclopeanController.isCyclops = true;

            GameObject RecyclinderOrbital = GuonToolbox.MakeAnimatedOrbital("Recyclinder LibramOrbital",
                3f, //Orbital radius
                120f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_recycle_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_recycle_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_recycle_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_recycle_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController RecycleController = RecyclinderOrbital.AddComponent<LibramOrbitalController>();
            RecycleController.shoots = false;
            RecycleController.respondsToHit = true;
            RecycleController.isRecycle = true;

             GameObject BarrelChamberOrbital = GuonToolbox.MakeAnimatedOrbital("Barrel Chamber LibramOrbital",
                3f, //Orbital radius
                120f, //Orbital degrees per second
                0, //Orbital Tier
                PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS, //Orbit mode
                0, //Perfect orbital factor (synergy guons have it set around 10, other guons are 0)
                new List<string>()
                {
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_barrel_001",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_barrel_002",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_barrel_003",
                     "NevernamedsItems/Resources/GuonStones/LibramOrbitals/libramorbtial_barrel_004",
                },
                6, //FPS
                new Vector2(9, 9), //Collider Dimensions
                new Vector2(1, 2), //Collider Offsets
                tk2dBaseSprite.Anchor.LowerLeft, //Sprite Anchor
                tk2dSpriteAnimationClip.WrapMode.Loop);
            LibramOrbitalController BarrelController = BarrelChamberOrbital.AddComponent<LibramOrbitalController>();
            BarrelController.shoots = false;
            BarrelController.shootDelay = 1;
            BarrelController.isBarrel = true;

            chamberOrbitalPrefabs.Add(570, YellowChamberOrbital); //Charm shots
            chamberOrbitalPrefabs.Add(407, SixthChamberOrbital);
            chamberOrbitalPrefabs.Add(165, OiledCylinderOrbital); //No shots
            chamberOrbitalPrefabs.Add(Nitroglycylinder.NitroglycylinderID, NitroglycylinderOrbital); //Explosive shots
            chamberOrbitalPrefabs.Add(GlassChamber.GlassChamberID, GlassChamberOrbital); //Glass shots, shoots an extra bullet for each guon held
            chamberOrbitalPrefabs.Add(FlameChamber.ID, FlamechamberOrbital); //Fireballs
            chamberOrbitalPrefabs.Add(SpringloadedChamber.ID, SpringloadedChamberOrbital);
            chamberOrbitalPrefabs.Add(WitheringChamber.ID, WitheringChamberOrbital); 
            chamberOrbitalPrefabs.Add(HeavyChamber.HeavyChamberID, HeavyChamberOrbital); //No Shots
            chamberOrbitalPrefabs.Add(CyclopeanChamber.ID, CyclopeanChamberOrbital);
            chamberOrbitalPrefabs.Add(Recyclinder.RecyclinderID, RecyclinderOrbital); //Gives +1 ammo when blocking a bullet
            chamberOrbitalPrefabs.Add(BarrelChamber.ID, BarrelChamberOrbital);
        }
        private int currentItems, lastItems;
        private int currentActives, lastActives;
        private int currentActiveID, lastActiveID;
        public override void Update()
        {
            if (Owner)
            {
                CalculateStats(Owner);
            }
            else { return; }
        }
        private void CalculateStats(PlayerController player)
        {
            currentItems = player.passiveItems.Count;
            currentActives = player.activeItems.Count;
            bool ActiveChanged = false;
            if (player.CurrentItem)
            {
                currentActiveID = player.CurrentItem.PickupObjectId;
                if (currentActiveID != lastActiveID && currentActives == 1) ActiveChanged = true;
            }
            if ((currentItems != lastItems) || (currentActives != lastActives) || ActiveChanged)
            {
                RemoveStat(PlayerStats.StatType.Damage);
                RemoveStat(PlayerStats.StatType.Health);
                foreach (PassiveItem item in player.passiveItems)
                {
                    if (item is BasicStatPickup && (item as BasicStatPickup).IsMasteryToken)
                    {
                        AddStat(PlayerStats.StatType.Damage, 1.15f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        AddStat(PlayerStats.StatType.Health, -1f, StatModifier.ModifyMethod.ADDITIVE);
                    }
                }
                SortOrbitals();

                lastItems = currentItems;
                lastActives = currentActives;
                lastActiveID = currentActiveID;
                player.stats.RecalculateStats(player, true, false);
            }
        }
        private void SortOrbitals()
        {
            if (currentOrbitals != null && currentOrbitals.Count > 0)
            {
                for (int i = currentOrbitals.Count - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(currentOrbitals[i]);
                }
                currentOrbitals.Clear();
            }
            foreach (int id in chamberOrbitalPrefabs.Keys)
            {
                if (Owner.HasPickupID(id)) MakeOrbital(chamberOrbitalPrefabs[id]);
            }
            Owner.RecalculateOrbitals();
        }
        private void MakeOrbital(GameObject prefab)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, Owner.transform.position, Quaternion.identity);
            PlayerOrbital component = gameObject.GetComponent<PlayerOrbital>();
            if (Owner)
            {
                component.Initialize(Owner);
                LibramOrbitalController orbcon = gameObject.GetComponent<LibramOrbitalController>();
                if (orbcon) orbcon.owner = Owner;
            }
            currentOrbitals.Add(gameObject);
        }
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier
            {
                amount = amount,
                statToBoost = statType,
                modifyType = method
            };

            if (this.passiveStatModifiers == null)
                this.passiveStatModifiers = new StatModifier[] { modifier };
            else
                this.passiveStatModifiers = this.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }
        private void RemoveStat(PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < passiveStatModifiers.Length; i++)
            {
                if (passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(passiveStatModifiers[i]);
            }
            this.passiveStatModifiers = newModifiers.ToArray();
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            SortOrbitals();
        }
        public override DebrisObject Drop(PlayerController player)
        {
            if (currentOrbitals != null && currentOrbitals.Count > 0)
            {
                for (int i = currentOrbitals.Count - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(currentOrbitals[i]);
                }
                currentOrbitals.Clear();
            }
            return base.Drop(player);
        }
        public static Dictionary<int, GameObject> chamberOrbitalPrefabs = new Dictionary<int, GameObject>();
        public static List<GameObject> currentOrbitals = new List<GameObject>();
        public class LibramOrbitalController : MonoBehaviour
        {
            public LibramOrbitalController()
            {
                shootDelay = 4f;
                respondsToHit = false;
                shoots = false;
                gunIDToShoot = 56;

                isGlass = false;
                isRecycle = false;
            }
            private void Start()
            {
                timer = shootDelay;
                self = base.gameObject;
                sprite = self.GetComponent<tk2dSprite>();
                if (self.GetComponent<SpeculativeRigidbody>())
                {
                    body = self.GetComponent<SpeculativeRigidbody>();
                    if (respondsToHit) body.OnPreRigidbodyCollision += OnGuonHitByBullet;
                }
            }
            private void OnDestroy()
            {
                if (respondsToHit) body.OnPreRigidbodyCollision -= OnGuonHitByBullet;
            }
            private void OnGuonHitByBullet(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody other, PixelCollider otherCollider)
            {
                if (other.projectile && other.projectile.Owner && !(other.projectile.Owner is PlayerController))
                {
                    if (isRecycle) if (owner && owner.CurrentGun && owner.CurrentGun.CanGainAmmo) owner.CurrentGun.GainAmmo(1);
                }
            }
            private void Update()
            {
                if (timer >= 0) timer -= BraveTime.DeltaTime;
                else
                {
                    if (owner && owner.IsInCombat)
                    {
                        if (shoots)
                        {
                            int numToFire = 1;
                            if (isGlass) numToFire += owner.GetNumberOfItemInInventory(565);
                            for (int i = 0; i < numToFire; i++)
                            {
                                AIActor nearestEnemy = ((Vector2)base.transform.position).GetNearestEnemyToPosition();
                                if (nearestEnemy != null)
                                {
                                    Vector2 nearestEnemyPos = nearestEnemy.transform.position;
                                    if (nearestEnemy.specRigidbody) nearestEnemyPos = nearestEnemy.specRigidbody.UnitCenter;

                                    float angleVar = 5;
                                    if (i > 1 && isGlass) angleVar = 10;

                                    GameObject proj = ProjSpawnHelper.SpawnProjectileTowardsPoint((PickupObjectDatabase.GetById(gunIDToShoot) as Gun).DefaultModule.projectiles[0].gameObject, sprite.WorldCenter, nearestEnemyPos, 0, angleVar, owner);
                                    Projectile component = proj.GetComponent<Projectile>();
                                    if (component)
                                    {
                                        component.Owner = owner;
                                        component.Shooter = body;

                                        component.ApplyCompanionModifierToBullet(owner);
                                        component.baseData.damage *= owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                                        component.baseData.range *= owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                                        component.baseData.speed *= owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                                        component.baseData.force *= owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                                        owner.DoPostProcessProjectile(component);

                                        if (curseShot)
                                        {
                                            component.AdjustPlayerProjectileTint(ExtendedColours.cursedBulletsPurple, 2);
                                            component.CurseSparks = true;
                                            component.baseData.damage *= ((0.15f * owner.stats.GetStatValue(PlayerStats.StatType.Curse)) + 1);
                                        }
                                        if (isCyclops)
                                        {
                                            component.AdditionalScaleMultiplier *= 1.5f;
                                            component.baseData.damage *= 3f;
                                            PierceProjModifier pierce = component.gameObject.AddComponent<PierceProjModifier>();
                                            pierce.penetration = 3;
                                        }
                                    }
                                }
                            }
                        }
                        else if (isBarrel)
                        {
                            Vector2 barrelPos = sprite.WorldCenter;
                            CellData cell = GameManager.Instance.Dungeon.data.cellData[(int)barrelPos.x][(int)barrelPos.y];
                            if (cell.type == CellType.FLOOR)
                            {
                               
                                GameObject barrel = UnityEngine.Object.Instantiate<GameObject>(EasyPlaceableObjects.GenericBarrel, barrelPos, Quaternion.identity);
                                barrel.GetComponentInChildren<MinorBreakable>().OnlyBrokenByCode = true;
                                barrel.GetComponentInChildren<SpeculativeRigidbody>().OnPreRigidbodyCollision += HandlePreCollision;
                                barrel.GetComponentInChildren<SpeculativeRigidbody>().PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;
                                barrel.GetComponentInChildren<tk2dSprite>().PlaceAtPositionByAnchor(barrelPos, tk2dSprite.Anchor.LowerCenter);
                                StartCoroutine(BreakBarrel(barrel.GetComponentInChildren<MinorBreakable>()));
                            }
                        }
                    }


                    timer = shootDelay;
                }
            }
            private IEnumerator BreakBarrel(MinorBreakable breakable)
            {
                yield return new WaitForSeconds(15f);
                breakable.Break();
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
            private GameObject self;
            private SpeculativeRigidbody body;
            private tk2dSprite sprite;
            public float shootDelay;
            private float timer;
            public bool shoots;
            public bool respondsToHit;
            public PlayerController owner;
            public int gunIDToShoot;
            public bool curseShot;
            public bool isCyclops;
            public bool isBarrel;

            public bool isGlass;
            public bool isRecycle;

        }
    }
}