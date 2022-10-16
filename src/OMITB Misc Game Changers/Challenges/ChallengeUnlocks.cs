using Alexandria.Misc;
using SaveAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    class ChallengeUnlocks
    {
        public static void Init()
        {
            CustomActions.OnAnyHealthHaverDie += AnyHealthHaverKilled;
        }

        public static void AnyHealthHaverKilled(HealthHaver target)
        {
            if (target && target.aiActor && GameManager.Instance.PrimaryPlayer)
            {
                ETGMod.StartGlobalCoroutine(SaveDeaths(target.aiActor.EnemyGuid));
            }
        }
        public static IEnumerator SaveDeaths(string guid)
        {
            AIActor prefabForGUID = EnemyDatabase.GetOrLoadByGuid(guid);
            GlobalDungeonData.ValidTilesets currentTileset = GameManager.Instance.Dungeon.tileIndices.tilesetId;

            if (prefabForGUID.healthHaver && prefabForGUID.healthHaver.IsBoss && !prefabForGUID.healthHaver.IsSubboss)
            {
                if (GameManager.Instance.CurrentGameMode != GameManager.GameMode.BOSSRUSH)
                {
                    if (currentTileset == GlobalDungeonData.ValidTilesets.CATACOMBGEON)
                    {
                        if (Challenges.CurrentChallenge == ChallengeType.KEEP_IT_COOL && !SaveAPIManager.GetFlag(CustomDungeonFlags.CHALLENGE_KEEPITCOOL_BEATEN)) SaveAPIManager.SetFlag(CustomDungeonFlags.CHALLENGE_KEEPITCOOL_BEATEN, true);
                    }
                    if (currentTileset == GlobalDungeonData.ValidTilesets.FORGEGEON)
                    {
                        switch (Challenges.CurrentChallenge)
                        {
                            case ChallengeType.WHAT_ARMY:
                                if (!SaveAPIManager.GetFlag(CustomDungeonFlags.CHALLENGE_WHATARMY_BEATEN)) SaveAPIManager.SetFlag(CustomDungeonFlags.CHALLENGE_WHATARMY_BEATEN, true);
                                break;
                            case ChallengeType.TOIL_AND_TROUBLE:
                                if (!SaveAPIManager.GetFlag(CustomDungeonFlags.CHALLENGE_TOILANDTROUBLE_BEATEN)) SaveAPIManager.SetFlag(CustomDungeonFlags.CHALLENGE_TOILANDTROUBLE_BEATEN, true);
                                break;
                            case ChallengeType.INVISIBLEO:
                                if (!SaveAPIManager.GetFlag(CustomDungeonFlags.CHALLENGE_INVISIBLEO_BEATEN)) SaveAPIManager.SetFlag(CustomDungeonFlags.CHALLENGE_INVISIBLEO_BEATEN, true);
                                break;
                        }                      
                    }
                }
            }
            yield break;
        }
    }
}
