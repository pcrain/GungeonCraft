namespace CwaffingTheGungy;

public class HeckedShrine : MonoBehaviour, IPlayerInteractable
{
    const Anchor SHRINE_ANCHOR = Anchor.LowerCenter;

    private static HeckedShrine _RetrashedShrine    = null;
    private static HeckedShrine _LordFortressShrine = null;

    public tk2dBaseSprite       sprite         = null;
    public SpeculativeRigidbody body           = null;
    public string               text           = "";
    public Vector2              positionInRoom = Vector2.zero;

    public static void Init()
    {
      _RetrashedShrine    = SetupShrine("retrashed_statue",    _RetrashedText,    new Vector2( 4f, 3f));
      _LordFortressShrine = SetupShrine("lordfortress_statue", _LordFortressText, new Vector2(-4f, 3f));

      CwaffEvents.OnFirstFloorFullyLoaded += SpawnInShrines;
    }

    internal static HeckedShrine SetupShrine(string spritePath, string flavorText, Vector2 positionInRoom)
    {
      GameObject g        = VFX.Create(spritePath, 2, scale: 2.0f, anchor: SHRINE_ANCHOR);
      HeckedShrine shrine = g.AddComponent<HeckedShrine>();
        shrine.body           = null; // defer setup until spawn time so we can account for sprite flipping
        shrine.sprite         = g.GetComponent<tk2dBaseSprite>();
        shrine.text           = flavorText;
        shrine.positionInRoom = positionInRoom;
      return shrine;
    }

    private static void SpawnInShrines()
    {
      GameManager.Instance.StartCoroutine(SpawnInShrines_CR());
    }

    private static IEnumerator SpawnInShrines_CR()
    {
        while (GameManager.Instance.IsLoadingLevel)
            yield return null;  //wait for level to fully load

        Vector3 v3          = Vector3.zero;
        bool found          = false;
        PlayerController p1 = GameManager.Instance.BestActivePlayer;
        foreach (AdvancedShrineController a in StaticReferenceManager.AllAdvancedShrineControllers)
        {
            if (a.IsLegendaryHeroShrine)
            {
                found = true;
                v3 = a.transform.position + (new Vector2(a.sprite.GetCurrentSpriteDef().position3.x/2,-3f)).ToVector3ZisY(0);
            }
        }
        if (!found)
        {
          ETGModConsole.Log($"failed to find hero shrine");
          yield break;
        }

      SpawnIn(_RetrashedShrine, v3);
      SpawnIn(_LordFortressShrine, v3);
    }

    private static void SpawnIn(HeckedShrine shrinePrefab, Vector3 heroShrinePos)
    {
      HeckedShrine shrine = shrinePrefab.gameObject.Instantiate(heroShrinePos + shrinePrefab.positionInRoom.ToVector3ZisY()).GetComponent<HeckedShrine>();
      heroShrinePos.GetAbsoluteRoom().RegisterInteractable(shrine.GetComponent<IPlayerInteractable>());
      // Material m = shrine.gameObject.GetOrAddShader(Shader.Find("Brave/Internal/GlitterPassAdditive"));
      // Material m = shrine.gameObject.GetOrAddShader(Shader.Find("Brave/Effects/InterdimensionalHorrorPortal"));
      Material m = shrine.gameObject.GetOrAddShader(Shader.Find("Brave/GoopShader"));
      // m.SetColor("_OverrideColor", Color.yellow);
      // m.SetFloat("_Period", 1.0f);
      // m.SetFloat("_PixelWidth", 5.0f);
      // m.SetFloat("_Perpendicular", 0f);

      // shrine.sprite.transform.localScale = shrine.sprite.transform.localScale.WithX(-1f);
      shrine.sprite.FlipX = shrinePrefab.positionInRoom.x < 0;
      shrine.sprite.HeightOffGround = -2f;
      shrine.sprite.UpdateZDepth();
      shrine.body         = shrine.gameObject.AutoRigidBody(anchor: SHRINE_ANCHOR/*, canBePushed: true*/);
      SpriteOutlineManager.AddOutlineToSprite(shrine.sprite, Color.black, 1f, 0.005f);
    }

    private void Update()
    {
      // this.sprite.SetGlowiness(10f + 100f * Mathf.Abs(Mathf.Sin(3f * BraveTime.ScaledTimeSinceStartup)));
    }

    public void Interact(PlayerController interactor)
    {
      interactor.CurrentRoom.DeregisterInteractable(this);
      StartCoroutine(InteractWithShrine(interactor));
    }

    private IEnumerator InteractWithShrine(PlayerController interactor)
    {
      Transform talkPoint = base.transform;
      TextBoxManager.ShowStoneTablet(this.sprite.WorldTopCenter, talkPoint, -1f, this.text);
      interactor.SetInputOverride("shrineConversation");
      yield return null;

      GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, "cool O:", string.Empty);
      while (!GameUIRoot.Instance.GetPlayerConversationResponse(out int selectedResponse))
        yield return null;

      interactor.ClearInputOverride("shrineConversation");
      TextBoxManager.ClearTextBox(talkPoint);
      base.transform.position.GetAbsoluteRoom().RegisterInteractable(base.GetComponent<IPlayerInteractable>());
      yield break;
    }

    public void OnEnteredRange(PlayerController interactor)
    {
      if (!this)
        return;
      SpriteOutlineManager.RemoveOutlineFromSprite(this.sprite);
      SpriteOutlineManager.AddOutlineToSprite(this.sprite, Color.white, 1f, 0.005f);
    }

    public void OnExitRange(PlayerController interactor)
    {
      if (!this)
        return;
      SpriteOutlineManager.RemoveOutlineFromSprite(this.sprite);
      SpriteOutlineManager.AddOutlineToSprite(this.sprite, Color.black, 1f, 0.005f);
    }

    public float GetDistanceToPoint(Vector2 point)
    {
      if (!this)
        return 1000f;
      if (this.sprite == null)
        return 100f;
      Vector3 v = BraveMathCollege.ClosestPointOnRectangle(point, this.body.UnitBottomLeft, this.body.UnitDimensions);
      return Vector2.Distance(point, v) / 1.5f;
    }

    public float GetOverrideMaxDistance()
    {
      return -1f;
    }

    public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
    {
      shouldBeFlipped = false;
      return string.Empty;
    }


    private static string _RetrashedText =
"""
hello there
  how's it going

    pretty good eh?

~ pat
""";

    private static string _LordFortressText =
"""
hello there
  how's it going

    pretty good eh?

~ pat
""";
}
