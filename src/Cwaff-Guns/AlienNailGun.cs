namespace CwaffingTheGungy;


/* TODO:
    - when colliding with an enemy
        - if enemy is killed
            - fragment them into 16 mini sprites
            - siphon fragments into alien nail gun
            - register enemy as killed
    - when reloading with full ammo
        - switch with registered enemy will be spawned
    - when firing charged shot
        - decrease ammo by 10
        - spawn a temporary charmed hologram of an enemy

    - add save serialization stuff
*/

public class AlienNailgun : AdvancedGunBehavior
{
    public static string ItemName         = "Alien Nailgun";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _RECONSTRUCT_DELAY = 0.5f;
    private const float _RECONSTRUCT_TIME  = 2.0f;
    private const int _FRAGMENT_EDGE       = 4;
    private const int _FRAGMENTS           = _FRAGMENT_EDGE * _FRAGMENT_EDGE;
    private const float _FRAGMENT_GAP      = _RECONSTRUCT_TIME / (float)_FRAGMENTS;

    internal static GameActorCharmEffect _Charm = null;
    internal static List<AIActor> _Replicants       = new();

    private Coroutine _dnaReconstruct       = null;
    private List<string> _registeredEnemies = new();
    private int _spawnIndex                 = -1;
    private string _targetGuid              = null;
    private float _curChargeTime            = 0.0f;
    private bool _constructionComplete      = false;
    private List<GameObject> _fragments     = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<AlienNailgun>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.0f, ammo: 480);
            gun.SetAnimationFPS(gun.idleAnimation, 24);
            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("paintball_shoot_sound");
            gun.SetReloadAudio("paintball_reload_sound");
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);

        gun.InitProjectile(GunData.New(clipSize: 16, cooldown: 0.25f, shootStyle: ShootStyle.Charged, damage: 2.0f,
            sprite: "alien_nailgun_projectile", customClip: true, chargeTime: 0.0f, useDummyChargeModule: true)
          );

        _Charm = new(){
            AffectsPlayers   = false,
            AffectsEnemies   = true,
            effectIdentifier = "replicant",
            resistanceType   = 0,
            stackMode        = GameActorEffect.EffectStackingMode.Refresh,
            duration         = 36000f,
            };
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (gun.IsReloading || !manualReload || (gun.ClipShotsRemaining < gun.ClipCapacity))
            return;
        if (player.IsDodgeRolling || !player.AcceptingNonMotionInput)
            return;

        if (this._registeredEnemies.Count == 0)
            return;

        this._spawnIndex = (this._spawnIndex + 1) % this._registeredEnemies.Count;
        SwitchEnemyToSpawn(this._registeredEnemies[this._spawnIndex]);
    }

    private void SwitchEnemyToSpawn(string guid)
    {
        this._targetGuid = guid;
        // throw new NotImplementedException();
    }

    protected override void Update()
    {
        base.Update();
        if (!this.Player)
            return;
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsCharging || !this.Player.IsInCombat)
        {
            StopReconstruction();
            return;
        }
        if (this._constructionComplete)
            return; // idle until we release the charge button
        // this._dnaReconstruct ??= StartCoroutine(ReconstructFromDNA(this._targetGuid ?? Enemies.RedShotgunKin)); //NOTE: not rotated
        this._dnaReconstruct ??= StartCoroutine(ReconstructFromDNA(this._targetGuid ?? Enemies.GunNut)); //NOTE: rotated
        // this._dnaReconstruct ??= StartCoroutine(ReconstructFromDNA(this._targetGuid ?? Enemies.BulletKin)); //NOTE: rotated
    }

    private IEnumerator ReconstructFromDNA(string guid)
    {
        // delay before reconstruction begins
        for (float elapsed = 0f; elapsed < _RECONSTRUCT_DELAY; elapsed += BraveTime.DeltaTime)
            yield return null;

        // begin reconstruction process
        Vector2 position = this.Player.CenterPosition;
        float timer = 0.0f;
        for (int i = 0; i < _FRAGMENTS; timer += BraveTime.DeltaTime)
        {
            while (timer > _FRAGMENT_GAP)
            {
                // ETGModConsole.Log($"creating fragment {i+1} / {_FRAGMENTS}");
                this._fragments.Add(CreateEnemyFragment(guid, i, position)); //TODO: unfinished
                timer -= _FRAGMENT_GAP;
                if ((++i) >= _FRAGMENTS)
                    break;
            }
            yield return null;
        }

        // finish reconstruction process
        this._constructionComplete = true;
        AIActor replicant = AIActor.Spawn(EnemyDatabase.GetOrLoadByGuid(guid), position.ToIntVector2(VectorConversions.Floor), position.GetAbsoluteRoom(), true);
        if (replicant)
        {
            replicant.sprite.PlaceAtPositionByAnchor(position, Anchor.MiddleCenter);
            replicant.specRigidbody.Initialize();
            replicant.IgnoreForRoomClear = true;
            replicant.ApplyEffect(AlienNailgun._Charm);
            replicant.sprite.usesOverrideMaterial = true;
            replicant.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
            _Replicants.Add(replicant);
        }
        foreach (GameObject g in this._fragments)
            g.SafeDestroy();
        this._fragments.Clear();
        yield break;
    }

    //NOTE: makes sure AIActor is set properly on bullet script bullets; should probably be moved to a better location later
    [HarmonyPatch(typeof(AIBulletBank), nameof(AIBulletBank.BulletSpawnedHandler))]
    private class GetRealProjectileOwnerPatch
    {
        public static void Postfix(AIBulletBank __instance, Bullet bullet)
        {
            if (bullet.Parent.GetComponent<Projectile>() is not Projectile p)
                return;
            if (__instance.aiActor is not AIActor actor)
                return;
            p.Owner = actor;
            CheckFromReplicantOwner(p);
        }
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        if (!this.everPickedUpByPlayer)
            StaticReferenceManager.ProjectileAdded += CheckFromReplicantOwner;
        base.OnPickedUpByPlayer(player);
        player.OnRoomClearEvent += DestroyReplicants;
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        player.OnRoomClearEvent -= DestroyReplicants;
        StopReconstruction();
        DestroyReplicants(player);
        base.OnPostDroppedByPlayer(player);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        StopReconstruction();
    }

    private static void CheckFromReplicantOwner(Projectile p)
    {
        if (!p || p.Owner is not AIActor enemy)
            return;
        if (!_Replicants.Contains(enemy))
            return;
        p.StopCollidingWithPlayers();
        p.sprite.usesOverrideMaterial = true;
        p.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
    }

    private void StopReconstruction()
    {
        if (this._dnaReconstruct != null)
        {
            StopCoroutine(this._dnaReconstruct);
            this._dnaReconstruct = null;
            foreach (GameObject g in this._fragments)
                g.SafeDestroy();
            this._fragments.Clear();
        }
        this._curChargeTime = 0.0f;
        this._constructionComplete = false;
    }

    private static GameObject CreateEnemyFragment(string guid, int index, Vector2 position)
    {
        AIActor enemy                 = EnemyDatabase.GetOrLoadByGuid(guid);
        int bestSpriteId              = CwaffToolbox.GetIdForBestIdleAnimation(enemy);
        tk2dSpriteCollectionData coll = enemy.sprite.collection;
        tk2dSpriteDefinition baseDef  = coll.spriteDefinitions[bestSpriteId];
        tk2dSpriteDefinition fragDef  = Lazy.GetSpriteFragment(
            orig     : baseDef,
            x        : index % _FRAGMENT_EDGE,
            y        : index / _FRAGMENT_EDGE,
            edgeSize : _FRAGMENT_EDGE
            );

        string fragName = fragDef.name;
        int newSpriteId;
        if (coll.spriteNameLookupDict == null)
            coll.InitDictionary();
        if (!coll.spriteNameLookupDict.TryGetValue(fragName, out newSpriteId))
        {
            newSpriteId = coll.spriteDefinitions.Length;
            Array.Resize(ref coll.spriteDefinitions, coll.spriteDefinitions.Length + 1);
            coll.spriteDefinitions[newSpriteId] = fragDef;
            coll.spriteNameLookupDict[fragName] = newSpriteId;
        }

        GameObject g = new GameObject();
        tk2dSprite sprite = g.AddComponent<tk2dSprite>();
        sprite.SetSprite(coll, newSpriteId);
        sprite.PlaceAtPositionByAnchor(position, Anchor.MiddleCenter);
        sprite.usesOverrideMaterial = true;
        sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
        return g;
    }

    private static void DestroyReplicants(PlayerController player)
    {
        foreach (AIActor replicant in _Replicants)
        {
            if (!replicant)
                continue;
            replicant.EraseFromExistence();
        }
        _Replicants.Clear();
    }
}
