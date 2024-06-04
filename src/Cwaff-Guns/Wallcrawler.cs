namespace CwaffingTheGungy;

public class Wallcrawler : CwaffGun
{
    public static string ItemName         = "Wallcrawler";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _WallCrawlerPrefab = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Wallcrawler>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4);

        _WallCrawlerPrefab = VFX.Create("wallcrawler", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 5f);
        SpeculativeRigidbody body = _WallCrawlerPrefab.AutoRigidBody(anchor: Anchor.MiddleCenter, clayer: CollisionLayer.Projectile);
            body.CollideWithOthers = false;
        _WallCrawlerPrefab.AddComponent<Crawlyboi>();

        ETGModConsole.Commands.AddGroup("ww", delegate (string[] args)
        {
            RoomHandler room = GameManager.Instance.PrimaryPlayer.CurrentRoom;
            if (room == null)
                return;

            // get the cell under the player
            IntVector2 playerPos = GameManager.Instance.PrimaryPlayer.specRigidbody.UnitBottomCenter.ToIntVector2(VectorConversions.Floor);
            Dungeon d = GameManager.Instance.Dungeon;
            if (!d.data.CheckInBoundsAndValid(playerPos))
            {
                ETGModConsole.Log($"out of bounds!");
                return;
            }
            // ETGModConsole.Log($"current cell at {playerPos} is {CellType(d.data[playerPos])}");
            // ETGModConsole.Log($"left    cell at {playerPos + IntVector2.Left} is {CellType(d.data[playerPos + IntVector2.Left])}");
            // ETGModConsole.Log($"right   cell at {playerPos + IntVector2.Right} is {CellType(d.data[playerPos + IntVector2.Right])}");
            // ETGModConsole.Log($"up      cell at {playerPos + IntVector2.Up} is {CellType(d.data[playerPos + IntVector2.Up])}");
            // ETGModConsole.Log($"down    cell at {playerPos + IntVector2.Down} is {CellType(d.data[playerPos + IntVector2.Down])}");

            UnityEngine.Object.Instantiate(_WallCrawlerPrefab, GameManager.Instance.PrimaryPlayer.CenterPosition, Quaternion.identity);
        });
    }

    private static string CellType(IntVector2 pos)
    {
        return GameManager.Instance.Dungeon.data[pos].type switch {
            Dungeonator.CellType.WALL  => "wall",
            Dungeonator.CellType.PIT   => "pit",
            Dungeonator.CellType.FLOOR => "floor",
            _                          => "???",
            };
    }
}


public class Crawlyboi : MonoBehaviour
{
    private const float _SPEED = 10f;

    private SpeculativeRigidbody _body;
    private tk2dSprite _sprite;
    private Vector2 _velocity;
    private Vector2 _wallNormal;
    private bool _hitWall = false;
    // private IntVector2 _cellIndex;

    private void Start()
    {
        ETGModConsole.Log($"spawned at {base.transform.position}");
        this._body = base.GetComponent<SpeculativeRigidbody>();
        this._sprite = base.GetComponent<tk2dSprite>();
            this._sprite.HeightOffGround = 3f;
            this._sprite.UpdateZDepth();

        this._wallNormal = Vector2.zero;
        this._body.OnTileCollision += this.OnTileCollision;
        this._body.OnPostRigidbodyMovement += this.OnPostRigidbodyMovement;
        this._velocity = GameManager.Instance.PrimaryPlayer.CurrentGun.gunAngle.Clamp360().Quantize(90f, VectorConversions.Round).ToVector();
        this._body.Velocity = _SPEED * this._velocity;
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
        Vector2 oldNormal = this._wallNormal;
        if (oldNormal == Vector2.zero)
            oldNormal = tileCollision.Normal.Rotate(90f);
        this._wallNormal = tileCollision.Normal;
        this._velocity = oldNormal.normalized;
        PhysicsEngine.PostSliceVelocity = _SPEED * this._velocity;
        this._hitWall = true;
    }

    private void OnPostRigidbodyMovement(SpeculativeRigidbody specRigidbody, Vector2 unitDelta, IntVector2 pixelDelta)
    {
        if (!this._hitWall)
            return; // don't do anything until we've hit a wall once
        if (this._body.IsAgainstWall(-this._wallNormal.ToIntVector2()))
            return; // if we're already up against a wall, no adjustments are needed
        // move slightly towards the direction we're supposed to be going
        Vector2 newVelocity = -this._wallNormal.normalized;
        this._body.transform.position += (C.PIXEL_SIZE * newVelocity).ToVector3ZUp(0f);
        this._body.Reinitialize();
        // snap to the wall in the opposite direction we've overshot from
        int pixelsAdjusted = this._body.PushAgainstWalls(-this._velocity.normalized.ToIntVector2());
        // set the new normal to our current velocity
        this._wallNormal = this._velocity.normalized;
        // set the new velocity
        this._velocity = newVelocity;
        this._body.Velocity = _SPEED * this._velocity;
    }
}

/* wall crawling logic
    - check next position in our current movement direction
    - if we run into a wall
        - snap to the wall
        - update our current wall
        - continue along that wall
        - go to top
    - if we run into an obstacle that is not a wall
        - snap to obstacle while still hugging wall
        - reverse direction
        - go to top
    - if our old wall is no longer next to us
        - if another wall is next to us
            - update our current wall
            - go to top
        - snap diagonally to outer corner of wall
        - update our current wall
        - continue along that wall
        - go to top
    - if our current wall is still next to us
        - move to next position
        - continue
*/
