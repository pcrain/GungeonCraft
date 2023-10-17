using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class Ticonderogun : AdvancedGunBehavior
    {
        public static string ItemName         = "Ticonderogun";
        public static string SpriteName       = "ticonderogun";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Cursed Canvas";
        public static string LongDescription  = "TBD\n\n+2 Curse";

        private const float _MIN_SEGMENT_DIST    = 0.5f;
        private const float _DRAW_RATE           = 0.2f;
        private const int   _POINT_CAP           = 100;//40;
        private const float _BASE_DAMAGE         = 10f;
        private const float _AMMO_DRAIN_TIME     = 1f; // how frequently we lose ammo

        private static GameObject _VFXPrefab     = null;
        private static GameObject _RunePrefab    = null;
        private static float _LastWriteSound     = 0f;

        private Vector2? _lastCursorPos          = null;
        private List<Vector2> _extantPoints      = new();
        private List<GameObject> _extantSprites  = new();
        private bool _isCharging                 = false;
        private float _lastDrawTime              = 0f;
        private float _maxDistanceFromFirstPoint = 0f;
        private float _timeCharging              = 0f;

        private Vector2 _cameraPositionAtChargeStart;
        private Vector2 _playerPositionAtChargeStart;

        public static float TrackingSpeed = 5f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<Ticonderogun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.reloadTime                        = 1.2f;
                gun.quality                           = PickupObject.ItemQuality.A;
                gun.SetBaseMaxAmmo(150);
                gun.SetAnimationFPS(gun.shootAnimation, 1);
                gun.SetAnimationFPS(gun.reloadAnimation, 1);
                gun.SetAnimationFPS(gun.chargeAnimation, 24);
                gun.ClearDefaultAudio();
                gun.AddStatToGun(PlayerStats.StatType.Curse, 2f, StatModifier.ModifyMethod.ADDITIVE);

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

            _VFXPrefab = VFX.RegisterVFXObject("PencilSparkles", ResMap.Get("pencil_sparkles"), 12, loops: false, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
            // FPS must be nonzero or sprites don't update properly
            _RunePrefab = VFX.RegisterVFXObject("PencilRunes", ResMap.Get("pencil_runes"), 0.01f, loops: false, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
        }

        protected override void Update()
        {
            base.Update();
            if (BraveTime.DeltaTime == 0.0f)
                return;
            if (this.Owner is not PlayerController pc)
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
            if (this._lastDrawTime + _DRAW_RATE > time)
                return;
            foreach (Vector2 p in this._extantPoints)
                SpawnManager.SpawnVFX(_VFXPrefab, p, Quaternion.identity, ignoresPools: false);
            this._lastDrawTime = time;
        }

        private void BeginCharge()
        {
            this._targetEnemy = null;
            this._maxDistanceFromFirstPoint = 0f;
            this._lastCursorPos = null;
            this._extantPoints.Clear();
            foreach(GameObject g in this._extantSprites)
                UnityEngine.GameObject.Destroy(g);
            this._extantSprites.Clear();

            this._cameraPositionAtChargeStart = GameManager.Instance.MainCameraController.previousBasePosition;
            this._playerPositionAtChargeStart = this.Owner.sprite.WorldCenter;
            this.adjustedAimPoint = this._playerPositionAtChargeStart + (this.Owner as PlayerController).m_currentGunAngle.ToVector(1f);

            this._isCharging = true;
        }

        private void CheckIfEnemiesAreEncircled(Vector2 hullCenter)
        {
            AkSoundEngine.PostEvent("pencil_circle_sound", base.gameObject);
            if (this.Owner is not PlayerController pc)
                return;
            List<AIActor> activeEnemies = pc?.CurrentRoom?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null)
                return; // TODO: not sure why we don't need to do this check elsewhere

            List<AIActor> theEncircled = new();
            foreach (AIActor enemy in activeEnemies)
                if (enemy && enemy.IsHostile(canBeNeutral: true) && enemy.sprite && enemy.sprite.WorldCenter.IsPointInPolygonHull(this._extantPoints))
                    theEncircled.Add(enemy);
            if (theEncircled.Count == 0)
                return;  // return early if we haven't actually done anything

            // Compute the damage we need to do base
            float damage = ComputeCircleDamage(hullCenter);
            foreach (AIActor enemy in theEncircled)
                DoEncirclingMagic(enemy, damage);
        }

        private float ComputeCircleDamage(Vector2 hullCenter)
        {
            PlayerController pc = this.Owner as PlayerController;
            float maxSquareDistToCenter = 0f;
            foreach (Vector2 point in this._extantPoints)
                maxSquareDistToCenter = Mathf.Max(maxSquareDistToCenter, (hullCenter - point).sqrMagnitude);
            return Mathf.Ceil(_BASE_DAMAGE * pc.stats.GetStatValue(PlayerStats.StatType.Damage) * Mathf.Min(1f, 10f / maxSquareDistToCenter));
        }

        private void DoEncirclingMagic(AIActor enemy, float damage)
        {
            enemy.healthHaver.ApplyDamage(damage, Vector2.zero, ItemName, CoreDamageTypes.Magic, DamageCategory.Normal);
            for (int i = 0; i < 3; ++i)
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

        private void RestartCharge(Vector2 cursorPos)
        {
            this._maxDistanceFromFirstPoint = 0f;
            // this._lastCursorPos = null;
            this._extantPoints.Clear();
            this._extantPoints.Add(cursorPos);
            foreach(GameObject g in this._extantSprites)
                UnityEngine.GameObject.Destroy(g);
            this._extantSprites.Clear();
        }

        private void EndCharge()
        {
            this._targetEnemy = null;
            this._maxDistanceFromFirstPoint = 0f;
            this._lastCursorPos = null;
            this._extantPoints.Clear();
            foreach(GameObject g in this._extantSprites)
                UnityEngine.GameObject.Destroy(g);
            this._extantSprites.Clear();

            GameManager.Instance.MainCameraController.SetManualControl(false, true);

            this._isCharging = false;
        }

        // Draw a nice tiled sprite from start to target
        public static GameObject FancyLine(Vector2 start, Vector2 target, float width)
        {
            Vector2 delta         = target - start;
            Quaternion rot        = delta.EulerZ();
            GameObject reticle    = UnityEngine.Object.Instantiate(new GameObject(), start, rot);
            tk2dSlicedSprite quad = reticle.AddComponent<tk2dSlicedSprite>();
            quad.SetSprite(VFX.SpriteCollection, VFX.sprites["fancy_line"]);
            quad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude, width));
            quad.transform.localRotation = rot;
            quad.transform.position      = start + (0.5f * width * delta.normalized.Rotate(-90f));
            return reticle;
        }

        private Vector2 adjustedAimPoint = Vector2.zero;
        private float m_currentTrackingSpeed = 0f;
        private float m_lifeElapsed = 0f;
        private int skipCount = 0;
        private const float _MAX_CONTROLLER_DIST = 12f;
        private const float _AUTOTARGET_MAX_DELTA = 20f;

        private AIActor _targetEnemy = null;
        private Vector2 _targetEnemyPos = Vector2.zero;

        private AIActor ChooseNewTarget()
        {
            if (this.Owner is not PlayerController pc)
                return null;

            List<AIActor> activeEnemies = pc?.CurrentRoom?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null)
                return null;

            float aimAngle = pc.m_currentGunAngle;
            float minDelta = _AUTOTARGET_MAX_DELTA;
            AIActor bestEnemy = null;
            foreach (AIActor enemy in activeEnemies)
            {
                if (enemy?.sprite == null || !enemy.IsHostile(canBeNeutral: true))
                    continue;
                Vector2 enemyDelta = (enemy.sprite.WorldCenter - pc.sprite.WorldCenter);
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
            if (this.Owner is not PlayerController pc)
                return Vector2.zero;
            if (pc.IsKeyboardAndMouse())
                return pc.unadjustedAimPoint.XY();

            // Autocircle input
            bool restartCharge = false;
            if (!(this._targetEnemy?.IsHostile(canBeNeutral: true) ?? false))
                this._targetEnemy = null;
            if (this._targetEnemy == null)
            {
                this._targetEnemy = ChooseNewTarget();
                if (this._targetEnemy)
                {
                    // this._targetEnemyPos = this._targetEnemy.sprite.WorldCenter;
                    restartCharge = true;
                }
            }
            if (this._targetEnemy?.sprite != null)
            {
                Vector2 target = this._targetEnemy.sprite.WorldCenter + 4f * pc.m_activeActions.Aim.Vector;
                // Vector2 target = this._targetEnemyPos + 4f * pc.m_activeActions.Aim.Vector;
                if (restartCharge)
                    RestartCharge(target);
                return target; // Autotracked input
            }

            // return pc.sprite.WorldCenter + 10f * pc.m_activeActions.Aim.Vector; // Raw input

            // Tracked input
            // this.m_currentTrackingSpeed = 20f * Mathf.Lerp(0f, TrackingSpeed, Mathf.Clamp01(this._timeCharging / 3f));
            this.m_currentTrackingSpeed = 40f;
            this.adjustedAimPoint += pc.m_activeActions.Aim.Vector/*.normalized*/ * m_currentTrackingSpeed * BraveTime.DeltaTime;
            Vector2 delta = this.adjustedAimPoint - pc.sprite.WorldCenter;
            if (delta.magnitude > _MAX_CONTROLLER_DIST)
                this.adjustedAimPoint = pc.sprite.WorldCenter + (_MAX_CONTROLLER_DIST * delta.normalized);
            return this.adjustedAimPoint;
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

            // Figure out if we should add a new point to our list
            PlayerController player = this.Owner as PlayerController;
            Vector2 playerPos = player.sprite.WorldCenter;
            Vector2 pencilPos = GetControllerTrackingVector();

            // Stabilize the camera while we're using this weapon
            GameManager.Instance.MainCameraController.SetManualControl(true, true);
            GameManager.Instance.MainCameraController.OverridePosition = /*player.IsKeyboardAndMouse() ?*/
                this._cameraPositionAtChargeStart + (this.Owner.sprite.WorldCenter - this._playerPositionAtChargeStart)
                /*: 0.5f * (this.Owner.sprite.WorldCenter + pencilPos)*/;

            bool shouldDraw = false;

            if (player.IsKeyboardAndMouse())
            {
                if (this._lastCursorPos.HasValue && (pencilPos - this._lastCursorPos.Value).magnitude < _MIN_SEGMENT_DIST)
                    return;
                shouldDraw = true;
            }
            else
            {
                if (!this._lastCursorPos.HasValue)
                    this._lastCursorPos = playerPos; // controller should always set the cursor position
                shouldDraw = true;
                this.skipCount = 0;
            }

            if (!shouldDraw)
            {
                ETGModConsole.Log($"skipping drawing");
                return;
            }

            if (this._lastCursorPos.HasValue)
                this._extantSprites.Add(FancyLine(this._lastCursorPos.Value, pencilPos, 0.3f));

            // Add the point and register the last cursor position
            if (_LastWriteSound + 0.1f < BraveTime.ScaledTimeSinceStartup) // play sounds at most once every 0.1 seconds
            {
                _LastWriteSound = BraveTime.ScaledTimeSinceStartup;
                AkSoundEngine.PostEvent("pencil_write_stop", base.gameObject);
                AkSoundEngine.PostEvent("pencil_write", base.gameObject);
            }

            this._lastCursorPos = pencilPos;
            this._extantPoints.Add(pencilPos);
            if (this._extantPoints.Count >= _POINT_CAP) // too many points, don't want lag
            {
                RestartCharge(pencilPos);
                return;
            }

            // Play some nice VFX
            SpawnManager.SpawnVFX(_VFXPrefab, pencilPos, Quaternion.identity, ignoresPools: false);

            // Update the max distance we've reached from our start point
            float distanceFromStart = (pencilPos - this._extantPoints[0]).magnitude;
            if (distanceFromStart > this._maxDistanceFromFirstPoint)
                this._maxDistanceFromFirstPoint = distanceFromStart;

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
                RestartCharge(pencilPos);
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

}
