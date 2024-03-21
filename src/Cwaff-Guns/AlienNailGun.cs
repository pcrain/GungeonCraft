namespace CwaffingTheGungy;

/* TODO:
    - nothing for now :D
*/

public class AlienNailgun : AdvancedGunBehavior
{
    public static string ItemName         = "Alien Nailgun";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Attack, Adapt, Assimilate";
    public static string LongDescription  = "Fires nails that extract DNA when dealing the fatal blow to enemies. Charging the gun while in combat consumes a full clip of ammo to assemble a replicant enemy from DNA. Replicants are invulnerable, have no collision, cannot harm the player with projectiles or contact damage, and dissipate when combat ends. Reloading with a full clip cycles through extracted DNA sequences.";
    public static string Lore             = "Having arrived on Earth en masse in a strange meteorite, this gadget launches fingers that quickly retract on impact to physically scrape data off of recently-deceased life forms. Unbeknownst to Gungeoneers, the meteorite was actually an intergalactic standard wastebin, and this eons-old prototype was discarded after its inventors discovered light-based replicators were cheaper, faster, less convoluted, and ultimately less weird.";

    private const float _RECONSTRUCT_DELAY   = 0.2f;
    private const float _RECONSTRUCT_TIME    = 1.3f;
    private const int   _RECONSTRUCT_COST    = 16;
    private const float _FRAGMENT_SPAWN_TIME = 0.3f;
    private const float _PREVIEW_TIME        = 0.8f;
    private const int _FRAGMENT_EDGE         = 4;
    private const int _FRAGMENTS             = _FRAGMENT_EDGE * _FRAGMENT_EDGE;
    private const float _FRAGMENT_GAP        = _RECONSTRUCT_TIME / (float)_FRAGMENTS;

    internal static GameActorCharmEffect _Charm = null;
    internal static List<AIActor> _Replicants   = new();

    private Coroutine _dnaReconstruct       = null;
    private List<string> _registeredEnemies = new();
    private int _spawnIndex                 = -1;
    private string _targetGuid              = null;
    private float _curChargeTime            = 0.0f;
    private bool _constructionComplete      = false;
    private List<GameObject> _fragments     = new();
    private GameObject _preview             = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<AlienNailgun>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.PISTOL, reloadTime: 0.7f, ammo: 480, idleFps: 24, shootFps: 24, reloadFps: 60,
                loopReloadAt: 0, muzzleFrom: Items.Mailbox, fireAudio: "alien_nailgun_shoot_sound", reloadAudio: "gorgun_eye_activate");
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);

        gun.InitProjectile(GunData.New(clipSize: _RECONSTRUCT_COST, cooldown: 0.14f, shootStyle: ShootStyle.Charged, damage: 2.0f,
            sprite: "alien_nailgun_projectile", customClip: true, chargeTime: 0.0f, useDummyChargeModule: true));

        _Charm = new(){
            AffectsPlayers   = false,
            AffectsEnemies   = true,
            effectIdentifier = "replicant",
            resistanceType   = 0,
            stackMode        = GameActorEffect.EffectStackingMode.Refresh,
            duration         = 36000f,
            };
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.GetComponent<ExtractDNAOnKill>())
            return;
        projectile.AddComponent<ExtractDNAOnKill>().Setup(this);
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

        if (this._preview) // only cycle if the preview is already visiblew, otherwise just show the current selection
            this._spawnIndex = (this._spawnIndex + 1) % this._registeredEnemies.Count;
        SwitchEnemyToSpawn(this._registeredEnemies[this._spawnIndex]);
    }

    private void SwitchEnemyToSpawn(string guid, bool isNew = false)
    {
        if (this.Player is not PlayerController pc)
            return;
        this._targetGuid = guid;

        if (this._preview)
            this._preview.SafeDestroy();

        AIActor actor = EnemyDatabase.GetOrLoadByGuid(guid);
        tk2dSprite sprite = Lazy.SpriteObject(actor.sprite.collection, CwaffToolbox.GetIdForBestIdleAnimation(actor));
            sprite.PlaceAtPositionByAnchor(pc.sprite.WorldTopCenter + new Vector2(0f, 0.5f), Anchor.LowerCenter);
            sprite.MakeHolographic(green: true);

        this._preview = sprite.gameObject;
        this._preview.transform.parent = pc.transform;

        this._preview.ExpireIn(_PREVIEW_TIME);
        if (isNew)
            this._preview.Play("replicant_select_new_sound");
        else
            this._preview.Play("replicant_select_sound");
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
            this.gun.SynchronizeReloadAcrossAllModules();
            return;
        }
        if (this._constructionComplete)
            return; // idle until we release the charge button
        if (string.IsNullOrEmpty(this._targetGuid))
            return; // nothing to construct
        if (this.gun.CurrentAmmo < _RECONSTRUCT_COST)
            return; // can't afford to construct
        this._dnaReconstruct ??= StartCoroutine(ReconstructFromDNA(this._targetGuid));
    }

    private IEnumerator ReconstructFromDNA(string guid)
    {
        // delay before reconstruction begins
        Vector2 position = this.Player.CenterPosition;
        for (float elapsed = 0f; elapsed < _RECONSTRUCT_DELAY; elapsed += BraveTime.DeltaTime)
            yield return null;

        // begin reconstruction process
        float timer = 0.0f;
        for (int i = 0; i < _FRAGMENTS; timer += BraveTime.DeltaTime)
        {
            while (timer > _FRAGMENT_GAP)
            {
                Vector2 startPos = (this.Player && this.Player.CurrentGun)
                    ? this.Player.CurrentGun.barrelOffset.position
                    : position + Lazy.RandomVector(4f);
                GameObject fragment = CreateEnemyFragment(guid, i, position, startPos, _FRAGMENT_SPAWN_TIME);
                this._fragments.Add(fragment);
                fragment.Play("replicant_assemble_sound");
                timer -= _FRAGMENT_GAP;
                if ((++i) >= _FRAGMENTS)
                    break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(_FRAGMENT_SPAWN_TIME);
        if (this.Player)
            this.Player.gameObject.Play("replicant_created_sound");

        // finish reconstruction process
        AIActor replicant = AIActor.Spawn(
            prefabActor     : EnemyDatabase.GetOrLoadByGuid(guid),
            position        : position.ToIntVector2(VectorConversions.Floor),
            source          : position.GetAbsoluteRoom(),
            correctForWalls : true, //NOTE: could possibly be false, Chain Gunners don't have good offsets when spawned like this
            awakenAnimType  : AIActor.AwakenAnimationType.Spawn
            );
        if (replicant)
        {
            replicant.SpawnInInstantly();
            replicant.sprite.PlaceAtPositionByAnchor(position, Anchor.MiddleCenter);
            replicant.specRigidbody.Initialize();
            replicant.specRigidbody.CollideWithOthers = false;
            replicant.specRigidbody.CollideWithTileMap = false;
            replicant.specRigidbody.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile));
            replicant.HitByEnemyBullets = false;
            replicant.IgnoreForRoomClear = true;
            replicant.IsHarmlessEnemy = true;
            replicant.ApplyEffect(AlienNailgun._Charm);
            replicant.sprite.MakeHolographic(green: true);
            if (replicant.GetComponent<SpawnEnemyOnDeath>() is SpawnEnemyOnDeath seod)
                seod.chanceToSpawn = 0.0f; // prevent enemies such as Blobulons from replicating on death
            if (replicant.healthHaver is HealthHaver hh)
                hh.IsVulnerable = false; // can't be harmed
            if (replicant.knockbackDoer is KnockbackDoer kb)
                kb.SetImmobile(true, "replicant"); // can't be knocked back
            if (replicant.CurrentGun is Gun gun)
                gun.sprite.MakeHolographic(green: true);
            for (int i = 0; i < replicant.transform.childCount; ++i)
            {
                Transform child = replicant.transform.GetChild(i);
                if (child.GetComponent<tk2dSprite>() is not tk2dSprite sprite)
                    continue;
                sprite.MakeHolographic(green: true);
            }
            _Replicants.Add(replicant);
        }
        foreach (GameObject g in this._fragments)
            g.SafeDestroy();
        this._fragments.Clear();
        this.gun.LoseAmmo(_RECONSTRUCT_COST);
        this._constructionComplete = true;
        yield break;
    }

    //NOTE: makes sure AIActor is set properly on bullet scripts; should probably factor out CheckFromReplicantOwner() and moved to a better location later
    //NOTE: could be useful for Schrodinger's Gat -> might want to set up an event listener
    //WARNING: doesn't seem to work properly on large Bullats
    [HarmonyPatch(typeof(AIBulletBank), nameof(AIBulletBank.BulletSpawnedHandler))]
    private class GetRealProjectileOwnerPatch
    {
        public static void Postfix(AIBulletBank __instance, Bullet bullet)
        {
            if (__instance.aiActor is not AIActor actor)
                return;
            if ((bullet == null) || (bullet.Parent == null) || bullet.Parent.GetComponent<Projectile>() is not Projectile p)
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
        p.AddComponent<ReplicantProjectile>();
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

    private static GameObject CreateEnemyFragment(string guid, int index, Vector2 targetPosition, Vector2 startPosition, float travelTime, float delay = 0.0f, bool autoDestroy = false)
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

        tk2dSprite sprite = Lazy.SpriteObject(spriteColl: coll, spriteId: newSpriteId);
        sprite.PlaceAtPositionByAnchor(startPosition, Anchor.MiddleCenter);
        sprite.AddComponent<DissipatingSpriteFragment>().Setup(startPosition, targetPosition, travelTime, delay, autoDestroy);
        sprite.MakeHolographic(green: true);
        return sprite.gameObject;
    }

    private static void DestroyReplicants(PlayerController player)
    {
        foreach (AIActor replicant in _Replicants)
        {
            if (!replicant)
                continue;
            Vector2 pos = replicant.CenterPosition;
            for (int i = 0; i < _FRAGMENTS; ++i)
                CreateEnemyFragment(replicant.EnemyGuid, i, pos + Lazy.RandomVector(16f), pos, 0.2f, 0.05f * i, true);
            replicant.EraseFromExistence();
        }
        _Replicants.Clear();
    }

    public void RegisterEnemyDNA(string guid)
    {
        if (this._registeredEnemies.Contains(guid))
            return;
        this._registeredEnemies.Add(guid);
        this._spawnIndex = this._registeredEnemies.Count - 1;
        SwitchEnemyToSpawn(this._registeredEnemies[this._spawnIndex], isNew: true);
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this._registeredEnemies.Count);
        foreach (string enemy in this._registeredEnemies)
            data.Add(enemy);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        int count = (int)data[i++];
        for (int n = 0; n < count; ++n)
            this._registeredEnemies.Add((string)data[i++]);
        if (count > 0)
            this._spawnIndex = 0;
    }
}

/// <summary>Class to temporarily make projectiles replicants without the shader effects bleeding back into the projectile pool</summary>
public class ReplicantProjectile : MonoBehaviour
{
    private Shader _oldShader = null;

    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();
        if (!p || !p.sprite)
            return;
        this._oldShader = p.sprite.renderer.material.shader;
        p.sprite.MakeHolographic(green: true);
        p.OnDestruction += DestroyReplicantProjectile;
    }

    private void DestroyReplicantProjectile(Projectile p)
    {
        p.sprite.usesOverrideMaterial = false;
        p.sprite.renderer.material.shader = this._oldShader;
        this.SafeDestroy();
    }
}

public class ExtractDNAOnKill : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private AlienNailgun _gun;

    public void Setup(AlienNailgun gun)
    {
        this._gun = gun;
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._projectile.OnWillKillEnemy += OnWillKillEnemy;
    }

    private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemyBody)
    {
        if (enemyBody.GetComponent<AIActor>() is not AIActor enemy)
            return;
        if (enemy.IsHostileAndNotABoss(canBeDead: true, canBeNeutral: false))
            this._gun.RegisterEnemyDNA(enemy.EnemyGuid);
    }
}
