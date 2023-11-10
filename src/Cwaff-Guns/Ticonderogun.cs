namespace CwaffingTheGungy;

public class Ticonderogun : AdvancedGunBehavior
{
    public static string ItemName         = "Ticonderogun";
    public static string SpriteName       = "ticonderogun";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "A Picture is Worth 1000 Swords";
    public static string LongDescription  = "Creates magic brushstrokes at the cursor while continuously fired. Encircling enemies with brushstrokes damages them, and multiple enemies can be encircled at once for slightly reduced per-enemy damage. (On controller, brushstrokes auto-lock onto nearby enemies.) Increases curse by 2 while in inventory.\n\nA truly bizarre relic from another dimension where the pen is mightier than the gun. It radiates with an aura that gives one the sense that the arcane sketches it's capable of producing are lying dormant inside the relic itself, just waiting for the right user to draw out their power.";

    private const float _BASE_DAMAGE             = 10f;   // base damage of being encircled
    private const float _AMMO_DRAIN_TIME         = 1f;    // how frequently we lose ammo

    private const float _DRAW_FX_RATE            = 0.2f;  // how frequently particles are spawned for extant drawing lines
    private const int   _POINT_CAP               = 40;    // max segment sthat can be on the screen before calling it a bust and starting over
    private const int   _NUM_RUNES               = 3;     // number of runs to spawn when damaging enemies
    private const float _MIN_SEGMENT_DIST        = 0.5f;  // [mouse] minimum distance cursor must travel before drawing a new segment
    private const float _TRACKING_SPEED          = 40f;   // [controller] how quickly the cursor moves
    private const float _MAX_CONTROLLER_DIST     = 12f;   // [controller] how far away we can draw with the control stick at max distance
    private const float _AUTOTARGET_MAX_DELTA    = 20f;   // [controller] the cone of vision we have before we can lock on with controller
    private const float _ENEMY_TRACK_RADIUS      = 4f;    // [controller] the radius around a tracked enemy where we can draw

    internal static GameObject _SparklePrefab    = null;  // prefab for the sparklies
    internal static GameObject _RunePrefab       = null;  // prefab for the runes

    private static float _LastWriteSound         = 0f;    // how long its been since we played the scribbling sound

    private PlayerController _owner              = null;  // player owner of the weapon
    private Vector2? _lastCursorPos              = null;  // position of the pencil cursor last time we updated
    private AIActor _trackedEnemy                = null;  // [controller] the enemy we are currently tracking
    private bool _isCharging                     = false; // whether we are currently charging our weapon
    private float _lastDrawTime                  = 0f;    // the last time that VFX were drawn
    private float _timeCharging                  = 0f;
    private float _lifeElapsed                   = 0f;
    private Vector2 _adjustedAimPoint            = Vector2.zero;
    private Vector2 _targetEnemyPos              = Vector2.zero;
    private Vector2 _cameraPositionAtChargeStart = Vector2.zero;
    private Vector2 _playerPositionAtChargeStart = Vector2.zero;
    private List<Vector2> _extantPoints          = new();
    private List<GameObject> _extantSprites      = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Ticonderogun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: PickupObject.ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 1.0f, ammo: 150);
            gun.SetAnimationFPS(gun.shootAnimation, 1);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);
            gun.SetAnimationFPS(gun.chargeAnimation, 24);
            gun.AddStatToGun(PlayerStats.StatType.Curse, 2f, StatModifier.ModifyMethod.ADDITIVE);
            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 0;
            mod.ammoType            = GameUIAmmoType.AmmoType.BEAM;
            mod.shootStyle          = ProjectileModule.ShootStyle.Charged;
            mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            mod.cooldownTime        = 0.1f;
            mod.numberOfShotsInClip = -1;

        // Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
        //     projectile.AddDefaultAnimation(_BulletSprite);
        //     projectile.transform.parent = gun.barrelOffset;
        //     projectile.gameObject.AddComponent<TranquilizerBehavior>();

        _SparklePrefab = VFX.RegisterVFXObject("PencilSparkles", ResMap.Get("pencil_sparkles"), 12, loops: false, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
        // FPS must be nonzero or sprites don't update properly
        _RunePrefab = VFX.RegisterVFXObject("PencilRunes", ResMap.Get("pencil_runes"), 0.01f, loops: false, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);
        this._owner = player;
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        this._owner = null;
        base.OnPostDroppedByPlayer(player);
    }

    protected override void Update()
    {
        base.Update();
        if (!this._owner || BraveTime.DeltaTime == 0.0f)
            return;

        DrawVFXAtExtantPoints();

        if (!this.gun.IsCharging)
        {
            if (this._isCharging)
                EndCharge();
            return;
        }

        if (this.gun.CurrentAmmo == 0)
            return;

        if (!this._isCharging)
            BeginCharge();
        ContinueCharge();
    }

    private void DrawVFXAtExtantPoints()
    {
        float time = BraveTime.ScaledTimeSinceStartup;
        if (this._lastDrawTime + _DRAW_FX_RATE > time)
            return;
        foreach (Vector2 p in this._extantPoints)
            SpawnManager.SpawnVFX(_SparklePrefab, p, Quaternion.identity, ignoresPools: false);
        this._lastDrawTime = time;
    }

    private void CheckIfEnemiesAreEncircled(Vector2 hullCenter)
    {
        AkSoundEngine.PostEvent("pencil_circle_sound", base.gameObject);
        if (!this._owner)
            return;
        List<AIActor> activeEnemies = this._owner.CurrentRoom?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
        if (activeEnemies == null)
            return; // TODO: not sure why we don't need to do this check elsewhere

        List<AIActor> theEncircled = new();
        foreach (AIActor enemy in activeEnemies)
            if (enemy && enemy.IsHostile(canBeNeutral: true) && enemy.sprite && enemy.sprite.WorldCenter.IsPointInPolygonHull(this._extantPoints))
                theEncircled.Add(enemy);
        if (theEncircled.Count == 0)
            return;  // return early if we haven't actually done anything

        // Compute the damage we need to do base
        float damage = ComputeCircleDamage(hullCenter, theEncircled.Count());
        foreach (AIActor enemy in theEncircled)
            DoEncirclingMagic(enemy, damage);
    }

    private float ComputeCircleDamage(Vector2 hullCenter, int numEncircled)
    {
        float baseDamage = _BASE_DAMAGE * this._owner.stats.GetStatValue(PlayerStats.StatType.Damage);
        switch(numEncircled)
        {
            case 1:  return baseDamage;        // 10 * 1 = 10
            case 2:  return baseDamage * 0.8f; //  8 * 2 = 16
            case 3:  return baseDamage * 0.7f; //  7 * 3 = 21
            case 4:  return baseDamage * 0.6f; //  6 * 4 = 24
            default: return baseDamage * 0.5f; //  5 * n = 5n = 25+
        }
    }

    private float ComputeCircleDamageOld(Vector2 hullCenter)
    {
        if (!this._owner.IsKeyboardAndMouse()) // controller users always get max damage because this weapon is already hard enough to use with controllers
            return Mathf.Ceil(_BASE_DAMAGE * this._owner.stats.GetStatValue(PlayerStats.StatType.Damage));

        float maxSquareDistToCenter = 0f;
        foreach (Vector2 point in this._extantPoints)
            maxSquareDistToCenter = Mathf.Max(maxSquareDistToCenter, (hullCenter - point).sqrMagnitude);
        return Mathf.Ceil(_BASE_DAMAGE * this._owner.stats.GetStatValue(PlayerStats.StatType.Damage) * Mathf.Min(1f, 10f / maxSquareDistToCenter));
    }

    private void DoEncirclingMagic(AIActor enemy, float damage)
    {
        enemy.healthHaver.ApplyDamage(damage, Vector2.zero, ItemName, CoreDamageTypes.Magic, DamageCategory.Normal);
        for (int i = 0; i < _NUM_RUNES; ++i)
        {
            Vector2 offset = Lazy.RandomVector(0.3f);
            Vector2 velocity = 2f * offset.normalized;
            FancyVFX fv = FancyVFX.Spawn(_RunePrefab, position: enemy.sprite.WorldCenter + offset, rotation: Quaternion.identity, velocity: velocity,
                lifetime: 1f, fadeOutTime: 1f, parent: enemy.sprite.transform);
            tk2dSpriteAnimator anim = fv.GetComponent<tk2dSpriteAnimator>();
            int newSpriteId = anim.currentClip.frames[UnityEngine.Random.Range(0, anim.currentClip.frames.Count())].spriteId;
            fv.sprite.SetSprite(newSpriteId);
        }
    }

    private void ResetCharge()
    {
        foreach(GameObject g in this._extantSprites)
            UnityEngine.GameObject.Destroy(g);
        this._extantSprites.Clear();
        this._extantPoints.Clear();
    }

    private void ResetCharge(Vector2 cursorPos)
    {
        ResetCharge();
        this._extantPoints.Add(cursorPos);
    }

    private void EndCharge()
    {
        ResetCharge();
        this._trackedEnemy = null;
        this._lastCursorPos = null;

        GameManager.Instance.MainCameraController.SetManualControl(false, true);

        this._isCharging = false;
    }

    private void BeginCharge()
    {
        ResetCharge();
        this._trackedEnemy = null;
        this._lastCursorPos = null;

        // Override camera position
        this._cameraPositionAtChargeStart = GameManager.Instance.MainCameraController.previousBasePosition;
        GameManager.Instance.MainCameraController.SetManualControl(true, true);
        GameManager.Instance.MainCameraController.OverridePosition = this._cameraPositionAtChargeStart;

        this._playerPositionAtChargeStart = this.Owner.sprite.WorldCenter;
        this._adjustedAimPoint = this._playerPositionAtChargeStart + (this.Owner as PlayerController).m_currentGunAngle.ToVector(1f);

        this._isCharging = true;
    }

    // Draw a nice tiled sprite from start to target
    public static GameObject FancyLine(Vector2 start, Vector2 target, float width, int? spriteId = null)
    {
        Vector2 delta         = target - start;
        Quaternion rot        = delta.EulerZ();
        GameObject reticle    = UnityEngine.Object.Instantiate(new GameObject(), start, rot);
        tk2dSlicedSprite quad = reticle.AddComponent<tk2dSlicedSprite>();
        quad.SetSprite(VFX.SpriteCollection, spriteId ?? VFX.sprites["fancy_line"]);
        quad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude, width));
        quad.transform.localRotation = rot;
        quad.transform.position      = start + (0.5f * width * delta.normalized.Rotate(-90f));
        return reticle;
    }

    // Choose the enemy with the smallest angle from our aim point that is also within _MAX_CONTROLLER_DIST
    private AIActor ChooseNewTarget()
    {
        List<AIActor> activeEnemies = this._owner.CurrentRoom?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
        if (activeEnemies == null)
            return null;

        float aimAngle = this._owner.m_currentGunAngle;
        float minDelta = _AUTOTARGET_MAX_DELTA;
        AIActor bestEnemy = null;
        foreach (AIActor enemy in activeEnemies)
        {
            if (enemy?.sprite == null || !enemy.IsHostile(canBeNeutral: true))
                continue;
            Vector2 enemyDelta = (enemy.sprite.WorldCenter - this._owner.sprite.WorldCenter);
            if (enemyDelta.magnitude > _MAX_CONTROLLER_DIST)
                continue;
            float enemyAngle = enemyDelta.ToAngle();
            float deltaAngle = aimAngle.AbsAngleTo(enemyAngle);
            if (deltaAngle < minDelta)
            {
                minDelta = deltaAngle;
                bestEnemy = enemy;
            }
        }

        return bestEnemy;
    }

    private Vector2 GetControllerTrackingVector()
    {
        // If we're using a mouse, just use the cursor position, easy
        if (this._owner.IsKeyboardAndMouse())
            return this._owner.unadjustedAimPoint.XY();

        // If we're using a controller, determine if we should be tracking a specific enemy
        bool restartCharge = false;
        if (!(this._trackedEnemy?.IsHostile(canBeNeutral: true) ?? false))
        {
            this._trackedEnemy = ChooseNewTarget();
            if (this._trackedEnemy)
                restartCharge = true; // if we just chose a new target, we need to restart our charge
        }

        // If we're tracking an enemy, set our target based on the enemy's position and our cursor direction
        if (this._trackedEnemy?.sprite != null)
        {
            Vector2 target = this._trackedEnemy.sprite.WorldCenter + _ENEMY_TRACK_RADIUS * this._owner.m_activeActions.Aim.Vector;
            if (restartCharge)
                ResetCharge(target);
            this._adjustedAimPoint = target; // set our adjusted aim point for when we stop tracking the enemy
            return target; // Autotracked input
        }

        // If we're not tracking an enemy, we're just freehanding input
        this._adjustedAimPoint += this._owner.m_activeActions.Aim.Vector * _TRACKING_SPEED * BraveTime.DeltaTime;
        Vector2 delta = this._adjustedAimPoint - this._owner.sprite.WorldCenter;
        if (delta.magnitude > _MAX_CONTROLLER_DIST)
            this._adjustedAimPoint = this._owner.sprite.WorldCenter + (_MAX_CONTROLLER_DIST * delta.normalized);
        return this._adjustedAimPoint;
    }

    private void ContinueCharge()
    {
        // Decrement ammo and stop charging if necessary
        this._timeCharging += BraveTime.DeltaTime;
        if (this._timeCharging > _AMMO_DRAIN_TIME)
        {
            this._timeCharging -= _AMMO_DRAIN_TIME;
            this.gun.LoseAmmo(1);
            if (this.gun.CurrentAmmo == 0)
            {
                EndCharge();
                return;
            }
        }

        // If our input has been taken from us (e.g., during a boss cutscene), we need to stop charging
        if (this._owner.CurrentInputState != PlayerInputState.AllInput)
        {
            EndCharge();
            return;
        }

        // Figure out if we should add a new point to our list
        Vector2 playerPos = this._owner.sprite.WorldCenter;
        Vector2 pencilPos = GetControllerTrackingVector();

        // Stabilize the camera while we're using this weapon on keyboard and mouse
        bool usingMouse = this._owner.IsKeyboardAndMouse();
        GameManager.Instance.MainCameraController.SetManualControl(usingMouse, true);
        if (usingMouse)
            GameManager.Instance.MainCameraController.OverridePosition =
                this._cameraPositionAtChargeStart + (this.Owner.sprite.WorldCenter - this._playerPositionAtChargeStart);

        // Don't draw or update anything if we've barely moved the cursor
        if (this._lastCursorPos.HasValue && (pencilPos - this._lastCursorPos.Value).magnitude < _MIN_SEGMENT_DIST)
            return;

        // Play a scribbling sound if enough time has elapsed
        if (_LastWriteSound + 0.1f < BraveTime.ScaledTimeSinceStartup)
        {
            _LastWriteSound = BraveTime.ScaledTimeSinceStartup;
            AkSoundEngine.PostEvent("pencil_write_stop", base.gameObject);
            AkSoundEngine.PostEvent("pencil_write", base.gameObject);
        }

        // Add the point and register the last cursor position
        if (this._lastCursorPos.HasValue)
            this._extantSprites.Add(FancyLine(this._lastCursorPos.Value, pencilPos, 0.3f));
        this._lastCursorPos = pencilPos;
        this._extantPoints.Add(pencilPos);

        // Play some nice VFX
        SpawnManager.SpawnVFX(_SparklePrefab, pencilPos, Quaternion.identity, ignoresPools: false);

        // If we have too many points, remove everything and start over
        if (this._extantPoints.Count >= _POINT_CAP)
        {
            ResetCharge(pencilPos);
            return;
        }

        // We're done if we don't have enough points to make a remotely circular shape
        if (this._extantPoints.Count < 5)
            return;
        // Get the convex hull of the current point list
        List<Vector2> hull = this._extantPoints.GetConvexHull();
        // Get the centroid of that hull
        Vector2 hullCentroid = hull.GetCentroid(); // hull.HullCenter();
        // Get the angle from the center of the hull to our starting point
        float startAngle = (this._extantPoints[0] - hullCentroid).ToAngle();
        // Get the angle from the center of the hull to our latest point
        float endAngle = (pencilPos - hullCentroid).ToAngle();
        // Determine if we've made a full circle-y shape
        if (endAngle.IsNearAngle(startAngle, 30f))
        {
            CheckIfEnemiesAreEncircled(hullCentroid);
            ResetCharge(pencilPos);
        }
    }
}

/* References:
    https://stackoverflow.com/a/46371357 // convex hull
    https://stackoverflow.com/a/57624683 // point in polygon
    https://stackoverflow.com/a/19750258 // centroid calculation
*/
public static class HullHelper
{

    public static double Cross(Vector2 O, Vector2 A, Vector2 B)
    {
        return (A.x - O.x) * (B.y - O.y) - (A.y - O.y) * (B.x - O.x);
    }

    public static List<Vector2> GetConvexHull(this List<Vector2> points)
    {
        if ((points?.Count() ?? 0) <= 1)
            return points;

        points = new List<Vector2>(points);  // make a copy so we don't modify in place

        int n = points.Count(), k = 0;
        List<Vector2> H = new List<Vector2>(new Vector2[2 * n]);

        points.Sort((a, b) =>
             a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

        // Build lower hull
        for (int i = 0; i < n; ++i)
        {
            while (k >= 2 && Cross(H[k - 2], H[k - 1], points[i]) <= 0)
                k--;
            H[k++] = points[i];
        }

        // Build upper hull
        for (int i = n - 2, t = k + 1; i >= 0; i--)
        {
            while (k >= t && Cross(H[k - 2], H[k - 1], points[i]) <= 0)
                k--;
            H[k++] = points[i];
        }

        return H.Take(k - 1).ToList();
    }

    public static bool IsPointInPolygon(this Vector2 point, List<Vector2> polygon)
    {
        if (polygon.Count < 3)
            return false;

        List<int> intersects = new List<int>();
        Vector2 a = polygon.Last();
        foreach (Vector2 b in polygon)
        {
            if (b.x == point.x && b.y == point.y)
                return true;

            if (b.x == a.x && point.x == a.x && point.x >= Math.Min(a.y, b.y) && point.y <= Math.Max(a.y, b.y))
                return true;

            if (b.y == a.y && point.y == a.y && point.x >= Math.Min(a.x, b.x) && point.x <= Math.Max(a.x, b.x))
                return true;

            if ((b.y < point.y && a.y >= point.y) || (a.y < point.y && b.y >= point.y))
            {
                int px = (int)(b.x + 1.0 * (point.y - b.y) / (a.y - b.y) * (a.x - b.x));
                intersects.Add(px);
            }

            a = b;
        }

        intersects.Sort();
        return intersects.IndexOf((int)point.x) % 2 == 0 || intersects.Count(x => x < point.x) % 2 == 1;
    }

    public static bool IsPointInPolygonHull(this Vector2 point, List<Vector2> polygonHull)
    {
        return point.IsPointInPolygon(polygonHull.GetConvexHull());
    }

    public static Vector2 GetCentroid(this List<Vector2> poly)
    {
        float accumulatedArea = 0.0f;
        float centerX = 0.0f;
        float centerY = 0.0f;

        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
           float temp = poly[i].x * poly[j].y - poly[j].x * poly[i].y;
           accumulatedArea += temp;
           centerX += (poly[i].x + poly[j].x) * temp;
           centerY += (poly[i].y + poly[j].y) * temp;
        }

        if (Math.Abs(accumulatedArea) < 1E-7f)
           return Vector2.zero;  // Avoid division by zero

        accumulatedArea *= 3f;
        return new Vector2(centerX / accumulatedArea, centerY / accumulatedArea);
    }
}
