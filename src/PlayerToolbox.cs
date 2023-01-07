using Alexandria.Misc;
using Dungeonator;
using MonoMod.RuntimeDetour;
using SaveAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CwaffingTheGungy
{
    class PlayerToolsSetup
    {
        public static void Init()
        {
            playerStartHook = new Hook(
                typeof(PlayerController).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance),
                typeof(PlayerToolsSetup).GetMethod("DoSetup"));
        }
        public static void DoSetup(Action<PlayerController> action, PlayerController player)
        {
            action(player);
            if (player.GetComponent<HatController>() == null) player.gameObject.AddComponent<HatController>();
            // if (player.GetComponent<PlayerToolbox>() == null) player.gameObject.AddComponent<PlayerToolbox>();
        }
        public static Hook playerStartHook;
    }
    // class PlayerToolbox : MonoBehaviour
    // {
    //     private void Start()
    //     {
    //         m_attachedPlayer = base.GetComponent<PlayerController>();
    //         if (m_attachedPlayer)
    //         {
    //             m_attachedPlayer.OnAnyEnemyReceivedDamage += this.OnPlayerDamagedEnemy;
    //             m_attachedPlayer.PostProcessThrownGun += this.PostProcessThrownGun;
    //             m_attachedPlayer.PostProcessProjectile += this.PostProcessProjectile;
    //             m_attachedPlayer.PostProcessBeam += this.PostProcessBeam;
    //             m_attachedPlayer.OnHitByProjectile += this.OnHitByProjectile;
    //             m_attachedPlayer.healthHaver.OnDamaged += this.OnDamaged;
    //             m_attachedPlayer.healthHaver.ModifyDamage += this.ModifyDamage;
    //             m_attachedPlayer.OnEnteredCombat += this.EnteredCombat;
    //             m_attachedPlayer.OnRoomClearEvent += this.ClearedRoom;
    //             m_attachedPlayer.OnNewFloorLoaded += this.NewFloor;
    //             m_attachedPlayer.PostProcessThrownGun += this.PostProcessThrownGun;
    //             if (GameManager.Instance.SecondaryPlayer != null && GameManager.Instance.SecondaryPlayer == m_attachedPlayer) { isSecondaryPlayer = true; }
    //         }
    //         //Stats
    //         roomsSinceAllJamAmmoDrop = 0;

    //         DoubleDamageStatMod = new StatModifier();
    //         DoubleDamageStatMod.statToBoost = PlayerStats.StatType.Damage;
    //         DoubleDamageStatMod.amount = 2f;
    //         DoubleDamageStatMod.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;

    //         keepItCoolSpeedBuff = new StatModifier();
    //         keepItCoolSpeedBuff.statToBoost = PlayerStats.StatType.MovementSpeed;
    //         keepItCoolSpeedBuff.amount = 2.5f;
    //         keepItCoolSpeedBuff.modifyType = StatModifier.ModifyMethod.ADDITIVE;
    //     }
    //     private void NewFloor(PlayerController self)
    //     {
    //     }
    //     private void EnteredCombat()
    //     {
    //         if (m_attachedPlayer && m_attachedPlayer.CurrentRoom != null)
    //         {
    //             if (CurseManager.CurseIsActive("Curse of The Flames"))
    //             {
    //                 for (int i = 0; i < 5; i++)
    //                 {
    //                     IntVector2 pos = (IntVector2)m_attachedPlayer.CurrentRoom.GetRandomAvailableCell(null, CellTypes.FLOOR);
    //                     DeadlyDeadlyGoopManager goop = null;
    //                     if (GameManager.Instance.AnyPlayerHasActiveSynergy("The Last Crusade")) goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.PlayerFriendlyFireGoop);
    //                     else goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.EnemyFriendlyFireGoop);
    //                     goop.TimedAddGoopCircle(pos.ToVector2(), UnityEngine.Random.Range(2.5f, 4f), 0.75f, true);
    //                 }
    //             }
    //         }
    //     }
    //     private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
    //     {
    //         if ((m_attachedPlayer != null) && (m_attachedPlayer.CurrentGun != null) && (m_attachedPlayer.CurrentGun.CanActuallyBeDropped(m_attachedPlayer)))
    //         {
    //             if (CurseManager.CurseIsActive("Curse of Butterfingers"))
    //             {
    //                 if (GameManager.Instance.AnyPlayerHasActiveSynergy("The Last Crusade"))
    //                 {
    //                     StartCoroutine(GoodButterfingersEffect());
    //                 }
    //                 else StartCoroutine(ButterFingersGun());
    //             }
    //         }
    //     }
    //     private IEnumerator GoodButterfingersEffect()
    //     {
    //         yield return null;
    //         for (int i = 0; i < 6; i++)
    //         {
    //             GameObject ProjToSpawn = (PickupObjectDatabase.GetById(503) as Gun).DefaultModule.projectiles[0].gameObject;
    //             if (UnityEngine.Random.value <= 0.5f) ProjToSpawn = (PickupObjectDatabase.GetById(512) as Gun).DefaultModule.projectiles[0].gameObject;
    //             GameObject spawnedShot = ProjSpawnHelper.SpawnProjectileTowardsPoint(ProjToSpawn, m_attachedPlayer.CenterPosition, m_attachedPlayer.unadjustedAimPoint, 0, 10, m_attachedPlayer);
    //             Projectile component = spawnedShot.GetComponent<Projectile>();
    //             if (component != null)
    //             {
    //                 component.Owner = m_attachedPlayer;
    //                 component.Shooter = m_attachedPlayer.specRigidbody;
    //                 component.baseData.damage *= m_attachedPlayer.stats.GetStatValue(PlayerStats.StatType.Damage);
    //                 component.baseData.speed *= m_attachedPlayer.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
    //                 component.baseData.force *= m_attachedPlayer.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
    //                 component.baseData.range *= m_attachedPlayer.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
    //                 m_attachedPlayer.DoPostProcessProjectile(component);
    //             }
    //             yield return new WaitForSeconds(0.2f);
    //         }
    //         yield break;
    //     }
    //     private IEnumerator ButterFingersGun()
    //     {
    //         yield return null;
    //         if (m_attachedPlayer.CurrentGun != null)
    //         {
    //             Gun gunToSlip = m_attachedPlayer.CurrentGun;
    //             m_attachedPlayer.inventory.RemoveGunFromInventory(gunToSlip);
    //             gunToSlip.gameObject.AddComponent<ButterfingersedGun>();
    //             gunToSlip.ForceThrowGun();
    //             yield return new WaitForSeconds(0.1f);
    //             gunToSlip.ToggleRenderers(true);
    //             gunToSlip.RegisterMinimapIcon();
    //         }
    //         yield break;
    //     }
    //     private IEnumerator ButterfingersLateReTeleport(Projectile proj)
    //     {
    //         yield return null;
    //         bool hasTeleportedOnce = false;
    //         while (hasTeleportedOnce == false)
    //         {
    //             try
    //             {
    //                 proj.specRigidbody.Position = new Position(m_attachedPlayer.specRigidbody.UnitCenter);
    //                 //ETGModConsole.Log("LateTeleported gun");
    //                 hasTeleportedOnce = true;
    //             }
    //             catch (Exception e)
    //             {
    //                 ETGModConsole.Log(e.ToString());
    //             }
    //         }
    //         yield break;
    //     }
    //     private void PostProcessThrownGun(Projectile gun)
    //     {
    //         if (gun.GetComponentInChildren<ButterfingersedGun>() != null) StartCoroutine(ButterfingersLateReTeleport(gun));
    //         gun.OnHitEnemy += this.OnThrownGunHitEnemy;

    //     }
    //     private void ModifyDamage(HealthHaver player, HealthHaver.ModifyDamageEventArgs args)
    //     {
    //         //ETGModConsole.Log("OnDamaged ran");
    //         //ETGModConsole.Log("Initial Damage: "+args.InitialDamage);
    //         if (args.InitialDamage > 0 && m_attachedPlayer.characterIdentity == OMITBChars.Shade)
    //         {
    //             GameManager.Instance.StartCoroutine(PostDamageCheck(m_attachedPlayer));
    //         }
    //     }
    //     private void OnHitByProjectile(Projectile bullet, PlayerController self)
    //     {
    //         if (bullet && bullet.Owner && (bullet.Owner is AIActor) && (bullet.Owner as AIActor).EnemyGuid == "e5cffcfabfae489da61062ea20539887")
    //         {
    //             if (!SaveAPIManager.GetFlag(CustomDungeonFlags.HURT_BY_SHROOMER))
    //             {
    //                 SaveAPIManager.SetFlag(CustomDungeonFlags.HURT_BY_SHROOMER, true);
    //             }
    //         }
    //     }
    //     private void PostProcessProjectile(Projectile proj, float shit)
    //     {
    //         if (Challenges.CurrentChallenge == ChallengeType.INVISIBLEO) proj.sprite.renderer.enabled = false;
    //         else if (Challenges.CurrentChallenge == ChallengeType.KEEP_IT_COOL)
    //         {
    //             if (UnityEngine.Random.value <= 0.4f)
    //             {
    //                 proj.statusEffectsToApply.Add(StaticStatusEffects.frostBulletsEffect);
    //                 proj.AdjustPlayerProjectileTint(ExtendedColours.frostBulletsTint, 2);
    //             }
    //         }
    //     }
    //     private void PostProcessBeam(BeamController bem)
    //     {
    //         if (Challenges.CurrentChallenge == ChallengeType.INVISIBLEO) bem.sprite.renderer.enabled = false;
    //     }
    //     private void OnPlayerDamagedEnemy(float huh, bool fatal, HealthHaver enemy)
    //     {
    //     }
    //     private void OnThrownGunHitEnemy(Projectile gun, SpeculativeRigidbody enemy, bool fatal)
    //     {
    //         if (enemy && enemy.healthHaver && fatal)
    //         {
    //             if (!SaveAPIManager.GetFlag(CustomDungeonFlags.KILLEDENEMYWITHTHROWNGUN))
    //             {
    //                 SaveAPIManager.SetFlag(CustomDungeonFlags.KILLEDENEMYWITHTHROWNGUN, true);
    //             }
    //         }
    //     }
    //     private void ClearedRoom(PlayerController playa)
    //     {
    //         if (AllJammedState.AllJammedActive)
    //         {
    //             if (roomsSinceAllJamAmmoDrop < 6)
    //             {
    //                 roomsSinceAllJamAmmoDrop++;
    //             }
    //             else
    //             {
    //                 roomsSinceAllJamAmmoDrop = 0;
    //                 if (UnityEngine.Random.value <= 0.5f) LootEngine.SpawnItem(PickupObjectDatabase.GetById(78).gameObject, playa.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
    //                 else LootEngine.SpawnItem(PickupObjectDatabase.GetById(600).gameObject, playa.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
    //             }
    //         }
    //     }
    //     private void Update()
    //     {
    //         if (m_attachedPlayer != null && !Dungeon.IsGenerating)
    //         {
    //             if (m_attachedPlayer.healthHaver.Armor != armourLastChecked)
    //             {
    //                 if (!SaveAPIManager.GetFlag(CustomDungeonFlags.PLAYERHELDMORETHANFIVEARMOUR))
    //                 {
    //                     int threshHold = 5;
    //                     if (m_attachedPlayer.ForceZeroHealthState) threshHold = 11;
    //                     if (m_attachedPlayer.healthHaver.Armor >= threshHold) SaveAPIManager.SetFlag(CustomDungeonFlags.PLAYERHELDMORETHANFIVEARMOUR, true);
    //                 }
    //                 armourLastChecked = (int)m_attachedPlayer.healthHaver.Armor;
    //             }
    //             if (m_attachedPlayer.stats.GetStatValue(PlayerStats.StatType.Health) != hpStatLastChecked)
    //             {
    //                 SaveAPIManager.UpdateMaximum(CustomTrackedMaximums.MAX_HEART_CONTAINERS_EVER, m_attachedPlayer.stats.GetStatValue(PlayerStats.StatType.Health));
    //                 hpStatLastChecked = (int)m_attachedPlayer.stats.GetStatValue(PlayerStats.StatType.Health);
    //             }
    //             if (m_attachedPlayer.passiveItems.Count != itemCountLastChecked)
    //             {
    //                 OnInventoryItemsChanged();
    //                 itemCountLastChecked = m_attachedPlayer.passiveItems.Count;
    //             }
    //             if (m_attachedPlayer.CurrentGun)
    //             {
    //                 if (m_attachedPlayer.CurrentGun.PickupObjectId != gunIDLastChecked)
    //                 {
    //                     OnCurrentGunChanged();
    //                 }
    //             }

    //             #region InvisibleO
    //             if (Challenges.CurrentChallenge == ChallengeType.INVISIBLEO && m_attachedPlayer.IsVisible == true)
    //             {
    //                 playerIsInvisibleForChallenge = true;
    //                 m_attachedPlayer.DoDustUps = false;
    //                 m_attachedPlayer.IsVisible = false;
    //             }
    //             if (playerIsInvisibleForChallenge && Challenges.CurrentChallenge != ChallengeType.INVISIBLEO && m_attachedPlayer.IsVisible == false)
    //             {
    //                 playerIsInvisibleForChallenge = false;
    //                 m_attachedPlayer.DoDustUps = true;
    //                 m_attachedPlayer.IsVisible = true;
    //             }
    //             if (playerIsInvisibleForChallenge && m_attachedPlayer.gameActor.ShadowObject.GetComponent<Renderer>().enabled == true && !playerShadowInvisible)
    //             {
    //                 m_attachedPlayer.gameActor.ShadowObject.GetComponent<Renderer>().enabled = false;
    //                 playerShadowInvisible = true;
    //             }
    //             else if (!playerIsInvisibleForChallenge && m_attachedPlayer.gameActor.ShadowObject.GetComponent<Renderer>().enabled == false && playerShadowInvisible)
    //             {
    //                 m_attachedPlayer.gameActor.ShadowObject.GetComponent<Renderer>().enabled = true;
    //                 playerShadowInvisible = false;
    //             }
    //             if (Challenges.CurrentChallenge == ChallengeType.INVISIBLEO && m_attachedPlayer.CurrentGun && m_attachedPlayer.CurrentGun.GetComponent<InvisibleGun>() == null) m_attachedPlayer.CurrentGun.gameObject.AddComponent<InvisibleGun>();
    //             if (Challenges.CurrentChallenge == ChallengeType.INVISIBLEO && m_attachedPlayer.CurrentSecondaryGun && m_attachedPlayer.CurrentSecondaryGun.GetComponent<InvisibleGun>() == null) m_attachedPlayer.CurrentSecondaryGun.gameObject.AddComponent<InvisibleGun>();
    //             if (playerIsInvisibleForChallenge && m_attachedPlayer.primaryHand.ForceRenderersOff == false) m_attachedPlayer.primaryHand.ForceRenderersOff = true;
    //             if (playerIsInvisibleForChallenge && m_attachedPlayer.secondaryHand.ForceRenderersOff == false) m_attachedPlayer.secondaryHand.ForceRenderersOff = true;
    //             if (Challenges.CurrentChallenge == ChallengeType.INVISIBLEO && GameUIRoot.Instance.GetReloadBarForPlayer(m_attachedPlayer))
    //             {
    //                 int i = m_attachedPlayer.PlayerIDX;
    //                 GameUIRoot.Instance.ForceClearReload(i);
    //             }
    //             #endregion

    //             if (Challenges.CurrentChallenge == ChallengeType.KEEP_IT_COOL)
    //             {
    //                 DeadlyDeadlyGoopManager goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.WaterGoop);
    //                 goop.TimedAddGoopCircle(m_attachedPlayer.specRigidbody.UnitCenter, 2f, 0.01f, true);
    //                 DeadlyDeadlyGoopManager.FreezeGoopsCircle(m_attachedPlayer.specRigidbody.UnitBottomCenter, 2);
    //                 if (!m_attachedPlayer.ownerlessStatModifiers.Contains(keepItCoolSpeedBuff))
    //                 {
    //                     m_attachedPlayer.ownerlessStatModifiers.Add(keepItCoolSpeedBuff);
    //                     m_attachedPlayer.stats.RecalculateStats(m_attachedPlayer);
    //                 }
    //                 if (m_attachedPlayer.HasPickupID(256))
    //                 {
    //                     m_attachedPlayer.RemovePassiveItem(256);
    //                     IntVector2 bestRewardLocation2 = m_attachedPlayer.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
    //                     Chest red_Chest = GameManager.Instance.RewardManager.A_Chest;
    //                     red_Chest.IsLocked = false;
    //                     red_Chest.ChestType = (UnityEngine.Random.value <= 0.5f ? Chest.GeneralChestType.ITEM : Chest.GeneralChestType.WEAPON);
    //                     Chest spawnedRed = Chest.Spawn(red_Chest, bestRewardLocation2);
    //                     spawnedRed.lootTable.lootTable = (UnityEngine.Random.value <= 0.5f ? GameManager.Instance.RewardManager.GunsLootTable : GameManager.Instance.RewardManager.ItemsLootTable);
    //                     spawnedRed.RegisterChestOnMinimap(spawnedRed.GetAbsoluteParentRoom());
    //                     TextBubble.DoAmbientTalk(m_attachedPlayer.transform, new Vector3(1, 2, 0), "Nice Try", 4f);
    //                 }
    //             }
    //             else if (m_attachedPlayer.ownerlessStatModifiers.Contains(keepItCoolSpeedBuff))
    //             {
    //                 m_attachedPlayer.ownerlessStatModifiers.Remove(keepItCoolSpeedBuff);
    //                 m_attachedPlayer.stats.RecalculateStats(m_attachedPlayer);
    //             }
    //         }
    //     }

    //     private void OnCurrentGunChanged()
    //     {
    //         gunIDLastChecked = m_attachedPlayer.CurrentGun.PickupObjectId;
    //     }
    //     private void OnInventoryItemsChanged()
    //     {
    //         int amountOfJunk = 0;
    //         foreach (PassiveItem item in m_attachedPlayer.passiveItems)
    //         {
    //             if (item.PickupObjectId == 127) amountOfJunk++;
    //         }
    //         if (amountOfJunk >= 5 && m_attachedPlayer.characterIdentity == PlayableCharacters.Robot)
    //         {
    //             if (!SaveAPIManager.GetFlag(CustomDungeonFlags.ROBOT_HELD_FIVE_JUNK))
    //             {
    //                 SaveAPIManager.SetFlag(CustomDungeonFlags.ROBOT_HELD_FIVE_JUNK, true);
    //             }
    //         }
    //     }

    //     #region HandleShadeCheatedDeathUnlock
    //     private IEnumerator PostDamageCheck(PlayerController player)
    //     {
    //         //ETGModConsole.Log("Began post damage check");
    //         yield return new WaitForSeconds(5);
    //         if (player.healthHaver.IsAlive)
    //         {
    //             //ETGModConsole.Log("Player is alive!");
    //             if (!SaveAPIManager.GetFlag(CustomDungeonFlags.CHEATED_DEATH_SHADE))
    //             {
    //                 SaveAPIManager.SetFlag(CustomDungeonFlags.CHEATED_DEATH_SHADE, true);
    //             }
    //         }
    //         yield break;
    //     }
    //     #endregion

    //     #region RageCode
    //     public void Enrage(float dur)
    //     {
    //         if (remainingRageTime > 0) { remainingRageTime += dur; }
    //         else m_attachedPlayer.StartCoroutine(HandleRageDur(dur));
    //     }
    //     private IEnumerator HandleRageDur(float dur)
    //     {
    //         remainingRageTime = dur;
    //         m_attachedPlayer.stats.RecalculateStats(m_attachedPlayer, true, false);
    //         this.instanceVFX = m_attachedPlayer.PlayEffectOnActor(RageVFX, new Vector3(0f, 1.375f, 0f), true, true, false);
    //         ///ETGModConsole.Log("Applied VFX");
    //         m_attachedPlayer.ownerlessStatModifiers.Add(DoubleDamageStatMod);
    //         m_attachedPlayer.stats.RecalculateStats(m_attachedPlayer, true, false);
    //         // ETGModConsole.Log("Applied DMG");

    //         float elapsed = 0f;
    //         float particleCounter = 0f;
    //         while (elapsed < remainingRageTime)
    //         {
    //             elapsed += BraveTime.DeltaTime;
    //             m_attachedPlayer.baseFlatColorOverride = this.flatColorOverride.WithAlpha(Mathf.Lerp(this.flatColorOverride.a, 0f, Mathf.Clamp01(elapsed - (remainingRageTime - 1f))));
    //             if (GameManager.Options.ShaderQuality != GameOptions.GenericHighMedLowOption.LOW && GameManager.Options.ShaderQuality != GameOptions.GenericHighMedLowOption.VERY_LOW && m_attachedPlayer && m_attachedPlayer.IsVisible && !m_attachedPlayer.IsFalling)
    //             {
    //                 particleCounter += BraveTime.DeltaTime * 40f;
    //                 if (this.instanceVFX && elapsed > 1f)
    //                 {
    //                     this.instanceVFX.GetComponent<tk2dSpriteAnimator>().PlayAndDestroyObject("rage_face_vfx_out", null);
    //                     this.instanceVFX = null;
    //                 }
    //                 if (particleCounter > 1f)
    //                 {
    //                     int num = Mathf.FloorToInt(particleCounter);
    //                     particleCounter %= 1f;
    //                     GlobalSparksDoer.DoRandomParticleBurst(num, m_attachedPlayer.sprite.WorldBottomLeft.ToVector3ZisY(0f), m_attachedPlayer.sprite.WorldTopRight.ToVector3ZisY(0f), Vector3.up, 90f, 0.5f, null, null, null, GlobalSparksDoer.SparksType.BLACK_PHANTOM_SMOKE);
    //                 }
    //             }
    //             yield return null;
    //         }
    //         if (this.instanceVFX) this.instanceVFX.GetComponent<tk2dSpriteAnimator>().PlayAndDestroyObject("rage_face_vfx_out", null);
    //         m_attachedPlayer.ownerlessStatModifiers.Remove(DoubleDamageStatMod);
    //         m_attachedPlayer.stats.RecalculateStats(m_attachedPlayer, true, false);
    //         remainingRageTime = 0;
    //     }
    //     float remainingRageTime;
    //     public static GameObject RageVFX = PickupObjectDatabase.GetById(353).GetComponent<RagePassiveItem>().OverheadVFX.gameObject;
    //     private GameObject instanceVFX;
    //     public static StatModifier DoubleDamageStatMod;
    //     public Color flatColorOverride = new Color(0.5f, 0f, 0f, 0.75f);
    //     #endregion

    //     #region TimedStatModifier
    //     public void DoTimedStatModifier(PlayerStats.StatType statToBoost, float amount, float time, StatModifier.ModifyMethod modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE)
    //     {
    //         m_attachedPlayer.StartCoroutine(HandleTimedStatModifier(statToBoost, amount, time, modifyMethod));
    //     }
    //     private IEnumerator HandleTimedStatModifier(PlayerStats.StatType statToBoost, float amount, float dur, StatModifier.ModifyMethod method)
    //     {
    //         StatModifier timedMod = new StatModifier()
    //         {
    //             amount = amount,
    //             statToBoost = statToBoost,
    //             modifyType = method,
    //         };
    //         m_attachedPlayer.ownerlessStatModifiers.Add(timedMod);
    //         m_attachedPlayer.stats.RecalculateStats(m_attachedPlayer);
    //         yield return new WaitForSeconds(dur);
    //         m_attachedPlayer.ownerlessStatModifiers.Remove(timedMod);
    //         m_attachedPlayer.stats.RecalculateStats(m_attachedPlayer);
    //         yield break;
    //     }
    //     #endregion



    //     private bool playerIsInvisibleForChallenge;
    //     private bool playerShadowInvisible;

    //     private StatModifier keepItCoolSpeedBuff;
    //     private PlayerController m_attachedPlayer;
    //     private bool isSecondaryPlayer;
    //     private int armourLastChecked;
    //     private int hpStatLastChecked;
    //     private int itemCountLastChecked;
    //     private int gunIDLastChecked;
    //     private int roomsSinceAllJamAmmoDrop;
    // }
}
