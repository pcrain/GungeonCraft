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
        if (ResMap.Has($"{name}_north_northeast"))
            return SixteenWay;
        if (ResMap.Has($"{name}_northeast"))
            return EightWayOrdinal;
        if (ResMap.Has($"{name}_north"))
            return FourWayCardinal;
        if (ResMap.Has($"{name}_front_right"))
        {
            if (ResMap.Has($"{name}_right"))
                return EightWay;
            if (ResMap.Has($"{name}_front"))
                return SixWay;
            return FourWay;
        }
        if (ResMap.Has($"{name}_right"))
            return TwoWayHorizontal;
        if (ResMap.Has($"{name}_front"))
            return TwoWayVertical;
        if (ResMap.Has(name))
            return Single;
        return None;
    }

    public static void MakeIntangible(this CwaffCompanionController friend)
    {
        friend.CanCrossPits = true;
        friend.aiActor.healthHaver.PreventAllDamage = true;
        friend.aiActor.CollisionDamage = 0f;
        friend.aiActor.specRigidbody.CollideWithOthers = false;
        friend.aiActor.specRigidbody.CollideWithTileMap = false;
    }
}
