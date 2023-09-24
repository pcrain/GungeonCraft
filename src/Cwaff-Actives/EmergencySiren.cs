using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel; // ReadOnlyCollection
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    class EmergencySiren : PlayerItem
    {
        public static string ItemName         = "Emergency Siren";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/emergency_siren_icon";
        public static string ShortDescription = "WEE WOO! WEE WOO!";
        public static string LongDescription  = "(Opens locked doors and renders enemies and traps in a room harmless until leaving and returning to it.)";

        private static StatModifier[] _EmergencyMods = null;

        private RoomHandler _roomToReset = null;
        private bool _anyEnemyInRoomDied = false;
        private bool _anyGunFiredInRoom = false;

        public static void Init()
        {
            PlayerItem item = Lazy.SetupActive<EmergencySiren>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.B;
            item.consumable   = false;
            item.CanBeDropped = true;
            item.SetCooldownType(ItemBuilder.CooldownType.Damage, 300f);

            _EmergencyMods = new[] {
                new StatModifier(){
                    amount      = 2.00f,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                    statToBoost = PlayerStats.StatType.MovementSpeed,
                },
            };
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnEnteredCombat += this.OnEnteredCombat;
            player.PostProcessProjectile += this.OnFired;
        }

        public override void OnPreDrop(PlayerController player)
        {
            player.PostProcessProjectile -= this.OnFired;
            player.OnEnteredCombat -= this.OnEnteredCombat;
            base.OnPreDrop(player);
        }

        private void OnFired(Projectile p, float f)
        {
            this._anyGunFiredInRoom = true;
        }

        private void OnEnemyKilled(Vector2 v)
        {
            this._anyEnemyInRoomDied = true;
        }

        private void OnEnteredCombat()
        {
            this._anyEnemyInRoomDied = false;
            this._anyGunFiredInRoom = false;
            foreach (AIActor enemy in Lazy.CurrentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
                enemy.healthHaver.OnPreDeath += OnEnemyKilled;
        }

        public override bool CanBeUsed(PlayerController user)
        {
            RoomHandler room = user.CurrentRoom;
            if (this._roomToReset != null || this._anyEnemyInRoomDied || this._anyGunFiredInRoom || user.InBossRoom()
                || room.area.IsProceduralRoom || !user.IsInCombat || !room.IsSealed || !room.EverHadEnemies)
                return false; // can only be used in sealed rooms with non-boss enemies before firing a gun or killing any enemy

            if (room.NewWaveOfEnemiesIsSpawning())
                return false; // cannot use while enemies are actively awakening

            return base.CanBeUsed(user);
        }

        public override void DoEffect(PlayerController user)
        {
            RoomHandler room = user.CurrentRoom;
            room.UnsealRoom();

            List<AIActor> activeEnemies = room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            foreach (AIActor enemy in activeEnemies)
            {
                if (!enemy)
                    continue; // stupid grenades D:
                enemy.behaviorSpeculator?.InterruptAndDisable();
                enemy.knockbackDoer?.SetImmobile(true, "emergency_siren");
                if (enemy.healthHaver)
                {
                    enemy.healthHaver.IsVulnerable = false;
                    enemy.healthHaver.TriggerInvulnerabilityPeriod(999999f);
                }
                if (enemy.specRigidbody)
                {
                    enemy.specRigidbody.CollideWithOthers = false;
                    enemy.specRigidbody.CollideWithTileMap = false;
                    enemy.specRigidbody.OnPreMovement += (SpeculativeRigidbody b) => b.Velocity = Vector2.zero;
                }
                enemy.State = AIActor.ActorState.Inactive;
            }

            ReadOnlyCollection<Projectile> allProjectiles = StaticReferenceManager.AllProjectiles;
            for (int num = allProjectiles.Count - 1; num >= 0; num--)
                allProjectiles[num]?.DieInAir();

            this._roomToReset = room;
            this.passiveStatModifiers = _EmergencyMods;
            user.stats.RecalculateStats(user);
            user.StartCoroutine(AwaitRoomChange(user));
        }

        private IEnumerator AwaitRoomChange(PlayerController user)
        {
            GameObject v = SpawnManager.SpawnVFX(VFX.animations["EmergencySiren"], user.sprite.WorldTopCenter + new Vector2(0f, 0.75f), Quaternion.identity);
                v.transform.parent = user.transform;
            while (user.CurrentRoom == this._roomToReset)
            {
                Lazy.PlaySoundUntilDeathOrTimeout("siren_sound", v, 0.1f);
                yield return null;
            }
            UnityEngine.Object.Destroy(v);

            ResetPredefinedRoomForEmergency(this._roomToReset);
            FixDoorsInRoom(this._roomToReset);

            this.passiveStatModifiers = null;
            user.stats.RecalculateStats(user);
            this._roomToReset = null;
        }

        private static void ForceDoorClosed(DungeonDoorController door)
        {
            door.m_wasOpenWhenSealed = false;
            door.hasEverBeenOpen = false;
            if (door.GetComponent<InteractableDoorController>() is InteractableDoorController idoor)
            {
                idoor.m_hasOpened = false;
                idoor.OpensAutomaticallyOnUnlocked = false;
            }
            if (door.SupportsSubsidiaryDoors && door.subsidiaryDoor)
                ForceDoorClosed(door.subsidiaryDoor);
            door.Close();
        }

        private static void FixDoorsInRoom(RoomHandler room)
        {
            foreach (DungeonDoorController door in room.connectedDoors)
                ForceDoorClosed(door);

            foreach(RoomHandler adjacentRoom in room.connectedRooms)
                foreach (DungeonDoorController door in adjacentRoom.connectedDoors)
                    ForceDoorClosed(door);
        }

        // Modified from base game's ResetPredefinedRoomLikeDarkSouls()
        public static void ResetPredefinedRoomForEmergency(RoomHandler room)
        {
            // NOTE: if I ever change these conditions, I might need to pull in additional logic from ResetPredefinedRoomLikeDarkSouls()
            //       everything only works with these simplifying assumptions about the room state
            if (GameManager.Instance.PrimaryPlayer.CurrentRoom == room
                || room.visibility == RoomHandler.VisibilityStatus.OBSCURED
                || GameManager.Instance.InTutorial
                || room.area.IsProceduralRoom)
                return;

            if (room.activeEnemies != null)
            {
                for (int num = room.activeEnemies.Count - 1; num >= 0; num--)
                {
                    AIActor aIActor = room.activeEnemies[num];
                    if (!aIActor)
                        continue;

                    if ((bool)aIActor.behaviorSpeculator)
                        aIActor.behaviorSpeculator?.InterruptAndDisable();
                    if (aIActor.healthHaver.IsBoss && aIActor.healthHaver.IsAlive)
                        aIActor.healthHaver.EndBossState(false);
                    UnityEngine.Object.Destroy(aIActor.gameObject);
                }
                room.activeEnemies.Clear();
            }

            foreach (TalkDoerLite talker in room.GetComponentsInRoom<TalkDoerLite>())
                talker.SendPlaymakerEvent("resetRoomLikeDarkSouls");

            for (int m = 0; m < (room.bossTriggerZones?.Count ?? 0); m++)
                room.bossTriggerZones[m].HasTriggered = false;

            room.remainingReinforcementLayers?.Clear();
            room.visibility = RoomHandler.VisibilityStatus.REOBSCURED;
            room.PreventStandardRoomReward = true;

            for (int num2 = -1; num2 < room.area.runtimePrototypeData.additionalObjectLayers.Count; num2++)
            {
                if (num2 != -1 && room.area.runtimePrototypeData.additionalObjectLayers[num2].layerIsReinforcementLayer)
                {
                    PrototypeRoomObjectLayer prototypeRoomObjectLayer = room.area.runtimePrototypeData.additionalObjectLayers[num2];
                    if (prototypeRoomObjectLayer.numberTimesEncounteredRequired > 0)
                    {
                        if (room.area.prototypeRoom != null)
                        {
                            if (GameStatsManager.Instance.QueryRoomEncountered(room.area.prototypeRoom.GUID) < prototypeRoomObjectLayer.numberTimesEncounteredRequired)
                                continue;
                        }
                        else if (room.area.runtimePrototypeData != null && GameStatsManager.Instance.QueryRoomEncountered(room.area.runtimePrototypeData.GUID) < prototypeRoomObjectLayer.numberTimesEncounteredRequired)
                            continue;
                    }
                    if (!(prototypeRoomObjectLayer.probability < 1f) || !(UnityEngine.Random.value > prototypeRoomObjectLayer.probability))
                    {
                        if (room.remainingReinforcementLayers == null)
                            room.remainingReinforcementLayers = new List<PrototypeRoomObjectLayer>();
                        if (room.area.runtimePrototypeData.additionalObjectLayers[num2].placedObjects.Count > 0)
                            room.remainingReinforcementLayers.Add(room.area.runtimePrototypeData.additionalObjectLayers[num2]);
                    }
                    continue;
                }
                List<PrototypePlacedObjectData> list = ((num2 != -1) ? room.area.runtimePrototypeData.additionalObjectLayers[num2].placedObjects : room.area.runtimePrototypeData.placedObjects);
                List<Vector2> list2 = ((num2 != -1) ? room.area.runtimePrototypeData.additionalObjectLayers[num2].placedObjectBasePositions : room.area.runtimePrototypeData.placedObjectPositions);
                for (int num3 = 0; num3 < list.Count; num3++)
                {
                    PrototypePlacedObjectData prototypePlacedObjectData = list[num3];
                    if (prototypePlacedObjectData.spawnChance < 1f && UnityEngine.Random.value > prototypePlacedObjectData.spawnChance)
                    {
                        continue;
                    }
                    GameObject gameObject = null;
                    IntVector2 location = list2[num3].ToIntVector2();
                    if (prototypePlacedObjectData.placeableContents != null)
                    {
                        DungeonPlaceable placeableContents = prototypePlacedObjectData.placeableContents;
                        gameObject = placeableContents.InstantiateObject(room, location, true);
                    }
                    if (prototypePlacedObjectData.nonenemyBehaviour != null)
                    {
                        DungeonPlaceableBehaviour nonenemyBehaviour = prototypePlacedObjectData.nonenemyBehaviour;
                        gameObject = ((GameManager.Instance.CurrentLevelOverrideState != GameManager.LevelOverrideState.TUTORIAL || !(nonenemyBehaviour.GetComponent<TalkDoerLite>() != null)) ? nonenemyBehaviour.InstantiateObjectOnlyActors(room, location) : nonenemyBehaviour.InstantiateObject(room, location));
                    }
                    if (!string.IsNullOrEmpty(prototypePlacedObjectData.enemyBehaviourGuid))
                    {
                        DungeonPlaceableBehaviour orLoadByGuid = EnemyDatabase.GetOrLoadByGuid(prototypePlacedObjectData.enemyBehaviourGuid);
                        gameObject = orLoadByGuid.InstantiateObjectOnlyActors(room, location);
                    }
                    if (gameObject != null)
                    {
                        AIActor component = gameObject.GetComponent<AIActor>();
                        if ((bool)component)
                        {
                            if ((bool)component.healthHaver && component.healthHaver.IsBoss)
                            {
                                component.HasDonePlayerEnterCheck = true;
                            }
                            if (component.EnemyGuid == GlobalEnemyGuids.GripMaster)
                            {
                                UnityEngine.Object.Destroy(component.gameObject);
                                continue;
                            }
                        }
                        if (prototypePlacedObjectData.xMPxOffset != 0 || prototypePlacedObjectData.yMPxOffset != 0)
                        {
                            Vector2 vector = new Vector2((float)prototypePlacedObjectData.xMPxOffset * 0.0625f, (float)prototypePlacedObjectData.yMPxOffset * 0.0625f);
                            gameObject.transform.position = gameObject.transform.position + vector.ToVector3ZUp();
                        }
                        IPlayerInteractable[] interfacesInChildren = gameObject.GetInterfacesInChildren<IPlayerInteractable>();
                        for (int num4 = 0; num4 < interfacesInChildren.Length; num4++)
                        {
                            room.interactableObjects.Add(interfacesInChildren[num4]);
                        }
                        room.HandleFields(prototypePlacedObjectData, gameObject);
                        gameObject.transform.parent = room.hierarchyParent;
                    }
                    if (prototypePlacedObjectData.linkedTriggerAreaIDs != null && prototypePlacedObjectData.linkedTriggerAreaIDs.Count > 0 && gameObject != null)
                    {
                        for (int num5 = 0; num5 < prototypePlacedObjectData.linkedTriggerAreaIDs.Count; num5++)
                        {
                            int key = prototypePlacedObjectData.linkedTriggerAreaIDs[num5];
                            if (room.eventTriggerMap != null && room.eventTriggerMap.ContainsKey(key))
                            {
                                room.eventTriggerMap[key].AddGameObject(gameObject);
                            }
                        }
                    }
                    if (prototypePlacedObjectData.assignedPathIDx != -1 && (bool)gameObject)
                    {
                        PathMover component2 = gameObject.GetComponent<PathMover>();
                        if (component2 != null)
                        {
                            component2.Path = room.area.runtimePrototypeData.paths[prototypePlacedObjectData.assignedPathIDx];
                            component2.PathStartNode = prototypePlacedObjectData.assignedPathStartNode;
                            component2.RoomHandler = room;
                        }
                    }
                }
            }

            Pixelator.Instance.ProcessOcclusionChange(IntVector2.Zero, 0f, room, false);
        }
    }
}
