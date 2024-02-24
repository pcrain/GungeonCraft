namespace CwaffingTheGungy;

public class KALI : AdvancedGunBehavior
{
    public static string ItemName         = "KALI";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal GameObject _timeShifter = null;

    // [HarmonyPatch(typeof(TrailController), nameof(TrailController.UpdateIfDirty))]
    // private class TrailOffsetPatch
    // {
    //     static bool Prefix(TrailController __instance)
    //     {
    //         if (__instance.m_isDirty)
    //         {
    //             __instance.m_minBonePosition = new Vector2(float.MaxValue, float.MaxValue);
    //             __instance.m_maxBonePosition = new Vector2(float.MinValue, float.MinValue);
    //             for (LinkedListNode<TrailController.Bone> linkedListNode = __instance.m_bones.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
    //             {
    //                 __instance.m_minBonePosition = Vector2.Min(__instance.m_minBonePosition, linkedListNode.Value.pos);
    //                 __instance.m_maxBonePosition = Vector2.Max(__instance.m_maxBonePosition, linkedListNode.Value.pos);
    //             }
    //             // __instance.transform.position = new Vector3(__instance.m_minBonePosition.x, __instance.m_minBonePosition.y, __instance.m_minBonePosition.y + -0.5f);
    //             __instance.transform.position = new Vector3(
    //                 __instance.m_minBonePosition.x + (__instance.m_maxBonePosition.x - __instance.m_minBonePosition.x) / 16f,
    //                 __instance.m_minBonePosition.y + (__instance.m_maxBonePosition.y - __instance.m_minBonePosition.y) / 16f,
    //                 __instance.m_minBonePosition.y + -0.5f);
    //             __instance.m_sprite.ForceBuild();
    //             __instance.m_sprite.UpdateZDepth();
    //             __instance.m_isDirty = false;
    //         }
    //         return false;    // skip the original method
    //     }
    // }

    [HarmonyPatch(typeof(TrailController), nameof(TrailController.SetTiledSpriteGeom))]
    private class TrailGeomPatch
    {
        static bool Prefix(TrailController __instance, Vector3[] pos, Vector2[] uv, int offset, out Vector3 boundsCenter, out Vector3 boundsExtents, tk2dSpriteDefinition spriteDef, Vector3 scale, Vector2 dimensions, tk2dBaseSprite.Anchor anchor, float colliderOffsetZ, float colliderExtentZ)
        {
            int num = Mathf.RoundToInt(spriteDef.untrimmedBoundsDataExtents.x / spriteDef.texelSize.x);
            int num2 = num / 4;
            int num3 = Mathf.Max(__instance.m_bones.Count - 1, 0);
            int num4 = Mathf.CeilToInt((float)num3 / (float)num2);
            boundsCenter = (__instance.m_minBonePosition + __instance.m_maxBonePosition) / 2f;
            boundsExtents = (__instance.m_maxBonePosition - __instance.m_minBonePosition) / 2f;
            LinkedListNode<TrailController.Bone> linkedListNode = __instance.m_bones.First;
            int num5 = 0;
            for (int i = 0; i < num4; i++)
            {
                int num6 = 0;
                int num7 = num2 - 1;
                if (i == num4 - 1 && num3 % num2 != 0)
                {
                    num7 = num3 % num2 - 1;
                }
                tk2dSpriteDefinition tk2dSpriteDefinition2 = spriteDef;
                if (__instance.usesStartAnimation && i == 0)
                {
                    int num8 = Mathf.Clamp(Mathf.FloorToInt(linkedListNode.Value.AnimationTimer * __instance.m_startAnimationClip.fps), 0, __instance.m_startAnimationClip.frames.Length - 1);
                    tk2dSpriteDefinition2 = __instance.m_sprite.Collection.spriteDefinitions[__instance.m_startAnimationClip.frames[num8].spriteId];
                }
                else if (__instance.usesAnimation && linkedListNode.Value.IsAnimating)
                {
                    int num9 = Mathf.Min((int)(linkedListNode.Value.AnimationTimer * __instance.m_animationClip.fps), __instance.m_animationClip.frames.Length - 1);
                    tk2dSpriteDefinition2 = __instance.m_sprite.Collection.spriteDefinitions[__instance.m_animationClip.frames[num9].spriteId];
                }
                float num10 = 0f;
                for (int j = num6; j <= num7; j++)
                {
                    float num11 = 1f;
                    if (i == num4 - 1 && j == num7)
                    {
                        num11 = Vector2.Distance(linkedListNode.Next.Value.pos, linkedListNode.Value.pos);
                    }
                    int num12 = offset + num5;  //HACK: figure out why 19f/16f basically works (this is the only part that's changed)
                    pos[num12++] = (19f/16f) * (linkedListNode.Value.pos      - __instance.m_minBonePosition) + linkedListNode.Value.normal      * (tk2dSpriteDefinition2.position0.y * __instance.m_projectileScale);
                    pos[num12++] = (19f/16f) * (linkedListNode.Next.Value.pos - __instance.m_minBonePosition) + linkedListNode.Next.Value.normal * (tk2dSpriteDefinition2.position1.y * __instance.m_projectileScale);
                    pos[num12++] = (19f/16f) * (linkedListNode.Value.pos      - __instance.m_minBonePosition) + linkedListNode.Value.normal      * (tk2dSpriteDefinition2.position2.y * __instance.m_projectileScale);
                    pos[num12++] = (19f/16f) * (linkedListNode.Next.Value.pos - __instance.m_minBonePosition) + linkedListNode.Next.Value.normal * (tk2dSpriteDefinition2.position3.y * __instance.m_projectileScale);
                    num12 = offset + num5;
                    pos[num12++] += new Vector3(0f, 0f, 0f - linkedListNode.Value.HeightOffset);
                    pos[num12++] += new Vector3(0f, 0f, 0f - linkedListNode.Next.Value.HeightOffset);
                    pos[num12++] += new Vector3(0f, 0f, 0f - linkedListNode.Value.HeightOffset);
                    pos[num12++] += new Vector3(0f, 0f, 0f - linkedListNode.Next.Value.HeightOffset);
                    Vector2 vector = Vector2.Lerp(tk2dSpriteDefinition2.uvs[0], tk2dSpriteDefinition2.uvs[1], num10);
                    Vector2 vector2 = Vector2.Lerp(tk2dSpriteDefinition2.uvs[2], tk2dSpriteDefinition2.uvs[3], num10 + num11 / (float)num2);
                    num12 = offset + num5;
                    if (tk2dSpriteDefinition2.flipped == tk2dSpriteDefinition.FlipMode.Tk2d)
                    {
                        uv[num12++] = new Vector2(vector.x, vector.y);
                        uv[num12++] = new Vector2(vector.x, vector2.y);
                        uv[num12++] = new Vector2(vector2.x, vector.y);
                        uv[num12++] = new Vector2(vector2.x, vector2.y);
                    }
                    else if (tk2dSpriteDefinition2.flipped == tk2dSpriteDefinition.FlipMode.TPackerCW)
                    {
                        uv[num12++] = new Vector2(vector.x, vector.y);
                        uv[num12++] = new Vector2(vector2.x, vector.y);
                        uv[num12++] = new Vector2(vector.x, vector2.y);
                        uv[num12++] = new Vector2(vector2.x, vector2.y);
                    }
                    else
                    {
                        uv[num12++] = new Vector2(vector.x, vector.y);
                        uv[num12++] = new Vector2(vector2.x, vector.y);
                        uv[num12++] = new Vector2(vector.x, vector2.y);
                        uv[num12++] = new Vector2(vector2.x, vector2.y);
                    }
                    if (linkedListNode.Value.Hide)
                    {
                        uv[num12 - 4] = Vector2.zero;
                        uv[num12 - 3] = Vector2.zero;
                        uv[num12 - 2] = Vector2.zero;
                        uv[num12 - 1] = Vector2.zero;
                    }
                    if (__instance.FlipUvsY)
                    {
                        Vector2 vector3 = uv[num12 - 4];
                        uv[num12 - 4] = uv[num12 - 2];
                        uv[num12 - 2] = vector3;
                        vector3 = uv[num12 - 3];
                        uv[num12 - 3] = uv[num12 - 1];
                        uv[num12 - 1] = vector3;
                    }
                    num5 += 4;
                    num10 += num11 / (float)__instance.m_spriteSubtileWidth;
                    if (linkedListNode != null)
                    {
                        linkedListNode = linkedListNode.Next;
                    }
                }
            }
            return false;    // skip the original method
        }
    }

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<KALI>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.1f, ammo: 100);
            gun.SetFireAudio("alligator_shoot_sound");

        Projectile p = gun.InitProjectile(new(damage: 100f, speed: 700f, range: 9999f, force: 3f, cooldown: 0.1f, clipSize: 1, invisibleProjectile: true)
        ).Attach<KaliProjectile>(
        ).Attach<PierceProjModifier>(pierce => { pierce.penetration = 999; pierce.penetratesBreakables = true; }
        ).Attach<BounceProjModifier>(bounce => { bounce.numberOfBounces = Mathf.Max(bounce.numberOfBounces, 99); }
        );
            TrailController tc = p.AddTrailToProjectilePrefab(ResMap.Get("aimu_beam_mid")[0], new Vector2(25, 39), new Vector2(12, 19),
                ResMap.Get("aimu_beam_mid"), 60, cascadeTimer: C.FRAME, softMaxLength: 1f, destroyOnEmpty: false);
                tc.UsesDispersalParticles = true;
                tc.DispersalParticleSystemPrefab = (ItemHelper.Get(Items.FlashRay) as Gun).DefaultModule.projectiles[0].GetComponentInChildren<TrailController>().DispersalParticleSystemPrefab;
            p.SetAllImpactVFX(VFX.CreatePool("gaster_beam_impact", fps: 20, loops: false, scale: 1.0f, anchor: Anchor.MiddleCenter));
            p.sprite.renderer.enabled = false;  // invisible projectile

            // TrailController tc = (ItemHelper.Get(Items.Railgun) as Gun)
            //     .DefaultModule
            //     .chargeProjectiles[1]
            //     .Projectile
            //     .GetComponentInChildren<TrailController>()
            //     .gameObject
            //     .ClonePrefab()
            //     .GetComponent<TrailController>();
            // p.AddTrailToProjectilePrefab(tc);
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);
        this._timeShifter.SafeDestroy();
        this._timeShifter = new GameObject();
        this._timeShifter.AddComponent<KaliTimeshifter>();
        this._timeShifter.transform.parent = player.transform;
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);
        this._timeShifter.SafeDestroy();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.GetComponent<KaliProjectile>() is not KaliProjectile kp)
            return;
        kp.Setup();
        this._timeShifter.GetComponent<KaliTimeshifter>().Reset();
    }
}


public class KaliProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private bool _setup = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
    }

    public void Setup()
    {
        this._setup = true;
        base.gameObject.Play("magunet_launch_sound");
        Exploder.DoDistortionWave(center: base.gameObject.transform.position.XY(),
            distortionIntensity: 1.5f, distortionRadius: 0.05f, maxRadius: 0.75f, duration: 0.25f);
    }
}

public class KaliTimeshifter : MonoBehaviour
{
    private const float _BASE_TIME_SCALE = 0.1f;
    private const float _TIME_SCALE_FACTOR = 2.0f;

    private float _timeScale = 1.0f;

    private void Start()
    {
        this._timeScale = 1.0f;
    }

    private void Update()
    {
        this._timeScale = Mathf.Min(this._timeScale + _TIME_SCALE_FACTOR * BraveTime.DeltaTime, 1.0f);
        if (this._timeScale >= 1f)
        {
            BraveTime.ClearMultiplier(base.gameObject);
            return;
        }

        BraveTime.SetTimeScaleMultiplier(this._timeScale, base.gameObject);
    }

    public void Reset()
    {
        this._timeScale = _BASE_TIME_SCALE;
    }

    private void OnDestroy()
    {
        BraveTime.ClearMultiplier(base.gameObject);
    }
}
