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
    public Transform talkPoint;
    public Vector3 talkPointAdjustment;
    public bool autoFlipSprite = true;

    protected bool canInteract;
    protected bool m_canUse = true;
    protected PlayerController m_interactor;
    protected Vector3 talkPointOffset;
    protected GameObject talkPointObject;

    protected int PromptResult()
    {
        return LastResponse;
    }

    private int LastResponse { get; set; }

    // minimum amount of time to show textboxes during interactive dialogue
    protected const float MIN_TEXTBOX_TIME = 0.2f;
    // minimum amount of time to play talking animation assuming instant text is enabled
    protected const float MIN_ANIMATION_TIME = 1.0f;

    public static GameObject Setup<T>(string name, string prefix, List<SimpleAnimationData> animationData, Vector3? talkPointAdjust = null)
        where T : FancyNPC
    {
        AssetBundle shared_auto_001 = null;
        try
        {
            shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");

            GameObject npcObj = SpriteBuilder.SpriteFromResource(animationData[0].animPaths[0], new GameObject(prefix + ":" + name));
                FakePrefab.MarkAsFakePrefab(npcObj);
                UnityEngine.Object.DontDestroyOnLoad(npcObj);
                npcObj.SetActive(false);
                npcObj.layer = 22;
                npcObj.name = prefix + ":" + name;

            tk2dSpriteAnimator spriteAnimator = npcObj.AddComponent<tk2dSpriteAnimator>();
            tk2dSpriteCollectionData collection = npcObj.GetComponent<tk2dSprite>().Collection;
                List<string> animNames = new List<string>();
                foreach (SimpleAnimationData ad in animationData)
                {
                    List<int> idList = AtlasHelper.AddSpritesToCollection(ad.animPaths, collection).AsRange();
                    foreach (int fid in idList)
                        collection.spriteDefinitions[fid].BetterConstructOffsetsFromAnchor(Anchor.LowerCenter);
                    SpriteBuilder.AddAnimation(spriteAnimator, collection, idList, ad.animName, tk2dSpriteAnimationClip.WrapMode.Loop, ad.animFPS);
                    animNames.Add(ad.animName);
                }

            AIAnimator aIAnimator = ShopAPI.GenerateBlankAIAnimator(npcObj);
                aIAnimator.spriteAnimator  = spriteAnimator;
                aIAnimator.OtherAnimations = Lazy.EasyNamedDirectionalAnimations(animNames.ToArray());

            SpeculativeRigidbody rigidbody = ShopAPI.GenerateOrAddToRigidBody(npcObj, CollisionLayer.BulletBlocker, PixelCollider.PixelColliderGeneration.Manual, true, true, true, false, false, false, false, true, new IntVector2(20, 18), new IntVector2(5, 0));

            FancyNPC ci = npcObj.AddComponent<T>() as FancyNPC;
                ci.talkPointAdjustment = talkPointAdjust.HasValue ? talkPointAdjust.Value : Vector3.zero;

            UltraFortunesFavor dreamLuck = npcObj.AddComponent<UltraFortunesFavor>();
                dreamLuck.goopRadius = 2;
                dreamLuck.beamRadius = 2;
                dreamLuck.bulletRadius = 2;
                dreamLuck.bulletSpeedModifier = 0.8f;
                dreamLuck.vfxOffset = 0.625f;
                dreamLuck.sparkOctantVFX = shared_auto_001.LoadAsset<GameObject>("FortuneFavor_VFX_Spark");

            shared_auto_001 = null; //this fixes crashes apparently
            return npcObj;
        }
        catch (Exception message)
        {
            ETGModConsole.Log(message.ToString());
            shared_auto_001 = null; //this fixes crashes apparently
            return null;
        }
    }

    protected virtual void Start()
    {
        this.canInteract = true;
        this.m_canUse = true;
        this.talkPointObject = new GameObject();
        this.talkPointObject.transform.position = base.transform.position;
        this.talkPoint = this.talkPointObject.transform;
        // base.sprite.SetSprite(base.sprite.GetSpriteIdByName("talk"));
        Vector3 size = base.sprite.GetCurrentSpriteDef().position3;
        // base.sprite.SetSprite(base.sprite.GetSpriteIdByName("idle"));
        // this.talkPointOffset = new Vector3(size.x / 2, size.y, 0) + this.talkPointAdjustment;
        this.talkPointOffset = new Vector3(0, size.y, 0) + this.talkPointAdjustment;
        // SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
        base.aiAnimator.PlayUntilCancelled("idler");
        // base.aiAnimator.sprite.color = base.aiAnimator.sprite.color.WithAlpha(0f);
        // base.renderer.enabled = false;
    }

    protected bool CanBeginConversation()
    {
        if (TextBoxManager.HasTextBox(this.talkPoint))
            return false;
        if (this.m_interactor != null)
            return false;
        if (!this.canInteract)
            return false;
        return true;
    }

    protected void BeginConversation(PlayerController interactor)
    {
        this.m_interactor = interactor;
        SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
        this.m_interactor.SetInputOverride("npcConversation");
        Pixelator.Instance.LerpToLetterbox(0.35f, 0.25f);
    }

    protected void EndConversation()
    {
        // TextBoxManager.ClearTextBox(this.talkPoint);
        SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
        this.m_interactor.ClearInputOverride("npcConversation");
        Pixelator.Instance.LerpToLetterbox(1, 0.25f);
        this.m_interactor = null;  //if this method is overridden, needs to be set to null after conversation is done
        GameManager.Instance.MainCameraController.SetManualControl(false, true);
    }

    public void AppearInAPuffOfSmoke()
    {
      GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
      tk2dBaseSprite sprite = gameObject2.GetComponent<tk2dBaseSprite>();
      sprite.PlaceAtPositionByAnchor(base.sprite.WorldCenter.ToVector3ZUp(0f), Anchor.MiddleCenter);
      sprite.transform.position = sprite.transform.position.Quantize(0.0625f);
      sprite.HeightOffGround = 5f;
      sprite.UpdateZDepth();
    }

    protected void VanishInAPuffOfSmoke()
    {
      GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
      tk2dBaseSprite sprite = gameObject2.GetComponent<tk2dBaseSprite>();
      sprite.PlaceAtPositionByAnchor(base.sprite.WorldCenter.ToVector3ZUp(0f), Anchor.MiddleCenter);
      sprite.transform.position = sprite.transform.position.Quantize(0.0625f);
      sprite.HeightOffGround = 5f;
      sprite.UpdateZDepth();
      this.transform.position.GetAbsoluteRoom().DeregisterInteractable(this);
      UnityEngine.Object.Destroy(base.gameObject);
    }

    public void Interact(PlayerController interactor)
    {
        if (!(CanBeginConversation()))
            return;
        base.StartCoroutine(this.HandleConversation(interactor));
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
        this.talkPointObject.transform.position = base.sprite.WorldTopCenter;

        TextBoxManager.ShowTextBox(
            this.talkPoint.position,
            this.talkPoint,
            autoContinueTimer,
            convoLine,
            audioTag: "",
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
        base.aiAnimator.PlayUntilCancelled("idler");
        EndConversation();
    }

    public void SetAnimation(string animation)
    {
        base.aiAnimator.PlayUntilCancelled(animation);
    }

    public Coroutine Converse(string dialogueLine, string talkAnimation = null, string pauseAnimation = null, string audioEvent =  null)
    {
        return StartCoroutine(Dialogue(new(){dialogueLine}, talkAnimation, pauseAnimation, audioEvent));
    }

    public IEnumerator Dialogue(List<string> dialogue, string talkAnimation = null, string pauseAnimation = null, string audioEvent =  null)
    {
        for (int ci = 0; ci < dialogue.Count; ++ci)
        {
            TextBoxManager.ClearTextBox(this.talkPoint);
            if (talkAnimation != null)
                base.aiAnimator.PlayUntilCancelled(talkAnimation);
            this.ShowText(dialogue[ci]);
            float timer = 0;
            bool playingTalkingAnimation = true;
            if (!string.IsNullOrEmpty(audioEvent))
                base.gameObject.PlayUnique(audioEvent);
            while (!BraveInput.GetInstanceForPlayer(this.m_interactor.PlayerIDX).ActiveActions.GetActionFromType(GungeonActions.GungeonActionType.Interact).WasPressed || timer < MIN_TEXTBOX_TIME)
            {
                timer += BraveTime.DeltaTime;
                bool npcIsTalking = TextBoxManager.TextBoxCanBeAdvanced(this.talkPoint);
                if (playingTalkingAnimation && timer >= MIN_ANIMATION_TIME && !npcIsTalking)
                {
                    playingTalkingAnimation = false;
                    if (pauseAnimation != null)
                        base.aiAnimator.PlayUntilCancelled(pauseAnimation);
                }
                yield return null;
            }
            if (pauseAnimation != null)
                base.aiAnimator.PlayUntilCancelled(pauseAnimation);
        }
        TextBoxManager.ClearTextBox(this.talkPoint);
        yield break;
    }

    public Coroutine Prompt(string optionA, string optionB)
    {
        return StartCoroutine(Prompt_CR(optionA, optionB));
    }

    private IEnumerator Prompt_CR(string optionA, string optionB)
    {
        int selectedResponse = -1;
        GameUIRoot.Instance.DisplayPlayerConversationOptions(this.m_interactor, null, optionA, optionB);
        while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
            yield return null;
        LastResponse = selectedResponse;
        yield break;
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

    public float GetOverrideMaxDistance()
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
    this._animator.sprite.FlipX = GameManager.Instance.BestActivePlayer.sprite.WorldBottomCenter.x < this._centerX;
    if (this._animator.sprite.FlipX == this._cachedFlipped)
      return;

    this._cachedFlipped = this._animator.sprite.FlipX;
    base.transform.localPosition = base.transform.localPosition.WithX(
      this._baseX + (this._cachedFlipped ? _flipOffset : 0f));
    this._speechPoint.position = this._baseSpeechPos;
  }

  // private void FlipSpriteIfNecessaryClose()
  // {
  //   this._animator.sprite.FlipX = GameManager.Instance.BestActivePlayer.sprite.WorldBottomCenter.x < this._centerX;
  //   if (this._animator.sprite.FlipX == this._cachedFlipped)
  //     return;

  //   this._cachedFlipped = this._animator.sprite.FlipX;
  //   this._animator.sprite.transform.localPosition = this._animator.sprite.transform.localPosition.WithX(
  //     this._baseX + (this._cachedFlipped ? _flipOffset : 0f));
  // }
}
