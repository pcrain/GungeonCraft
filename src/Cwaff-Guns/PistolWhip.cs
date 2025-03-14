namespace CwaffingTheGungy;

/* TODO:
    - Make Pistol Whip projectiles respect players' stats
*/

public class PistolWhip : CwaffGun
{
    public static string ItemName         = "Pistol Whip";
    public static string ShortDescription = "What a Horrible Night";
    public static string LongDescription  = "A long range weapon that deals high melee damage at its tip and fires a fast projectile when fully extended. Can only melee hit enemies when fully extended. Increases curse by 2 while in inventory.";
    public static string Lore             = "Once wielded by elite foot soldiers in the army of the great Pharaoh Tutancannon, this weapon is contraband in modern gunfare. On top of flouting the Guneva Conventions with its absurd muzzle range and ability to reach around rather tall walls, it is also reported to have been cursed by Tutancannon himself on his deathbed, bound to unleash the foulest creatures upon those who would dare wield it within the Gungeon's chambers.";

    internal const int _MINI_BLANKS_ON_KILL = 3; // mini blanks to replenish on kill for mastery

    internal static Projectile _PistolWhipProjectile;
    internal static Projectile _PistolButtProjectile;

    public int miniBlanks = 0;

    public static void Init()
    {
        Lazy.SetupGun<PistolWhip>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.PISTOL, reloadTime: 0.01f, ammo: 100, shootFps: 30, reloadFps: 40, curse: 2f,
            infiniteAmmo: true, attacksThroughWalls: true)
          .Attach<PistolWhipAmmoDisplay>()
          .AddToShop(ItemBuilder.ShopType.Cursula)
          .InitProjectile(GunData.New(ammoCost: 0, clipSize: -1, cooldown: WhipChainStart.TOTAL_TIME + C.FRAME, shootStyle: ShootStyle.SemiAutomatic,
            damage: 0.0f, speed: 0.01f, range: 999.0f, hideAmmo: true))
          .Attach<WhipChainStartProjectile>();

        _PistolWhipProjectile = Items.Ak47.CloneProjectile(GunData.New(damage: 15.0f, speed: 80.0f, force: 10.0f, range: 80.0f))
          .Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.3f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.05f;
            trail.BaseColor  = Color.yellow;
            trail.EndColor   = Color.yellow;
          });

        _PistolButtProjectile = Items.Ak47.CloneProjectile(GunData.New(damage: 30.0f, speed: 1.0f, force: 40.0f, range: 0.01f))
          .AddAnimations(AnimatedBullet.Create(name: "pistol_whip_dummy_bullet", fps: 12, anchor: Anchor.MiddleCenter)) // Not really visible, just used for pixel collider size
          .SetAllImpactVFX(VFX.CreatePool("whip_particles", fps: 20, loops: false, scale: 0.5f))
          .Attach<PistolButtProjectile>();
    }

    public void ReplenishMiniBlanks(Projectile p, SpeculativeRigidbody body)
    {
        if (body.healthHaver && body.healthHaver.IsAlive) // don't restore mini blanks from corpses
            this.miniBlanks = _MINI_BLANKS_ON_KILL;
    }

    public void MaybeDoMiniBlank(Vector2 pos)
    {
        if (this.miniBlanks <= 0)
            return;
        --this.miniBlanks;
        Lazy.DoMicroBlankAt(pos, this.PlayerOwner);
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this.miniBlanks);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this.miniBlanks = (int)data[i++];
    }

    private class PistolWhipAmmoDisplay : CustomAmmoDisplay
    {
        private PistolWhip whip;
        private PlayerController _owner;

        private void Start()
        {
            Gun gun     = base.GetComponent<Gun>();
            this.whip   = gun.GetComponent<PistolWhip>();
            this._owner = gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this.whip.Mastered)
                return false;

            uic.GunAmmoCountLabel.Text = $"[sprite \"mini_blank_ui\"]x{this.whip.miniBlanks}\n[sprite \"infinite-big\"]";
            return true;
        }
    }
}

public class PistolButtProjectile : MonoBehaviour  //TODO: possibly reuse this as a TransientProjectile (e.g., for Vladimir)
{
    private void Start()
    {
        Projectile p = base.gameObject.GetComponent<Projectile>();
        p.sprite.renderer.enabled = false;
        if (p.Owner is PlayerController player && player.HasSynergy(Synergy.WICKED_CHILD))
        {
            p.BlackPhantomDamageMultiplier = 2f; // for jammed bosses, normal enemies are instantly killed so this is a moot point
            p.OnHitEnemy += this.OnHitEnemy;
        }
        StartCoroutine(ExpireInTwoFrames()); // needs two frames to actually be able to hit anyone
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody other, bool killed)
    {
        if (other.GetComponent<AIActor>() is not AIActor enemy)
            return;
        if (!enemy.IsBlackPhantom)
            return;
        if (enemy.healthHaver is not HealthHaver hh)
            return;
        if (hh.IsBoss || hh.IsSubboss)
            return;
        hh.ApplyDamage(damage: 9999f, direction: p.Direction, sourceName: "Doesn't Belong in this World", damageTypes: CoreDamageTypes.Magic);
    }

    private IEnumerator ExpireInTwoFrames()
    {
        yield return null;
        yield return null;
        base.gameObject.GetComponent<Projectile>().DieInAir(suppressInAirEffects: false, allowActorSpawns: true, allowProjectileSpawns: true, killedEarly: false);
    }
}

public class WhipChainStartProjectile : MonoBehaviour
{
    private void Start()
    {
        Projectile p = base.gameObject.GetComponent<Projectile>();
        UnityEngine.GameObject.Instantiate(new GameObject(), Vector3.zero, Quaternion.identity)
            .AddComponent<WhipChainStart>()
            .Setup(p.Owner as PlayerController, p.Direction.ToAngle());
        UnityEngine.GameObject.Destroy(base.gameObject);
    }
}

public class WhipChainStart : MonoBehaviour
{
    internal const int   _CHAIN_LENGTH     = 60;
    internal const float _INVLINKS         = 1.0f / _CHAIN_LENGTH;
    internal const int   _HANDLE_LENGTH    = _CHAIN_LENGTH / 10;
    internal const float _WHIP_WIDTH       = 3f * C.PIXEL_SIZE;

    internal const float _SPEED_SCALE      = 1f; // animation speed scale, for debugging

    internal const float _WHIP_RANGE       = 6.0f;
    internal const float _RETRACT_MAX      = 0.65f;
    internal const float _MAX_AMP          = 0.5f;
    internal static readonly float[] TIMES = {
        _SPEED_SCALE * 0.00f, // start
        _SPEED_SCALE * 0.05f, // charge
        _SPEED_SCALE * 0.20f, // whip
        _SPEED_SCALE * 0.25f, // hold
        _SPEED_SCALE * 0.40f, // retract
    };
    internal static readonly float TOTAL_TIME = TIMES[TIMES.Length - 1];

    internal static tk2dBaseSprite gunSprite = null;

    private PlayerController _owner;
    private float _angle;
    private List<GameObject> _links;
    private PistolWhip _whip;

    public void Setup(PlayerController owner, float angle)
    {
        if (!owner)
            return;

        if (owner.CurrentGun is Gun gun)
            this._whip = gun.GetComponent<PistolWhip>();
        gunSprite ??= Items.Magnum.AsGun().sprite;
        this._owner = owner;
        this._angle = angle;
        this._links = new();
        for (int i = 0; i < _CHAIN_LENGTH; ++i)
            this._links.Add(null);

        StartCoroutine(WhipItGood());
    }

    private void OnDestroy()
    {
        this._links.SafeDestroyAll();
    }

    private IEnumerator WhipItGood()
    {
        const float CYCLES   = 3f;
        float freq           = (CYCLES * (2f * Mathf.PI)) / _CHAIN_LENGTH;
        bool flipped         = Mathf.Abs(this._angle) > 90f;
        Quaternion baseEuler = this._angle.EulerZ();

        this._owner.CurrentGun.ToggleRenderers(false);
        this._owner.gameObject.PlayOnce("whip_sound");

        tk2dSprite pistolSprite = Lazy.SpriteObject(gunSprite.collection, gunSprite.spriteId);
            pistolSprite.transform.rotation = baseEuler;
            pistolSprite.transform.localScale = new Vector3(1f, flipped ? -1f : 1f, 1f);

        int phase = 1;
        bool spawnProjectile = false;
        float whipRange = _WHIP_RANGE;
        bool mastered = this._owner.HasSynergy(Synergy.MASTERY_PISTOL_WHIP);
        if (mastered)
        {
            Vector2 start  = this._owner.primaryHand.transform.position;
            Vector2 end    = start + whipRange * this._owner.m_currentGunAngle.ToVector();
            AIActor target = Lazy.NearestEnemyInLineOfSight(start: start, end: end, canBeNeutral: true);
            if (target)
                whipRange = Mathf.Max(1f, (target.CenterPosition - start).magnitude - 1.0f);
        }
        for (float elapsed = 0f; elapsed < TOTAL_TIME; elapsed += BraveTime.DeltaTime)
        {
            if (elapsed > TIMES[phase])
            {
                ++phase;
                if (phase == 3)
                    spawnProjectile = true;
            }
            float phasePercent = (elapsed - TIMES[phase-1]) / (TIMES[phase] - TIMES[phase-1]);

            float maxDistance = 0f;
            switch(phase)
            {
                case 1: // charge
                    float invPhasePercent = 1f - phasePercent;
                    float invEaseSquare = 1f - (invPhasePercent * invPhasePercent);
                    maxDistance = -invEaseSquare * whipRange * _RETRACT_MAX; // pull back the whip to 65% of its max length
                    break;
                case 2: // whip
                    float easeCubic = (phasePercent * phasePercent * phasePercent);
                    maxDistance = whipRange * Mathf.Lerp(-_RETRACT_MAX, 1.0f, easeCubic); // whip slowly extends
                    break;
                case 3: // hold
                    maxDistance = whipRange; // whip holds for a bit
                    break;
                case 4: // retract
                    maxDistance = (1 - phasePercent) * whipRange; // retract the whip fully
                    break;
                default: // shouldn't happen
                    break;
            }
            float absRange       = Mathf.Abs(maxDistance) / whipRange;
            float maxAmp         = (flipped ? -_MAX_AMP : _MAX_AMP) * (1f - (absRange * absRange));
            float linkDistance   = maxDistance * _INVLINKS;
            Vector3 basePos      = this._owner ? this._owner.primaryHand.transform.position : Vector3.zero;
            Vector2 segBegin = Vector2.zero;
            Vector2 segEnd   = Vector2.zero;

            if (spawnProjectile)
            {
                Vector2 barrelOffset = new Vector2(1.25f, flipped ? -0.18f : 0.18f);

                Vector3 pos = basePos + baseEuler * (whipRange * Vector2.right + barrelOffset);

                Projectile proj = SpawnManager.SpawnProjectile(PistolWhip._PistolWhipProjectile.gameObject, pos, baseEuler).GetComponent<Projectile>();
                    proj.SetOwnerAndStats(this._owner);
                    proj.collidesWithEnemies = true;
                    proj.collidesWithPlayer = false;

                Projectile proj2 = SpawnManager.SpawnProjectile(PistolWhip._PistolButtProjectile.gameObject, pos, baseEuler).GetComponent<Projectile>();
                    proj2.SetOwnerAndStats(this._owner);
                    proj2.collidesWithEnemies = true;
                    proj2.collidesWithPlayer = false;

                if (mastered && this._whip)
                {
                    proj.OnWillKillEnemy += this._whip.ReplenishMiniBlanks;
                    proj2.OnWillKillEnemy += this._whip.ReplenishMiniBlanks;
                    this._whip.MaybeDoMiniBlank(pos);
                }

                this._owner.gameObject.PlayOnce("whip_crack_sound");
                spawnProjectile = false;
            }

            Quaternion lastRotation = Quaternion.identity;
            for (int i = 0 ; i < _CHAIN_LENGTH; ++i)
            {
                segEnd = basePos + baseEuler * (new Vector2((i + 1) * linkDistance, maxAmp * Mathf.Sin((i + 1) * freq)));
                if (this._links[i] == null)
                    this._links[i] = Ticonderogun.FancyLine(
                        segBegin, segEnd, _WHIP_WIDTH, spriteId: VFX.Collection.GetSpriteIdByName(i >= _HANDLE_LENGTH ? "whip_segment" : "whip_segment_base"));
                else
                {
                    Vector2 delta                = segEnd - segBegin;
                    lastRotation                 = delta.EulerZ();
                    tk2dSlicedSprite quad        = this._links[i].GetComponent<tk2dSlicedSprite>();
                    quad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude, _WHIP_WIDTH));
                    quad.transform.localRotation = lastRotation;
                    quad.transform.position      = segBegin + (0.5f * _WHIP_WIDTH * delta.normalized.Rotate(-90f));
                }
                segBegin = segEnd;
            }

            pistolSprite.PlaceAtRotatedPositionByAnchor(segEnd, Anchor.MiddleLeft);

            yield return null;
        }

        this._owner.CurrentGun.ToggleRenderers(true);
        UnityEngine.Object.Destroy(pistolSprite.gameObject);
        UnityEngine.Object.Destroy(base.gameObject);
        yield break;
    }
}
