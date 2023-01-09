using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace CwaffingTheGungy
{
    class BulletThatCanKillTheFuture : PlayerItem
    {
        public static string activeName       = "Bullet That Can Kill the Future";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/future_killing_bullet_icon";
        public static string shortDescription = "Seriously, Don't Miss";
        public static string longDescription  = "(enemy will not spawn for the rest of the run)";

        private PlayerController m_owner = null;
        private RoomHandler lastCheckedRoom = null;
        private bool inBossRoom = false;
        private bool isUsable = false;
        private List<AIActor> validEnemies = new List<AIActor>{};

        public static void Init()
        {
            PlayerItem item = Lazy.SetupActive<BulletThatCanKillTheFuture>(activeName, spritePath, shortDescription, longDescription, "cg");
            item.quality    = PickupObject.ItemQuality.C;

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.PerRoom, 1);
            item.consumable   = true;
            item.quality      = ItemQuality.S;
            item.CanBeDropped = true;
        }

        public override void Pickup(PlayerController player)
        {
            this.m_owner = player;
            base.Pickup(player);
        }

        public override void OnPreDrop(PlayerController player)
        {
            this.m_owner = null;
            base.OnPreDrop(player);
        }

        public override void Update()
        {
            if (this.m_owner)
            {
                if (this.m_owner.CurrentRoom != lastCheckedRoom)
                {
                    this.lastCheckedRoom = this.m_owner.CurrentRoom;
                    this.inBossRoom = CheckIfBossIsPresent();
                }
                this.isUsable = !(this.m_owner.InExitCell || this.inBossRoom);
                if (this.isUsable)
                {
                    this.validEnemies = CheckForValidEnemies(this.m_owner);
                    this.isUsable = this.validEnemies.Count > 0;
                }
            }
            base.Update();
        }

        public override bool CanBeUsed(PlayerController user)
        {
            return this.isUsable && base.CanBeUsed(user);
        }

        private bool CheckIfBossIsPresent()
        {
            if (lastCheckedRoom == null)
                return false;
            List<AIActor> activeEnemies =
                this.m_owner.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                if (activeEnemies[i].healthHaver?.IsBoss ?? false)
                    return true;
            }
            return false;
        }

        private static List<AIActor> CheckForValidEnemies(PlayerController player)
        {
            List<AIActor> candidates = new List<AIActor>();
            List<AIActor> activeEnemies = player.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null || activeEnemies.Count == 0)
                return candidates;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                AIActor otherEnemy = activeEnemies[i];
                if (!(otherEnemy && otherEnemy.specRigidbody && otherEnemy.healthHaver) || otherEnemy.IsGone || otherEnemy.healthHaver.IsBoss)
                    continue;
                candidates.Add(otherEnemy);
            }
            return candidates;
        }

        public override void DoEffect(PlayerController user)
        {
            base.DoEffect(user);
            user.StartCoroutine(Futureless(user));
        }

        private static Vector2 GetTargetClockhairPosition(BraveInput input, Vector2 currentClockhairPosition)
        {
            Vector2 rhs2 = Vector2.Max(rhs: (!input.IsKeyboardAndMouse()) ? (currentClockhairPosition + input.ActiveActions.Aim.Vector * 10f * BraveTime.DeltaTime) : (GameManager.Instance.MainCameraController.Camera.ScreenToWorldPoint(Input.mousePosition).XY() + new Vector2(0.375f, -0.25f)), lhs: GameManager.Instance.MainCameraController.MinVisiblePoint);
            return Vector2.Min(GameManager.Instance.MainCameraController.MaxVisiblePoint, rhs2);
        }

        private static void PointGunAtClockhair(PlayerController interactor, Transform clockhairTransform)
        {
            Vector2 centerPosition = interactor.CenterPosition;
            Vector2 vector = clockhairTransform.position.XY() - centerPosition;
            float value = BraveMathCollege.Atan2Degrees(vector);
            value = value.Quantize(3f);
            interactor.GunPivot.rotation = Quaternion.Euler(0f, 0f, value);
            interactor.ForceIdleFacePoint(vector, false);
        }


        public static void FreezeInPlace(SpeculativeRigidbody myRigidbody)
        {
            myRigidbody.Velocity = Vector2.zero;
        }

        // Stolen from HandleClockhair() in ArkController.cs
        private static IEnumerator Futureless(PlayerController interactor)
        {
            // Extra stuff to make this work properly outside the intended cutscene
            Pixelator.Instance.DoFinalNonFadedLayer = true;
            Pixelator.Instance.DoRenderGBuffer = true;
            interactor.SetInputOverride("future");
            interactor.specRigidbody.CollideWithTileMap = false;
            interactor.specRigidbody.CollideWithOthers = false;

            // Figure out which enemies we should freeze in place
            Dictionary<AIActor,AIActor.ActorState> frozenEnemies = new Dictionary<AIActor,AIActor.ActorState>();
            List<AIActor> curEnemies = CheckForValidEnemies(interactor);
            foreach (AIActor a in curEnemies)
            {
                if (a.healthHaver.IsDead)
                    continue;
                frozenEnemies[a] = a.State;
                a.State = AIActor.ActorState.Inactive;
                a.specRigidbody.OnPreMovement = (Action<SpeculativeRigidbody>)Delegate.Combine(a.specRigidbody.OnPreMovement, new Action<SpeculativeRigidbody>(FreezeInPlace));
                if ((bool)a.knockbackDoer)
                    a.knockbackDoer.SetImmobile(true, "future");
            }

            // Do normal crosshair logic minus some unneeded steps
            Transform clockhairTransform = ((GameObject)UnityEngine.Object.Instantiate(BraveResources.Load("Clockhair"))).transform;
            ClockhairController clockhair = clockhairTransform.GetComponent<ClockhairController>();
            float elapsed = 0f;
            float duration = clockhair.ClockhairInDuration;
            Vector3 clockhairTargetPosition = interactor.CenterPosition;
            Vector3 clockhairStartPosition = interactor.CenterPosition;
            clockhair.renderer.enabled = true;
            clockhair.spriteAnimator.alwaysUpdateOffscreen = true;
            clockhair.spriteAnimator.Play("clockhair_intro");
            clockhair.hourAnimator.Play("hour_hand_intro");
            clockhair.minuteAnimator.Play("minute_hand_intro");
            clockhair.secondAnimator.Play("second_hand_intro");
            BraveInput currentInput = BraveInput.GetInstanceForPlayer(interactor.PlayerIDX);

            while (elapsed < duration)
            {
                // UpdateCameraPositionDuringClockhair(interactor.CenterPosition);
                if (GameManager.INVARIANT_DELTA_TIME == 0f)
                {
                    elapsed += 0.05f;
                }
                elapsed += GameManager.INVARIANT_DELTA_TIME;
                float t = elapsed / duration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                clockhairTargetPosition = GetTargetClockhairPosition(currentInput, clockhairTargetPosition);
                Vector3 currentPosition = Vector3.Slerp(clockhairStartPosition, clockhairTargetPosition, smoothT);
                clockhairTransform.position = currentPosition.WithZ(0f);
                if (t > 0.5f)
                {
                    clockhair.renderer.enabled = true;
                }
                if (t > 0.75f)
                {
                    clockhair.hourAnimator.GetComponent<Renderer>().enabled = true;
                    clockhair.minuteAnimator.GetComponent<Renderer>().enabled = true;
                    clockhair.secondAnimator.GetComponent<Renderer>().enabled = true;
                    GameCursorController.CursorOverride.SetOverride("future", true);
                }
                clockhair.sprite.UpdateZDepth();
                PointGunAtClockhair(interactor, clockhairTransform);
                yield return null;
            }
            clockhair.SetMotionType(1f);
            float shotTargetTime = 0f;
            float holdDuration = 4f;
            Vector3 lastJitterAmount = Vector3.zero;
            bool m_isPlayingChargeAudio = false;
            while (true)
            {
                // UpdateCameraPositionDuringClockhair(interactor.CenterPosition);
                clockhair.transform.position = clockhair.transform.position - lastJitterAmount;
                clockhair.transform.position = GetTargetClockhairPosition(currentInput, clockhair.transform.position.XY());
                clockhair.sprite.UpdateZDepth();
                clockhair.SetMotionType(-10f);
                if (currentInput.ActiveActions.UseItemAction.IsPressed)
                {
                    if (!m_isPlayingChargeAudio)
                    {
                        m_isPlayingChargeAudio = true;
                        AkSoundEngine.PostEvent("Play_OBJ_pastkiller_charge_01", interactor.gameObject);
                    }
                    shotTargetTime += BraveTime.DeltaTime;
                }
                else
                {
                    shotTargetTime = Mathf.Max(0f, shotTargetTime - BraveTime.DeltaTime * 3f);
                    if (m_isPlayingChargeAudio)
                    {
                        m_isPlayingChargeAudio = false;
                        AkSoundEngine.PostEvent("Stop_OBJ_pastkiller_charge_01", interactor.gameObject);
                    }
                }
                if (currentInput.ActiveActions.UseItemAction.WasReleased && shotTargetTime > holdDuration && !GameManager.Instance.IsPaused)
                {
                    break;
                }
                if (shotTargetTime > 0f)
                {
                    float distortionPower = Mathf.Lerp(0f, 0.35f, shotTargetTime / holdDuration);
                    float distortRadius = 0.5f;
                    float edgeRadius = Mathf.Lerp(4f, 7f, shotTargetTime / holdDuration);
                    clockhair.UpdateDistortion(distortionPower, distortRadius, edgeRadius);
                    float desatRadiusUV = Mathf.Lerp(2f, 0.25f, shotTargetTime / holdDuration);
                    clockhair.UpdateDesat(true, desatRadiusUV);
                    shotTargetTime = Mathf.Min(holdDuration + 0.25f, shotTargetTime + BraveTime.DeltaTime);
                    float num = Mathf.Lerp(0f, 0.5f, (shotTargetTime - 1f) / (holdDuration - 1f));
                    Vector3 vector = (UnityEngine.Random.insideUnitCircle * num).ToVector3ZUp();
                    BraveInput.DoSustainedScreenShakeVibration(shotTargetTime / holdDuration * 0.8f);
                    clockhair.transform.position = clockhair.transform.position + vector;
                    lastJitterAmount = vector;
                    clockhair.SetMotionType(Mathf.Lerp(-10f, -2400f, shotTargetTime / holdDuration));
                }
                else
                {
                    lastJitterAmount = Vector3.zero;
                    clockhair.UpdateDistortion(0f, 0f, 0f);
                    clockhair.UpdateDesat(false, 0f);
                    shotTargetTime = 0f;
                    BraveInput.DoSustainedScreenShakeVibration(0f);
                }
                PointGunAtClockhair(interactor, clockhairTransform);
                yield return null;
            }

            // Figure out closest enemy to the crosshair and wipe them off the face of the earth
            AIActor victim = null;
            float victimDistance = 3f;  //start with a maximum distance
            foreach (AIActor a in frozenEnemies.Keys)
            {
                a.State = frozenEnemies[a];
                a.specRigidbody.OnPreMovement = (Action<SpeculativeRigidbody>)Delegate.Remove(a.specRigidbody.OnPreMovement, new Action<SpeculativeRigidbody>(FreezeInPlace));
                if ((bool)a.knockbackDoer)
                    a.knockbackDoer.SetImmobile(false, "future");
                float dist = Vector2.Distance(clockhair.transform.PositionVector2(),a.transform.PositionVector2());
                ETGModConsole.Log("found distance of "+dist);
                if (dist < victimDistance)
                {
                    victim = a;
                    victimDistance = dist;
                }
            }
            if (victim != null)
            {
                CwaffToolbox.enemyWithoutAFuture = victim.EnemyGuid;
                Lazy.CustomNotification("Future Erased",victim.GetActorName());
                ETGModConsole.Log("future erased for "+victim.EnemyGuid);
                VFXPool vfx = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(0) as Gun
                    ).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
                    vfx.SpawnAtPosition(
                        victim.sprite.WorldCenter.ToVector3ZisY(-1f), /* -1 = above player sprite */
                        0, null, null, null, -0.05f);
                victim.EraseFromExistence(true);
            }
            else
            {
                // TODO: make fun of the player for wasting the bullet
                // Lazy.CustomNotification("You Missed","");
            }

            // finish up the base original script
            BraveInput.DoSustainedScreenShakeVibration(0f);
            BraveInput.DoVibrationForAllPlayers(Vibration.Time.Normal, Vibration.Strength.Hard);
            clockhair.StartCoroutine(clockhair.WipeoutDistortionAndFade(0.5f));
            clockhair.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
            Pixelator.Instance.FadeToColor(1f, Color.white, true, 0.2f);
            Pixelator.Instance.DoRenderGBuffer = false;
            clockhair.spriteAnimator.Play("clockhair_fire");
            clockhair.hourAnimator.GetComponent<Renderer>().enabled = false;
            clockhair.minuteAnimator.GetComponent<Renderer>().enabled = false;
            clockhair.secondAnimator.GetComponent<Renderer>().enabled = false;

            yield return null;

            // clean up our mess
            interactor.ClearInputOverride("future");
            interactor.specRigidbody.CollideWithTileMap = true;
            interactor.specRigidbody.CollideWithOthers = true;
            GameCursorController.CursorOverride.RemoveOverride("future");
            Pixelator.Instance.DoFinalNonFadedLayer = false;
            yield return new WaitForSeconds(0.5f);

            UnityEngine.Object.Destroy(clockhair.gameObject);
            yield break;
        }
    }
}
