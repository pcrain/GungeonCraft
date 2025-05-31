namespace CwaffingTheGungy;

public class Scotsman : CwaffGun
{
    public static string ItemName         = "Scotsman";
    public static string ShortDescription = "Situationally Sticky";
    public static string LongDescription  = "Launches sticky bombs that stick to enemies, obstacles, walls, and the floor. Reloading detonates all stationary sticky bombs after a short delay.";
    public static string Lore             = "Hailing straight from the Motherland, this weapon is a favorite among the explosion-loving Scots whose name it bears. The gun's sticky projectiles and ability to detonate them on command takes out much of the guesswork involved when using traditional firearms, ensuring substantial destructive output even when its wielder happens to be drunk, half-blind, or both.";

    private const float _MAX_RETICLE_RANGE = 16f;
    private const float _BASE_EXPLOSION_DAMAGE = 24f;

    internal static ExplosionData _ScotsmanExplosion = null;

    internal List<Stickybomb> _extantStickies = new();

    private Vector2 _aimPoint                = Vector2.zero;
    private int _nextIndex                   = 0;
    private Vector2 _whereIsThePlayerLooking = Vector2.zero;

    public static void Init()
    {
        Lazy.SetupGun<Scotsman>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.EXPLOSIVE, reloadTime: 2.00f, ammo: 300, canReloadNoMatterAmmo: true,
            shootFps: 24, reloadFps: 12, fireAudio: "stickybomblauncher_shoot", reloadAudio: "stickybomblauncher_worldreload",
            muzzleVFX: "muzzle_scotsman", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleLeft)
          .AddReticle<CwaffReticle>(reticleVFX : VFX.Create("scotsman_reticle", fps: 12, loops: true, anchor: Anchor.MiddleCenter),
            controllerScale : _MAX_RETICLE_RANGE, visibility : CwaffReticle.Visibility.CONTROLLER)
          .AddToShop(ItemBuilder.ShopType.Trorc)
          .InitProjectile(GunData.New(clipSize: 20, cooldown: 0.22f, shootStyle: ShootStyle.SemiAutomatic, customClip: true,
            damage: _BASE_EXPLOSION_DAMAGE, speed: 40.0f, sprite: "stickybomb_projectile", fps: 12, anchor: Anchor.MiddleCenter))
          .Attach<Stickybomb>();

        _ScotsmanExplosion = Explosions.ExplosiveRounds.With(damage: _BASE_EXPLOSION_DAMAGE, force: 100f, debrisForce: 10f, radius: 1.5f,
            preventPlayerForce: false, shake: false);
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        DetonateStickies(player);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        if (this._meshRenderer)
            this._meshRenderer.enabled = true;
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        if (this.PlayerOwner is PlayerController pc)
            pc.forceAimPoint = null;
        if (this._meshRenderer)
            this._meshRenderer.enabled = false;
        base.OnSwitchedAwayFromThisGun();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        player.OnEnteredCombat += this.OnEnteredCombat;
        base.OnPlayerPickup(player);
    }
    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.OnEnteredCombat -= this.OnEnteredCombat;
        if (this._meshRenderer)
            this._meshRenderer.enabled = false;
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            this.PlayerOwner.forceAimPoint = null;
            this.PlayerOwner.OnEnteredCombat -= this.OnEnteredCombat;
        }
        if (this._coneMeshObject)
            UnityEngine.Object.Destroy(this._coneMeshObject);
        base.OnDestroy();
    }

    private LinkedList<MinorBreakable> _roomBreakables = new();
    private void OnEnteredCombat()
    {
        if (!this.PlayerOwner)
            return;
        if (this.PlayerOwner.CurrentRoom is not RoomHandler currentRoom)
            return;
        this._roomBreakables.Clear();
        foreach (MinorBreakable minorBreakable in StaticReferenceManager.AllMinorBreakables)
        {
            if (!minorBreakable || minorBreakable.IsBroken || minorBreakable.CenterPoint.GetAbsoluteRoom() != currentRoom)
                continue;
            this._roomBreakables.AddLast(minorBreakable);
        }
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player || !player.AcceptingNonMotionInput)
            return;

        // smoothly handle reticle postion, compensating extra distance for controller users (modified from Gunbrella)
        if (player.IsKeyboardAndMouse())
        {
            player.forceAimPoint = null;
            this._aimPoint = player.unadjustedAimPoint.XY();
            Vector2 gunPos = player.CurrentGun.barrelOffset.PositionVector2();
            if ((this._aimPoint - player.CenterPosition).sqrMagnitude < 32f)
                this._aimPoint = gunPos + player.m_currentGunAngle.ToVector(1f);
            return;
        }

        if (base.GetComponent<CwaffReticle>() is CwaffReticle reticle && reticle.IsVisible())
        {
            this._aimPoint = reticle.GetTargetPos();
            player.forceAimPoint = this._aimPoint;
        }
        else
            this._aimPoint = player.CenterPosition + player.m_currentGunAngle.ToVector(_MAX_RETICLE_RANGE);
    }

    private void LateUpdate()
    {
        if (!this.PlayerOwner)
            return;
        if (this.Mastered)
            UpdateExplosiveDecor();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.PlayerOwner is not PlayerController player)
            return;

        projectile.GetComponent<Stickybomb>().Setup(this._aimPoint);
    }

    private void DetonateStickies(PlayerController pc)
    {
        List<Stickybomb> remainingStickies = new();
        bool anythingDetonated = false;
        foreach (Stickybomb sticky in this._extantStickies)
        {
            if (!sticky)
                continue;
            if (sticky.Detonate(pc))
                anythingDetonated = true;
            else
                remainingStickies.Add(sticky);
        }
        this._extantStickies = remainingStickies;
        if (DetonateTheWorld(pc))
            anythingDetonated = true;
        if (anythingDetonated)
            pc.gameObject.Play("stickybomblauncher_det");
    }

    private List<AIActor> _activeEnemiesInRoom = new();
    private bool DetonateTheWorld(PlayerController pc)
    {
        if (!this.Mastered || !this.gun || !this.gun.barrelOffset || pc.CurrentRoom is not RoomHandler room)
            return false;

        bool anythingDetonated = false;
        int numBreakables = this._roomBreakables.Count;
        Vector3 barrelPos = this.gun.barrelOffset.position;
        float gunAngle = pc.m_currentGunAngle;

        // Nuke the decor
        for (int i = 0; i < numBreakables; ++i)
        {
            LinkedListNode<MinorBreakable> node = this._roomBreakables.First;
            this._roomBreakables.RemoveFirst();
            //NOTE: LinkedListNode.Value returns true-ish even if the MinorBreakable is invalid, so need explicit !b check
            if (node.Value is not MinorBreakable b || !b || !b.isActiveAndEnabled || b.IsBroken)
                continue;
            Vector3 bpos = (b.sprite ? b.sprite.WorldCenter : b.transform.position);
            Vector2 delta = bpos - barrelPos;
            if (delta.sqrMagnitude > _CONE_SQR_MAG || Mathf.Abs((delta.ToAngle() - gunAngle).Clamp180()) > (0.5f * _CONE_SPREAD))
            {
                this._roomBreakables.AddLast(node);
                continue;
            }
            if (!b.explodesOnBreak || b.explosionData == null)
            {
                ExplosionData ed = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData.Clone();
                ed.damageToPlayer = 0f;
                Exploder.Explode(position: bpos, data: ed, sourceNormal: Vector2.zero, ignoreQueues: true);
            }
            else
                b.Break();
            anythingDetonated = true;
        }

        // Nuke the tables
        ReadOnlyCollection<IPlayerInteractable> roomInteractables = room.GetRoomInteractables();
        for (int i = 0; i < roomInteractables.Count; i++)
        {
            if (!room.IsRegistered(roomInteractables[i]))
                continue;
            if (roomInteractables[i] is not FlippableCover flippableCover || flippableCover.IsFlipped || flippableCover.IsGilded)
                continue;
            if (!flippableCover.m_breakable)
                continue;
            flippableCover.m_breakable.ApplyDamage(9999f, Vector2.zero, false, true);
            ExplosionData ed = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData.Clone();
            ed.damageToPlayer = 0f;
            Exploder.Explode(position: (flippableCover.sprite ? flippableCover.sprite.WorldCenter : flippableCover.transform.position),
              data: ed, sourceNormal: Vector2.zero, ignoreQueues: true);
            anythingDetonated = true;
        }

        // Nuke the enemies
        room.SafeGetEnemiesInRoom(ref this._activeEnemiesInRoom);
        for (int j = this._activeEnemiesInRoom.Count - 1; j >= 0; j--)
        {
            AIActor enemy = this._activeEnemiesInRoom[j];
            if (!enemy || enemy.IsSignatureEnemy)
                continue;
            if (enemy.healthHaver is not HealthHaver healthHaver || healthHaver.IsDead || healthHaver.IsBoss)
                continue;
            if (enemy.GetComponent<ExplodeOnDeath>() is not ExplodeOnDeath component || component.immuneToIBombApp)
                continue;

            Vector2 delta = enemy.CenterPosition - barrelPos.XY();
            if (delta.sqrMagnitude > _CONE_SQR_MAG || Mathf.Abs((delta.ToAngle() - gunAngle).Clamp180()) > (0.5f * _CONE_SPREAD))
                continue;

            healthHaver.ApplyDamage(2.1474836E+09f, Vector2.zero, "DetonateTheWorld", CoreDamageTypes.None, DamageCategory.Normal, true);
            anythingDetonated = true;
        }
        return anythingDetonated;
    }

    private GameObject _coneMeshObject = null;
    private Mesh _mesh = null;
    private MeshRenderer _meshRenderer = null;
    private Vector3[] _vertices;
    private const int _CONE_SEGMENTS = 4;
    private const float _CONE_SPREAD = 30f;
    private const float _CONE_START = -0.5f * _CONE_SPREAD;
    private const float _CONE_GAP = _CONE_SPREAD / _CONE_SEGMENTS;
    private const float _CONE_MAG = 12f;
    private const float _CONE_SQR_MAG = _CONE_MAG * _CONE_MAG;
    private void UpdateExplosiveDecor()
    {
        if (!this || this.PlayerOwner is not PlayerController player || player.CurrentGun != this.gun)
            return;

        if (!this._coneMeshObject || !this._mesh || !this._meshRenderer)
        {
            this._coneMeshObject = new GameObject("scotsman_targeting_cone");
            this._coneMeshObject.SetLayerRecursively(LayerMask.NameToLayer("FG_Critical"));

            this._mesh = new Mesh();

            this._vertices  = new Vector3[_CONE_SEGMENTS + 2];
            int[] triangles = new int[3 * _CONE_SEGMENTS];
            for (int i = 0; i < _CONE_SEGMENTS; i++) //NOTE: triangle fan
            {
                triangles[i * 3]     = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            this._mesh.vertices  = this._vertices;
            this._mesh.triangles = triangles;
            this._mesh.uv        = new Vector2[_CONE_SEGMENTS + 2];

            this._coneMeshObject.AddComponent<MeshFilter>().mesh = this._mesh;

            this._meshRenderer = this._coneMeshObject.AddComponent<MeshRenderer>();
            Material mat = this._meshRenderer.material = BraveResources.Load("Global VFX/WhiteMaterial", ".mat") as Material;
            mat.shader = ShaderCache.Acquire("tk2d/BlendVertexColorAlphaTintableTilted");
            mat.SetColor(CwaffVFX._OverrideColorId, new Color(1.0f, 0.0f, 0.0f, 0.15f));
        }

        // rebuild cone mesh
        Vector3 basePos = this._vertices[0] = this.gun.barrelOffset.position;
        float startAngle = player.m_currentGunAngle + _CONE_START;
        for (int i = 0; i <= _CONE_SEGMENTS; ++i)
            this._vertices[i + 1] = basePos + (startAngle + i * _CONE_GAP).ToVector3(_CONE_MAG);
        this._mesh.vertices = this._vertices; // necessary to actually trigger an update for some reason
        this._mesh.RecalculateBounds();
        this._mesh.RecalculateNormals();
    }
}

public class Stickybomb : MonoBehaviour
{
    private const float _DET_TIMER      = 0.6f;
    private const float _BASE_GLOW      = 10f;
    private const float _DET_GLOW       = 100f;
    private const float _FALLBACK_RANGE = 3f; // range we launch if not launched from Scotsman

    private PlayerController _owner;
    private Projectile       _projectile;
    private Scotsman         _scotsman = null;
    private bool             _detonateSequenceStarted;
    private bool             _stuck;
    private Vector2          _startPos;
    private float            _targetDist;
    private Vector2          _stickPoint = Vector2.zero;
    private AIActor          _stuckEnemy = null;
    private bool             _setup = false;

    private void Start()
    {
        this._projectile              = base.GetComponent<Projectile>();
        this._owner                   = _projectile.Owner as PlayerController;
        if (this._owner && this._owner.CurrentGun)
            if (this._scotsman = this._owner.CurrentGun.GetComponent<Scotsman>())
                this._scotsman._extantStickies.Add(this);
        this._detonateSequenceStarted = false;
        this._stuck                   = false;

        if (!this._setup)
        {
            this._startPos   = base.transform.position.XY();
            this._targetDist = _FALLBACK_RANGE;
        }
        StartCoroutine(LockAndLoad());
    }

    public void Setup(Vector2 target)
    {
        this._startPos   = base.transform.position.XY();
        this._targetDist = (target - this._startPos).magnitude;

        this._setup = true;
    }

    private void StickToSurface(Vector2 stickPoint)
    {
        this._projectile.specRigidbody.OnRigidbodyCollision -= this.StickToSurface;
        this._projectile.specRigidbody.OnTileCollision -= this.StickToSurface;
        this._projectile.specRigidbody.CollideWithOthers = false;
        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.collidesWithEnemies = false;

        this._stickPoint = stickPoint;
        this._projectile.specRigidbody.Position = new Position(this._stickPoint);
        this._projectile.specRigidbody.UpdateColliderPositions();
        this._projectile.SetSpeed(0f);

        this._projectile.sprite.HeightOffGround = 10f;
        this._projectile.sprite.UpdateZDepth();

        this._stuck = true;
    }

    private void StickToSurface(CollisionData coll)
    {
        StickToSurface(coll.Contact);
        if (!coll.OtherRigidbody || coll.OtherRigidbody.GetComponent<AIActor>() is not AIActor enemy)
            return;

        this._stuckEnemy = enemy;
        this._stickPoint -= enemy.specRigidbody.transform.position.XY();
        this._projectile.specRigidbody.transform.parent = enemy.specRigidbody.transform;
    }

    private void Update()
    {
        if (!this._stuckEnemy || !this._stuckEnemy.specRigidbody)
            return;

        this._projectile.specRigidbody.Position = new Position(this._stickPoint + this._stuckEnemy.specRigidbody.transform.position.XY());
        this._projectile.specRigidbody.UpdateColliderPositions();
    }

    private IEnumerator LockAndLoad()
    {
        float launchTime = BraveTime.ScaledTimeSinceStartup;
        float originalDamage = this._projectile.baseData.damage;
        this._projectile.sprite.SetGlowiness(glowAmount: _BASE_GLOW, glowColor: Color.red);

        // Phase 1, fire towards target
        this._projectile.shouldRotate = false; // prevent automatic rotation after creation
        float explosionDamage = this._projectile.baseData.damage;
        this._projectile.baseData.damage = 0f;
        this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
        this._projectile.BulletScriptSettings.surviveTileCollisions = true;
        this._projectile.specRigidbody.OnRigidbodyCollision += this.StickToSurface;
        this._projectile.specRigidbody.OnTileCollision += this.StickToSurface;
        while (!this._stuck)
        {
            Vector2 curpos = base.transform.position.XY(); //NOTE: can't use specrigidbody position for first frame of existence as it's not valid
            if ((this._startPos - curpos).magnitude > this._targetDist)
            {
                StickToSurface(curpos);
                break;
            }
            float lifetime = BraveTime.ScaledTimeSinceStartup - launchTime;
            this._projectile.sprite.transform.localRotation = (3000f * Mathf.Sin(lifetime)).EulerZ();
            yield return null;
        }

        // Phase 2, lie in wait
        this._projectile.m_usesNormalMoveRegardless = true; // disable movement modifiers such as Helix Bullets
        this._projectile.damageTypes &= (~CoreDamageTypes.Electric);  // remove electric effect after stopping
        while (!this._detonateSequenceStarted && this._scotsman)  // skip this sequence if not fired from Scotsman
            yield return null;

        // Phase 3, primed for detonation
        for (int i = 0; i < 3; ++i)
        {
            this._projectile.sprite.SetGlowiness(glowAmount: _DET_GLOW, glowColor: Color.red);
            yield return new WaitForSeconds(_DET_TIMER / 6);
            this._projectile.sprite.SetGlowiness(glowAmount: _BASE_GLOW, glowColor: Color.red);
            yield return new WaitForSeconds(_DET_TIMER / 6);
        }

        // Phase 4, explode
        Exploder.Explode(this._projectile.transform.position, Scotsman._ScotsmanExplosion.With(damage: explosionDamage), Vector2.zero, ignoreQueues: true);
        this._projectile.DieInAir(suppressInAirEffects: true);
    }

    public bool Detonate(PlayerController pc)
    {
        if (pc != this._owner)
            return false; // don't launch projectiles that don't belong to us
        if (!this._stuck)
            return false; // don't detonate moving stickies
        this._detonateSequenceStarted = true;
        return true;
    }
}
