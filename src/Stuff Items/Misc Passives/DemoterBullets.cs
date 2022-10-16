using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.ItemAPI;
using UnityEngine;
using Gungeon;
using Dungeonator;

namespace NevernamedsItems
{
    class DemoterBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Demoter Bullets";
            string resourceName = "NevernamedsItems/Resources/demoterbullets_icon";
            GameObject obj = new GameObject();
            var item = obj.AddComponent<DemoterBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "You're Fired!";
            string longDesc = "Chance to downgrade enemies into a less powerful form." + "\n\nBusiness is Business, and Business is universal. The Gungeon is no exception. It's unfortunate, but those not up to the Gungeon's rigorous standards may have to be... fired." + "\n\nDemoter? I hardly even know her!";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
        }
        public void onFired(Projectile bullet, float eventchancescaler)
        {
            if (UnityEngine.Random.value < 0.3f) bullet.OnHitEnemy += this.onHitEnemy;
        }
        public void onHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (!fatal)
            {
                string demotionGuid = null;
                switch (enemy.aiActor.EnemyGuid)
                {
                    case "db35531e66ce41cbb81d507a34366dfe": //AK47 Kin
                    case "88b6b6a93d4b4234a67844ef4728382c": //Bandana Kin 
                    case "70216cae6c1346309d86d4a0b4603045": //Veteran Bullet Kin
                    case "df7fb62405dc4697b7721862c7b6b3cd": //Tanker
                    case "3cadf10c489b461f9fb8814abc1a09c1": //Minelet
                    case "8bb5578fba374e8aae8e10b754e61d62": //Cardinal
                    case "e5cffcfabfae489da61062ea20539887": //Shroomer
                    case "1a78cfb776f54641b832e92c44021cf2": //Ashen Bullet Kin
                    case "d4a9836f8ab14f3fadd0f597438b1f1f": //Mutant Bullet Kin
                    case "c4cf0620f71c4678bb8d77929fd4feff": //Titan Bullet Kin
                    case "47bdfec22e8e4568a619130a267eab5b": //Summoned Tanker
                    case "05cb719e0178478685dc610f8b3e8bfc": //Brollet
                        demotionGuid = "01972dee89fc4404a5c408d50007dad5"; //Set the Target demotion to: BULLET KIN
                        break;
                    case "43426a2e39584871b287ac31df04b544": //Wizbang
                        demotionGuid = "8bb5578fba374e8aae8e10b754e61d62"; //Set the Target demotion to: CARDINAL
                        break;
                    case "b54d89f9e802455cbb2b8a96a31e8259": //Blue Shotgun Kin
                    case "7f665bd7151347e298e4d366f8818284": //Mutant Shotgun Kin
                    case "ddf12a4881eb43cfba04f36dd6377abb": //Western Shotgun Kin
                        demotionGuid = "128db2f0781141bcb505d8f00f9e4d47"; //Set the Target demotion to: RED SHOTGUN KIN
                        break;
                    case "2752019b770f473193b08b4005dc781f": //Veteran Shotgun Kin
                    case "1bd8e49f93614e76b140077ff2e33f2b": //Ashen Shotgun Kin
                    case "b1770e0f1c744d9d887cc16122882b4f": //Executioner
                        demotionGuid = "b54d89f9e802455cbb2b8a96a31e8259"; //Set the Target demotion to: BLUE SHOTGUN KIN
                        break;
                    case "044a9f39712f456597b9762893fbc19c": //Shotgrub
                        demotionGuid = "2752019b770f473193b08b4005dc781f"; //Set the Target demotion to: VETERAN SHOTGUN KIN
                        break;
                    case "5288e86d20184fa69c91ceb642d31474": //Gummy
                    case "95ec774b5a75467a9ab05fa230c0c143": //Skullmet
                        demotionGuid = "336190e29e8a4f75ab7486595b700d4a"; //Set the Target demotion to: SKULLET
                        break;
                    case "e61cab252cfb435db9172adc96ded75f": //Poisbulon
                    case "022d7c822bc146b58fe3b0287568aaa2": //Blizzbulon
                    case "ccf6d241dad64d989cbcaca2a8477f01": //Leadbulon
                    case "062b9b64371e46e195de17b6f10e47c8": //Bloodbulon
                    case "116d09c26e624bca8cca09fc69c714b3": //Poopulon
                    case "864ea5a6a9324efc95a0dd2407f42810": //Cubulon
                        demotionGuid = "0239c0680f9f467dbe5c4aab7dd1eca6"; //Set the Target demotion to: BLOBULON
                        break;
                    case "fe3fe59d867347839824d5d9ae87f244": //Poisbuloid
                    case "0239c0680f9f467dbe5c4aab7dd1eca6": //Blobulon
                        demotionGuid = "042edb1dfb614dc385d5ad1b010f2ee3"; //Set the Target demotion to: BLOBULOID
                        break;
                    case "b8103805af174924b578c98e95313074": //Poisbulin
                    case "042edb1dfb614dc385d5ad1b010f2ee3": //Blobuloid
                        demotionGuid = "42be66373a3d4d89b91a35c9ff8adfec"; //Set the Target demotion to: BLOBULIN
                        break;
                    case "0b547ac6b6fc4d68876a241a88f5ca6a": //Cubulead
                    case "1bc2a07ef87741be90c37096910843ab": //Chancebulon
                        demotionGuid = "864ea5a6a9324efc95a0dd2407f42810"; //Set the Target demotion to: CUBULON
                        break;
                    case "9b2cf2949a894599917d4d391a0b7394": //High Gunjurer
                    case "56fb939a434140308b8f257f0f447829": //Lore Gunjurer
                        demotionGuid = "c4fba8def15e47b297865b18e36cbef8"; //Set the Target demotion to: GUNJURER
                        break;
                    case "c4fba8def15e47b297865b18e36cbef8": //Gunjurer
                        demotionGuid = "206405acad4d4c33aac6717d184dc8d4"; //Set the Target demotion to: APPRENTICE GUNJURER
                        break;
                    case "6f22935656c54ccfb89fca30ad663a64": //Blue Bookllet
                    case "a400523e535f41ac80a43ff6b06dc0bf": //Green Bookllet
                    case "216fd3dfb9da439d9bd7ba53e1c76462": //Necronomicon
                    case "78e0951b097b46d89356f004dda27c42": //Tablet
                        demotionGuid = "c0ff3744760c4a2eb0bb52ac162056e6"; //Set the Target demotion to: BOOKLLET
                        break;
                    case "c50a862d19fc4d30baeba54795e8cb93": //Aged Gunsinger
                    case "b1540990a4f1480bbcb3bea70d67f60d": //Ammomancer
                    case "8b4a938cdbc64e64822e841e482ba3d2": //Jammomancer
                        demotionGuid = "cf2b7021eac44e3f95af07db9a7c442c"; //Set the Target demotion to: GUNSINGER
                        break;
                    case "ba657723b2904aa79f9e51bce7d23872": //Jamerlengo
                        demotionGuid = "8b4a938cdbc64e64822e841e482ba3d2"; //Set the Target demotion to: JAMMOMANCER
                        break;
                    case "d8a445ea4d944cc1b55a40f22821ae69": //Muzzle Flare
                        demotionGuid = "ffdc8680bdaa487f8f31995539f74265"; //Set the Target demotion to: MUZZLE WISP
                        break;
                    case "98fdf153a4dd4d51bf0bafe43f3c77ff": //Tazie
                        demotionGuid = "6b7ef9e5d05b4f96b04f05ef4a0d1b18"; //Set the Target demotion to: RUBBER KIN
                        break;
                    case "844657ad68894a4facb1b8e1aef1abf9": //Confirmed
                        demotionGuid = "f020570a42164e2699dcf57cac8a495c"; //Set the Target demotion to: KBULLET
                        break;
                    case "ed37fa13e0fa4fcf8239643957c51293": //Gigi
                    case "4b21a913e8c54056bc05cafecf9da880": //Parrot
                        demotionGuid = "76bc43539fc24648bff4568c75c686d1"; //Set the Target demotion to: CHICKEN
                        break;
                    case "8a9e9bedac014a829a48735da6daf3da": //Gunzockie
                        demotionGuid = "6e972cd3b11e4b429b888b488e308551"; //Set the Target demotion to: GUNZOOKIE
                        break;
                    case "c5b11bfc065d417b9c4d03a5e385fe2c": //Professional
                        demotionGuid = "c5b11bfc065d417b9c4d03a5e385fe2c"; //Set the Target demotion to: SNIPER SHELL
                        break;
                    case "b70cbd875fea498aa7fd14b970248920": //Great Bullet Shark
                        demotionGuid = "72d2f44431da43b8a3bae7d8a114a46d"; //Set the Target demotion to: BULLET SHARK
                        break;
                    case "1cec0cdf383e42b19920787798353e46": //Black Skusket
                        demotionGuid = "af84951206324e349e1f13f9b7b60c1a"; //Set the Target demotion to: SKUSKET
                        break;
                    case "af84951206324e349e1f13f9b7b60c1a": //Skusket
                        demotionGuid = "c2f902b7cbe745efb3db4399927eab34"; //Set the Target demotion to: SKUSKET HEAD
                        break;
                    case "eed5addcc15148179f300cc0d9ee7f94": //Spogre
                        demotionGuid = "f905765488874846b7ff257ff81d6d0c"; //Set the Target demotion to: FUNGUN
                        break;
                    case "c0260c286c8d4538a697c5bf24976ccf": //Nitra
                    case "19b420dec96d4e9ea4aebc3398c0ba7a": //Bombshee
                        demotionGuid = "4d37ce3d666b4ddda8039929225b7ede"; //Set the Target demotion to: PINHEAD
                        break;
                    case "5f15093e6f684f4fb09d3e7e697216b4": //m80 Kin
                        demotionGuid = "c0260c286c8d4538a697c5bf24976ccf"; //Set the Target demotion to: NITRA
                        break;
                    case "33b212b856b74ff09252bf4f2e8b8c57": //Lead Cube
                        demotionGuid = "f155fd2759764f4a9217db29dd21b7eb"; //Set the Target demotion to: MOUNTAIN CUBE
                        break;
                    case "3f2026dc3712490289c4658a2ba4a24b": //Flesh Cube
                    case "ba928393c8ed47819c2c5f593100a5bc": //Brick Cube
                        demotionGuid = "33b212b856b74ff09252bf4f2e8b8c57"; //Set the Target demotion to: LEAD CUBE
                        break;
                    case "56f5a0f2c1fc4bc78875aea617ee31ac": //Spectre
                        demotionGuid = "4db03291a12144d69fe940d5a01de376"; //Set the Target demotion to: HOLLOWPOINT
                        break;
                    case "e861e59012954ab2b9b6977da85cb83c": //Western Snake
                        demotionGuid = "1386da0f42fb4bcabc5be8feb16a7c38"; //Set the Target demotion to: SNAKE
                        break;
                    case "463d16121f884984abe759de38418e48": //Chain Gunner
                    case "383175a55879441d90933b5c4e60cf6f": //Spectral Gun Nut
                        demotionGuid = "ec8ea75b557d4e7b8ceeaacdf6f8238c"; //Set the Target demotion to: GUN NUT
                        break;
                    case "7ec3e8146f634c559a7d58b19191cd43": //Spirat
                    case "1a4872dafdb34fd29fe8ac90bd2cea67": //King Bullat
                        demotionGuid = "2feb50a6a40f4f50982e89fd276f6f15"; //Set the Target demotion to: BULLAT
                        break;
                    case "981d358ffc69419bac918ca1bdf0c7f7": //Gargoyle
                        demotionGuid = "1a4872dafdb34fd29fe8ac90bd2cea67"; //Set the Target demotion to: KING BULLAT
                        break;
                    case "1f290ea06a4c416cabc52d6b3cf47266": //Boss Titan Bullet
                        demotionGuid = "906d71ccc1934c02a6f4ff2e9c07c9ec"; //Set the Target demotion to: OFFICE BULLET KIN
                        break;
                    case "df4e9fedb8764b5a876517431ca67b86": //Titaness Bullet
                        demotionGuid = "9eba44a0ea6c4ea386ff02286dd0e6bd"; //Set the Target demotion to: OFFICE BULLETTE KIN
                        break;
                }
                if (enemy.aiActor.IsTransmogrified == true) enemy.aiActor.IsTransmogrified = false;
                if (demotionGuid != null) SpecialTransmogrify(enemy.aiActor, EnemyDatabase.GetOrLoadByGuid(demotionGuid), (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
            }
        }
        public void SpecialTransmogrify(AIActor InputEnemy, AIActor OutputEnemy, GameObject EffectVFX)
        {
            if (InputEnemy.IsTransmogrified && InputEnemy.ActorName == OutputEnemy.ActorName)
            {
                return;
            }
            if (InputEnemy.IsMimicEnemy || !InputEnemy.healthHaver || InputEnemy.healthHaver.IsBoss)
            {
                return;
            }
            if (InputEnemy.ParentRoom == null)
            {
                return;
            }
            Vector2 centerPosition = InputEnemy.CenterPosition;
            float healthPercentage = InputEnemy.healthHaver.GetCurrentHealthPercentage();
            if (EffectVFX != null)
            {
                SpawnManager.SpawnVFX(EffectVFX, centerPosition, Quaternion.identity);
            }
            AIActor aiactor = AIActor.Spawn(OutputEnemy, centerPosition.ToIntVector2(VectorConversions.Floor), InputEnemy.ParentRoom, true, AIActor.AwakenAnimationType.Default, true);
            if (aiactor)
            {
                aiactor.IsTransmogrified = true;
                float currentHealth = aiactor.healthHaver.GetCurrentHealth();
                float modifiedHealth = currentHealth *= healthPercentage;
                aiactor.healthHaver.ForceSetCurrentHealth(modifiedHealth);
                if (aiactor.healthHaver.GetCurrentHealth() <= 0) aiactor.healthHaver.ApplyDamage(100000, Vector2.zero, "Demoted To Corpse");
            }
            AkSoundEngine.PostEvent("Play_ENM_wizardred_appear_01", base.gameObject);

            InputEnemy.EraseFromExistenceWithRewards(false);
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFired;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.PostProcessProjectile -= this.onFired;
            return result;
        }

    }
}