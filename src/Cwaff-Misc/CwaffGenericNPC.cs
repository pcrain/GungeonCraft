namespace CwaffingTheGungy;

public struct SimpleAnimationData
{
    public SimpleAnimationData(string name, int fps, List<string> paths)
    {
        this.animName  = name;
        this.animFPS   = fps;
        this.animPaths = paths;
    }
    public string animName { get; set; }
    public int animFPS { get; set; }
    public List<string> animPaths { get; set; }
}

public class FancyNPC : BraveBehaviour, IPlayerInteractable
{
    private static GameObject _FortunesFavorVFX = null;

    public Transform talkPoint;
    public Vector3 talkPointAdjustment;
    public bool autoFlipSprite = false;
    public bool noOutlines = false;
    public bool lockCamera = false;
    public string defaultAudioEvent = null;
    public List<string> defaultAudioEvents = new();
    public string audioTag = string.Empty;
    public string defaultTalkAnimation = null;
    public string defaultPauseAnimation = null;
    public float voiceRate = 0.25f;
    public bool alwaysReturnToIdle = true;
    public GameObject mapIcon = null;

    protected bool canInteract;
    protected bool m_canUse = true;
    protected PlayerController m_interactor;
    protected Vector3 talkPointOffset;

    private bool didSetup = false;
    private bool existingNpc = false;

    protected int PromptResult()
    {
        return LastResponse;
    }

    private int LastResponse { get; set; }

    // minimum amount of time to show textboxes during interactive dialogue
    protected const float MIN_TEXTBOX_TIME = 0.2f;
    // minimum amount of time to play talking animation assuming instant text is enabled
    protected const float MIN_ANIMATION_TIME = 0.5f;

    public static GameObject Setup<T>(string name, List<string> animNames, Vector3? talkPointAdjust = null)
        where T : FancyNPC
    {
        if (_FortunesFavorVFX == null)
        {
          AssetBundle shared_auto_001 = null;
          try
          {
            shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");
            _FortunesFavorVFX = shared_auto_001.LoadAsset<GameObject>("FortuneFavor_VFX_Spark");
            shared_auto_001 = null;
          }
          catch (Exception message)
          {
            ETGModConsole.Log(message.ToString());
            shared_auto_001 = null; //this fixes crashes apparently
          }
        }

        GameObject npcObj = SpriteBuilder.SpriteFromResource(ResMap.Get(animNames[0])[0], new GameObject(C.MOD_PREFIX + ":" + name));
            FakePrefab.MarkAsFakePrefab(npcObj);
            UnityEngine.Object.DontDestroyOnLoad(npcObj);
            npcObj.SetActive(false);
            npcObj.layer = 22;
            npcObj.name = C.MOD_PREFIX + ":" + name;

        tk2dSpriteAnimator spriteAnimator = npcObj.AddComponent<tk2dSpriteAnimator>();
        tk2dSpriteCollectionData collection = npcObj.GetComponent<tk2dSprite>().Collection;
        string animPrefix = name + "_";
        foreach (string an in animNames)
        {
            string animName = an.RemovePrefix(animPrefix);
            List<int> idList = AtlasHelper.AddSpritesToCollection(ResMap.Get(an), collection).AsRange();
            foreach (int fid in idList)
                collection.spriteDefinitions[fid].BetterConstructOffsetsFromAnchor(Anchor.LowerCenter);
            SpriteBuilder.AddAnimation(spriteAnimator, collection, idList, animName, tk2dSpriteAnimationClip.WrapMode.Loop, fps: 5);
        }

        AIAnimator aIAnimator = ShopAPI.GenerateBlankAIAnimator(npcObj);
            aIAnimator.spriteAnimator  = spriteAnimator;
            aIAnimator.OtherAnimations = Lazy.EasyNamedDirectionalAnimations(animNames.ToArray());

        npcObj.AutoRigidBody(clayer: CollisionLayer.EnemyCollider, height: 0.5f);

        FancyNPC npc = npcObj.AddComponent<T>() as FancyNPC;
            npc.talkPointAdjustment = talkPointAdjust.HasValue ? talkPointAdjust.Value : Vector3.zero;

        string npcIconName = ResMap.Get($"{name}_icon", quietFailure: true)?[0];
        if (npcIconName != null)
        {
          npc.mapIcon = new GameObject($"{name}_minimap_icon_sprite").RegisterPrefab(deactivate: false);
          npc.mapIcon.AddComponent<tk2dSprite>().SetSprite(collection, AtlasHelper.AddSpritesToCollection([npcIconName], collection).x);
        }

        UltraFortunesFavor dreamLuck = npcObj.AddComponent<UltraFortunesFavor>();
            dreamLuck.goopRadius          = 2;
            dreamLuck.beamRadius          = 2;
            dreamLuck.bulletRadius        = 2;
            dreamLuck.bulletSpeedModifier = 0.8f;
            dreamLuck.vfxOffset           = 0.625f;
            dreamLuck.sparkOctantVFX      = _FortunesFavorVFX;

        return npcObj;
    }

    public void SetAnimationFPS(string animationName, float fps)
    {
      base.spriteAnimator.GetClipByName(animationName).fps = fps;
    }

    protected virtual void Start()
    {
        Setup();
    }

    public void Setup()
    {
        if (this.didSetup)
            return;

        Vector3 pos = base.transform.position;
        this.didSetup = true;
        this.canInteract = true;
        this.m_canUse = true;
        SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
        RoomHandler room = pos.GetAbsoluteRoom();
        room.RegisterInteractable(this);
        if (mapIcon)
            Minimap.Instance.RegisterRoomIcon(room, mapIcon, false);
        if (base.gameObject.GetComponent<TalkDoerLite>() is TalkDoerLite talker)
        {
            this.existingNpc = true;
            this.talkPoint = talker.speakPoint;
            return;
        }
        this.talkPoint = new GameObject().transform;
        this.talkPoint.position = pos;
        Vector3 size = base.sprite.GetCurrentSpriteDef().position3;
        this.talkPointOffset = new Vector3(0, size.y, 0) + this.talkPointAdjustment;
        if (!this.noOutlines)
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
        base.aiAnimator.PlayUntilCancelled("idle");
        // base.aiAnimator.sprite.color = base.aiAnimator.sprite.color.WithAlpha(0f);
        // base.renderer.enabled = false;
    }

    protected bool CanBeginConversation()
    {
        // if (TextBoxManager.HasTextBox(this.talkPoint))
        //     return false; // NOTE: commented out to allow conversation with transient textboxes active
        if (this.m_interactor != null)
            return false;
        if (!this.canInteract)
            return false;
        return true;
    }

    protected void BeginConversation(PlayerController interactor)
    {
        this.m_interactor = interactor;
        if (this.noOutlines)
            SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite);
        else
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
        this.m_interactor.SetInputOverride("npcConversation");
        Pixelator.Instance.LerpToLetterbox(0.35f, 0.25f);
        Pixelator.Instance.DoFinalNonFadedLayer = true;
        GameUIRoot.Instance.ToggleLowerPanels(false, false, "conversation");
        GameUIRoot.Instance.HideCoreUI("conversation");
        Minimap.Instance.TemporarilyPreventMinimap = true;
        if (this.lockCamera)
          LockCamera();
    }

    protected void EndConversation()
    {
        // TextBoxManager.ClearTextBox(this.talkPoint);
        SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
        this.m_interactor.ClearInputOverride("npcConversation");
        // Pixelator.Instance.LerpToLetterbox(1, 0.25f);
        Pixelator.Instance.LerpToLetterbox(0.5f, 0.25f);
        Pixelator.Instance.DoFinalNonFadedLayer = false;
        GameUIRoot.Instance.ToggleLowerPanels(true, false, "conversation");
        GameUIRoot.Instance.ShowCoreUI("conversation");
        Minimap.Instance.TemporarilyPreventMinimap = false;
        this.m_interactor = null;  //if this method is overridden, needs to be set to null after conversation is done
        if (this.lockCamera)
          UnlockCamera();
    }

    private void LockCamera() // mostly stolen from basegame StartConversation FSM class
    {
        Vector2 talkPos = this.talkPoint.transform.position.XY();
        Vector2 screenBuffer = new Vector2(0.3f, 0.3f);
        CameraController mainCameraController = GameManager.Instance.MainCameraController;
        Vector2 minPos = CameraController.CameraToWorld(screenBuffer.x, screenBuffer.y);
        Vector2 maxPos = CameraController.CameraToWorld(1f - screenBuffer.x, 1f - screenBuffer.y);
        Vector2 deltaPos = maxPos - minPos;
        mainCameraController.SetManualControl(true);
        if (new Rect(minPos.x, minPos.y, deltaPos.x, deltaPos.y).Contains(talkPos))
        {
          mainCameraController.OverridePosition = mainCameraController.transform.position;
        }
        else
        {
          Vector2 nearestPos = BraveMathCollege.ClosestPointOnRectangle(talkPos, minPos, maxPos - minPos);
          mainCameraController.OverridePosition = mainCameraController.transform.position + (Vector3)(talkPos - nearestPos);
        }
    }

    private void UnlockCamera()
    {
        GameManager.Instance.MainCameraController.SetManualControl(false, true);
    }

    public void Interact(PlayerController interactor)
    {
        if (!(CanBeginConversation()))
            return;
        base.StartCoroutine(this.HandleConversation(interactor));
    }

    protected PlayerController Interactor()
    {
      return this.m_interactor;
    }

    protected void Reset()
    {
      SetAnimation("idle");
    }

    protected void ShowText(string convoLine, float autoContinueTimer = -1f)
    {
        if (TextBoxManager.HasTextBox(this.talkPoint))
            TextBoxManager.ClearTextBox(this.talkPoint);
        // if (this.m_interactor == null)
        // {
        //     ETGModConsole.Log("trying to talk with null interactor!");
        //     return;
        // }

        // Vector3 size = base.sprite.GetCurrentSpriteDef().position3;
        // this.talkPointOffset = new Vector3(base.sprite.FlipX ? -size.x/2 : size.x/2, size.y, 0) + this.talkPointAdjustment;
        if (!this.existingNpc)
            this.talkPoint.position = base.sprite.WorldTopCenter + this.talkPointAdjustment.XY();

        TextBoxManager.ShowTextBox(
            this.talkPoint.position,
            this.talkPoint,
            autoContinueTimer,
            convoLine,
            audioTag: this.audioTag,
            // this.m_interactor.characterAudioSpeechTag,
            instant: false,
            showContinueText: true
            );
    }

    private IEnumerator HandleConversation(PlayerController interactor)
    {
        // Verify we can actually interact with this interactible
        if (!this.m_canUse)
        {
            // base.aiAnimator.PlayForDuration("talker", 2f);
            if (this.m_interactor == null)
                this.ShowText("I have nothing to say right now.", 2f);
            // this.m_interactor = null;
            yield break;
        }

        // Set up input overrides and letterboxing
        BeginConversation(interactor);
        yield return null;

        // Run the actual script
        IEnumerator script = NPCTalkingScript();
        while(script.MoveNext())
            yield return script.Current;

        // Tear down input overrides and letterboxing
        if (!this.existingNpc && this.alwaysReturnToIdle)
            base.aiAnimator.PlayUntilCancelled("idler");
        EndConversation();
    }

    public void SetAnimation(string animation)
    {
        base.aiAnimator.PlayUntilCancelled(animation);
    }

    public Coroutine Converse(string dialogueLine, string talkAnimation = null, string pauseAnimation = null, string audioEvent =  null)
    {
        return StartCoroutine(Dialogue([dialogueLine], talkAnimation, pauseAnimation, audioEvent));
    }

    public IEnumerator Dialogue(List<string> dialogue, string talkAnimation = null, string pauseAnimation = null, string audioEvent =  null)
    {
        if (string.IsNullOrEmpty(audioEvent))
          audioEvent = defaultAudioEvent; // singular defaultAudioEvent has precedence over defaultAudioEvents list
        for (int ci = 0; ci < dialogue.Count; ++ci)
        {
            TextBoxManager.ClearTextBox(this.talkPoint);
            if (talkAnimation != null)
                base.aiAnimator.PlayUntilCancelled(talkAnimation);
            else if (defaultTalkAnimation != null)
                base.aiAnimator.PlayUntilCancelled(defaultTalkAnimation);
            this.ShowText(dialogue[ci]);
            float timer = 0;
            bool playingTalkingAnimation = true;
            string lastFrameAudioEvent = string.Empty;
            while (!BraveInput.GetInstanceForPlayer(this.m_interactor.PlayerIDX).ActiveActions.GetActionFromType(GungeonActions.GungeonActionType.Interact).WasPressed || timer < MIN_TEXTBOX_TIME)
            {
                timer += BraveTime.DeltaTime;
                bool npcIsTalking = TextBoxManager.TextBoxCanBeAdvanced(this.talkPoint);
                string frameAudioEvent = audioEvent;
                if (string.IsNullOrEmpty(frameAudioEvent) && defaultAudioEvents.Count > 0)
                  frameAudioEvent = defaultAudioEvents.ChooseRandom();
                if (npcIsTalking && !string.IsNullOrEmpty(frameAudioEvent))
                {
                  if (base.gameObject.PlayUnique(frameAudioEvent, soundRate: voiceRate)) // returns true only if it actually plays
                  {
                    if (!string.IsNullOrEmpty(lastFrameAudioEvent) && lastFrameAudioEvent != frameAudioEvent)
                     base.gameObject.Stop(lastFrameAudioEvent);
                    lastFrameAudioEvent = frameAudioEvent;
                  }
                }
                if (!npcIsTalking && !string.IsNullOrEmpty(lastFrameAudioEvent))
                  base.gameObject.Stop(lastFrameAudioEvent);
                if (playingTalkingAnimation && timer >= MIN_ANIMATION_TIME && !npcIsTalking)
                {
                    playingTalkingAnimation = false;
                    if (pauseAnimation != null)
                        base.aiAnimator.PlayUntilCancelled(pauseAnimation);
                    else if (defaultPauseAnimation != null)
                        base.aiAnimator.PlayUntilCancelled(defaultPauseAnimation);
                }
                yield return null;
            }
            if (pauseAnimation != null)
                base.aiAnimator.PlayUntilCancelled(pauseAnimation);
            else if (defaultPauseAnimation != null)
                base.aiAnimator.PlayUntilCancelled(defaultPauseAnimation);
        }
        TextBoxManager.ClearTextBox(this.talkPoint);
        yield break;
    }

    public Coroutine Prompt(string optionA, string optionB = null)
    {
        return StartCoroutine(Prompt_CR(optionA, optionB));

        IEnumerator Prompt_CR(string optionA, string optionB)
        {
            int selectedResponse = -1;
            GameUIRoot.Instance.DisplayPlayerConversationOptions(this.m_interactor, null, optionA, optionB);
            while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
                yield return null;
            LastResponse = selectedResponse;
            yield break;
        }
    }

    protected virtual void Update()
    {
        if (autoFlipSprite)
            base.transform.localScale = base.transform.localScale.WithX(
                (GameManager.Instance.PrimaryPlayer.CenterPosition.x < base.transform.position.x) ? -1 : 1);
            // base.sprite.FlipX = (GameManager.Instance.PrimaryPlayer.CenterPosition.x < base.transform.position.x);
    }

    protected virtual IEnumerator NPCTalkingScript()
    {
        //NOTE: this should rarely be called directly, should generally be called from the inherited child; use as reference only

        List<string> conversation = new List<string> {
            "Hey guys!",
            "Got custom NPCs working o:",
            "Neat huh?",
            };

        IEnumerator script = Dialogue(conversation,"talker","idler");
        while(script.MoveNext())
            yield return script.Current;

        // var acceptanceTextToUse = "i accept" + " (" + 5 + "[sprite \"ui_coin\"])";
        // var declineTextToUse = "i decline" + " (" + 5 + "[sprite \"hbux_text_icon\"])";
        var acceptanceTextToUse = "Very neat! :D";
        var declineTextToUse = "Not impressed. :/" + " (pay " + 99 + "[sprite \"hbux_text_icon\"] to disagree)";
        GameUIRoot.Instance.DisplayPlayerConversationOptions(this.m_interactor, null, acceptanceTextToUse, declineTextToUse);
        int selectedResponse = -1;
        while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
            yield return null;

        // IEnumerator prompt = Prompt("Very neat! :D","Not impressed. :/" + " (pay " + 99 + "[sprite \"hbux_text_icon\"] to disagree)");
        // while(prompt.MoveNext())
        //     yield return prompt.Current;
        yield return Prompt("Very neat! :D","Not impressed. :/" + " (pay " + 99 + "[sprite \"hbux_text_icon\"] to disagree)");

        this.ShowText((selectedResponse == 0) ? "Yay!" : "Aw ):",2f);
    }

    public void OnEnteredRange(PlayerController interactor)
    {
        SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
        base.sprite.UpdateZDepth();
    }

    public void OnExitRange(PlayerController interactor)
    {
        if (this.noOutlines)
            SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite);
        else
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
    }

    public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
    {
        shouldBeFlipped = false;
        return string.Empty;
    }

    public float GetDistanceToPoint(Vector2 point)
    {
        if (base.sprite == null)
            return 100f;
        Vector3 v = BraveMathCollege.ClosestPointOnRectangle(point, base.specRigidbody.UnitBottomLeft, base.specRigidbody.UnitDimensions);
        return Vector2.Distance(point, v) / 1.5f;
    }

    public virtual float GetOverrideMaxDistance()
    {
        return -1f;
    }
}

public class FlipsToFacePlayer : MonoBehaviour
{
  private AIAnimator _animator;
  private Transform _speechPoint;
  private float _flipOffset;
  private float _centerX;
  private float _baseX;
  private Vector3 _baseSpeechPos;
  private bool _cachedFlipped;

  private void Start()
  {
    this._animator      = base.GetComponent<AIAnimator>();
    this._flipOffset    = this._animator.sprite.GetUntrimmedBounds().size.x /** 0.5f*/;
    this._centerX       = this._animator.sprite.WorldBottomCenter.x;
    this._baseX         = this._animator.sprite.transform.localPosition.x;

    this._speechPoint   = base.transform.Find("SpeechPoint");
    this._baseSpeechPos = this._speechPoint.position;

    this._cachedFlipped = false;
  }

  private void Update()
  {
    // this._animator.sprite.FlipX = GameManager.Instance.BestActivePlayer.CenterPosition.x < this._animator.transform.position.x;
    // this._animator.sprite.transform.localScale = this._animator.sprite.transform.localScale.WithX(
    //   (GameManager.Instance.BestActivePlayer.CenterPosition.x < this._animator.transform.position.x) ? -1f : 1f);
    FlipSpriteIfNecessary();
  }

  private void FlipSpriteIfNecessary()
  {
    this._animator.sprite.FlipX = GameManager.Instance.BestActivePlayer.SpriteBottomCenter.x < this._centerX;
    if (this._animator.sprite.FlipX == this._cachedFlipped)
      return;

    this._cachedFlipped = this._animator.sprite.FlipX;
    base.transform.localPosition = base.transform.localPosition.WithX(
      this._baseX + (this._cachedFlipped ? _flipOffset : 0f));
    this._speechPoint.position = this._baseSpeechPos;
  }

  // private void FlipSpriteIfNecessaryClose()
  // {
  //   this._animator.sprite.FlipX = GameManager.Instance.BestActivePlayer.SpriteBottomCenter.x < this._centerX;
  //   if (this._animator.sprite.FlipX == this._cachedFlipped)
  //     return;

  //   this._cachedFlipped = this._animator.sprite.FlipX;
  //   this._animator.sprite.transform.localPosition = this._animator.sprite.transform.localPosition.WithX(
  //     this._baseX + (this._cachedFlipped ? _flipOffset : 0f));
  // }
}
