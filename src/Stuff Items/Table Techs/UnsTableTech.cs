using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Dungeonator;
using ItemAPI;
using System.Collections;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class UnsTableTech : TableFlipItem
    {
        public static void Init()
        {
            string itemName = "Uns-Table Tech";
            string resourceName = "NevernamedsItems/Resources/unstabletech_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<UnsTableTech>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Flip Chaos";
            string longDesc = "Triggers a random effect upon the flipping of a table." + "\n\n\"He who flips without reason may outwit not only his foes, but himself\" - Addendum 8 of the Tabla Sutra.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.C;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnTableFlipped += this.OnTableFlipped;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.OnTableFlipped -= this.OnTableFlipped;
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnTableFlipped -= this.OnTableFlipped;
            }
            base.OnDestroy();
        }
        private void OnTableFlipped(FlippableCover obj)
        {
            if (Owner && obj)
            {
                int selectedEffect = UnityEngine.Random.Range(1, (27 + 1));
                ETGModConsole.Log("Selected Effect: " + selectedEffect);
                switch (selectedEffect)
                {
                    case 1:
                        SpawnFoldingTable();
                        break;
                    case 2:
                        DoSafeExplosion(obj.specRigidbody.UnitCenter);
                        break;
                    case 3:
                        DoGoop(obj.specRigidbody.UnitCenter, EasyGoopDefinitions.FireDef);
                        break;
                    case 4:
                        DoGoop(obj.specRigidbody.UnitCenter, EasyGoopDefinitions.PoisonDef);
                        break;
                    case 5:
                        DoGoop(obj.specRigidbody.UnitCenter, EasyGoopDefinitions.CharmGoopDef);
                        break;
                    case 6:
                        DoGoop(obj.specRigidbody.UnitCenter, EasyGoopDefinitions.CheeseDef);
                        break;
                    case 7:
                        LootEngine.SpawnCurrency(obj.specRigidbody.UnitCenter, UnityEngine.Random.Range(5, 10), false);
                        break;
                    case 8:
                        Owner.DoEasyBlank( obj.specRigidbody.UnitCenter, EasyBlankType.FULL);
                        break;
                    case 9:
                        Owner.DoEasyBlank( obj.specRigidbody.UnitCenter, EasyBlankType.MINI);
                        break;
                    case 10:
                        FullRoomStatusEffect(StaticStatusEffects.charmingRoundsEffect);
                        break;
                    case 11:
                        FullRoomStatusEffect(PickupObjectDatabase.GetById(569).GetComponent<ChaosBulletsItem>().FreezeModifierEffect);
                        break;
                    case 12:
                        FullRoomStatusEffect(StaticStatusEffects.tripleCrossbowSlowEffect);
                        break;
                    case 13:
                        Exploder.DoRadialKnockback(obj.specRigidbody.UnitCenter, 200, 100);
                        break;
                    case 14:
                        SpawnBlackHole(obj.specRigidbody.UnitCenter);
                        break;
                    case 15:
                        if (Owner.CurrentGun != null) Owner.CurrentGun.GainAmmo(UnityEngine.Random.Range(5, 26));
                        break;
                    case 16:
                        FreezeTime();
                        break;
                    case 17:
                        TurnTableIntoRocket(obj);
                        break;
                    case 18:
                        StunEnemies();
                        break;
                    case 19:
                        LootEngine.SpawnCurrency(obj.specRigidbody.UnitCenter, UnityEngine.Random.Range(2, 6), true);
                        break;
                    case 20:
                        CompanionisedEnemyUtility.SpawnCompanionisedEnemy(Owner, "01972dee89fc4404a5c408d50007dad5", Owner.sprite.WorldCenter.ToIntVector2(), false, Color.red, 7, 2, false, true);
                        break;
                    case 21:
                        int numBullat = UnityEngine.Random.Range(2, 6);
                        for (int i = 0; i < numBullat; i++) CompanionisedEnemyUtility.SpawnCompanionisedEnemy(Owner, "2feb50a6a40f4f50982e89fd276f6f15", Owner.sprite.WorldCenter.ToIntVector2(), false, Color.red, 15, 2, false, false);
                        break;
                    case 22:
                        FullRoomStatusEffect(StaticStatusEffects.hotLeadEffect);
                        break;
                    case 23:
                        float degrees = 0;
                        for (int i = 0; i < 15; i++)
                        {
                            SpawnBullets((PickupObjectDatabase.GetById(50) as Gun).DefaultModule.projectiles[0], obj.specRigidbody.UnitCenter, degrees);
                            degrees += 24;
                        }
                        break;
                    case 24:
                        SpawnBullets((PickupObjectDatabase.GetById(372) as Gun).DefaultModule.projectiles[0], Owner.sprite.WorldCenter, Owner.sprite.WorldCenter.GetVectorToNearestEnemy().ToAngle());
                        break;
                    case 25:
                        StartCoroutine(HandleShield(Owner, 7));
                        break;
                    case 26:
                        Owner.StartCoroutine(this.HandleSlowBullets());
                        break;
                    case 27:
                        Owner.GetExtComp().Enrage(4f, false);
                        break;
                }
            }
        }

        private IEnumerator HandleShield(PlayerController user, float duration)
        {
            m_activeDuration = duration;
            m_usedOverrideMaterial = user.sprite.usesOverrideMaterial;
            user.sprite.usesOverrideMaterial = true;
            user.SetOverrideShader(ShaderCache.Acquire("Brave/ItemSpecific/MetalSkinShader"));
            SpeculativeRigidbody specRigidbody = user.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision += MetalSkinReflecter;
            user.healthHaver.IsVulnerable = false;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += BraveTime.DeltaTime;
                user.healthHaver.IsVulnerable = false;
                yield return null;
            }
            if (user)
            {
                user.healthHaver.IsVulnerable = true;
                user.ClearOverrideShader();
                user.sprite.usesOverrideMaterial = this.m_usedOverrideMaterial;
                SpeculativeRigidbody specRigidbody2 = user.specRigidbody;
                specRigidbody2.OnPreRigidbodyCollision -= this.MetalSkinReflecter;
                //IsCurrentlyActive = false;
            }
            if (this)
            {
                AkSoundEngine.PostEvent("Play_OBJ_metalskin_end_01", base.gameObject);
            }
            yield break;
        }
        float m_activeDuration;
        bool m_usedOverrideMaterial;
        private void MetalSkinReflecter(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            Projectile component = otherRigidbody.GetComponent<Projectile>();
            if (component != null && !(component.Owner is PlayerController))
            {
                PassiveReflectItem.ReflectBullet(component, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
                PhysicsEngine.SkipCollision = true;
            }
        }
        private IEnumerator HandleSlowBullets()
        {
            yield return new WaitForEndOfFrame();
            float slowMultiplier = PickupObjectDatabase.GetById(270).GetComponent<IounStoneOrbitalItem>().SlowBulletsMultiplier;
            Projectile.BaseEnemyBulletSpeedMultiplier *= slowMultiplier;
            this.m_slowDurationRemaining = PickupObjectDatabase.GetById(270).GetComponent<IounStoneOrbitalItem>().SlowBulletsDuration;
            while (this.m_slowDurationRemaining > 0f)
            {
                yield return null;
                this.m_slowDurationRemaining -= BraveTime.DeltaTime;
                Projectile.BaseEnemyBulletSpeedMultiplier /= slowMultiplier;
                slowMultiplier = Mathf.Lerp(PickupObjectDatabase.GetById(270).GetComponent<IounStoneOrbitalItem>().SlowBulletsMultiplier, 1f, 1f - this.m_slowDurationRemaining);
                Projectile.BaseEnemyBulletSpeedMultiplier *= slowMultiplier;
            }
            Projectile.BaseEnemyBulletSpeedMultiplier /= slowMultiplier;
            yield break;

        }
        private float m_slowDurationRemaining;
        private void StunEnemies()
        {
            List<AIActor> activeEnemies = Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies != null)
            {
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    if (!activeEnemies[i].healthHaver.IsBoss && activeEnemies[i] && activeEnemies[i].behaviorSpeculator)
                    {
                        activeEnemies[i].ClearPath();
                        activeEnemies[i].behaviorSpeculator.Interrupt();
                        activeEnemies[i].behaviorSpeculator.Stun(this.StunDuration, true);
                    }
                }
            }
        }
        private void FreezeTime()
        {
            var timeSlow = new RadialSlowInterface();
            timeSlow.DoesSepia = false;
            timeSlow.RadialSlowHoldTime = 5f;
            timeSlow.RadialSlowTimeModifier = 0.01f;
            timeSlow.UpdatesForNewEnemies = false;
            timeSlow.DoRadialSlow(Owner.specRigidbody.UnitCenter, Owner.CurrentRoom);
        }
        private void SpawnBullets(Projectile bullet, Vector2 pos, float rot)
        {
            GameObject gameObject = SpawnManager.SpawnProjectile(bullet.gameObject, pos, Quaternion.Euler(0, 0, rot), true);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component != null)
            {
                PierceProjModifier piercing = component.gameObject.GetOrAddComponent<PierceProjModifier>();
                piercing.penetration += 1;
                piercing.penetratesBreakables = true;
                component.Owner = Owner;
                component.Shooter = Owner.specRigidbody;
            }
        }
        private void SpawnBlackHole(Vector2 pos)
        {
            Projectile projectile2 = ((Gun)ETGMod.Databases.Items["black_hole_gun"]).DefaultModule.projectiles[0];
            GameObject gameObject = SpawnManager.SpawnProjectile(projectile2.gameObject, pos, Quaternion.identity, true);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component != null)
            {
                component.Owner = base.Owner;
                component.Shooter = base.Owner.specRigidbody;
                component.baseData.speed = 0;
            }

        }
        private void FullRoomStatusEffect(GameActorEffect effect)
        {
            List<AIActor> activeEnemies = Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies != null)
            {
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    AIActor aiactor = activeEnemies[i];
                    if (aiactor.IsNormalEnemy)
                    {
                        aiactor.gameActor.ApplyEffect(effect, 1f, null);
                    }
                }
            }
        }
        private void DoGoop(Vector2 pos, GoopDefinition def)
        {
            DeadlyDeadlyGoopManager goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(def);
            goop.TimedAddGoopCircle(pos, 7, 0.75f, true);
        }
        private void DoSafeExplosion(Vector2 position)
        {
            ExplosionData data = DataCloners.CopyExplosionData(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData);
            data.ignoreList.Add(Owner.specRigidbody);
            Exploder.Explode(position, data, Vector2.zero);
        }
        private void SpawnFoldingTable()
        {
            Vector2 nearbyPoint = Owner.CenterPosition + (Owner.unadjustedAimPoint.XY() - Owner.CenterPosition).normalized;
            IntVector2? nearestAvailableCell = Owner.CurrentRoom.GetNearestAvailableCell(nearbyPoint, new IntVector2?(IntVector2.One), new CellTypes?(CellTypes.FLOOR), false, null);
            FoldingTableItem component6 = PickupObjectDatabase.GetById(644).GetComponent<FoldingTableItem>();
            GameObject gameObject5 = component6.TableToSpawn.gameObject;
            GameObject gameObject6 = UnityEngine.Object.Instantiate<GameObject>(gameObject5.gameObject, nearestAvailableCell.Value.ToVector2(), Quaternion.identity);
            SpeculativeRigidbody componentInChildren = gameObject6.GetComponentInChildren<SpeculativeRigidbody>();
            FlippableCover component7 = gameObject6.GetComponent<FlippableCover>();
            component7.transform.position.XY().GetAbsoluteRoom().RegisterInteractable(component7);
            component7.ConfigureOnPlacement(component7.transform.position.XY().GetAbsoluteRoom());
            componentInChildren.Initialize();
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(componentInChildren, null, false);
        }
        private void TurnTableIntoRocket(FlippableCover table)
        {
            GameObject gameObject = (GameObject)ResourceCache.Acquire("Global VFX/VFX_Table_Exhaust");
            Vector2 vector = DungeonData.GetIntVector2FromDirection(table.DirectionFlipped).ToVector2();
            float num = BraveMathCollege.Atan2Degrees(vector);
            Vector3 zero = Vector3.zero;
            switch (table.DirectionFlipped)
            {
                case DungeonData.Direction.NORTH:
                    zero = Vector3.zero;
                    break;
                case DungeonData.Direction.EAST:
                    zero = new Vector3(-0.5f, 0.25f, 0f);
                    break;
                case DungeonData.Direction.SOUTH:
                    zero = new Vector3(0f, 0.5f, 1f);
                    break;
                case DungeonData.Direction.WEST:
                    zero = new Vector3(0.5f, 0.25f, 0f);
                    break;
            }
            GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, table.specRigidbody.UnitCenter.ToVector3ZisY(0f) + zero, Quaternion.Euler(0f, 0f, num));
            gameObject2.transform.parent = table.specRigidbody.transform;
            Projectile projectile = table.specRigidbody.gameObject.AddComponent<Projectile>();
            projectile.Shooter = Owner.specRigidbody;
            projectile.Owner = Owner;
            projectile.baseData.damage = PickupObjectDatabase.GetById(398).GetComponent<TableFlipItem>().DirectHitBonusDamage;
            projectile.baseData.range = 1000f;
            projectile.baseData.speed = 20f;
            projectile.baseData.force = 50f;
            projectile.baseData.UsesCustomAccelerationCurve = true;
            projectile.baseData.AccelerationCurve = PickupObjectDatabase.GetById(398).GetComponent<TableFlipItem>().CustomAccelerationCurve;
            projectile.baseData.CustomAccelerationCurveDuration = PickupObjectDatabase.GetById(398).GetComponent<TableFlipItem>().CustomAccelerationCurveDuration;
            projectile.shouldRotate = false;
            projectile.Start();
            projectile.SendInDirection(vector, true, true);
            projectile.collidesWithProjectiles = true;
            projectile.projectileHitHealth = 20;
            Action<Projectile> value = delegate (Projectile p)
            {
                if (table && table.shadowSprite)
                {
                    table.shadowSprite.renderer.enabled = false;
                }
            };
            projectile.OnDestruction += value;
            ExplosiveModifier explosiveModifier = projectile.gameObject.AddComponent<ExplosiveModifier>();
            explosiveModifier.explosionData = PickupObjectDatabase.GetById(398).GetComponent<TableFlipItem>().ProjectileExplosionData;
            table.PreventPitFalls = true;
        }
    }
}
