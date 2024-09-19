namespace CwaffingTheGungy;

using static DirectionalAnimation.DirectionType;

public abstract class CwaffCompanionController : CompanionController
{
}

public static class CwaffCompanionBuilder
{
    public static Friend InitCompanion<Friend>(this PassiveItem item, int baseFps = 4, List<string> extraAnims = null)
        where Friend : CwaffCompanionController
    {
        if (item is not CwaffCompanion cc)
        {
            Lazy.RuntimeWarn("Trying to create a companion for an item that is not a CwaffCompanion");
            return null;
        }

        string name = item.itemName.ToID();

        Friend friend = CompanionBuilder.BuildPrefab(name, $"{C.MOD_PREFIX}:{name}_companion", $"{name}_idle_001", IntVector2.Zero, IntVector2.One)
          .AddComponent<Friend>();
        friend.SetupAnimations(baseFps: baseFps, extraAnims: extraAnims);
        friend.companionID = CompanionController.CompanionIdentifier.NONE;

        cc.CompanionGuid = friend.aiActor.EnemyGuid;
        return friend;
    }

    private static void SetupAnimations(this CwaffCompanionController friend, int baseFps, List<string> extraAnims = null)
    {
        string name = friend.gameObject.name;

        tk2dSpriteCollectionData coll     = VFX.Collection;
        AIAnimator aiAnimator             = friend.gameObject.GetComponent<AIAnimator>();
        tk2dSpriteAnimator spriteAnimator = friend.gameObject.GetComponent<tk2dSpriteAnimator>();
        spriteAnimator.library            = friend.gameObject.AddComponent<tk2dSpriteAnimation>();
        aiAnimator.OtherAnimations        = new();

        List<tk2dSpriteAnimationClip> clips = new();
        aiAnimator.IdleAnimation   = coll.AutoAnimation(ref clips, $"{name}_idle",   fps: baseFps);
        aiAnimator.MoveAnimation   = coll.AutoAnimation(ref clips, $"{name}_move",   fps: baseFps);
        aiAnimator.FlightAnimation = coll.AutoAnimation(ref clips, $"{name}_flight", fps: baseFps);
        aiAnimator.HitAnimation    = coll.AutoAnimation(ref clips, $"{name}_hit",    fps: baseFps);
        aiAnimator.TalkAnimation   = coll.AutoAnimation(ref clips, $"{name}_talk",   fps: baseFps);
        if (coll.AutoAnimation(ref clips, $"{name}_pet", fps: baseFps) is DirectionalAnimation petAnim)
        {
            aiAnimator.OtherAnimations.Add(new(){name = "pet", anim = petAnim});
            friend.CanBePet = true; //TODO: fix m_petOffset after calling DoPet() with patch
        }
        if (coll.AutoAnimation(ref clips, $"{name}_fidget", fps: baseFps) is DirectionalAnimation fidgetAnim)
            aiAnimator.IdleFidgetAnimations = [fidgetAnim];
        foreach (string anim in extraAnims.EmptyIfNull())
            aiAnimator.OtherAnimations.Add(new(){name = anim, anim = coll.AutoAnimation(ref clips, $"{name}_{anim}", fps: baseFps)});
        spriteAnimator.library.clips = clips.ToArray();
    }

    /// <summary>Sets up the appropriate directional animation from the available sprites with the given prefix</summary>
    private static DirectionalAnimation AutoAnimation(this tk2dSpriteCollectionData coll, ref List<tk2dSpriteAnimationClip> clips, string name, int fps)
    {
        DirectionalAnimation.DirectionType dType = AutoDetectDirectionFromSpriteName(name);
        if (dType == None)
        {
            // Lazy.RuntimeWarn($"failed to get animations for {name}");
            return null;
        }

        DirectionalAnimation.SingleAnimation[] sa = DirectionalAnimation.m_combined[(int)dType];
        int nanims = sa.Length;
        string[] animNames = new string[nanims];
        for (int i = 0; i < nanims; ++i)
        {
            string aname = string.IsNullOrEmpty(sa[i].suffix) ? name : $"{name}_{sa[i].suffix}";
            tk2dSpriteAnimationClip clip = coll.AddAnimation(aname, fps: fps);
            if (clip == null)
            {
                Lazy.RuntimeWarn($"  FAILED TO ADD CLIP {aname}");
                return null;
            }
            Lazy.DebugLog($"  added clip {aname}");
            animNames[i] = aname;
            clips.Add(clip);
        }

        return new(){
            Type      = dType,
            Prefix    = name,
            AnimNames = animNames,
            Flipped   = new DirectionalAnimation.FlipType[nanims],
        };
    }

    private static DirectionalAnimation.DirectionType AutoDetectDirectionFromSpriteName(string name)
    {
        if (ResMap.Get($"{name}_north_northeast", quietFailure: true) != null)
            return SixteenWay;
        if (ResMap.Get($"{name}_northeast", quietFailure: true) != null)
            return EightWayOrdinal;
        if (ResMap.Get($"{name}_north", quietFailure: true) != null)
            return FourWayCardinal;
        if (ResMap.Get($"{name}_front_right", quietFailure: true) != null)
        {
            if (ResMap.Get($"{name}_right", quietFailure: true) != null)
                return EightWay;
            if (ResMap.Get($"{name}_front", quietFailure: true) != null)
                return SixWay;
            return FourWay;
        }
        if (ResMap.Get($"{name}_right", quietFailure: true) != null)
            return TwoWayHorizontal;
        if (ResMap.Get($"{name}_front", quietFailure: true) != null)
            return TwoWayVertical;
        if (ResMap.Get($"{name}", quietFailure: true) != null)
            return Single;
        return None;
    }
}
