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

    private Coroutine _dnaReconstruct = null;
    private List<string> _registeredEnemies = new();
    private int _spawnIndex = -1;
    private string _targetGuid = null;
    private float _curChargeTime = 0.0f;
    private List<GameObject> _fragments = new();

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
        if (!this.gun.IsCharging)
        {
            StopReconstruction();
            return;
        }
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
        yield break;
    }

    public override void OnDropped()
    {
        base.OnDropped();
        StopReconstruction();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        StopReconstruction();
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
    }

    private static GameObject CreateEnemyFragment(string guid, int index, Vector2 position)
    {

        AIActor enemy                 = EnemyDatabase.GetOrLoadByGuid(guid);
        int bestSpriteId              = CwaffToolbox.GetIdForBestIdleAnimation(enemy);
        tk2dSpriteCollectionData coll = enemy.sprite.collection;
        tk2dSpriteDefinition baseDef  = coll.spriteDefinitions[bestSpriteId];
        tk2dSpriteDefinition fragDef  = GetSpriteFragment(
            orig     : baseDef,
            x        : index % _FRAGMENT_EDGE,
            y        : index / _FRAGMENT_EDGE,
            edgeSize : _FRAGMENT_EDGE,
            coll     : coll
            );
        int newSpriteId = coll.spriteDefinitions.Length;
        Array.Resize(ref coll.spriteDefinitions, coll.spriteDefinitions.Length + 1);
        coll.spriteDefinitions[newSpriteId] = fragDef;

        GameObject g = new GameObject();
        tk2dSprite sprite = g.AddComponent<tk2dSprite>();
        sprite.SetSprite(coll, newSpriteId);
        sprite.PlaceAtPositionByAnchor(position, Anchor.MiddleCenter);
        return g;
    }

    private static Dictionary<string, tk2dSpriteDefinition> _FragmentDict = new();
    private static tk2dSpriteDefinition GetSpriteFragment(tk2dSpriteDefinition orig, int x, int y, int edgeSize, tk2dSpriteCollectionData coll)
    {
        string fragmentName = $"{orig.name}_{x}_{y}_{edgeSize}";
        if (_FragmentDict.TryGetValue(fragmentName, out tk2dSpriteDefinition cachedDef))
            return cachedDef;

        // If the x coordinate of the first two UVs match, we're using a rotated sprite
        bool isRotated = (orig.uvs[0].x == orig.uvs[1].x);

        float fragSize     = 1f / (float)edgeSize;
        Vector3 opos       = orig.position0;
        Vector2 newExtents = fragSize * orig.boundsDataExtents;
        Vector2 newgap     = fragSize * (orig.uvs[3] - orig.uvs[0]);

        Vector2[] newUvs;
        if (isRotated) // math gets a little more complicated when individual fragment UVs and positions need to be rotated
        {
            int rotx = y;
            int roty = edgeSize - x - 1;
            Vector2 newmin = orig.uvs[0] + new Vector2(rotx       * newgap.x, roty       * newgap.y);
            Vector2 newmax = orig.uvs[0] + new Vector2((rotx + 1) * newgap.x, (roty + 1) * newgap.y);
            newUvs         = new Vector2[]
                { //NOTE: texture is flipped vertically in memory AND rotated horizontally in the atlas
                  new Vector2(newmin.x, newmax.y),
                  newmin,
                  newmax,
                  new Vector2(newmax.x, newmin.y),
                };
        }
        else
        {
            Vector2 newmin = orig.uvs[0] + new Vector2(x       * newgap.x, y       * newgap.y);
            Vector2 newmax = orig.uvs[0] + new Vector2((x + 1) * newgap.x, (y + 1) * newgap.y);
            newUvs         = new Vector2[]
                { //NOTE: texture is flipped vertically in memory
                  newmin,
                  new Vector2(newmax.x, newmin.y),
                  new Vector2(newmin.x, newmax.y),
                  newmax,
                };
        }

        tk2dSpriteDefinition def = new tk2dSpriteDefinition
        {
            name                       = fragmentName,
            texelSize                  = orig.texelSize,
            flipped                    = orig.flipped,
            physicsEngine              = orig.physicsEngine,
            colliderType               = orig.colliderType,
            collisionLayer             = orig.collisionLayer,
            material                   = orig.material,
            materialInst               = orig.materialInst,
            position0                  = opos + new Vector3(x     * newExtents.x, y       * newExtents.y, 0f),
            position1                  = opos + new Vector3((x+1) * newExtents.x, y       * newExtents.y, 0f),
            position2                  = opos + new Vector3(x     * newExtents.x, (y + 1) * newExtents.y, 0f),
            position3                  = opos + new Vector3((x+1) * newExtents.x, (y + 1) * newExtents.y, 0f),
            boundsDataExtents          = orig.boundsDataExtents,
            boundsDataCenter           = orig.boundsDataCenter,
            untrimmedBoundsDataExtents = orig.untrimmedBoundsDataExtents,
            untrimmedBoundsDataCenter  = orig.untrimmedBoundsDataCenter,
            uvs                        = newUvs,
        };
        _FragmentDict[fragmentName] = def;
        return def;
    }
}
