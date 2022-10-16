using Alexandria.ChestAPI;
using Alexandria.Misc;
using Dungeonator;
using Alexandria.ItemAPI;
using Pathfinding;
using SaveAPI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NevernamedsItems
{
    class Keygen : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Keygen";
            string resourceName = "NevernamedsItems/Resources/NeoActiveSprites/keygen_icon";

            GameObject obj = new GameObject(itemName);

            var item = obj.AddComponent<Keygen>();

            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "wHO n n n eEDS KEYs?!1!";
            string longDesc = "A strange fragment of corrupted software initially developed to generate free access to the contents of chests within the Gungeon." + "\n\nIn the years since it's creation however, it has become... chaotic, unpredictable, and dangerous. Use with extreme caution.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 70);

            item.consumable = false;
            item.quality = ItemQuality.B;

            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.BOSSRUSH_PILOT, true);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnNewFloorLoaded += this.NewLevel;
        }
        public override void OnPreDrop(PlayerController user)
        {
            base.OnPreDrop(user);
            user.OnNewFloorLoaded -= this.NewLevel;

        }
        public override void OnDestroy()
        {
            if (LastOwner != null) LastOwner.OnNewFloorLoaded -= NewLevel;
            base.OnDestroy();
        }
        private void NewLevel(PlayerController player)
        {
            base.StartCoroutine(this.LaunchChestSpawns());
        }
        public override void DoEffect(PlayerController user)
        {
            IPlayerInteractable nearestInteractable = user.CurrentRoom.GetNearestInteractable(user.CenterPosition, 1f, user);
            if (!(nearestInteractable is Chest)) return;

            AkSoundEngine.PostEvent("Play_ENM_electric_charge_01", user.gameObject);

            Chest rerollChest = nearestInteractable as Chest;
            int selected = UnityEngine.Random.Range(1, 15);
            ETGModConsole.Log(selected.ToString());

            VFXToolbox.GlitchScreenForSeconds(1.5f);


            switch (selected)
            {
                case 1: //Blow up chest
                    if (user.PlayerHasActiveSynergy("KEYGEN"))
                    {
                        SpareChest(rerollChest);
                    }
                    else
                    {
                        Exploder.DoDefaultExplosion(rerollChest.specRigidbody.UnitCenter, Vector2.zero);
                        if (rerollChest.IsMimic) OMITBReflectionHelpers.ReflectSetField<bool>(typeof(Chest), "m_isMimic", false, rerollChest);
                        rerollChest.majorBreakable.Break(Vector2.zero);
                        if (rerollChest.GetChestTier() == ChestUtility.ChestTier.RAT) UnityEngine.Object.Destroy(rerollChest.gameObject);
                    }
                    break;
                case 2: //Open Chest
                    rerollChest.ForceOpen(user);
                    break;
                case 3: //Break Lock
                    if (user.PlayerHasActiveSynergy("KEYGEN"))
                    {
                        SpareChest(rerollChest);
                    }
                    else
                    {
                        if (rerollChest.IsLocked) rerollChest.BreakLock();
                        else rerollChest.majorBreakable.Break(Vector2.zero);
                    }
                    break;
                case 4: //Duplicate Chest
                    DupeChest(rerollChest, user);
                    break;
                case 5: //Turn into mimic
                    if (!rerollChest.IsMimic) { rerollChest.overrideMimicChance = 100; rerollChest.MaybeBecomeMimic(); }
                    rerollChest.ForceOpen(user);
                    break;
                case 6: //Englitch
                    List<GlobalDungeonData.ValidTilesets> bannedGlitchFloors = new List<GlobalDungeonData.ValidTilesets>()
                    {
                         GlobalDungeonData.ValidTilesets.CATACOMBGEON,
                         GlobalDungeonData.ValidTilesets.HELLGEON,
                         GlobalDungeonData.ValidTilesets.OFFICEGEON,
                         GlobalDungeonData.ValidTilesets.FORGEGEON,
                    };
                    if (!bannedGlitchFloors.Contains(GameManager.Instance.Dungeon.tileIndices.tilesetId)) rerollChest.BecomeGlitchChest();
                    else
                    {
                        if (!rerollChest.IsMimic) rerollChest.MaybeBecomeMimic();
                        rerollChest.ForceOpen(user);
                    }
                    break;
                case 7: //Enrainbow
                    if (UnityEngine.Random.value <= 0.65f) UpgradeChest(rerollChest, user);
                    else rerollChest.BecomeRainbowChest();
                    break;
                case 8: //Reroll
                    RerollChest(rerollChest, user);
                    break;
                case 9: //Turn into pickups
                    if (user.PlayerHasActiveSynergy("KEYGEN"))
                    {
                        SpareChest(rerollChest);
                    }
                    else
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            LootEngine.SpawnItem(PickupObjectDatabase.GetById(BraveUtility.RandomElement(BabyGoodChanceKin.lootIDlist)).gameObject, rerollChest.sprite.WorldCenter, Vector2.zero, 0);
                        }
                        LootEngine.SpawnCurrency(rerollChest.sprite.WorldCenter, UnityEngine.Random.Range(5, 11));
                        user.CurrentRoom.DeregisterInteractable(rerollChest);
                        rerollChest.DeregisterChestOnMinimap();
                        UnityEngine.Object.Destroy(rerollChest.gameObject);
                    }
                    break;
                case 10: //Fuse
                    if (user.PlayerHasActiveSynergy("KEYGEN"))
                    {
                        SpareChest(rerollChest);
                    }
                    else
                    {
                        var type = typeof(Chest);
                        var func = type.GetMethod("TriggerCountdownTimer", BindingFlags.Instance | BindingFlags.NonPublic);
                        var ret = func.Invoke(rerollChest, null);
                        AkSoundEngine.PostEvent("Play_OBJ_fuse_loop_01", rerollChest.gameObject);
                    }
                    break;
                case 11: //Unlock
                    if (rerollChest.IsLocked) rerollChest.ForceUnlock();
                    else rerollChest.ForceOpen(user);
                    break;
                case 12: //Enjam
                    rerollChest.gameObject.GetOrAddComponent<JammedChestBehav>();
                    break;
                case 13: //TeleportChest
                    TeleportChest(rerollChest, user);
                    break;
                case 14: //Drill
                    FauxDrill(rerollChest, user);
                    break;
            }

        }
        private void SpareChest(Chest chest)
        {
            UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.GundetaleSpareVFX, (chest.sprite.WorldTopCenter + new Vector2(0, 0.25f)), Quaternion.identity);

        }
        private void TeleportChest(Chest chest, PlayerController user)
        {
            CachedChestData item = new CachedChestData(chest);
            SpawnManager.SpawnVFX(EasyVFXDatabase.ChestTeleporterTimeWarp, chest.sprite.WorldCenter, Quaternion.identity, true);
            user.CurrentRoom.DeregisterInteractable(chest);
            chest.DeregisterChestOnMinimap();
            if (chest.majorBreakable)
            {
                chest.majorBreakable.TemporarilyInvulnerable = true;
            }
            Object.Destroy(chest.gameObject, 0.8f);
            this.m_chestos.Add(item);
        }
        private List<CachedChestData> m_chestos = new List<CachedChestData>();
        private void UpgradeChest(Chest chest, PlayerController user)
        {
            ChestUtility.ChestTier tier = chest.GetChestTier();
            ChestUtility.ChestTier targetTier = ChestUtility.ChestTier.OTHER;
            switch (tier)
            {
                case ChestUtility.ChestTier.BROWN:
                    targetTier = ChestUtility.ChestTier.BLUE;
                    break;
                case ChestUtility.ChestTier.BLUE:
                    targetTier = ChestUtility.ChestTier.GREEN;
                    break;
                case ChestUtility.ChestTier.GREEN:
                    targetTier = ChestUtility.ChestTier.RED;
                    break;
                case ChestUtility.ChestTier.RED:
                    targetTier = ChestUtility.ChestTier.BLACK;
                    break;
                case ChestUtility.ChestTier.BLACK:
                    targetTier = ChestUtility.ChestTier.RAINBOW;
                    break;
                case ChestUtility.ChestTier.SYNERGY:
                    targetTier = ChestUtility.ChestTier.BLACK;
                    break;
            }

            ThreeStateValue isMimic = ThreeStateValue.UNSPECIFIED;
            if (chest.IsMimic) isMimic = ThreeStateValue.FORCEYES;
            else isMimic = ThreeStateValue.FORCENO;

            if (targetTier != ChestUtility.ChestTier.OTHER) ChestUtility.SpawnChestEasy(chest.sprite.WorldBottomLeft.ToIntVector2(), targetTier, chest.IsLocked, Chest.GeneralChestType.UNSPECIFIED, isMimic);
            else GameManager.Instance.RewardManager.SpawnRewardChestAt(chest.sprite.WorldBottomLeft.ToIntVector2());
            user.CurrentRoom.DeregisterInteractable(chest);
            chest.DeregisterChestOnMinimap();
            Destroy(chest.gameObject);
        }
        private void RerollChest(Chest chest, PlayerController user)
        {
            Chest newChest = GameManager.Instance.RewardManager.SpawnRewardChestAt(chest.sprite.WorldBottomLeft.ToIntVector2());
            user.CurrentRoom.DeregisterInteractable(chest);
            chest.DeregisterChestOnMinimap();
            Destroy(chest.gameObject);
        }
        private void DupeChest(Chest chest, PlayerController user)
        {
            IntVector2 bestRewardLocation = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
            ChestUtility.ChestTier tier = chest.GetChestTier();
            if (tier == ChestUtility.ChestTier.RAT)
            {
                tier = ChestUtility.ChestTier.RED;
            }
            else if (tier == ChestUtility.ChestTier.TRUTH)
            {
                tier = ChestUtility.ChestTier.BLUE;

            }

            ThreeStateValue isMimic = ThreeStateValue.UNSPECIFIED;
            if (chest.IsMimic) isMimic = ThreeStateValue.FORCEYES;
            else isMimic = ThreeStateValue.FORCENO;

            ChestUtility.SpawnChestEasy(bestRewardLocation, tier, chest.IsLocked, Chest.GeneralChestType.UNSPECIFIED, isMimic);
        }

        public override bool CanBeUsed(PlayerController user)
        {
            IPlayerInteractable nearestInteractable = user.CurrentRoom.GetNearestInteractable(user.CenterPosition, 1f, user);
            return nearestInteractable is Chest;
        }
        private IEnumerator LaunchChestSpawns()
        {
            while (Dungeon.IsGenerating)
            {
                yield return null;
            }
            yield return null;
            List<CachedChestData> failedList = new List<CachedChestData>();
            for (int i = 0; i < this.m_chestos.Count; i++)
            {
                CachedChestData cachedChestData = this.m_chestos[i];
                RoomHandler entrance = GameManager.Instance.Dungeon.data.Entrance;
                RoomHandler roomHandler = entrance;
                cachedChestData.Upgrade();

                CellValidator cellValidator = delegate (IntVector2 c)
                {
                    for (int n = 0; n < 5; n++)
                    {
                        for (int num2 = 0; num2 < 5; num2++)
                        {
                            if (!GameManager.Instance.Dungeon.data.CheckInBoundsAndValid(c.x + n, c.y + num2) || GameManager.Instance.Dungeon.data[c.x + n, c.y + num2].type == CellType.PIT || GameManager.Instance.Dungeon.data[c.x + n, c.y + num2].isOccupied)
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                };
                IntVector2? randomAvailableCell = roomHandler.GetRandomAvailableCell(new IntVector2?(IntVector2.One * 5), new CellTypes?(CellTypes.FLOOR), false, cellValidator);
                IntVector2? intVector = (randomAvailableCell == null) ? null : new IntVector2?(randomAvailableCell.GetValueOrDefault() + IntVector2.One);
                if (intVector != null)
                {
                    cachedChestData.SpawnChest(intVector.Value);
                    for (int j = 0; j < 3; j++)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            IntVector2 key = intVector.Value + IntVector2.One + new IntVector2(j, k);
                            GameManager.Instance.Dungeon.data[key].isOccupied = true;
                        }
                    }
                }
                else
                {
                    roomHandler = ((roomHandler != entrance) ? entrance : ChestTeleporterItem.FindBossFoyer());
                    if (roomHandler == null)
                    {
                        roomHandler = entrance;
                    }
                    IntVector2? randomAvailableCell2 = roomHandler.GetRandomAvailableCell(new IntVector2?(IntVector2.One * 5), new CellTypes?(CellTypes.FLOOR), false, cellValidator);
                    intVector = ((randomAvailableCell2 == null) ? null : new IntVector2?(randomAvailableCell2.GetValueOrDefault() + IntVector2.One));
                    if (intVector != null)
                    {
                        cachedChestData.SpawnChest(intVector.Value);
                        for (int l = 0; l < 3; l++)
                        {
                            for (int m = 0; m < 3; m++)
                            {
                                IntVector2 key2 = intVector.Value + IntVector2.One + new IntVector2(l, m);
                                GameManager.Instance.Dungeon.data[key2].isOccupied = true;
                            }
                        }
                    }
                    else
                    {
                        failedList.Add(cachedChestData);
                    }
                }
            }
            this.m_chestos.Clear();
            this.m_chestos.AddRange(failedList);
            yield break;
        }

        //---------------------------------------------------------------------------------------
        //-----------------CAUTION: EVERYTHING BEYOND THIS POINT IS DRILL CODE. ENTER AT OWN RISK.
        //---------------------------------------------------------------------------------------

        #region DrillShit

        private bool drillInEffect;
        private void FauxDrill(Chest chest, PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_OBJ_paydaydrill_start_01", GameManager.Instance.gameObject);
            AkSoundEngine.PostEvent("Play_OBJ_paydaydrill_loop_01", GameManager.Instance.gameObject);
            if (chest.IsLocked)
            {
                if (chest.IsLockBroken)
                {
                    chest.ForceUnlock();
                    AkSoundEngine.PostEvent("Stop_OBJ_paydaydrill_loop_01", GameManager.Instance.gameObject);
                }
                else if (chest.IsMimic && chest.majorBreakable)
                {
                    chest.majorBreakable.ApplyDamage(1000f, Vector2.zero, false, false, true);
                    AkSoundEngine.PostEvent("Stop_OBJ_paydaydrill_loop_01", GameManager.Instance.gameObject);
                }
                else
                {
                    chest.ForceKillFuse();
                    chest.PreventFuse = true;
                    RoomHandler absoluteRoom = chest.transform.position.GetAbsoluteRoom();
                    this.drillInEffect = true;
                    if (absoluteRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.REWARD)
                    {
                        GameManager.Instance.Dungeon.StartCoroutine(this.HandleSeamlessTransitionToCombatRoom(absoluteRoom, chest));
                    }
                    else
                    {
                        GameManager.Instance.Dungeon.StartCoroutine(this.HandleTransitionToFallbackCombatRoom(absoluteRoom, chest));
                    }
                }
            }
            else
            {
                chest.ForceOpen(user);
                AkSoundEngine.PostEvent("Stop_OBJ_paydaydrill_loop_01", GameManager.Instance.gameObject);
            }
        }
        protected IEnumerator HandleTransitionToFallbackCombatRoom(RoomHandler sourceRoom, Chest sourceChest)
        {
            Dungeon d = GameManager.Instance.Dungeon;
            sourceChest.majorBreakable.TemporarilyInvulnerable = true;
            sourceRoom.DeregisterInteractable(sourceChest);
            RoomHandler newRoom = d.AddRuntimeRoom(this.GenericFallbackCombatRoom, null, DungeonData.LightGenerationStyle.FORCE_COLOR);
            newRoom.CompletelyPreventLeaving = true;
            Vector3 oldChestPosition = sourceChest.transform.position;
            sourceChest.transform.position = newRoom.Epicenter.ToVector3();
            if (sourceChest.transform.parent == sourceRoom.hierarchyParent)
            {
                sourceChest.transform.parent = newRoom.hierarchyParent;
            }
            SpeculativeRigidbody component = sourceChest.GetComponent<SpeculativeRigidbody>();
            if (component)
            {
                component.Reinitialize();
                PathBlocker.BlockRigidbody(component, false);
            }
            tk2dBaseSprite component2 = sourceChest.GetComponent<tk2dBaseSprite>();
            if (component2)
            {
                component2.UpdateZDepth();
            }
            Vector3 chestOffset = this.m_baseChestOffset;
            if (sourceChest.name.Contains("_Red") || sourceChest.name.Contains("_Black"))
            {
                chestOffset += this.m_largeChestOffset;
            }
            GameObject spawnedVFX = SpawnManager.SpawnVFX(this.DrillVFXPrefab, sourceChest.transform.position + chestOffset, Quaternion.identity);
            tk2dBaseSprite spawnedSprite = spawnedVFX.GetComponent<tk2dBaseSprite>();
            spawnedSprite.HeightOffGround = 1f;
            spawnedSprite.UpdateZDepth();
            Vector2 oldPlayerPosition = GameManager.Instance.BestActivePlayer.transform.position.XY();
            Vector2 newPlayerPosition = newRoom.Epicenter.ToVector2() + new Vector2(0f, -3f);
            Pixelator.Instance.FadeToColor(0.25f, Color.white, true, 0.125f);
            Pathfinder.Instance.InitializeRegion(d.data, newRoom.area.basePosition, newRoom.area.dimensions);
            GameManager.Instance.BestActivePlayer.WarpToPoint(newPlayerPosition, false, false);
            if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
            {
                GameManager.Instance.GetOtherPlayer(GameManager.Instance.BestActivePlayer).ReuniteWithOtherPlayer(GameManager.Instance.BestActivePlayer, false);
            }
            yield return null;
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                GameManager.Instance.AllPlayers[i].WarpFollowersToPlayer(false);
                GameManager.Instance.AllPlayers[i].WarpCompanionsToPlayer(false);
            }
            yield return new WaitForSeconds(this.DelayPostExpansionPreEnemies);
            yield return base.StartCoroutine(this.HandleCombatWaves(d, newRoom, sourceChest));
            this.DisappearDrillPoof.SpawnAtPosition(spawnedSprite.WorldBottomLeft + new Vector2(-0.0625f, 0.25f), 0f, null, null, null, new float?(3f), false, null, null, false);
            Object.Destroy(spawnedVFX.gameObject);
            AkSoundEngine.PostEvent("Stop_OBJ_paydaydrill_loop_01", GameManager.Instance.gameObject);
            AkSoundEngine.PostEvent("Play_OBJ_item_spawn_01", GameManager.Instance.gameObject);
            sourceChest.ForceUnlock();
            bool goodToGo = false;
            while (!goodToGo)
            {
                goodToGo = true;
                for (int j = 0; j < GameManager.Instance.AllPlayers.Length; j++)
                {
                    float num = Vector2.Distance(sourceChest.specRigidbody.UnitCenter, GameManager.Instance.AllPlayers[j].CenterPosition);
                    if (num > 3f)
                    {
                        goodToGo = false;
                    }
                }
                yield return null;
            }
            Pixelator.Instance.FadeToColor(0.25f, Color.white, true, 0.125f);
            GameManager.Instance.BestActivePlayer.WarpToPoint(oldPlayerPosition, false, false);
            if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
            {
                GameManager.Instance.GetOtherPlayer(GameManager.Instance.BestActivePlayer).ReuniteWithOtherPlayer(GameManager.Instance.BestActivePlayer, false);
            }
            sourceChest.transform.position = oldChestPosition;
            if (sourceChest.transform.parent == newRoom.hierarchyParent)
            {
                sourceChest.transform.parent = sourceRoom.hierarchyParent;
            }
            SpeculativeRigidbody component3 = sourceChest.GetComponent<SpeculativeRigidbody>();
            if (component3)
            {
                component3.Reinitialize();
            }
            tk2dBaseSprite component4 = sourceChest.GetComponent<tk2dBaseSprite>();
            if (component4)
            {
                component4.UpdateZDepth();
            }
            sourceRoom.RegisterInteractable(sourceChest);
            this.drillInEffect = false;
            yield break;
        }
        protected IEnumerator HandleSeamlessTransitionToCombatRoom(RoomHandler sourceRoom, Chest sourceChest)
        {
            Dungeon d = GameManager.Instance.Dungeon;
            sourceChest.majorBreakable.TemporarilyInvulnerable = true;
            sourceRoom.DeregisterInteractable(sourceChest);
            int tmapExpansion = 13;
            RoomHandler newRoom = d.RuntimeDuplicateChunk(sourceRoom.area.basePosition, sourceRoom.area.dimensions, tmapExpansion, sourceRoom, true);
            newRoom.CompletelyPreventLeaving = true;
            List<Transform> movedObjects = new List<Transform>();
            for (int i = 0; i < this.c_rewardRoomObjects.Length; i++)
            {
                Transform transform = sourceRoom.hierarchyParent.Find(this.c_rewardRoomObjects[i]);
                if (transform)
                {
                    movedObjects.Add(transform);
                    this.MoveObjectBetweenRooms(transform, sourceRoom, newRoom);
                }
            }
            this.MoveObjectBetweenRooms(sourceChest.transform, sourceRoom, newRoom);
            if (sourceChest.specRigidbody)
            {
                PathBlocker.BlockRigidbody(sourceChest.specRigidbody, false);
            }
            Vector3 chestOffset = this.m_baseChestOffset;
            if (sourceChest.name.Contains("_Red") || sourceChest.name.Contains("_Black"))
            {
                chestOffset += this.m_largeChestOffset;
            }
            GameObject spawnedVFX = SpawnManager.SpawnVFX(this.DrillVFXPrefab, sourceChest.transform.position + chestOffset, Quaternion.identity);
            tk2dBaseSprite spawnedSprite = spawnedVFX.GetComponent<tk2dBaseSprite>();
            spawnedSprite.HeightOffGround = 1f;
            spawnedSprite.UpdateZDepth();
            Vector2 oldPlayerPosition = GameManager.Instance.BestActivePlayer.transform.position.XY();
            Vector2 playerOffset = oldPlayerPosition - sourceRoom.area.basePosition.ToVector2();
            Vector2 newPlayerPosition = newRoom.area.basePosition.ToVector2() + playerOffset;
            Pixelator.Instance.FadeToColor(0.25f, Color.white, true, 0.125f);
            Pathfinder.Instance.InitializeRegion(d.data, newRoom.area.basePosition, newRoom.area.dimensions);
            GameManager.Instance.BestActivePlayer.WarpToPoint(newPlayerPosition, false, false);
            if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
            {
                GameManager.Instance.GetOtherPlayer(GameManager.Instance.BestActivePlayer).ReuniteWithOtherPlayer(GameManager.Instance.BestActivePlayer, false);
            }
            yield return null;
            for (int j = 0; j < GameManager.Instance.AllPlayers.Length; j++)
            {
                GameManager.Instance.AllPlayers[j].WarpFollowersToPlayer(false);
                GameManager.Instance.AllPlayers[j].WarpCompanionsToPlayer(false);
            }
            yield return d.StartCoroutine(this.HandleCombatRoomExpansion(sourceRoom, newRoom, sourceChest));
            this.DisappearDrillPoof.SpawnAtPosition(spawnedSprite.WorldBottomLeft + new Vector2(-0.0625f, 0.25f), 0f, null, null, null, new float?(3f), false, null, null, false);
            Object.Destroy(spawnedVFX.gameObject);
            sourceChest.ForceUnlock();
            AkSoundEngine.PostEvent("Stop_OBJ_paydaydrill_loop_01", GameManager.Instance.gameObject);
            AkSoundEngine.PostEvent("Play_OBJ_item_spawn_01", GameManager.Instance.gameObject);
            bool goodToGo = false;
            while (!goodToGo)
            {
                goodToGo = true;
                for (int k = 0; k < GameManager.Instance.AllPlayers.Length; k++)
                {
                    float num = Vector2.Distance(sourceChest.specRigidbody.UnitCenter, GameManager.Instance.AllPlayers[k].CenterPosition);
                    if (num > 3f)
                    {
                        goodToGo = false;
                    }
                }
                yield return null;
            }
            GameManager.Instance.MainCameraController.SetManualControl(true, true);
            GameManager.Instance.MainCameraController.OverridePosition = GameManager.Instance.BestActivePlayer.CenterPosition;
            for (int l = 0; l < GameManager.Instance.AllPlayers.Length; l++)
            {
                GameManager.Instance.AllPlayers[l].SetInputOverride("shrinkage");
            }
            yield return d.StartCoroutine(this.HandleCombatRoomShrinking(newRoom));
            for (int m = 0; m < GameManager.Instance.AllPlayers.Length; m++)
            {
                GameManager.Instance.AllPlayers[m].ClearInputOverride("shrinkage");
            }
            Pixelator.Instance.FadeToColor(0.25f, Color.white, true, 0.125f);
            AkSoundEngine.PostEvent("Play_OBJ_paydaydrill_end_01", GameManager.Instance.gameObject);
            GameManager.Instance.MainCameraController.SetManualControl(false, false);
            GameManager.Instance.BestActivePlayer.WarpToPoint(oldPlayerPosition, false, false);
            if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
            {
                GameManager.Instance.GetOtherPlayer(GameManager.Instance.BestActivePlayer).ReuniteWithOtherPlayer(GameManager.Instance.BestActivePlayer, false);
            }
            this.MoveObjectBetweenRooms(sourceChest.transform, newRoom, sourceRoom);
            for (int n = 0; n < movedObjects.Count; n++)
            {
                this.MoveObjectBetweenRooms(movedObjects[n], newRoom, sourceRoom);
            }
            sourceRoom.RegisterInteractable(sourceChest);
            this.drillInEffect = false;
            yield break;
        }
        private IEnumerator HandleCombatRoomExpansion(RoomHandler sourceRoom, RoomHandler targetRoom, Chest sourceChest)
        {
            yield return new WaitForSeconds(this.DelayPreExpansion);
            float duration = 5.5f;
            float elapsed = 0f;
            int numExpansionsDone = 0;
            while (elapsed < duration)
            {
                elapsed += BraveTime.DeltaTime * 9f;
                while (elapsed > (float)numExpansionsDone)
                {
                    numExpansionsDone++;
                    this.ExpandRoom(targetRoom);
                    AkSoundEngine.PostEvent("Play_OBJ_rock_break_01", GameManager.Instance.gameObject);
                }
                yield return null;
            }
            Dungeon d = GameManager.Instance.Dungeon;
            Pathfinder.Instance.InitializeRegion(d.data, targetRoom.area.basePosition + new IntVector2(-5, -5), targetRoom.area.dimensions + new IntVector2(10, 10));
            yield return new WaitForSeconds(this.DelayPostExpansionPreEnemies);
            yield return this.HandleCombatWaves(d, targetRoom, sourceChest);
            yield break;
        }
        protected IEnumerator HandleCombatWaves(Dungeon d, RoomHandler newRoom, Chest sourceChest)
        {
            DrillWaveDefinition[] wavesToUse = this.D_Quality_Waves;
            switch (GameManager.Instance.RewardManager.GetQualityFromChest(sourceChest))
            {
                case PickupObject.ItemQuality.C:
                    wavesToUse = this.C_Quality_Waves;
                    break;
                case PickupObject.ItemQuality.B:
                    wavesToUse = this.B_Quality_Waves;
                    break;
                case PickupObject.ItemQuality.A:
                    wavesToUse = this.A_Quality_Waves;
                    break;
                case PickupObject.ItemQuality.S:
                    wavesToUse = this.S_Quality_Waves;
                    break;
            }
            foreach (DrillWaveDefinition currentWave in wavesToUse)
            {
                int numEnemiesToSpawn = Random.Range(currentWave.MinEnemies, currentWave.MaxEnemies + 1);
                for (int i = 0; i < numEnemiesToSpawn; i++)
                {
                    newRoom.AddSpecificEnemyToRoomProcedurally(d.GetWeightedProceduralEnemy().enemyGuid, true, null);
                }
                yield return new WaitForSeconds(3f);
                while (newRoom.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.RoomClear) > 0)
                {
                    yield return new WaitForSeconds(1f);
                }
                if (newRoom.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.All) > 0)
                {
                    List<AIActor> activeEnemies = newRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                    for (int j = 0; j < activeEnemies.Count; j++)
                    {
                        if (activeEnemies[j].IsNormalEnemy)
                        {
                            activeEnemies[j].EraseFromExistence(false);
                        }
                    }
                }
            }
            yield break;
        }
        private IEnumerator HandleCombatRoomShrinking(RoomHandler targetRoom)
        {
            float elapsed = 5.5f;
            int numExpansionsDone = 6;
            while (elapsed > 0f)
            {
                elapsed -= BraveTime.DeltaTime * 9f;
                while (elapsed < (float)numExpansionsDone && numExpansionsDone > 0)
                {
                    numExpansionsDone--;
                    this.ShrinkRoom(targetRoom);
                }
                yield return null;
            }
            yield break;
        }
        private void ShrinkRoom(RoomHandler r)
        {
            Dungeon dungeon = GameManager.Instance.Dungeon;
            AkSoundEngine.PostEvent("Play_OBJ_stone_crumble_01", GameManager.Instance.gameObject);
            tk2dTileMap tk2dTileMap = null;
            HashSet<IntVector2> hashSet = new HashSet<IntVector2>();
            for (int i = -5; i < r.area.dimensions.x + 5; i++)
            {
                for (int j = -5; j < r.area.dimensions.y + 5; j++)
                {
                    IntVector2 intVector = r.area.basePosition + new IntVector2(i, j);
                    CellData cellData = (!dungeon.data.CheckInBoundsAndValid(intVector)) ? null : dungeon.data[intVector];
                    if (cellData != null && cellData.type != CellType.WALL && cellData.HasTypeNeighbor(dungeon.data, CellType.WALL))
                    {
                        hashSet.Add(cellData.position);
                    }
                }
            }
            foreach (IntVector2 key in hashSet)
            {
                CellData cellData2 = dungeon.data[key];
                cellData2.breakable = true;
                cellData2.occlusionData.overrideOcclusion = true;
                cellData2.occlusionData.cellOcclusionDirty = true;
                tk2dTileMap = dungeon.ConstructWallAtPosition(key.x, key.y, true);
                r.Cells.Remove(cellData2.position);
                r.CellsWithoutExits.Remove(cellData2.position);
                r.RawCells.Remove(cellData2.position);
            }
            Pixelator.Instance.MarkOcclusionDirty();
            Pixelator.Instance.ProcessOcclusionChange(r.Epicenter, 1f, r, false);
            if (tk2dTileMap)
            {
                dungeon.RebuildTilemap(tk2dTileMap);
            }
        }
        private void ExpandRoom(RoomHandler r)
        {
            Dungeon dungeon = GameManager.Instance.Dungeon;
            AkSoundEngine.PostEvent("Play_OBJ_stone_crumble_01", GameManager.Instance.gameObject);
            tk2dTileMap tk2dTileMap = null;
            HashSet<IntVector2> hashSet = new HashSet<IntVector2>();
            for (int i = -5; i < r.area.dimensions.x + 5; i++)
            {
                for (int j = -5; j < r.area.dimensions.y + 5; j++)
                {
                    IntVector2 intVector = r.area.basePosition + new IntVector2(i, j);
                    CellData cellData = (!dungeon.data.CheckInBoundsAndValid(intVector)) ? null : dungeon.data[intVector];
                    if (cellData != null && cellData.type == CellType.WALL && cellData.HasTypeNeighbor(dungeon.data, CellType.FLOOR))
                    {
                        hashSet.Add(cellData.position);
                    }
                }
            }
            foreach (IntVector2 key in hashSet)
            {
                CellData cellData2 = dungeon.data[key];
                cellData2.breakable = true;
                cellData2.occlusionData.overrideOcclusion = true;
                cellData2.occlusionData.cellOcclusionDirty = true;
                tk2dTileMap = dungeon.DestroyWallAtPosition(key.x, key.y, true);
                if (Random.value < 0.25f)
                {
                    this.VFXDustPoof.SpawnAtPosition(key.ToCenterVector3((float)key.y), 0f, null, null, null, null, false, null, null, false);
                }
                r.Cells.Add(cellData2.position);
                r.CellsWithoutExits.Add(cellData2.position);
                r.RawCells.Add(cellData2.position);
            }
            Pixelator.Instance.MarkOcclusionDirty();
            Pixelator.Instance.ProcessOcclusionChange(r.Epicenter, 1f, r, false);
            if (tk2dTileMap)
            {
                dungeon.RebuildTilemap(tk2dTileMap);
            }
        }
        private void MoveObjectBetweenRooms(Transform foundObject, RoomHandler fromRoom, RoomHandler toRoom)
        {
            Vector2 vector = foundObject.position.XY() - fromRoom.area.basePosition.ToVector2();
            Vector2 vector2 = toRoom.area.basePosition.ToVector2() + vector;
            foundObject.transform.position = vector2;
            if (foundObject.parent == fromRoom.hierarchyParent)
            {
                foundObject.parent = toRoom.hierarchyParent;
            }
            SpeculativeRigidbody component = foundObject.GetComponent<SpeculativeRigidbody>();
            if (component)
            {
                component.Reinitialize();
            }
            tk2dBaseSprite component2 = foundObject.GetComponent<tk2dBaseSprite>();
            if (component2)
            {
                component2.UpdateZDepth();
            }
        }
        private string[] c_rewardRoomObjects = new string[]
    {
        "Gungeon_Treasure_Dais(Clone)",
        "GodRay_Placeable(Clone)"
    };
        private Vector3 m_baseChestOffset = new Vector3(0.5f, 0.25f, 0f);
        private Vector3 m_largeChestOffset = new Vector3(0.4375f, 0.0625f, 0f);
        public GameObject DrillVFXPrefab = PickupObjectDatabase.GetById(625).GetComponent<PaydayDrillItem>().DrillVFXPrefab;
        public VFXPool VFXDustPoof = PickupObjectDatabase.GetById(625).GetComponent<PaydayDrillItem>().VFXDustPoof;
        public VFXPool DisappearDrillPoof = PickupObjectDatabase.GetById(625).GetComponent<PaydayDrillItem>().DisappearDrillPoof;
        public PrototypeDungeonRoom GenericFallbackCombatRoom = PickupObjectDatabase.GetById(625).GetComponent<PaydayDrillItem>().GenericFallbackCombatRoom;
        public float DelayPreExpansion = 2.5f;
        public float DelayPostExpansionPreEnemies = 2f;
        public DrillWaveDefinition[] D_Quality_Waves = PickupObjectDatabase.GetById(625).GetComponent<PaydayDrillItem>().D_Quality_Waves;
        public DrillWaveDefinition[] C_Quality_Waves = PickupObjectDatabase.GetById(625).GetComponent<PaydayDrillItem>().C_Quality_Waves;
        public DrillWaveDefinition[] B_Quality_Waves = PickupObjectDatabase.GetById(625).GetComponent<PaydayDrillItem>().B_Quality_Waves;
        public DrillWaveDefinition[] A_Quality_Waves = PickupObjectDatabase.GetById(625).GetComponent<PaydayDrillItem>().A_Quality_Waves;
        public DrillWaveDefinition[] S_Quality_Waves = PickupObjectDatabase.GetById(625).GetComponent<PaydayDrillItem>().S_Quality_Waves;
        #endregion
    }
}

