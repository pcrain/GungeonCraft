using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using ChestType = Chest.GeneralChestType;
using Dungeonator;

namespace NevernamedsItems
{
    public class Gumgun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gumgun", "gumgun");
            Game.Items.Rename("outdated_gun_mods:gumgun", "nn:gumgun");
            gun.gameObject.AddComponent<Gumgun>();
            gun.SetShortDescription("Wumderful");
            gun.SetLongDescription("Fires globs of gum at your foes." + "\nHolding down fire causes it to enter 'Gumzooka' mode." + "\n\nThis tiny handcannon was designed for use by small creatures with no hands." + "\nCan you still call it a handcannon then?");

            gun.SetupSprite(null, "gumgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.SetAnimationFPS(gun.chargeAnimation, 3);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(13) as Gun).gunSwitchGroup;
            gun.gunHandedness = GunHandedness.HiddenOneHanded;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 1;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(334) as Gun).muzzleFlashEffects;
            gun.gunClass = GunClass.CHARGE;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 25;
            gun.barrelOffset.transform.localPosition = new Vector3(0.56f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.ammo = 300;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            RandomiseProjectileColourComponent colour = projectile.gameObject.GetOrAddComponent<RandomiseProjectileColourComponent>();
            projectile.baseData.damage *= 1.4f;
            projectile.AnimateProjectile(new List<string> {
                "gumgun_smallproj_001",
                "gumgun_smallproj_002",
                "gumgun_smallproj_001",
                "gumgun_smallproj_003",
            }, 10, true, new List<IntVector2> {
                new IntVector2(13, 10),
                new IntVector2(15, 8),
                new IntVector2(13, 10),
                new IntVector2(11, 12),
            }, AnimateBullet.ConstructListOfSameValues(false, 4), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4), AnimateBullet.ConstructListOfSameValues(true, 4), AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(12, 9), 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));


            //CHARGE BULLET STATS
            Projectile chargeprojectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            chargeprojectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(chargeprojectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(chargeprojectile);
            RandomiseProjectileColourComponent colour2 = chargeprojectile.gameObject.GetOrAddComponent<RandomiseProjectileColourComponent>();
            chargeprojectile.baseData.damage *= 4;
            chargeprojectile.AnimateProjectile(new List<string> {
                "gumgun_bigproj_001",
                "gumgun_bigproj_002",
                "gumgun_bigproj_001",
                "gumgun_bigproj_003",
            }, 10, true, new List<IntVector2> {
                new IntVector2(21, 14),
                new IntVector2(23, 12),
                new IntVector2(21, 14),
                new IntVector2(19, 16),
            }, AnimateBullet.ConstructListOfSameValues(false, 4), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4), AnimateBullet.ConstructListOfSameValues(true, 4), AnimateBullet.ConstructListOfSameValues(false, 4),
           AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(14, 8), 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));

            ProjectileModule.ChargeProjectile chargeProj1 = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 0f,
                VfxPool = null,

            };

            ProjectileModule.ChargeProjectile chargeProj2 = new ProjectileModule.ChargeProjectile
            {
                Projectile = chargeprojectile,
                ChargeTime = 1f,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj1, chargeProj2 };

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GumgunID = gun.PickupObjectId;
            Gumgun.BuildDenkSnavelPrefab();
        }
        public static int GumgunID;
        private GameObject extantCompanion;
        public static GameObject prefab;
        private static readonly string guid = "denksnavel01297129827873486";
        private bool hasSynergyLastWeChecked;
        protected override void Update()
        {
            if (gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                PlayerController owner = gun.CurrentOwner as PlayerController;

                if (owner.PlayerHasActiveSynergy("WEH") != hasSynergyLastWeChecked)
                {
                    if (owner.CurrentGun.PickupObjectId == GumgunID && owner.PlayerHasActiveSynergy("WEH"))
                    {
                        SpawnNewCompanion(guid);
                    }
                    if (!owner.PlayerHasActiveSynergy("WEH") && extantCompanion)
                    {
                        UnityEngine.Object.Destroy(extantCompanion);
                    }
                    hasSynergyLastWeChecked = owner.PlayerHasActiveSynergy("WEH");
                }
            }
            base.Update();
        }
        public override void OnSwitchedToThisGun()
        {
            if (gun.CurrentOwner && gun.CurrentOwner is PlayerController && (gun.CurrentOwner as PlayerController).PlayerHasActiveSynergy("WEH"))
            {
                SpawnNewCompanion(guid);
            }
            base.OnSwitchedToThisGun();
        }
        public override void OnSwitchedAwayFromThisGun()
        {
            if (extantCompanion)
            {
                UnityEngine.Object.Destroy(extantCompanion.gameObject);
            }
            base.OnSwitchedAwayFromThisGun();
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (player.PlayerHasActiveSynergy("WEH"))
            {
                SpawnNewCompanion(guid);
            }
            base.OnPickedUpByPlayer(player);
        }
        public override void OnDropped()
        {
            if (extantCompanion)
            {
                UnityEngine.Object.Destroy(extantCompanion.gameObject);
            }
            base.OnDropped();
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            if (extantCompanion)
            {
                UnityEngine.Object.Destroy(extantCompanion.gameObject);
            }
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (extantCompanion)
            {
                UnityEngine.Object.Destroy(extantCompanion.gameObject);
            }
            base.OnDestroy();
        }
        private void SpawnNewCompanion(string guid)
        {
            AIActor orLoadByGuid = EnemyDatabase.GetOrLoadByGuid(guid);
            Vector3 vector = Owner.transform.position;
            if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.FOYER)
            {
                vector += new Vector3(1.125f, -0.3125f, 0f);
            }
            GameObject extantCompanion2 = UnityEngine.Object.Instantiate<GameObject>(orLoadByGuid.gameObject, vector, Quaternion.identity);
            CompanionController orAddComponent = extantCompanion2.GetOrAddComponent<CompanionController>();
            extantCompanion = extantCompanion2;
            orAddComponent.Initialize(gun.CurrentOwner as PlayerController);
            if (orAddComponent.specRigidbody)
            {
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(orAddComponent.specRigidbody, null, false);
            }
        }
        public static void BuildDenkSnavelPrefab()
        {
            if (prefab == null || !CompanionBuilder.companionDictionary.ContainsKey(guid))
            {
                prefab = CompanionBuilder.BuildPrefab("Gumgun Denksnavel", guid, "NevernamedsItems/Resources/Companions/Denksnavel/denksnavel_idle_left_001", new IntVector2(5, 0), new IntVector2(5, 13));
                var companionController = prefab.AddComponent<DenksnavelController>();
                companionController.aiActor.MovementSpeed = 6.5f;
                companionController.CanCrossPits = true;
                companionController.aiActor.ActorShadowOffset = new Vector3(0, -0.5f);
                prefab.AddAnimation("idle_right", "NevernamedsItems/Resources/Companions/Denksnavel/denksnavel_idle_right", 12, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.TwoWayHorizontal, DirectionalAnimation.FlipType.None);
                prefab.AddAnimation("idle_left", "NevernamedsItems/Resources/Companions/Denksnavel/denksnavel_idle_left", 12, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.TwoWayHorizontal, DirectionalAnimation.FlipType.None);
                companionController.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
                BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
                CustomCompanionBehaviours.SimpleCompanionMeleeAttack denksnavelattack = new CustomCompanionBehaviours.SimpleCompanionMeleeAttack();
                denksnavelattack.DamagePerTick = 5;
                denksnavelattack.DesiredDistance = 2;
                denksnavelattack.TickDelay = 1;
                CustomCompanionBehaviours.SimpleCompanionApproach denksnavelapproach = new CustomCompanionBehaviours.SimpleCompanionApproach();
                denksnavelapproach.DesiredDistance = 1;
                component.MovementBehaviors.Add(denksnavelapproach);
                component.AttackBehaviors.Add(denksnavelattack);
                component.MovementBehaviors.Add(new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } });
            }
        }
        public class DenksnavelController : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                Owner.OnRoomClearEvent += this.OnRoomClear;
                Owner.ImmuneToPits.SetOverride("Denksnavel", true, null);
                Owner.OnPitfall += this.OnPitfall;

            }
            public override void OnDestroy()
            {
                if (Owner)
                {
                    Owner.OnRoomClearEvent -= this.OnRoomClear;
                    Owner.ImmuneToPits.SetOverride("Denksnavel", false, null);
                    Owner.OnPitfall -= this.OnPitfall;
                }
                base.OnDestroy();
            }
            private void OnPitfall()
            {
                base.transform.position = Owner.transform.position; 
                TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0),BraveUtility.RandomElement(possiblePitRemarks), 4f);

            }
            private List<string> possiblePitRemarks = new List<string>()
            {
                 "Keep throwing yourself around like that and I might not save you next time!",
                 "Be more careful! WEH!",
                 "Do you like pits or something, Scallywag?",
                 "You're too heavy for me to keep this up forever!",
                 "Watch your step, Scallywag!",
                 "...WEH!",
            };
            private void OnRoomClear(PlayerController owner)
            {
                TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0), "WEH!", 4f);
            }
            private int GetChestType(Chest chest)
            {
                var contents = chest.PredictContents(Owner);
                foreach (var item in contents)
                {
                    if (item is Gun) return 0;
                    if (item is PlayerItem) return 2;
                    if (item is PassiveItem) return 1;
                }
                return UnityEngine.Random.Range(0, 3);
            }
            public override void Update()
            {
                RoomHandler currentRoom = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition((base.specRigidbody.UnitCenter).ToIntVector2());
                if (currentRoom != null && currentRoom != lastCheckedRoom)
                {
                    Chest[] allChests = FindObjectsOfType<Chest>();
                    List<Chest> chestsInRoom = new List<Chest>();
                    foreach (Chest chest in allChests)
                    {
                        if (chest.transform.position.GetAbsoluteRoom() == currentRoom && !chest.IsOpen && !chest.IsBroken)
                        {
                            chestsInRoom.Add(chest);
                        }
                    }
                    if (chestsInRoom.Count > 0)
                    {
                        Chest selectedChest;
                        selectedChest = BraveUtility.RandomElement(chestsInRoom);
                        int selectedChestType = GetChestType(selectedChest);
                        if (selectedChest.IsRainbowChest)
                        {
                            TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0), "This chest smells like rainbows, lucky you!", 4f);
                        }
                        else
                        {
                            if (chestsInRoom.Count > 1)
                            {
                                if (selectedChestType == 0)
                                {
                                    TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0), "One of these chests smells like a gun, Scallywag!", 4f);
                                }
                                else if (selectedChestType == 1)
                                {
                                    TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0), "One of these chests smells like a passive item...", 4f);
                                }
                                else if (selectedChestType == 2)
                                {
                                    TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0), "One of these chests is definitely an active item!", 4f);
                                }
                            }
                            else
                            {
                                if (selectedChestType == 0)
                                {
                                    TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0), "This chest smells like a gun, Scallywag!", 4f);
                                }
                                else if (selectedChestType == 1)
                                {
                                    TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0), "This chest smells like a passive item...", 4f);
                                }
                                else if (selectedChestType == 2)
                                {
                                    TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0), "This chest is definitely an active item!", 4f);
                                }
                                else
                                {
                                    TextBubble.DoAmbientTalk(base.transform, new Vector3(1, 2, 0), "This chest doesn't smell like anything at all..." + "\nwhat did you break?", 4f);
                                }
                            }
                        }
                    }
                    lastCheckedRoom = currentRoom;
                }
                base.Update();
            }
            private PlayerController Owner;
            private RoomHandler lastCheckedRoom;
        }
        public Gumgun()
        {

        }
    }
}
