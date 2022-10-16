using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Dungeonator;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    static class JammedChests
    {
        public static void Init()
        {
            CustomActions.OnChestPostSpawn += PostProcessChest;
            CustomActions.OnChestPreOpen += ChestPreOpen;
            CustomActions.OnChestBroken += OnBroken;
        }
        public static void PostProcessChest(Chest self)
        {
            JammedChestBehav jammedMaybe = self.gameObject.GetComponent<JammedChestBehav>();
            PassedOverForJammedChest unjammedMaybe = self.gameObject.GetComponent<PassedOverForJammedChest>();
            if (jammedMaybe == null && unjammedMaybe == null)
            {
                PlayerController TumblerPlayer = null;
                if (GameManager.Instance && GameManager.Instance.PrimaryPlayer && GameManager.Instance.PrimaryPlayer.HasPickupID(CursedTumbler.CursedTumblerID)) TumblerPlayer = GameManager.Instance.PrimaryPlayer;
                else if (GameManager.Instance && GameManager.Instance.SecondaryPlayer && GameManager.Instance.SecondaryPlayer.HasPickupID(CursedTumbler.CursedTumblerID)) TumblerPlayer = GameManager.Instance.SecondaryPlayer;

                if (AllJammedState.AllJammedActive || TumblerPlayer != null)
                {
                    float TumblerProc = 0;
                    float AlljamProc = 0;
                    if (TumblerPlayer)
                    {
                        float localCurse = TumblerPlayer.stats.GetStatValue(PlayerStats.StatType.Curse);
                        TumblerProc = 0.25f;
                        if (localCurse >= 10) TumblerProc = 1f;
                        else if (localCurse >= 9) TumblerProc = 0.9f;
                        else if (localCurse >= 8) TumblerProc = 0.8f;
                        else if (localCurse >= 7) TumblerProc = 0.7f;
                        else if (localCurse >= 6) TumblerProc = 0.6f;
                        else if (localCurse >= 5) TumblerProc = 0.5f;
                        else if (localCurse >= 4) TumblerProc = 0.4f;
                        else if (localCurse >= 3) TumblerProc = 0.35f;
                        else if (localCurse >= 2) TumblerProc = 0.3f;
                    }
                    if (AllJammedState.AllJammedActive)
                    {
                        int globalCurse = PlayerStats.GetTotalCurse();
                        if (globalCurse > 0) AlljamProc = globalCurse * 0.1f;
                        else AlljamProc = 0.1f;
                    }
                    float finalProc = AlljamProc + TumblerProc;
                    if (UnityEngine.Random.value <= finalProc)
                    {
                        JammedChestBehav jammo = self.gameObject.GetOrAddComponent<JammedChestBehav>();
                    }
                    else
                    {
                        self.gameObject.GetOrAddComponent<PassedOverForJammedChest>();
                    }
                }
            }
        }
        public static void ChestPreOpen(Chest self, PlayerController opener)
        {
            JammedChestBehav jamness = self.gameObject.GetComponent<JammedChestBehav>();
            if (jamness != null)
            {
                self.PredictContents(opener);
                if (UnityEngine.Random.value <= 0.5f)
                {
                    List<PickupObject> items = GenerateContents(self.lootTable, self.breakertronLootTable, opener, 0, new System.Random());
                    self.contents.AddRange(items);
                }
                else
                {
                    int lootID = BraveUtility.RandomElement(LootIDs);
                    PickupObject obj = PickupObjectDatabase.GetById(lootID);
                    self.contents.Add(obj);
                }
            }
            if (jamness != null)
            {
                SaveAPIManager.RegisterStatChange(CustomTrackedStats.JAMMED_CHESTS_OPENED, 1);
                LootEngine.SpawnCurrency(self.sprite.WorldCenter, UnityEngine.Random.Range(10, 21), false);
                if (UnityEngine.Random.value <= 0.25f && opener.name != "PlayerShade(Clone)") opener.healthHaver.ApplyDamage(1f, Vector2.zero, "Jammed Chest");
            }
        }
        public static void OnBroken(Chest self)
        {
            if (!self.IsOpen && self.GetComponent<JammedChestBehav>() != null)
            {
                LootEngine.SpawnCurrency(self.sprite.WorldCenter, 10, false);
            }
        }
        private static List<int> LootIDs = new List<int>()
        {
            78, //Ammo
            600, //Spread Ammo
            565, //Glass Guon Stone
            73, //Half Heart
            85, //Heart
            120, //Armor
            224, //Blank
            67, //Key
        };
        private static List<PickupObject> GenerateContents(LootData lootTable, LootData breakertronLootTable, PlayerController player, int tierShift, System.Random safeRandom = null)
        {
            List<PickupObject> list = new List<PickupObject>();
            if (lootTable.lootTable == null)
            {
                list.Add(GameManager.Instance.Dungeon.baseChestContents.SelectByWeight(false).GetComponent<PickupObject>());
            }
            else if (lootTable != null)
            {
                if (tierShift <= -1)
                {
                    if (breakertronLootTable.lootTable != null)
                    {
                        list = breakertronLootTable.GetItemsForPlayer(player, -1, null, safeRandom);
                    }
                    else
                    {
                        list = lootTable.GetItemsForPlayer(player, tierShift, null, safeRandom);
                    }
                }
                else
                {
                    list = lootTable.GetItemsForPlayer(player, tierShift, null, safeRandom);
                    if (lootTable.CompletesSynergy)
                    {
                        float num = Mathf.Clamp01(0.6f - 0.1f * (float)lootTable.LastGenerationNumSynergiesCalculated);
                        num = Mathf.Clamp(num, 0.2f, 1f);
                        if (num > 0f)
                        {
                            float num2 = (safeRandom == null) ? UnityEngine.Random.value : ((float)safeRandom.NextDouble());
                            if (num2 < num)
                            {
                                lootTable.CompletesSynergy = false;
                                list = lootTable.GetItemsForPlayer(player, tierShift, null, safeRandom);
                                lootTable.CompletesSynergy = true;
                            }
                        }
                    }
                }
            }
            return list;
        }
    }
    public class PassedOverForJammedChest : MonoBehaviour { }
    public class JammedChestBehav : MonoBehaviour
    {
        public JammedChestBehav()
        {

        }
        private void Start()
        {
            particleCounter = 0;
            this.m_self = base.GetComponent<Chest>();
            if (m_self != null)
            {
                m_self.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitCutoutUberPhantom");
            }
        }
        private float particleCounter;
        private void Update()
        {
            particleCounter += BraveTime.DeltaTime * 40f;
            if (particleCounter > 1f)
            {
                int num = Mathf.FloorToInt(particleCounter);
                particleCounter %= 1f;
                GlobalSparksDoer.DoRandomParticleBurst(num, m_self.sprite.WorldBottomLeft.ToVector3ZisY(0f), m_self.sprite.WorldTopRight.ToVector3ZisY(0f), Vector3.up, 90f, 0.5f, null, null, null, GlobalSparksDoer.SparksType.BLACK_PHANTOM_SMOKE);
            }
        }
        private Chest m_self;
    }
}
