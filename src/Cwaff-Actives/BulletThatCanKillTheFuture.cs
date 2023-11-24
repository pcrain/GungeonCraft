namespace CwaffingTheGungy;

/* TODO:
    - make sure killed enemies serialize properly on game saves and reloads
*/

public class BulletThatCanKillTheFuture : PlayerItem
{
    public static string ItemName         = "Bullet That Can Kill the Future";
    public static string SpritePath       = "future_killing_bullet_icon";
    public static string ShortDescription = "Seriously, Don't Miss";
    public static string LongDescription  = "Any enemy shot with Bullet That Can Kill the Future will not spawn for the rest of the run.\n\nVery little is known about this bullet, as few know it exists at all. It was originally given to Bello by a mysterious blue-clad skeleton, who claims to have found it behind the Hero Shrine in the Keep of the Lead Lord. It's almost as if it's calling out to be fired.";

    internal static string _BelloItemHint = "A blue-clad skeleton stopped by earlier for some armor. I saw him walk behind the Hero Shrine and haven't seen him since.";
    internal static tk2dBaseSprite _Sprite = null;
    internal static bool _BulletSpawnedThisRun = false;

    private static Hook _RevertLevelHook = null;
    private static string NameOfPreviousFloor = ""; // used to set the floor elevator should take us to

    private PlayerController _owner = null;

    public static void Init()
    {
        PlayerItem item   = Lazy.SetupActive<BulletThatCanKillTheFuture>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality      = ItemQuality.SPECIAL;
        item.consumable   = true;
        item.CanBeDropped = true;

        _Sprite = item.sprite;
        CwaffEvents.OnRunStart += (_, _, _) => _BulletSpawnedThisRun = false;
        CwaffEvents.OnNewFloorFullyLoaded += SpawnFutureBullet;

        // ETGMod.Databases.Strings.Core.AddComplex("#SHOP_RUNBASEDMULTILINE_GENERIC", "more words");
        ETGMod.Databases.Strings.Core.AddComplex("#SHOP_RUNBASEDMULTILINE_STOPPER", _BelloItemHint);
    }

    private static void SpawnFutureBullet()
    {
        if (_BulletSpawnedThisRun)
            return;

        // ETGModConsole.Log($"FULLY LOADED {GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName}");
        foreach (AdvancedShrineController a in StaticReferenceManager.AllAdvancedShrineControllers)
        {
            if (!a.IsLegendaryHeroShrine)
                continue;

            Vector3 pos = a.transform.position + (new Vector2(a.sprite.GetCurrentSpriteDef().position3.x/2, 6f)).ToVector3ZisY(0);
            GameObject futureBulletObject = LootEngine.SpawnItem(
              item: PickupObjectDatabase.GetById(IDs.Pickups["bullet_that_can_kill_the_future"]).gameObject,
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
        List<AIActor> activeEnemies = player.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
        foreach (AIActor otherEnemy in activeEnemies)
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
            a.knockbackDoer?.SetImmobile(true, ItemName);
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
            a.knockbackDoer?.SetImmobile(false, ItemName);
            float dist = Vector2.Distance(clockhair.transform.PositionVector2(),a.transform.PositionVector2());
            if (dist < victimDistance)
            {
                victim = a;
                victimDistance = dist;
            }
        }

        // Check if player is reasonably close to the crosshair, and make magic happen if so
        float pdist = Vector2.Distance(clockhair.transform.PositionVector2(), interactor.sprite.WorldCenter);
        bool killedOwnFuture = (pdist < Mathf.Min(2f, victimDistance));
        if (!killedOwnFuture && victim != null)
        {
            CwaffToolbox.enemyWithoutAFuture = victim.EnemyGuid;
            Lazy.CustomNotification("Future Erased", victim.GetActorName(), _Sprite);
            // ETGModConsole.Log("future erased for "+victim.EnemyGuid);
            VFXPool vfx = VFX.CreatePoolFromVFXGameObject((ItemHelper.Get(Items.MagicLamp) as Gun
                ).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
                vfx.SpawnAtPosition(
                    victim.sprite.WorldCenter.ToVector3ZisY(-1f), /* -1 = above player sprite */
                    0, null, null, null, -0.05f);

            foreach (AIActor a in StaticReferenceManager.AllEnemies)
            {
                if (a.EnemyGuid != victim.EnemyGuid)
                    continue;
                if (!a.IsHostileAndNotABoss())
                    continue;

                CwaffToolbox.Memorialize(a);
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
            NameOfPreviousFloor = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
            GameManager.Instance.OnNewLevelFullyLoaded += ForceElevatorToReturnToPreviousFloor;
            GameManager.Instance.LoadCustomLevel("cg_sansfloor"); //TODO: rename later
        }
        yield break;
    }

    private static void ForceElevatorToReturnToPreviousFloor()
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= ForceElevatorToReturnToPreviousFloor;
        if (_RevertLevelHook != null)
            _RevertLevelHook.Dispose();
        _RevertLevelHook = new Hook(
            typeof(ElevatorDepartureController).GetMethod("TransitionToDepart", BindingFlags.Instance | BindingFlags.NonPublic),
            typeof(BulletThatCanKillTheFuture).GetMethod("TransitionToDepartHook", BindingFlags.Static | BindingFlags.NonPublic)
            );
    }

    private static void TransitionToDepartHook(Action<ElevatorDepartureController, tk2dSpriteAnimator, tk2dSpriteAnimationClip> orig, ElevatorDepartureController self, tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip)
    {
        self.UsesOverrideTargetFloor = false;
        if (GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName == "cg_sansfloor")
            GameManager.Instance.InjectedLevelName = NameOfPreviousFloor;

        _RevertLevelHook.Dispose();
        _RevertLevelHook = null;
        orig(self, animator, clip);
    }
}
