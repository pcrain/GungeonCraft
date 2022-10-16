using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class TestGun : AdvancedGunBehavior
    {


        public static void Add()
        {

            Gun gun = ETGMod.Databases.Items.NewGun("Testinator", "testinator");

            Game.Items.Rename("outdated_gun_mods:testinator", "nn:testinator");
            var behav = gun.gameObject.AddComponent<TestGun>();
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_GoldenEye_BulletFire";
            gun.SetLongDescription("Made for fun. Probably broken.");

            gun.SetupSprite(null, "wailingmagnum_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.DefaultModule.cooldownTime = 0.4f;
            gun.DefaultModule.numberOfShotsInClip = 500;
            gun.SetBaseMaxAmmo(500);



            gun.DefaultModule.projectiles[0] = GameOfLifeHandler.GOLProjPrefab;


            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
        }


    }
    public class GameOfLifeHandler : MonoBehaviour
    {
        public static Projectile GOLProjPrefab;
        public GameOfLifeHandler()
        {
        }
        public static void Init()
        {
            GOLProjPrefab = (PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0].gameObject.InstantiateAndFakeprefab().GetComponent<Projectile>();
            GOLProjPrefab.baseData.speed = 0f;
            GOLProjPrefab.shouldRotate = false;
            GOLProjPrefab.SetProjectileSpriteRight("gameoflife_projectile", 16, 16, true, tk2dBaseSprite.Anchor.MiddleCenter, 16, 16);
            GOLProjPrefab.gameObject.AddComponent<GameOfLifeProjectile>();
            GOLProjPrefab.specRigidbody.CollideWithTileMap = false;
        }
        private void Start()
        {
            timer = 0.5f;
            generationRunning = false;
            ETGModConsole.Log("GOL Handler Started");
        }
        private void OnDestroy()
        {
            ETGModConsole.Log("GOL Handler ended");

        }

        private void Update()
        {
            if (!generationRunning)
            {
                if (timer >= 0) { timer -= BraveTime.DeltaTime; }
                else
                {
                    ETGMod.StartGlobalCoroutine(HandleGeneration());
                }
            }
        }
        public IEnumerator HandleGeneration()
        {
            generationRunning = true;
            if (GameManager.Instance.Dungeon != null && GameManager.Instance.Dungeon.data != null && !Dungeon.IsGenerating)
            {
                Dictionary<Dungeonator.CellData, int> birthCandidates = new Dictionary<Dungeonator.CellData, int>();

                if (GameManager.Instance.PrimaryPlayer) VFXToolbox.DoStringSquirt("Generation", GameManager.Instance.PrimaryPlayer.CenterPosition, Color.red);
                //1 - Any live cell with fewer than two live neighbours dies, as if by underpopulation.
                //2 - Any live cell with two or three live neighbours lives on to the next generation.
                //3 - Any live cell with more than three live neighbours dies, as if by overpopulation.
                //4 - Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.

                //this is where we start a new generation. The first step is to apply any planned births or deaths from the previous generation
                if (plannedBirths != null && plannedBirths.Count > 0)
                {
                    for (int birth = plannedBirths.Count - 1; birth >= 0; birth--)//Births
                    {
                        BirthCell(plannedBirths[birth]);
                        yield return null;
                    }
                    plannedBirths.Clear();
                }
                if (plannedDeaths != null && plannedDeaths.Count > 0)
                {
                    for (int death = plannedDeaths.Count - 1; death >= 0; death--)//Deaths
                    {
                        KillCell(plannedDeaths[death]);
                        yield return null;
                    }
                    plannedDeaths.Clear();
                }

                //The second step is, after a frame delay, to calculate what births and deaths need to happen in the next generation
                yield return null;
                for (int i = registeredCells.Keys.Count - 1; i >= 0; i--)
                {
                    if (registeredCells.ElementAt(i).Value == null) registeredCells.Remove(registeredCells.ElementAt(i).Key);
                    else
                    {
                        List<Dungeonator.CellData> Neighbors = new List<Dungeonator.CellData>();
                        Neighbors.AddRange(GameManager.Instance.Dungeon.data.GetCellNeighbors(registeredCells.ElementAt(i).Key, true));
                        int numberOfNeighbors = NumberOfLiveCells(Neighbors);

                        //Mark cells for death in the next generation if they have more than 3 neighbors or fewer than 2
                        if (numberOfNeighbors > 3 || numberOfNeighbors < 2) plannedDeaths.Add(registeredCells.ElementAt(i).Key);


                        //We need to register all empty cells and figure out how many neighbors they have to figure out if they should come alive
                        //So we iterate through all alive cells, and add all their dead neighbors to a dictionary that stores how many times they have been checked
                        //Since they are checked by each alive cell, the amount of times they have been checked will tell us how many alive neighbors they have
                        foreach (Dungeonator.CellData potNeighbor in Neighbors)
                        {
                            if (!registeredCells.ContainsKey(potNeighbor))
                            {
                                if (birthCandidates.ContainsKey(potNeighbor)) birthCandidates[potNeighbor]++;
                                else birthCandidates.Add(potNeighbor, 1);
                            }
                            // yield return null;
                        }
                        foreach (Dungeonator.CellData potBirth in birthCandidates.Keys)
                        {
                            if (birthCandidates[potBirth] == 3) plannedBirths.Add(potBirth);
                        }
                    }
                }
            }
            generationRunning = false;
            timer = 0.5f;
            yield break;
        }
        public int NumberOfLiveCells(List<Dungeonator.CellData> cells)
        {
            int num = 0;
            foreach (Dungeonator.CellData data in cells)
            {
                if (registeredCells.ContainsKey(data)) num++;
            }
            return num;
        }
        public void KillCell(Dungeonator.CellData targetCell)
        {
            if (registeredCells.ContainsKey(targetCell))
            {
                VFXToolbox.DoStringSquirt("Cell Killed", targetCell.position.ToVector2() + new Vector2(0.5f, 0.5f), Color.blue);
                registeredCells[targetCell].DieInAir();
                registeredCells.Remove(targetCell);
            }
            else Debug.LogWarning("Tried to kill an unregistered cell, but we caught it.");
        }
        public void BirthCell(Dungeonator.CellData targetCell)
        {
            if (registeredCells.ContainsKey(targetCell)) { Debug.LogWarning("Tried to birth a cell where a cell already lives!"); return; }
            else
            {
                if (!targetCell.IsAnyFaceWall())
                {
                    GameObject gameObject = SpawnManager.SpawnProjectile(GOLProjPrefab.gameObject, targetCell.position.ToVector2() + new Vector2(0.5f, 0.5f), Quaternion.identity, true);
                    gameObject.GetComponent<GameOfLifeProjectile>().beginInactive = false;
                    Projectile component = gameObject.GetComponent<Projectile>();
                    if (component != null && GameManager.Instance.PrimaryPlayer != null)
                    {
                        component.Owner = GameManager.Instance.PrimaryPlayer;
                        component.Shooter = GameManager.Instance.PrimaryPlayer.specRigidbody;
                    }
                    VFXToolbox.DoStringSquirt("Cell Birthed", targetCell.position.ToVector2() + new Vector2(0.5f, 0.5f), Color.blue);

                    registeredCells.Add(targetCell, component);
                }
            }
        }
        private static bool generationRunning = false;
        private static float timer = 0.5f;
        public static List<Dungeonator.CellData> plannedBirths = new List<Dungeonator.CellData>();
        public static List<Dungeonator.CellData> plannedDeaths = new List<Dungeonator.CellData>();
        public static Dictionary<Dungeonator.CellData, Projectile> registeredCells = new Dictionary<Dungeonator.CellData, Projectile>();
        public static Dictionary<Dungeonator.CellData, Projectile> deactivatedCells = new Dictionary<Dungeonator.CellData, Projectile>();
        public class GameOfLifeProjectile : MonoBehaviour
        {
            public GameOfLifeProjectile()
            {
                beginInactive = true;
            }
            private void Start()
            {
                if (beginInactive) isActive = false;
                else isActive = true;
                self = base.GetComponent<Projectile>();
                if (self && self.ProjectilePlayerOwner() && beginInactive)
                {
                    self.AdjustPlayerProjectileTint(Color.grey, 2);
                    self.ProjectilePlayerOwner().OnReloadPressed += ActivateSelf;
                }
                StartCoroutine(PostStart());
            }
            private void ActivateSelf(PlayerController activator, Gun gun)
            {
                if (!isActive)
                {
                    if (initialCell != null)
                    {
                        if (!registeredCells.ContainsKey(initialCell)) registeredCells.Add(initialCell, self);
                        self.AdjustPlayerProjectileTint(Color.white, 1);
                    }
                    else Debug.LogError("Attempted to activate an inactive cell that does not have a set initial cell position");
                    isActive = true;
                }
                if (self && self.ProjectilePlayerOwner()) self.ProjectilePlayerOwner().OnReloadPressed -= ActivateSelf;
            }
            private IEnumerator PostStart()
            {
                yield return null;
                Dungeonator.CellData data = GameManager.Instance.Dungeon.data[((Vector2)self.LastPosition).ToIntVector2(VectorConversions.Floor)];
                Vector2 cellCenter = data.position.ToVector2() + new Vector2(0.5f, 0.5f);
                if (registeredCells.ContainsKey(data) && beginInactive)
                {
                    VFXToolbox.DoStringSquirt("Cell Killed for Dupe Register", data.position.ToVector2() + new Vector2(0.5f, 0.5f), Color.blue);
                    UnityEngine.Object.Destroy(self.gameObject);
                    yield break;
                }
                initialCell = data;
                if (data.IsAnyFaceWall())
                {
                    VFXToolbox.DoStringSquirt("Cell Killed for Wall overlap", data.position.ToVector2() + new Vector2(0.5f, 0.5f), Color.blue);
                    UnityEngine.Object.Destroy(self.gameObject);
                    yield break;
                }

                self.transform.position = cellCenter;
                self.specRigidbody.Reinitialize();


                yield break;
            }
            private void OnDestroy()
            {
                if (self && self.ProjectilePlayerOwner()) self.ProjectilePlayerOwner().OnReloadPressed -= ActivateSelf;
            }
            private Dungeonator.CellData initialCell;
            private Projectile self;
            public bool beginInactive;
            private bool isActive;
        }
    }
}
