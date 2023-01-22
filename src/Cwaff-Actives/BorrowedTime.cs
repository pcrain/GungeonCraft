using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using Dungeonator;
using ItemAPI;

namespace CwaffingTheGungy
{
    class BorrowedTime : PlayerItem
    {
        public static string activeName       = "Borrowed Time";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/88888888_icon";
        public static string shortDescription = "Clock's Ticking";
        public static string longDescription  = "(insta clear any room, but enemies will all respawn in boss room with increased jam chance. cannot pick up bosses or jammed enemies)";

        private static List<string> borrowedEnemies = new List<string>{};

        private PlayerController m_owner = null;
        private RoomHandler lastCheckedRoom = null;
        private bool inBossRoom = false;
        private bool isUsable = true;

        public static void Init()
        {
            PlayerItem item = Lazy.SetupActive<BorrowedTime>(activeName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.C;

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 1);
            item.consumable   = false;
            item.quality      = ItemQuality.D;
            item.CanBeDropped = true;
        }


        public override void Pickup(PlayerController player)
        {
            this.m_owner = player;
            base.Pickup(player);
        }

        public override void OnPreDrop(PlayerController player)
        {
            if (borrowedEnemies.Count > 0)
                this.m_owner.StartCoroutine(ReapWhatYouSow());
            this.m_owner = null;
            base.OnPreDrop(player);
        }

        public override void Update()
        {
            if (this.m_owner && this.m_owner.CurrentRoom != lastCheckedRoom)
            {
                lastCheckedRoom = this.m_owner.CurrentRoom;
                bool wasInBossRoom = this.inBossRoom;
                this.inBossRoom = CheckIfBossIsPresent();
                if (this.inBossRoom && !wasInBossRoom)
                    this.m_owner.StartCoroutine(ReapWhatYouSow());
            }
            this.isUsable = !this.m_owner.InExitCell;
            base.Update();
        }

        public override bool CanBeUsed(PlayerController user)
        {
            return this.isUsable && base.CanBeUsed(user);
        }

        private const float JAMMED_CHANCE = 0.1f;
        private IEnumerator ReapWhatYouSow()
        {
            while (GameManager.IsBossIntro)
                yield return null;

            if (borrowedEnemies.Count > 0)
            {
                int enemiesToSpawn = borrowedEnemies.Count;
                var tpvfx = (PickupObjectDatabase.GetById(573) as ChestTeleporterItem).TeleportVFX;
                for (int i = 0; i < enemiesToSpawn; i++)
                {
                    var Enemy = EnemyDatabase.GetOrLoadByGuid(borrowedEnemies[i]);
                    IntVector2? bestRewardLocation = this.m_owner.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
                    AIActor TargetActor = AIActor.Spawn(Enemy.aiActor, bestRewardLocation.Value, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(bestRewardLocation.Value), true, AIActor.AwakenAnimationType.Default, true);
                    if (UnityEngine.Random.value <= JAMMED_CHANCE)
                        TargetActor.BecomeBlackPhantom();
                    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);
                    // TargetActor.healthHaver.ForceSetCurrentHealth(enemy.HEALTH);

                    AkSoundEngine.PostEvent("Play_OBJ_chestwarp_use_01", gameObject);
                    SpawnManager.SpawnVFX(tpvfx, TargetActor.sprite.WorldCenter, Quaternion.identity, true);
                }
                if (!this.m_owner.CurrentRoom.IsSealed)
                {
                    this.m_owner.CurrentRoom.SealRoom();
                    GameManager.Instance.DungeonMusicController.SwitchToActiveMusic(null);
                }
                borrowedEnemies.Clear();
                this.CanBeDropped = true;
            }

            yield break;
        }

        private bool CheckIfBossIsPresent()
        {
            if (lastCheckedRoom == null)
                return false;
            List<AIActor> activeEnemies =
                this.m_owner.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                if (activeEnemies[i].healthHaver.IsBoss)
                    return true;
            }
            return false;
        }

        public override void DoEffect(PlayerController user)
        {
            // Ineffective in boss rooms
            if (this.inBossRoom)
                return;

            // Ineffective if the room has no active enemies
            RoomHandler curRoom = user.GetAbsoluteParentRoom();
            List<AIActor> activeEnemies = user.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null)
                return;

            if (activeEnemies.Count == 0)
            {
                if (borrowedEnemies.Count > 0 && user.GetAbsoluteParentRoom() != null)
                    this.m_owner.StartCoroutine(ReapWhatYouSow());
                return;
            }

            // Capture enemies for later
            VFXPool vfx = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(0) as Gun
                ).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
            AkSoundEngine.PostEvent("Play_OBJ_chestwarp_use_01", gameObject);
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                AIActor otherEnemy = activeEnemies[i];
                if (!(otherEnemy && otherEnemy.specRigidbody && otherEnemy.healthHaver)
                 || otherEnemy.IsGone || otherEnemy.healthHaver.IsBoss || otherEnemy.IsBlackPhantom)
                    continue;

                vfx.SpawnAtPosition(
                    otherEnemy.sprite.WorldCenter.ToVector3ZisY(-1f), /* -1 = above player sprite */
                    0, null, null, null, -0.05f);

                otherEnemy.EraseFromExistence(true);
                borrowedEnemies.Add(otherEnemy.EnemyGuid);
            }
            if (borrowedEnemies.Count > 0)
                this.CanBeDropped = false; //cannot be dropped if it contains enemies
        }
    }
}
