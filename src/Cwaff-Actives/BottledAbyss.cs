namespace CwaffingTheGungy;

public class BottledAbyss : CwaffActive
{
    public static string ItemName         = "Bottled Abyss";
    public static string ShortDescription = "Stares Back";
    public static string LongDescription  = "Summons a void under the player that expands to consume all grounded enemies (and players) in a large radius after a brief delay. Does not affect bosses or inanimate objects.";
    public static string Lore             = "A bottle that at first glance contains nothing inside of it, but upon further inspection contains even less. It is highly recommended that any attempt to open the bottle be quickly followed up with running away as fast as possible.";

    private const float _EXPAND_TIME   = 2f;
    private const float _HOLD_TIME     = 7f;
    private const float _COLLAPSE_TIME = 1f;
    private const float _MAX_SCALE     = 20f;

    internal static GameObject _VoidPrefab = null;
    internal static GameObject _VoidSplashVFX = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<BottledAbyss>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.C;
        item.consumable = false;
        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.PerRoom, 3);

        _VoidPrefab = ((GameObject)BraveResources.Load("Global Prefabs/VFX_ParadoxPortal")).ClonePrefab();
        UnityEngine.Object.Destroy(_VoidPrefab.GetComponent<ParadoxPortalController>());
        _VoidPrefab.AddComponent<Voidyboi>();
        Material mat = _VoidPrefab.GetComponent<MeshRenderer>().material;
            mat.SetTexture("_PortalTex", null); // texture of starry background
            mat.SetColor("_EdgeColor", Color.black); // color of center of portal
            mat.SetFloat("_RotSpeed", 120f);
            mat.SetFloat("_Parallax", 40f);
            mat.SetFloat("_Cutoff", 10f);
            mat.SetFloat("_SSMag", 40f);
            mat.SetFloat("_LSMag", 40f);
            mat.SetFloat("_HoleEdgeDepth", 0.02f);
            mat.SetColor("_Magnitudes", 0.05f * Color.white); //NOTE: actually a Vector4, but shader treats it like a color
            mat.SetFloat("_UVDistCutoff", 0.4f);

        _VoidSplashVFX = VFX.Create("void_splash_vfx");
    }

    public override bool CanBeUsed(PlayerController user)
    {
        return base.CanBeUsed(user) && user.IsInCombat;
    }

    public override void DoEffect(PlayerController user)
    {
        CwaffVFX.SpawnBurst(prefab: _VoidSplashVFX, numToSpawn: 30,
            basePosition: user.CenterPosition, positionVariance: 1f, minVelocity: 8f, velocityVariance: 4f,
            velType: CwaffVFX.Vel.AwayRadial, rotType: CwaffVFX.Rot.Random, lifetime: 0.4f,
            endScale: 0.1f);
        user.gameObject.Play("sound_of_opening_the_abyss");
        UnityEngine.Object.Instantiate(_VoidPrefab, user.CenterPosition, Quaternion.identity);
    }

    private class Voidyboi : MonoBehaviour
    {
        private float _scale      = 0.0f;
        private RoomHandler _room = null;
        private Vector2 _pos      = default;

        private IEnumerator Start()
        {
            base.gameObject.transform.localScale = Vector3.one;
            this._pos = base.gameObject.transform.position;
            this._room = this._pos.GetAbsoluteRoom();

            for (float elapsed = 0f; elapsed < _EXPAND_TIME; elapsed += BraveTime.DeltaTime)
            {
                float percentDone = elapsed / _EXPAND_TIME;
                this._scale = _MAX_SCALE * percentDone * percentDone;
                this.LoopSoundIf(percentDone > 0.5f, "sound_of_the_abyss_calling");
                yield return null;
            }

            for (float elapsed = 0f; elapsed < _HOLD_TIME; elapsed += BraveTime.DeltaTime)
            {
                this.LoopSoundIf(true, "sound_of_the_abyss_calling");
                yield return null;
            }

            for (float elapsed = 0f; elapsed < _COLLAPSE_TIME; elapsed += BraveTime.DeltaTime)
            {
                float percentDone = elapsed / _COLLAPSE_TIME;
                this._scale = _MAX_SCALE * (1f - percentDone * percentDone);
                this.LoopSoundIf(percentDone < 0.5f, "sound_of_the_abyss_calling");
                yield return null;
            }

            foreach (PlayerController player in GameManager.Instance.AllPlayers)
                player.SetIsFlying(false, ItemName, false); // disable mercy flight
            UnityEngine.Object.Destroy(base.gameObject);
        }

        private void Update()
        {
            base.gameObject.transform.localScale = new Vector3(this._scale, this._scale, 1f);
            if (this._room == null)
                return;

            float dangerRadius = Mathf.Max(0f, 0.5f * this._scale - 2.5f);
            if (dangerRadius == 0f)
                return;

            float sqrRadius = dangerRadius * dangerRadius;
            foreach (AIActor enemy in this._room.SafeGetEnemiesInRoom())
            {
                if (!enemy || enemy.IsFalling || !enemy.IsGrounded || !enemy.HasBeenEngaged || enemy.State != AIActor.ActorState.Normal)
                    continue;
                if (enemy.healthHaver is not HealthHaver hh || hh.IsBoss || hh.IsSubboss)
                    continue;
                if (enemy.specRigidbody is not SpeculativeRigidbody srb)
                    continue;
                if ((this._pos - srb.UnitBottomCenter).sqrMagnitude > sqrRadius)
                    continue;
                enemy.ForceFall();
            }

            foreach (PlayerController player in GameManager.Instance.AllPlayers)
            {
                if (!player || player.IsFalling || player.FallingProhibited || player.IsFlying || player.IsGhost || !player.QueryGroundedFrame())
                    continue;
                if (!player.specRigidbody)
                    continue;
                if ((this._pos - player.specRigidbody.UnitBottomCenter).sqrMagnitude > sqrRadius)
                    continue;
                player.ForceFall();
                player.SetIsFlying(true, ItemName, false); // mercy flight
            }
        }
    }
}

