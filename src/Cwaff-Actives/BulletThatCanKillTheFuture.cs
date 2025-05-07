namespace CwaffingTheGungy;

public class BulletThatCanKillTheFuture : CwaffActive
{
    public static string ItemName         = "Bullet That Can Kill the Future";
    public static string ShortDescription = "Seriously, Don't Miss";
    public static string LongDescription  = "Any enemy shot with Bullet That Can Kill the Future will not spawn for the rest of the run.";
    public static string Lore             = "Very little is known about this bullet, as few know it exists at all. It was originally given to Bello by a mysterious blue-clad skeleton, who claims to have found it behind the Hero Shrine in the Keep of the Lead Lord. It's almost as if it's calling out to be fired.";

    internal static string _BelloItemHint       = "A blue-clad skeleton stopped by earlier for some armor. I saw him walk behind the Hero Shrine and haven't seen him since.";
    internal static tk2dBaseSprite _Sprite      = null;
    internal static bool _BulletSpawnedThisRun  = false;
    internal static Texture2D _EeveeTexture     = null;

    private PlayerController _owner = null;

    public static void Init()
    {
        PlayerItem item   = Lazy.SetupActive<BulletThatCanKillTheFuture>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.SPECIAL;

        _Sprite = item.sprite;
        CwaffEvents.OnRunStartFromAnyFloor += ResetBTCKTF;
        CwaffEvents.OnNewFloorFullyLoaded += SpawnFutureBullet;

        ETGMod.Databases.Strings.Core.AddComplex("#SHOP_RUNBASEDMULTILINE_STOPPER", _BelloItemHint);

        _EeveeTexture = ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<Texture2D>("nebula_reducednoise");
    }

    private static void ResetBTCKTF(PlayerController arg1, PlayerController arg2, GameManager.GameMode arg3)
    {
        _BulletSpawnedThisRun = false;
    }

    private static void SpawnFutureBullet()
    {
        if (_BulletSpawnedThisRun)
            return;

        if (HeckedMode._HeckedModeStatus == HeckedMode.Hecked.Retrashed)
            return; // no cheating :)

        foreach (AdvancedShrineController a in StaticReferenceManager.AllAdvancedShrineControllers)
        {
            if (!a.IsLegendaryHeroShrine)
                continue;

            Vector3 pos = a.transform.position + (new Vector2(a.sprite.GetCurrentSpriteDef().position3.x/2, 6f)).ToVector3ZisY(0);
            GameObject futureBulletObject = LootEngine.SpawnItem(
              item: Lazy.Pickup<BulletThatCanKillTheFuture>().gameObject,
              spawnPosition: pos,
              spawnDirection: Vector2.zero,
              force: 0).gameObject;
            PickupObject futureBullet = futureBulletObject.GetComponent<PickupObject>();
                futureBullet.IgnoredByRat = true;
                futureBullet.ClearIgnoredByRatFlagOnPickup = false;
                futureBullet.StartCoroutine(DelayedRemoveBulletFromMinimap(futureBulletObject));
            _BulletSpawnedThisRun = true;
        }
    }

    private static IEnumerator DelayedRemoveBulletFromMinimap(GameObject futureBulletObject)
    {
        yield return null; // need to delay for a single frame for this to work properly
        futureBulletObject.GetComponent<PlayerItem>().GetRidOfMinimapIcon();
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        this._owner = player;
    }

    public override void OnPreDrop(PlayerController player)
    {
        this._owner = null;
        base.OnPreDrop(player);
    }

    public override bool CanBeUsed(PlayerController user)
    {
        return user.IsInCombat && user.CurrentRoom.IsSealed && !user.InBossRoom()
            && !user.CurrentRoom.NewWaveOfEnemiesIsSpawning() && base.CanBeUsed(user);
    }

    private static List<AIActor> CheckForValidEnemies(PlayerController player)
    {
        List<AIActor> candidates    = new List<AIActor>();
        foreach (AIActor otherEnemy in player.GetAbsoluteParentRoom().SafeGetEnemiesInRoom())
            if (otherEnemy.IsHostileAndNotABoss())
                candidates.Add(otherEnemy);
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
        interactor.SetInputOverride(ItemName);
        interactor.specRigidbody.CollideWithTileMap = false;
        interactor.specRigidbody.CollideWithOthers = false;
        PlayerController otherPlayer = GameManager.Instance.GetOtherPlayer(interactor);
        if (otherPlayer)
        {
            otherPlayer.SetInputOverride(ItemName);
            otherPlayer.specRigidbody.CollideWithTileMap = false;
            otherPlayer.specRigidbody.CollideWithOthers = false;
        }

        NoDamageBlankPatch.ForceNextBlankToDoNoDamage = true;
        interactor.ForceBlank(silent: true, breaksWalls: false, breaksObjects: false);

        // Figure out which enemies we should freeze in place
        Dictionary<AIActor,AIActor.ActorState> frozenEnemies = new Dictionary<AIActor,AIActor.ActorState>();
        List<AIActor> curEnemies = CheckForValidEnemies(interactor);
        foreach (AIActor a in curEnemies)
        {
            if (a.healthHaver.IsDead || !a.specRigidbody)
                continue;
            frozenEnemies[a] = a.State;
            a.State = AIActor.ActorState.Inactive;
            a.specRigidbody.OnPreMovement += FreezeInPlace;
            if (a.knockbackDoer)
                a.knockbackDoer.SetImmobile(true, ItemName);
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
            elapsed += Mathf.Max(0.05f,GameManager.INVARIANT_DELTA_TIME);
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            clockhairTargetPosition = GetTargetClockhairPosition(currentInput, clockhairTargetPosition);
            Vector3 currentPosition = Vector3.Slerp(clockhairStartPosition, clockhairTargetPosition, smoothT);
            clockhairTransform.position = currentPosition.WithZ(0f);
            if (t > 0.5f)
                clockhair.renderer.enabled = true;
            if (t > 0.75f)
            {
                clockhair.hourAnimator.GetComponent<Renderer>().enabled = true;
                clockhair.minuteAnimator.GetComponent<Renderer>().enabled = true;
                clockhair.secondAnimator.GetComponent<Renderer>().enabled = true;
                GameCursorController.CursorOverride.SetOverride(ItemName, true);
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
            if (currentInput.ActiveActions.UseItemAction.IsPressed || currentInput.ActiveActions.ShootAction.IsPressed)
            {
                if (!m_isPlayingChargeAudio)
                {
                    m_isPlayingChargeAudio = true;
                    interactor.gameObject.Play("Play_OBJ_pastkiller_charge_01");
                }
                shotTargetTime += BraveTime.DeltaTime;
            }
            else
            {
                shotTargetTime = Mathf.Max(0f, shotTargetTime - BraveTime.DeltaTime * 3f);
                if (m_isPlayingChargeAudio)
                {
                    m_isPlayingChargeAudio = false;
                    interactor.gameObject.Play("Stop_OBJ_pastkiller_charge_01");
                }
            }
            if ((currentInput.ActiveActions.UseItemAction.WasReleased || currentInput.ActiveActions.ShootAction.WasReleased)
                && shotTargetTime > holdDuration && !GameManager.Instance.IsPaused)
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
        float victimDistance = 8f;  //start with a maximum distance
        foreach (AIActor a in frozenEnemies.Keys)
        {
            if (!a || !a.healthHaver || a.healthHaver.IsDead || !a.specRigidbody)
                continue;
            a.State = frozenEnemies[a];
            a.specRigidbody.OnPreMovement -= FreezeInPlace;
            if (a.knockbackDoer)
                a.knockbackDoer.SetImmobile(false, ItemName);
            float dist = Vector2.Distance(clockhair.transform.PositionVector2(),a.transform.PositionVector2());
            if (dist < victimDistance)
            {
                victim = a;
                victimDistance = dist;
            }
        }

        // Check if player is reasonably close to the crosshair, and make magic happen if so
        float pdist = Vector2.Distance(clockhair.transform.PositionVector2(), interactor.CenterPosition);
        bool killedOwnFuture = (pdist < Mathf.Min(2f, victimDistance));
        if (!killedOwnFuture && victim != null)
        {
            CwaffRunData.Instance.btcktfEnemyGuid = victim.EnemyGuid;
            Lazy.CustomNotification("Future Erased", victim.GetActorName(), _Sprite);
            // ETGModConsole.Log("future erased for "+victim.EnemyGuid);
            VFXPool vfx = VFX.CreatePoolFromVFXGameObject(Items.MagicLamp.AsGun().DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
                vfx.SpawnAtPosition(
                    victim.CenterPosition.ToVector3ZisY(-1f), /* -1 = above player sprite */
                    0, null, null, null, -0.05f);

            foreach (AIActor a in StaticReferenceManager.AllEnemies)
            {
                if (a.EnemyGuid != victim.EnemyGuid)
                    continue;
                if (!a.IsHostileAndNotABoss())
                    continue;

                Memorialize(a);
                UnityEngine.Object.Destroy(a.gameObject);
            }

            victim.healthHaver.ApplyDamage(10000f,Vector2.zero,"Future Bullet",
                damageTypes: CoreDamageTypes.Void, damageCategory: DamageCategory.Unstoppable, ignoreDamageCaps: true);
        }
        else if (!killedOwnFuture)
            Lazy.CustomNotification("You Missed","Better Luck Next Time", _Sprite);

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
        interactor.ClearInputOverride(ItemName);
        interactor.specRigidbody.CollideWithTileMap = true;
        interactor.specRigidbody.CollideWithOthers = true;
        if (otherPlayer)
        {
            otherPlayer.ClearInputOverride(ItemName);
            otherPlayer.specRigidbody.CollideWithTileMap = true;
            otherPlayer.specRigidbody.CollideWithOthers = true;
        }
        GameCursorController.CursorOverride.RemoveOverride(ItemName);
        Pixelator.Instance.DoFinalNonFadedLayer = false;

        if (!killedOwnFuture)
        {
            yield return new WaitForSeconds(0.5f);
            UnityEngine.Object.Destroy(clockhair.gameObject);
        }
        else
        {
            UnityEngine.Object.Destroy(clockhair.gameObject);
            yield return new WaitForSeconds(0.25f);
            CwaffRunData.Instance.nameOfPreviousFloor = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
            GameManager.Instance.OnNewLevelFullyLoaded += ForceElevatorToReturnToPreviousFloor;
            GameManager.Instance.LoadCustomLevel(SansDungeon.INTERNAL_NAME);
        }
        yield break;
    }

    private static void ForceElevatorToReturnToPreviousFloor()
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= ForceElevatorToReturnToPreviousFloor;
        CwaffRunData.Instance.shouldReturnToPreviousFloor = true;
    }

    [HarmonyPatch(typeof(ElevatorDepartureController), nameof(ElevatorDepartureController.TransitionToDepart))]
    private class TransitionToDepartPatch
    {
        static void Prefix(ElevatorDepartureController __instance, tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip)
        {
            if (!CwaffRunData.Instance.shouldReturnToPreviousFloor)
                return;

            __instance.UsesOverrideTargetFloor = false;
            GameManager.Instance.InjectedLevelName = CwaffRunData.Instance.nameOfPreviousFloor;
            CwaffRunData.Instance.shouldReturnToPreviousFloor = false;
        }
    }

    [HarmonyPatch(typeof(AIActor), nameof(AIActor.Start))]
    private class MemorializeFuturelessEnemiesPatch
    {
        static bool Prefix(AIActor __instance)
        {
            string futureless = CwaffRunData.Instance.btcktfEnemyGuid;
            if (string.IsNullOrEmpty(futureless) || __instance.EnemyGuid != futureless)
                return true;

            Memorialize(__instance);
            UnityEngine.Object.Destroy(__instance.gameObject);
            return false; // skip original check
        }
    }

    public static void Memorialize(AIActor enemy)
    {
        tk2dSprite sprite = new GameObject().AddComponent<tk2dSprite>();
        sprite.SetSprite(enemy.sprite.collection, Lazy.GetIdForBestIdleAnimation(enemy));
        sprite.FlipX = enemy.sprite.FlipX;
        sprite.PlaceAtPositionByAnchor(enemy.sprite.transform.position, sprite.FlipX ? Anchor.LowerRight : Anchor.LowerLeft);
        sprite.StartCoroutine(Flicker(sprite));
    }

    public static IEnumerator Flicker(tk2dSprite gsprite)
    {
        gsprite.renderer.enabled = true;
        gsprite.OverrideMaterialMode = tk2dBaseSprite.SpriteMaterialOverrideMode.OVERRIDE_MATERIAL_COMPLEX;
        gsprite.usesOverrideMaterial = true;

        gsprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/GlitchEevee");
            gsprite.renderer.material.SetTexture("_EeveeTex", _EeveeTexture);
            gsprite.renderer.material.SetFloat("_WaveIntensity", 0.9f);
            gsprite.renderer.material.SetFloat("_ColorIntensity", 0.95f);
        gsprite.renderer.sharedMaterial.shader = ShaderCache.Acquire("Brave/Internal/GlitchEevee");
            gsprite.renderer.sharedMaterial.SetTexture("_EeveeTex", _EeveeTexture);
            gsprite.renderer.sharedMaterial.SetFloat("_WaveIntensity", 0.9f);
            gsprite.renderer.sharedMaterial.SetFloat("_ColorIntensity", 0.95f);

        gsprite.color = AfterImageHelpers.afterImageGray.WithAlpha(0.5f);
        gsprite.enabled = true;
        gsprite.UpdateZDepth();
        while (gsprite)
        {
            yield return new WaitForSeconds(0.05f);
            gsprite.renderer.enabled = true;
            yield return null;
            gsprite.renderer.enabled = false;
        }
    }
}

/// <summary>Prevent invisible blank triggered by BTCKTF from damaging enemies.</summary>
[HarmonyPatch]
static class NoDamageBlankPatch
{
    internal static bool ForceNextBlankToDoNoDamage = false;

    [HarmonyPatch(typeof(SilencerInstance), nameof(SilencerInstance.TriggerSilencer))]
    [HarmonyPrefix]
    static void MaybeForceNoDamageBlank(SilencerInstance __instance)
    {
        if (ForceNextBlankToDoNoDamage)
        {
            __instance.ForceNoDamage = true;
            ForceNextBlankToDoNoDamage = false;
        }
    }
}

