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
        public static string ShortDescription = "Creative Juices Flowing";
        public static string LongDescription  = "TBD\n\n+1 Curse";

        // internal static tk2dSpriteAnimationClip _BulletSprite;

        private const float _MIN_SEGMENT_DIST    = 0.5f;
        private const float _DRAW_RATE           = 0.2f;
        private const int   _POINT_CAP           = 40;
        private const float _BASE_DAMAGE         = 10f;

        private static GameObject _VFXPrefab     = null;

        private Vector2? _lastCursorPos          = null;
        private List<Vector2> _extantPoints      = new();
        private List<GameObject> _extantSprites  = new();
        private bool _isCharging                 = false;
        private float _lastDrawTime              = 0f;
        private float _maxDistanceFromFirstPoint = 0f;

        private Vector2 _cameraPositionAtChargeStart;
        private Vector2 _playerPositionAtChargeStart;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<Ticonderogun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.reloadTime                        = 1.2f;
                gun.quality                           = PickupObject.ItemQuality.A;
                gun.SetBaseMaxAmmo(60);
                gun.SetAnimationFPS(gun.shootAnimation, 30);
                gun.SetAnimationFPS(gun.reloadAnimation, 40);
                gun.ClearDefaultAudio();
                gun.SetFireAudio("blowgun_fire_sound");
                gun.SetReloadAudio("blowgun_reload_sound");
                gun.AddStatToGun(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.ammoType            = GameUIAmmoType.AmmoType.BEAM;
                mod.shootStyle          = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime        = 0.1f;
                mod.numberOfShotsInClip = 1;

            // _BulletSprite = AnimateBullet.CreateProjectileAnimation(
            //     ResMap.Get("tranquilizer_projectile").Base(),
            //     12, true, new IntVector2(13, 9),
            //     false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

            // Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            //     projectile.AddDefaultAnimation(_BulletSprite);
            //     projectile.transform.parent = gun.barrelOffset;
            //     projectile.gameObject.AddComponent<TranquilizerBehavior>();
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
            if (!this._isCharging)
                BeginCharge();
            ContinueCharge();
        }

        private void DrawVFXAtExtantPoints()
        {
            _VFXPrefab ??= VFX.animations["MiniPickup"];
            float time = BraveTime.ScaledTimeSinceStartup;
            if (this._lastDrawTime + _DRAW_RATE > time)
                return;
            foreach (Vector2 p in this._extantPoints)
                SpawnManager.SpawnVFX(_VFXPrefab, p, Quaternion.identity, ignoresPools: false);
            this._lastDrawTime = time;
        }

        private void BeginCharge()
        {
            this._maxDistanceFromFirstPoint = 0f;
            this._lastCursorPos = null;
            this._extantPoints.Clear();
            foreach(GameObject g in this._extantSprites)
                UnityEngine.GameObject.Destroy(g);
            this._extantSprites.Clear();

            this._cameraPositionAtChargeStart = GameManager.Instance.MainCameraController.previousBasePosition;
            this._playerPositionAtChargeStart = this.Owner.sprite.WorldCenter;

            this._isCharging = true;
        }

        private void CheckIfEnemiesAreEncircled(Vector2 hullCenter)
        {
            AkSoundEngine.PostEvent("soul_kaliber_drain", base.gameObject);
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
            float maxSquareDistToCenter = 0f;
            foreach (Vector2 point in this._extantPoints)
                maxSquareDistToCenter = Mathf.Max(maxSquareDistToCenter, (hullCenter - point).sqrMagnitude);
            return Mathf.Ceil(_BASE_DAMAGE * Mathf.Min(1f, 10f / maxSquareDistToCenter));
        }

        private void DoEncirclingMagic(AIActor enemy, float damage)
        {
            enemy.healthHaver.ApplyDamage(damage, Vector2.zero, ItemName, CoreDamageTypes.Magic, DamageCategory.Normal);
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
            this._maxDistanceFromFirstPoint = 0f;
            this._lastCursorPos = null;
            this._extantPoints.Clear();
            foreach(GameObject g in this._extantSprites)
                UnityEngine.GameObject.Destroy(g);
            this._extantSprites.Clear();

            GameManager.Instance.MainCameraController.SetManualControl(false, true);

            this._isCharging = false;
        }

        // Creates a napalm-strike-esque danger zone
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

        private void ContinueCharge()
        {
            // Stabilize the camera while we're using this weapon
            GameManager.Instance.MainCameraController.SetManualControl(true, true);
            GameManager.Instance.MainCameraController.OverridePosition =
                this._cameraPositionAtChargeStart + (this.Owner.sprite.WorldCenter - this._playerPositionAtChargeStart);

            // Figure out if we should add a new point to our list
            Vector2 cursorPos = (this.Owner as PlayerController).unadjustedAimPoint.XY();
            if (this._lastCursorPos.HasValue)
            {
                Vector2 delta = (cursorPos - this._lastCursorPos.Value);
                if (delta.magnitude < _MIN_SEGMENT_DIST)
                    return;
                this._extantSprites.Add(FancyLine(this._lastCursorPos.Value, cursorPos, 0.3f));
            }

            // Add the point and register the last cursor position
            this._extantPoints.Add(cursorPos);
            this._lastCursorPos = cursorPos;

            // Play some nice VFX
            SpawnManager.SpawnVFX(_VFXPrefab, cursorPos, Quaternion.identity, ignoresPools: false);

            // Update the max distance we've reached from our start point
            float distanceFromStart = (cursorPos - this._extantPoints[0]).magnitude;
            if (distanceFromStart > this._maxDistanceFromFirstPoint)
                this._maxDistanceFromFirstPoint = distanceFromStart;

            if (this._extantPoints.Count > _POINT_CAP) // too many points, don't want lag
            {
                RestartCharge(cursorPos);
                return;
            }
            if (this._extantPoints.Count < 5) // 5 points is enough for a circle
                return;

            // Get the convex hull of the current point list
            List<Vector2> hull = this._extantPoints.GetConvexHull();
            // Get the centroid of that hull
            Vector2 hullCentroid = hull.GetCentroid(); // hull.HullCenter();
            // Get the angle from the center of the hull to our starting point
            float startAngle = (hull[0] - hullCentroid).ToAngle().Clamp360();
            // Get the angle from the center of the hull to our latest point
            float endAngle = (cursorPos - hullCentroid).ToAngle().Clamp360();
            // Determine if we've made a full lap
            if (endAngle.IsNearAngle(startAngle, 30f))
            {
                CheckIfEnemiesAreEncircled(hullCentroid);
                RestartCharge(cursorPos);
            }
        }
    }

    /* References:
        https://stackoverflow.com/a/46371357 // convex hull
        https://stackoverflow.com/a/57624683 // point in polygon
    */
    public static class HullHelper
    {

        public static double cross(Vector2 O, Vector2 A, Vector2 B)
        {
            return (A.x - O.x) * (B.y - O.y) - (A.y - O.y) * (B.x - O.x);
        }

        public static List<Vector2> GetConvexHull(this List<Vector2> points)
        {
            if ((points?.Count() ?? 0) <= 1)
                return points;

            int n = points.Count(), k = 0;
            List<Vector2> H = new List<Vector2>(new Vector2[2 * n]);

            points.Sort((a, b) =>
                 a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            // Build lower hull
            for (int i = 0; i < n; ++i)
            {
                while (k >= 2 && cross(H[k - 2], H[k - 1], points[i]) <= 0)
                    k--;
                H[k++] = points[i];
            }

            // Build upper hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && cross(H[k - 2], H[k - 1], points[i]) <= 0)
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

        // public static Vector2 HullCenter(this List<Vector2> hull)
        // {
        //     if (hull?.Count == 0)
        //         return Vector2.zero;

        //     float averageX = 0f;
        //     float averageY = 0f;
        //     foreach (Vector2 point in hull)
        //     {
        //         averageX += point.x;
        //         averageY += point.y;
        //     }
        //     return new Vector2(averageX / hull.Count, averageY / hull.Count);
        // }

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
