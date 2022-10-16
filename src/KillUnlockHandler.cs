using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.Misc;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public static class KillUnlockHandler
    {
        public static void Init()
        {
            CustomActions.OnAnyHealthHaverDie += AnyHealthHaverKilled;

            //Stats incremented when a specific enemy guid is killed
            StatsToIncrementOnEnemyKill = new Dictionary<string, CustomTrackedStats>()
            {
                {"4b992de5b4274168a8878ef9bf7ea36b" ,CustomTrackedStats.BEHOLSTER_KILLS},
                {"8b0dd96e2fe74ec7bebc1bc689c0008a" ,CustomTrackedStats.MINEFLAYER_KILLS},
            };
            //Flags changed when a specific character kills any boss on a specific floor
            CharacterSpecificFloorSpecificUnlocks = new Dictionary<GlobalDungeonData.ValidTilesets, Dictionary<PlayableCharacters, CustomDungeonFlags>>()
            {
                { GlobalDungeonData.ValidTilesets.FORGEGEON, new Dictionary<PlayableCharacters, CustomDungeonFlags>()
                    { //Dragun / Advanced Dragun 
                        { PlayableCharacters.Guide, CustomDungeonFlags.DRAGUN_KILLED_HUNTER },
                        { ETGModCompatibility.ExtendEnum<PlayableCharacters>(Initialisation.GUID, "Shade"), CustomDungeonFlags.DRAGUN_KILLED_SHADE },
                    }},
            };
            //Flags changd when a specific character kills a specific boss
            CharacterSpecificBossSpecificUnlocks = new Dictionary<string, Dictionary<PlayableCharacters, CustomDungeonFlags>>()
            {
                { "7c5d5f09911e49b78ae644d2b50ff3bf", new Dictionary<PlayableCharacters, CustomDungeonFlags>()
                { //Infinilich
                    { PlayableCharacters.Eevee, CustomDungeonFlags.UNLOCKED_MISSINGUNO },
                    { ETGModCompatibility.ExtendEnum<PlayableCharacters>(Initialisation.GUID, "Shade"), CustomDungeonFlags.LICH_BEATEN_SHADE }
                }},
                { "05b8afe0b6cc4fffa9dc6036fa24c8ec", new Dictionary<PlayableCharacters, CustomDungeonFlags>()
                { //Advanced Dragun
                   { PlayableCharacters.Robot, CustomDungeonFlags.ADVDRAGUN_KILLED_ROBOT },
                   { ETGModCompatibility.ExtendEnum<PlayableCharacters>(Initialisation.GUID, "Shade"), CustomDungeonFlags.ADVDRAGUN_KILLED_SHADE },
                }}
            };
            //Flags changed when a specific character beats bossrush
            BossrushUnlocks = new Dictionary<PlayableCharacters, CustomDungeonFlags>()
            {
                {PlayableCharacters.Pilot, CustomDungeonFlags.BOSSRUSH_PILOT},
                {PlayableCharacters.Guide, CustomDungeonFlags.BOSSRUSH_HUNTER},
                {PlayableCharacters.Soldier, CustomDungeonFlags.BOSSRUSH_MARINE},
                {PlayableCharacters.Convict, CustomDungeonFlags.BOSSRUSH_CONVICT},
                {PlayableCharacters.Robot, CustomDungeonFlags.BOSSRUSH_ROBOT},
                {PlayableCharacters.Bullet, CustomDungeonFlags.BOSSRUSH_BULLET},
                {PlayableCharacters.Gunslinger, CustomDungeonFlags.BOSSRUSH_GUNSLINGER},
                {PlayableCharacters.Eevee, CustomDungeonFlags.BOSSRUSH_PARADOX},
                {ETGModCompatibility.ExtendEnum<PlayableCharacters>(Initialisation.GUID, "Shade"), CustomDungeonFlags.BOSSRUSH_SHADE},
            };
            //TURBO MODE
            //Flags changed when any character kills any boss on a specific floor in Turbo Mode
            TurboModeFloorUnlocks = new Dictionary<GlobalDungeonData.ValidTilesets, CustomDungeonFlags>()
            {
                {GlobalDungeonData.ValidTilesets.CASTLEGEON, CustomDungeonFlags.BEATEN_KEEP_TURBO_MODE },
                {GlobalDungeonData.ValidTilesets.MINEGEON, CustomDungeonFlags.BEATEN_MINES_BOSS_TURBO_MODE },
                {GlobalDungeonData.ValidTilesets.CATACOMBGEON, CustomDungeonFlags.BEATEN_HOLLOW_BOSS_TURBO_MODE },
            };
            //Flags changed when any character kills a specific boss in Turbo Mode
            TurboModeSpecificBossUnlocks = new Dictionary<string, CustomDungeonFlags>()
            {

            };

            //ALL JAMMED MODE
            //Flags changed when any character kills any boss on a specific floor in All-Jammed Mode
            AllJammedFloorUnlocks = new Dictionary<GlobalDungeonData.ValidTilesets, CustomDungeonFlags>()
            {
                {GlobalDungeonData.ValidTilesets.CASTLEGEON, CustomDungeonFlags.ALLJAMMED_BEATEN_KEEP },
                {GlobalDungeonData.ValidTilesets.SEWERGEON, CustomDungeonFlags.ALLJAMMED_BEATEN_OUB },
                {GlobalDungeonData.ValidTilesets.GUNGEON, CustomDungeonFlags.ALLJAMMED_BEATEN_PROPER },
                {GlobalDungeonData.ValidTilesets.CATHEDRALGEON, CustomDungeonFlags.ALLJAMMED_BEATEN_ABBEY },
                {GlobalDungeonData.ValidTilesets.MINEGEON, CustomDungeonFlags.ALLJAMMED_BEATEN_MINES },
                {GlobalDungeonData.ValidTilesets.CATACOMBGEON, CustomDungeonFlags.ALLJAMMED_BEATEN_HOLLOW },
                {GlobalDungeonData.ValidTilesets.OFFICEGEON, CustomDungeonFlags.ALLJAMMED_BEATEN_OFFICE },
                {GlobalDungeonData.ValidTilesets.FORGEGEON, CustomDungeonFlags.ALLJAMMED_BEATEN_FORGE },
            };
            //Flags changed when any character kills a specific boss in All-Jammed Mode
            AllJammedSpecificBossUnlocks = new Dictionary<string, CustomDungeonFlags>()
            {
                {"7c5d5f09911e49b78ae644d2b50ff3bf", CustomDungeonFlags.ALLJAMMED_BEATEN_HELL }, //Infinilich
                {"4d164ba3f62648809a4a82c90fc22cae", CustomDungeonFlags.ALLJAMMED_BEATEN_RAT }, //Rat Mech
            };

            //RAINBOW MODE
            //Flags changed when any character kills any boss on a specific floor in Rainbow Mode
            RainbowModeFloorUnlocks = new Dictionary<GlobalDungeonData.ValidTilesets, CustomDungeonFlags>()
            {

            };
            //Flags changed when any character kills a specific boss in Rainbow Mode
            RainbowModeSpecificBossUnlocks = new Dictionary<string, CustomDungeonFlags>()
            {
                {"7c5d5f09911e49b78ae644d2b50ff3bf", CustomDungeonFlags.RAINBOW_KILLED_LICH }, //Infinilich
            };
        }
        public static void AnyHealthHaverKilled(HealthHaver target)
        {
            if (target && target.aiActor && GameManager.Instance.PrimaryPlayer)
            {
                bool anyPlayerOnHalfHeart = false;
                foreach (var item in GameManager.Instance.AllPlayers)
                {
                    if (item && item.healthHaver) anyPlayerOnHalfHeart = ((item.healthHaver.GetCurrentHealth() <= 0f && item.healthHaver.Armor == 1) || (item.healthHaver.GetCurrentHealth() <= 0.5f && item.healthHaver.Armor == 0));
                }

                ETGMod.StartGlobalCoroutine(SaveDeaths(
                    target.aiActor.EnemyGuid,
                    target.aiActor.IsBlackPhantom,
                    target.aiActor.CanTargetEnemies && !target.aiActor.CanTargetPlayers,
                    GameManager.Instance.PrimaryPlayer.characterIdentity,
                    anyPlayerOnHalfHeart));
            }
        }
        public static IEnumerator SaveDeaths(string guid, bool jammed, bool charmed, PlayableCharacters primaryplayerid, bool anyPlayerOnHalfHeart)
        {
            AIActor prefabForGUID = EnemyDatabase.GetOrLoadByGuid(guid);
            GlobalDungeonData.ValidTilesets currentTileset = GameManager.Instance.Dungeon.tileIndices.tilesetId;
            bool allJammed = SaveAPIManager.GetFlag(CustomDungeonFlags.ALLJAMMEDMODE_ENABLED_GENUINE);

            if (charmed) SaveAPIManager.RegisterStatChange(CustomTrackedStats.CHARMED_ENEMIES_KILLED, 1);

            //Specific Enemy Kills
            if (guid == EnemyGuidDatabase.Entries["key_bullet_kin"])
            {
                if (jammed && !SaveAPIManager.GetFlag(CustomDungeonFlags.KILLEDJAMMEDKEYBULLETKIN)) SaveAPIManager.SetFlag(CustomDungeonFlags.KILLEDJAMMEDKEYBULLETKIN, true);
            }
            if (guid == EnemyGuidDatabase.Entries["chance_bullet_kin"])
            {
                if (jammed && !SaveAPIManager.GetFlag(CustomDungeonFlags.KILLEDJAMMEDCHANCEKIN)) SaveAPIManager.SetFlag(CustomDungeonFlags.KILLEDJAMMEDCHANCEKIN, true);
            }

            //Tag Based Unlocks
            if (prefabForGUID.HasTag("titan_bullet_kin")) SaveAPIManager.RegisterStatChange(CustomTrackedStats.TITAN_KIN_KILLED, 1);
            if (prefabForGUID.HasTag("mimic") && jammed && !SaveAPIManager.GetFlag(CustomDungeonFlags.KILLEDJAMMEDMIMIC)) SaveAPIManager.SetFlag(CustomDungeonFlags.KILLEDJAMMEDMIMIC, true);

            //Specific GUID incrementation
            if (StatsToIncrementOnEnemyKill.ContainsKey(guid)) SaveAPIManager.RegisterStatChange(StatsToIncrementOnEnemyKill[guid], 1);

            //Specific GUID, Specific Character Unlocks (IE: Rat, Lich)
            if (GameManager.Instance.CurrentGameMode != GameManager.GameMode.BOSSRUSH)
            {
                if (CharacterSpecificBossSpecificUnlocks.ContainsKey(guid))
                {
                    if (CharacterSpecificBossSpecificUnlocks[guid].ContainsKey(primaryplayerid))
                    {
                        if (!SaveAPIManager.GetFlag(CharacterSpecificBossSpecificUnlocks[guid][primaryplayerid])) SaveAPIManager.SetFlag(CharacterSpecificBossSpecificUnlocks[guid][primaryplayerid], true);
                    }
                }
                //Killing specific boss guids in turbo mode
                if (GameStatsManager.Instance.isTurboMode && TurboModeSpecificBossUnlocks.ContainsKey(guid) && !SaveAPIManager.GetFlag(TurboModeSpecificBossUnlocks[guid])) SaveAPIManager.SetFlag(TurboModeSpecificBossUnlocks[guid], true);
                //Killing specific boss guids in all jammed mode
                if (allJammed && AllJammedSpecificBossUnlocks.ContainsKey(guid) && !SaveAPIManager.GetFlag(AllJammedSpecificBossUnlocks[guid])) SaveAPIManager.SetFlag(AllJammedSpecificBossUnlocks[guid], true);
                //Killing specific boss guids in turbo mode
                if (GameStatsManager.Instance.IsRainbowRun && RainbowModeSpecificBossUnlocks.ContainsKey(guid) && !SaveAPIManager.GetFlag(RainbowModeSpecificBossUnlocks[guid])) SaveAPIManager.SetFlag(RainbowModeSpecificBossUnlocks[guid], true);
            }
           

            //Floor Based Boss Kill Unlocks
            // These are based on beating floor bosses like 'hollow boss' or 'oubliette boss', and not for specific bosses. For Lich, Rat, and Advanced Dragun, see above
            if (prefabForGUID.healthHaver && prefabForGUID.healthHaver.IsBoss && !prefabForGUID.healthHaver.IsSubboss)
            {
                //Mutagen unlock, killing any boss in any mode on half a heart
                if (anyPlayerOnHalfHeart && !SaveAPIManager.GetFlag(CustomDungeonFlags.HAS_BEATEN_BOSS_BY_SKIN_OF_TEETH)) SaveAPIManager.SetFlag(CustomDungeonFlags.HAS_BEATEN_BOSS_BY_SKIN_OF_TEETH, true);
                //Regular unlocks
                if (GameManager.Instance.CurrentGameMode != GameManager.GameMode.BOSSRUSH)
                {
                    if (!GameManager.Instance.InTutorial) //Makes sure Keep unlocks don't trigger on Manuel
                    {
                        //Non Mode specific floor based boss beating unlocks, for beating different floor bosses as specific characters
                        //Mainly used for the Forge, so that beating the Advanced Dragun also counts as beating the Dragun
                        if (CharacterSpecificFloorSpecificUnlocks.ContainsKey(currentTileset))
                        {
                            if (CharacterSpecificFloorSpecificUnlocks[currentTileset].ContainsKey(primaryplayerid))
                            {
                                if (!SaveAPIManager.GetFlag(CharacterSpecificFloorSpecificUnlocks[currentTileset][primaryplayerid])) SaveAPIManager.SetFlag(CharacterSpecificFloorSpecificUnlocks[currentTileset][primaryplayerid], true);
                            }
                        }

                        //Beating floors in turbo mode. Doesn't take into account character or specific bosses like the Lich, only whole floors
                        if (GameStatsManager.Instance.isTurboMode && TurboModeFloorUnlocks.ContainsKey(currentTileset) && !SaveAPIManager.GetFlag(TurboModeFloorUnlocks[currentTileset])) SaveAPIManager.SetFlag(TurboModeFloorUnlocks[currentTileset], true);

                        //Beating floors in all-jammed mode. Doesn't take into account character or specific bosses like the Lich, only whole floors
                        if (allJammed && AllJammedFloorUnlocks.ContainsKey(currentTileset) && !SaveAPIManager.GetFlag(AllJammedFloorUnlocks[currentTileset])) SaveAPIManager.SetFlag(AllJammedFloorUnlocks[currentTileset], true);
                       
                        //Beating floors in rainbow mode. Doesn't take into account character or specific bosses like the Lich, only whole floors
                        if (GameStatsManager.Instance.IsRainbowRun && RainbowModeFloorUnlocks.ContainsKey(currentTileset) && !SaveAPIManager.GetFlag(RainbowModeFloorUnlocks[currentTileset])) SaveAPIManager.SetFlag(RainbowModeFloorUnlocks[currentTileset], true);

                    }

                }
                else if (currentTileset == GlobalDungeonData.ValidTilesets.FORGEGEON)
                { //Trigger Bossrush Unlocks - Only runs if the boss beaten tileset is forge and bossrush is on. Checks for the current character.
                    if (BossrushUnlocks.ContainsKey(primaryplayerid) && !SaveAPIManager.GetFlag(BossrushUnlocks[primaryplayerid])) SaveAPIManager.SetFlag(BossrushUnlocks[primaryplayerid], true);
                }
            }

            yield break;
        }

        public static Dictionary<string, CustomTrackedStats> StatsToIncrementOnEnemyKill;
        public static Dictionary<GlobalDungeonData.ValidTilesets, Dictionary<PlayableCharacters, CustomDungeonFlags>> CharacterSpecificFloorSpecificUnlocks;
        public static Dictionary<string, Dictionary<PlayableCharacters, CustomDungeonFlags>> CharacterSpecificBossSpecificUnlocks;

        //Floor based mode unlock dictionaries
        public static Dictionary<GlobalDungeonData.ValidTilesets, CustomDungeonFlags> TurboModeFloorUnlocks;
        public static Dictionary<GlobalDungeonData.ValidTilesets, CustomDungeonFlags> AllJammedFloorUnlocks;
        public static Dictionary<GlobalDungeonData.ValidTilesets, CustomDungeonFlags> RainbowModeFloorUnlocks;

        //Specific Boss Mode Unlock Dictionaries
        public static Dictionary<string, CustomDungeonFlags> TurboModeSpecificBossUnlocks;
        public static Dictionary<string, CustomDungeonFlags> AllJammedSpecificBossUnlocks;
        public static Dictionary<string, CustomDungeonFlags> RainbowModeSpecificBossUnlocks;

        public static Dictionary<PlayableCharacters, CustomDungeonFlags> BossrushUnlocks;
    }
}
