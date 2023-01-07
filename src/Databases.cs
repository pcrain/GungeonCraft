using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Dungeonator;

namespace CwaffingTheGungy
{
    public class EnemyGuidDatabase // stolen from NN
    {
        public static Dictionary<string, string> Entries { get; set; } = new Dictionary<string, string>()
        {
            //Bullet Kin & Variants
            {"bullet_kin", "01972dee89fc4404a5c408d50007dad5"},
            {"ak47_bullet_kin", "db35531e66ce41cbb81d507a34366dfe"},
            {"bandana_bullet_kin", "88b6b6a93d4b4234a67844ef4728382c"},
            {"veteran_bullet_kin", "70216cae6c1346309d86d4a0b4603045"},
            {"treadnaughts_bullet_kin", "df7fb62405dc4697b7721862c7b6b3cd"},
            {"minelet", "3cadf10c489b461f9fb8814abc1a09c1"},
            {"cardinal", "8bb5578fba374e8aae8e10b754e61d62"},
            {"shroomer", "e5cffcfabfae489da61062ea20539887"},
            {"ashen_bullet_kin", "1a78cfb776f54641b832e92c44021cf2"},
            {"mutant_bullet_kin", "d4a9836f8ab14f3fadd0f597438b1f1f"},
            {"fallen_bullet_kin", "5f3abc2d561b4b9c9e72b879c6f10c7e"},
            {"hooded_bullet", "844657ad68894a4facb1b8e1aef1abf9"},
            //{"red_caped_bullet_kin", "fa6a7ac20a0e4083a4c2ee0d86f8bbf7"},
            {"office_bullet_kin", "906d71ccc1934c02a6f4ff2e9c07c9ec"},//ERROR SHELLS BLACKLIST
            {"office_bullette_kin", "9eba44a0ea6c4ea386ff02286dd0e6bd"},//ERROR SHELLS BLACKLIST
            {"brollet", "05cb719e0178478685dc610f8b3e8bfc"}, //ERROR SHELLS BLACKLIST
            {"western_bullet_kin", "5861e5a077244905a8c25c2b7b4d6ebb"},//ERROR SHELLS BLACKLIST
            {"pirate_bullet_kin", "6f818f482a5c47fd8f38cce101f6566c"},//ERROR SHELLS BLACKLIST
            {"armored_bullet_kin", "39e6f47a16ab4c86bec4b12984aece4c"},//ERROR SHELLS BLACKLIST
            {"key_bullet_kin", "699cd24270af4cd183d671090d8323a1"},
            {"chance_bullet_kin", "a446c626b56d4166915a4e29869737fd"},
            //{"tutorial_bullet_kin", "b08ec82bef6940328c7ecd9ffc6bd16c"},
            //Dead/Undead Bullet Kin
            {"hollowpoint", "4db03291a12144d69fe940d5a01de376"},
            {"spectre", "56f5a0f2c1fc4bc78875aea617ee31ac"},//ERROR SHELLS BLACKLIST
            {"skullet", "336190e29e8a4f75ab7486595b700d4a"},
            {"skullmet", "95ec774b5a75467a9ab05fa230c0c143"},
            {"gummy", "5288e86d20184fa69c91ceb642d31474"},
            {"spent", "249db525a9464e5282d02162c88e0357"},
            {"gummy_spent", "e21ac9492110493baef6df02a2682a0d"}, //ERROR SHELLS BLACKLIST
            //Shotgun Kin & Variants
            {"red_shotgun_kin", "128db2f0781141bcb505d8f00f9e4d47"},
            {"blue_shotgun_kin", "b54d89f9e802455cbb2b8a96a31e8259"},
            {"veteran_shotgun_kin", "2752019b770f473193b08b4005dc781f"},
            {"mutant_shotgun_kin", "7f665bd7151347e298e4d366f8818284"},
            {"executioner", "b1770e0f1c744d9d887cc16122882b4f"},
            {"ashen_shotgun_kin", "1bd8e49f93614e76b140077ff2e33f2b"},
            {"shotgrub", "044a9f39712f456597b9762893fbc19c"},
            {"creech", "37340393f97f41b2822bc02d14654172"},
            {"western_shotgun_kin", "ddf12a4881eb43cfba04f36dd6377abb"}, //ERROR SHELLS BLACKLIST
            {"pirate_shotgun_kin", "86dfc13486ee4f559189de53cfb84107"}, //ERROR SHELLS BLACKLIST
            //Blobulonians
            {"blobulon", "0239c0680f9f467dbe5c4aab7dd1eca6"},
            {"blobuloid", "042edb1dfb614dc385d5ad1b010f2ee3"}, //ERROR SHELLS BLACKLIST
            {"blobulin", "42be66373a3d4d89b91a35c9ff8adfec"}, //ERROR SHELLS BLACKLIST
            {"poisbulon", "e61cab252cfb435db9172adc96ded75f"},
            {"poisbuloid", "fe3fe59d867347839824d5d9ae87f244"}, //ERROR SHELLS BLACKLIST
            {"poisbulin", "b8103805af174924b578c98e95313074"}, //ERROR SHELLS BLACKLIST
            {"blizzbulon", "022d7c822bc146b58fe3b0287568aaa2"},
            {"leadbulon", "ccf6d241dad64d989cbcaca2a8477f01"},
            {"bloodbulon", "062b9b64371e46e195de17b6f10e47c8"},
            {"poopulon", "116d09c26e624bca8cca09fc69c714b3"},
            {"cubulon", "864ea5a6a9324efc95a0dd2407f42810"},
            {"cubulead", "0b547ac6b6fc4d68876a241a88f5ca6a"},
            {"chancebulon", "1bc2a07ef87741be90c37096910843ab"},
            //Skulls and Skeletons
            {"skusket", "af84951206324e349e1f13f9b7b60c1a"},
            {"black_skusket", "1cec0cdf383e42b19920787798353e46"}, //ERROR SHELLS BLACKLIST
            {"skusket_head", "c2f902b7cbe745efb3db4399927eab34"}, //ERROR SHELLS BLACKLIST
            {"shelleton", "21dd14e5ca2a4a388adab5b11b69a1e1"},
            {"revolvenant", "d5a7b95774cd41f080e517bea07bf495"},
            {"gunreaper", "88f037c3f93b4362a040a87b30770407"},
            //{"lord_of_the_jammed", "0d3f7c641557426fbac8596b61c9fb45"},
            //Bullats
            {"bullat", "2feb50a6a40f4f50982e89fd276f6f15"},//ERROR SHELLS BLACKLIST
            {"shotgat", "2d4f8b5404614e7d8b235006acde427a"}, //ERROR SHELLS BLACKLIST
            {"grenat", "b4666cb6ef4f4b038ba8924fd8adf38f"},//ERROR SHELLS BLACKLIST
            {"spirat", "7ec3e8146f634c559a7d58b19191cd43"},//ERROR SHELLS BLACKLIST
            {"king_bullat", "1a4872dafdb34fd29fe8ac90bd2cea67"},
            {"gargoyle", "981d358ffc69419bac918ca1bdf0c7f7"}, //ERROR SHELLS BLACKLIST
            //Gunjurers
            {"apprentice_gunjurer", "206405acad4d4c33aac6717d184dc8d4"},
            {"gunjurer", "c4fba8def15e47b297865b18e36cbef8"},
            {"high_gunjurer", "9b2cf2949a894599917d4d391a0b7394"},
            {"lore_gunjurer", "56fb939a434140308b8f257f0f447829"},
            //Bookllets
            {"bookllet", "c0ff3744760c4a2eb0bb52ac162056e6"},
            {"blue_bookllet", "6f22935656c54ccfb89fca30ad663a64"},
            {"green_bookllet", "a400523e535f41ac80a43ff6b06dc0bf"},
            {"necronomicon", "216fd3dfb9da439d9bd7ba53e1c76462"}, //ERROR SHELLS BLACKLIST
            {"tablet_bookllett", "78e0951b097b46d89356f004dda27c42"}, //ERROR SHELLS BLACKLIST
            //Sliding Cubes
            {"mountain_cube", "f155fd2759764f4a9217db29dd21b7eb"},
            {"lead_cube", "33b212b856b74ff09252bf4f2e8b8c57"},
            {"flesh_cube", "3f2026dc3712490289c4658a2ba4a24b"},
            //{"metal_cube_guy", "ba928393c8ed47819c2c5f593100a5bc"},
            //Mimics
            {"brown_chest_mimic", "2ebf8ef6728648089babb507dec4edb7"},//ERROR SHELLS BLACKLIST
            {"blue_chest_mimic", "d8d651e3484f471ba8a2daa4bf535ce6"},//ERROR SHELLS BLACKLIST
            {"green_chest_mimic", "abfb454340294a0992f4173d6e5898a8"},//ERROR SHELLS BLACKLIST
            {"red_chest_mimic", "d8fd592b184b4ac9a3be217bc70912a2"},//ERROR SHELLS BLACKLIST
            {"black_chest_mimic", "6450d20137994881aff0ddd13e3d40c8"},//ERROR SHELLS BLACKLIST
            {"rat_chest_mimic", "ac9d345575444c9a8d11b799e8719be0"},//ERROR SHELLS BLACKLIST
            {"pedestal_mimic", "796a7ed4ad804984859088fc91672c7f"},//ERROR SHELLS BLACKLIST
            {"wall_mimic", "479556d05c7c44f3b6abb3b2067fc778"},//ERROR SHELLS BLACKLIST
            //Gun Nuts
            {"gun_nut", "ec8ea75b557d4e7b8ceeaacdf6f8238c"},
            {"chain_gunner", "463d16121f884984abe759de38418e48"},
            {"spectral_gun_nut", "383175a55879441d90933b5c4e60cf6f"},
            //Det & all weird Det Variants
            {"det", "ac986dabc5a24adab11d48a4bccf4cb1"}, //ERROR SHELLS BLACKLIST
            {"x_det", "48d74b9c65f44b888a94f9e093554977"}, //ERROR SHELLS BLACKLIST
            {"diagonal_x_det", "c5a0fd2774b64287bf11127ca59dd8b4"}, //ERROR SHELLS BLACKLIST
            {"vertical_det", "b67ffe82c66742d1985e5888fd8e6a03"}, //ERROR SHELLS BLACKLIST
            {"diagonal_det", "d9632631a18849539333a92332895ebd"}, //ERROR SHELLS BLACKLIST
            {"horizontal_det", "1898f6fe1ee0408e886aaf05c23cc216"}, //ERROR SHELLS BLACKLIST
            {"vertical_x_det", "abd816b0bcbf4035b95837ca931169df"}, //ERROR SHELLS BLACKLIST
            {"horizontal_x_det", "07d06d2b23cc48fe9f95454c839cb361"}, //ERROR SHELLS BLACKLIST
            //Buff/Support Enemies
            {"gunsinger", "cf2b7021eac44e3f95af07db9a7c442c"},
            {"aged_gunsinger", "c50a862d19fc4d30baeba54795e8cb93"},
            {"ammomancer", "b1540990a4f1480bbcb3bea70d67f60d"},
            {"jammomancer", "8b4a938cdbc64e64822e841e482ba3d2"},
            {"jamerlengo", "ba657723b2904aa79f9e51bce7d23872"},
            //Humans
            {"gun_cultist", "57255ed50ee24794b7aac1ac3cfb8a95"},
            //{"robots_past_human_1", "1398aaccb26d42f3b998c367b7800b85"},
            //{"robots_past_human_2", "9044d8e4431f490196ba697927a4e3d4"},
            //{"robots_past_human_3", "40bf8b0d97794a26b8c440425ec8cac1"},
            //{"robots_past_human_4", "3590db6c4eac474a93299a908cb77ab2"},
            //{"dr_wolf", "ce2d2a0dced0444fb751b262ec6af08a"},
            //Robots
            {"gat", "9b4fb8a2a60a457f90dcf285d34143ac"},
            {"grey_cylinder", "d4f4405e0ff34ab483966fd177f2ece3"},//ERROR SHELLS BLACKLIST
            {"red_cylinder", "534f1159e7cf4f6aa00aeea92459065e"},//ERROR SHELLS BLACKLIST
            {"bullet_mech", "2b6854c0849b4b8fb98eb15519d7db1c"},//ERROR SHELLS BLACKLIST
            //Explosive Kin
            {"grenade_kin", "4d37ce3d666b4ddda8039929225b7ede"},
            {"dynamite_kin", "c0260c286c8d4538a697c5bf24976ccf"},
            {"bombshee", "19b420dec96d4e9ea4aebc3398c0ba7a"},
            {"m80_kin", "5f15093e6f684f4fb09d3e7e697216b4"}, //ERROR SHELLS BLACKLIST
            //Misc Enemies
            {"rubber_kin", "6b7ef9e5d05b4f96b04f05ef4a0d1b18"},
            {"tazie", "98fdf153a4dd4d51bf0bafe43f3c77ff"},
            {"sniper_shell", "31a3ea0c54a745e182e22ea54844a82d"},
            {"professional", "c5b11bfc065d417b9c4d03a5e385fe2c"},
            {"muzzle_wisp", "ffdc8680bdaa487f8f31995539f74265"},
            {"muzzle_flare", "d8a445ea4d944cc1b55a40f22821ae69"},
            {"wizbang", "43426a2e39584871b287ac31df04b544"},
            {"coaler", "9d50684ce2c044e880878e86dbada919"},
            {"fungun", "f905765488874846b7ff257ff81d6d0c"},
            {"bullet_shark", "72d2f44431da43b8a3bae7d8a114a46d"},
            {"arrow_head", "05891b158cd542b1a5f3df30fb67a7ff"},//ERROR SHELLS BLACKLIST
            {"pot_fairy", "c182a5cb704d460d9d099a47af49c913"}, //ERROR SHELLS BLACKLIST
            {"musketball", "226fd90be3a64958a5b13cb0a4f43e97"},//ERROR SHELLS BLACKLIST
            {"western_cactus", "3b0bd258b4c9432db3339665cc61c356"},//ERROR SHELLS BLACKLIST
            {"candle_kin", "37de0df92697431baa47894a075ba7e9"},//ERROR SHELLS BLACKLIST
            //Agressive Animals
            {"gigi", "ed37fa13e0fa4fcf8239643957c51293"},
            {"misfire_beast", "45192ff6d6cb43ed8f1a874ab6bef316"},
            {"phaser_spider", "98ca70157c364750a60f5e0084f9d3e2"},
            {"gunzookie", "6e972cd3b11e4b429b888b488e308551"},
            {"gunzockie", "8a9e9bedac014a829a48735da6daf3da"},
            {"chameleon", "80ab6cd15bfc46668a8844b2975c6c26"},//ERROR SHELLS BLACKLIST
            {"bird_parrot", "4b21a913e8c54056bc05cafecf9da880"},//ERROR SHELLS BLACKLIST
            {"western_snake", "e861e59012954ab2b9b6977da85cb83c"},//ERROR SHELLS BLACKLIST
            //Misc Mutants / Weirdos
            {"kalibullet", "ff4f54ce606e4604bf8d467c1279be3e"},//ERROR SHELLS BLACKLIST
            {"kbullet", "f020570a42164e2699dcf57cac8a495c"},//ERROR SHELLS BLACKLIST
            {"blue_fish_bullet_kin", "06f5623a351c4f28bc8c6cda56004b80"},//ERROR SHELLS BLACKLIST
            {"green_fish_bullet_kin", "143be8c9bbb84e3fb3ab98bcd4cf5e5b"},//ERROR SHELLS BLACKLIST
            //Misc Strong Enemies
            {"tarnisher", "475c20c1fd474dfbad54954e7cba29c1"},
            {"agonizer", "3f6d6b0c4a7c4690807435c7b37c35a5"},
            {"lead_maiden", "cd4a4b7f612a4ba9a720b9f97c52f38c"},
            {"grip_master", "22fc2c2c45fb47cf9fb5f7b043a70122"},//ERROR SHELLS BLACKLIST
            {"shambling_round", "98ea2fe181ab4323ab6e9981955a9bca"},
            {"great_bullet_shark", "b70cbd875fea498aa7fd14b970248920"},
            {"killithid", "3e98ccecf7334ff2800188c417e67c15"},
            //{"hammer", "a38e9dca103c4e4fa4bf478cf9a2f2de"},
            {"spogre", "eed5addcc15148179f300cc0d9ee7f94"},
            {"fridge_maiden", "9215d1a221904c7386b481a171e52859"}, //ERROR SHELLS BLACKLIST
            {"titan_bullet_kin", "c4cf0620f71c4678bb8d77929fd4feff"},//ERROR SHELLS BLACKLIST
            {"titan_bullet_kin_boss", "1f290ea06a4c416cabc52d6b3cf47266"},//ERROR SHELLS BLACKLIST
            {"titaness_bullet_kin_boss", "df4e9fedb8764b5a876517431ca67b86"},//ERROR SHELLS BLACKLIST
            //Boss Minions
            {"beadie", "7b0b1b6d9ce7405b86b75ce648025dd6"},
            {"tombstoner", "cf27dd464a504a428d87a8b2560ad40a"},
            {"ammoconda_ball", "f38686671d524feda75261e469f30e0b"},//ERROR SHELLS BLACKLIST
            {"summoned_treadnaughts_bullet_kin", "47bdfec22e8e4568a619130a267eab5b"},//ERROR SHELLS BLACKLIST
            {"mine_flayers_bell", "78a8ee40dff2477e9c2134f6990ef297"},//ERROR SHELLS BLACKLIST
            {"mine_flayers_claymore", "566ecca5f3b04945ac6ce1f26dedbf4f"}, //ERROR SHELLS BLACKLIST
            {"candle_guy", "eeb33c3a5a8e4eaaaaf39a743e8767bc"},
            {"bullet_kings_toadie", "b5e699a0abb94666bda567ab23bd91c4"},
            {"bullet_kings_toadie_revenge", "d4dd2b2bbda64cc9bcec534b4e920518"},
            {"old_kings_toadie", "02a14dec58ab45fb8aacde7aacd25b01"},
            {"fusebot", "4538456236f64ea79f483784370bc62f"},//ERROR SHELLS BLACKLIST
            {"mouser", "be0683affb0e41bbb699cb7125fdded6"},
            {"draguns_knife", "78eca975263d4482a4bfa4c07b32e252"},
            {"dragun_knife_advanced", "2e6223e42e574775b56c6349921f42cb"},
            {"marines_past_imp", "a9cc6a4e9b3d46ea871e70a03c9f77d4"},
            {"convicts_past_soldier", "556e9f2a10f9411cb9dbfd61e0e0f1e1"},
            {"robots_past_terminator", "12a054b8a6e549dcac58a82b89e319e5"},
            //Minibosses
            //{"ser_manuel", "fc809bd43a4d41738a62d7565456622c"},
            {"blockner", "bb73eeeb9e584fbeaf35877ec176de28"},
            {"blockner_rematch", "edc61b105ddd4ce18302b82efdc47178"},
            {"fuselier", "39de9bd6a863451a97906d949c103538"},
            {"shadow_agunim", "db97e486ef02425280129e1e27c33118"},
            //Bosses
            //{"smiley", "ea40fcc863d34b0088f490f4e57f8913"},
            //{"shades", "c00390483f394a849c36143eb878998f"},
            {"gatling_gull", "ec6b674e0acd4553b47ee94493d66422"}, //Blacklist
            {"bullet_king", "ffca09398635467da3b1f4a54bcfda80"}, //ERROR SHELLS BLACKLIST
            {"blobulord", "1b5810fafbec445d89921a4efb4e42b7"}, //ERROR SHELLS BLACKLIST
            {"beholster", "4b992de5b4274168a8878ef9bf7ea36b"}, //Blacklist
            {"gorgun", "c367f00240a64d5d9f3c26484dc35833"}, //Blacklist
            {"ammoconda", "da797878d215453abba824ff902e21b4"}, //Blacklist
            {"old_king", "5729c8b5ffa7415bb3d01205663a33ef"}, //ERROR SHELLS BLACKLIST
            {"treadnaught", "fa76c8cfdf1c4a88b55173666b4bc7fb"}, //Blacklist
            {"mine_flayer", "8b0dd96e2fe74ec7bebc1bc689c0008a"}, //Blacklist
            {"cannonbalrog", "5e0af7f7d9de4755a68d2fd3bbc15df4"}, //ERROR SHELLS BLACKLIST
            {"door_lord", "9189f46c47564ed588b9108965f975c9"}, //ERROR SHELLS BLACKLIST
            //{"resourceful_rat", "6868795625bd46f3ae3e4377adce288b"},
            //{"resourceful_rat_mech", "4d164ba3f62648809a4a82c90fc22cae"},
            //{"high_priest", "6c43fddfd401456c916089fdd1c99b1c"},
            //{"kill_pillars", "3f11bbbc439c4086a180eb0fb9990cb4"},
            //{"wallmonger", "f3b04a067a65492f8b279130323b41f0"},
            {"helicopter_agunim", "41ee1c8538e8474a82a74c4aff99c712"},//Blacklist
            {"dragun", "465da2bb086a4a88a803f79fe3a27677"}, //Blacklist
            {"dragun_advanced", "05b8afe0b6cc4fffa9dc6036fa24c8ec"}, //Blacklist
            {"lich", "cd88c3ce60c442e9aa5b3904d31652bc"}, //ERROR SHELLS BLACKLIST
            {"megalich", "68a238ed6a82467ea85474c595c49c6e"}, //ERROR SHELLS BLACKLIST
            {"infinilich", "7c5d5f09911e49b78ae644d2b50ff3bf"}, //ERROR SHELLS BLACKLIST
            //Past Bosses
            //{"dr_wolfs_monster", "8d441ad4e9924d91b6070d5b3438d066"},
            //{"black_stache", "8b913eea3d174184be1af362d441910d"},
            //{"hm_absolution", "b98b10fca77d469e80fb45f3c5badec5"},
            //{"interdimensional_horror", "dc3cd41623d447aeba77c77c99598426"},
            //{"last_human", "880bbe4ce1014740ba6b4e2ea521e49d"},
            //{"agunim", "2ccaa1b7ae10457396a1796decda9cf6"},
            //{"cannon", "39dca963ae2b4688b016089d926308ab"},
            //Weird, Unused Bosses and Entities
            //{"bunker", "8817ab9de885424d9ba83455ead5ffef"},
            //{"vampire_mantis", "575a37abca8d414d89c4e251dd606050"},
            //{"bishop", "5d045744405d4438b371eb5ed3e2cdb2"},
            //{"boss_template", "7ee0a3fbb3dc417db4c3073ba23e1be8"},
            //{"shopkeeper_boss", "70058857b0a641a888ac4389bd10f455"},
            //{"friendly_bullet_kin", "c1757107b9a44f0c823a707adeb4ae7e"},
            //{"bullet_kin_fakery", "b5503988e3684e8fa78274dd0dda0bf5"},
            //{"veteran_bullet_kin_fakery", "06a82532447247f9ada1940d079a31a7"},
            //{"unused_muzzle_flare", "98e52539e1964749a8bbce0fe6a85d6b"},
            //{"test_dummy", "5fa8c86a65234b538cd022f726af2aea"},
            //Passive Critters
            {"chicken", "76bc43539fc24648bff4568c75c686d1"},
            {"poopulons_corn", "0ff278534abb4fbaaa65d3f638003648"},
            {"snake", "1386da0f42fb4bcabc5be8feb16a7c38"},
            {"tiny_blobulord", "d1c9781fdac54d9e8498ed89210a0238"}, //ERROR SHELLS BLACKLIST
            {"robots_past_critter_1", "95ea1a31fc9e4415a5f271b9aedf9b15"},
            {"robots_past_critter_2", "42432592685e47c9941e339879379d3a"},
            {"robots_past_critter_3", "4254a93fc3c84c0dbe0a8f0dddf48a5a"},
            {"rat", "6ad1cafc268f4214a101dca7af61bc91"},
            {"rat_candle", "14ea47ff46b54bb4a98f91ffcffb656d"},
            {"dragun_egg_slimeguy", "8b43a5c59b854eb780f9ab669ec26b7a"},
            //Companions
            {"portable_turret", "998807b57e454f00a63d67883fcf90d6"},
            //{"super_space_turtle", "3a077fa5872d462196bb9a3cb1af02a3"},
            //{"dog", "c07ef60ae32b404f99e294a6f9acba75"},
            //{"hunters_past_dog", "ededff1deaf3430eaf8321d0c6b2bd80"},
            //{"r2g2", "1ccdace06ebd42dc984d46cb1f0db6cf"},
            {"friendly_gatling_gull", "538669d3b2cd4edca2e3699812bcf2b6"},
            {"cop", "705e9081261446039e1ed9ff16905d04"},
            //{"caterpillar", "d375913a61d1465f8e4ffcf4894e4427"},
            {"cucco", "7bd9c670f35b4b8d84280f52a5cc47f6"},
            //{"raccoon", "e9fa6544000942a79ad05b6e4afb62db"},
            //{"baby_mimic", "e456b66ed3664a4cb590eab3a8ff3814"},
            //{"pig", "fe51c83b41ce4a46b42f54ab5f31e6d0"},
            //{"blank_companion", "5695e8ffa77c4d099b4d9bd9536ff35e"},
            //{"dog_synergy_1", "ebf2314289ff4a4ead7ea7ef363a0a2e"},
            //{"dog_synergy_2", "ab4a779d6e8f429baafa4bf9e5dca3a9"},
            //"super_space_turtle_synergy", "9216803e9c894002a4b931d7ea9c6bdf"},
            {"super_space_turtle_dummy", "cc9c41aa8c194e17b44ac45f993dd212"},
            //{"payday_shooter_01", "45f5291a60724067bd3ccde50f65ac22"},
            //{"payday_shooter_02", "41ab10778daf4d3692e2bc4b370ab037"},
            //{"payday_shooter_03", "2976522ec560460c889d11bb54fbe758"},
            //{"turkey", "6f9c28403d3248c188c391f5e40774c5"},
            //{"ser_junkan", "c6c8e59d0f5d41969c74e802c9d67d07"},
            //{"phoenix", "11a14dbd807e432985a89f69b5f9b31e"},
            //{"pig_synergy", "86237c6482754cd29819c239403a2de7"},
            //{"blank_companion_synergy", "ad35abc5a3bf451581c3530417d89f2c"},
            {"cop_android", "640238ba85dd4e94b3d6f68888e6ecb8"},
            //{"baby_good_shelleton", "3f40178e10dc4094a1565cd4fdc4af56"},
            //Turrets (From the Tutorial, and really weird acting. Basically nonfunctional)
            //{"tutorial_turret", "e667fdd01f1e43349c03a18e5b79e579"},
            //{"faster_tutorial_turret", "41ba74c517534f02a62f2e2028395c58"},
        };
    }

    public class ExtendedColours // stolen from NN
    {
        //New Colours
        public static Color carrionRed = new Color(176 / 255f, 4f / 255f, 4f / 255f);
        public static Color pink = new Color(242f / 255f, 116f / 255f, 225f / 255f);
        public static Color paleYellow = new Color(242f / 255f, 238f / 255f, 148f / 255f);
        public static Color lime = new Color(111f / 255f, 252f / 255f, 3f / 255f);
        public static Color brown = new Color(122f / 255f, 71f / 255f, 16f / 255f);
        public static Color orange = new Color(240f / 255f, 160f / 255f, 22f / 255f);
        public static Color vibrantOrange = new Color(255f / 255f, 144f / 255f, 41f / 255f);
        public static Color purple = new Color(171f / 255f, 22f / 255f, 240f / 255f);
        public static Color skyblue = new Color(130f / 255f, 230f / 255f, 2255f / 255f);
        public static Color honeyYellow = new Color(255f / 255f, 180f / 255f, 18f / 255f);
        public static Color maroon = new Color(105f / 255f, 7f / 255f, 9f / 255f);
        public static Color veryDarkRed = new Color(71f / 255f, 4f / 255f, 3f / 255f);
        public static Color plaguePurple = new Color(242f / 255f, 161f / 255f, 255f / 255f);
        public static Color darkBrown = new Color(74f / 255f, 22f / 255f, 5f / 255f);

        // //Colours  taken from Status effects
        // public static Color freezeBlue = StaticStatusEffects.chaosBulletsFreeze.TintColor;
        // public static Color poisonGreen = StaticStatusEffects.irradiatedLeadEffect.TintColor;
        // public static Color charmPink = StaticStatusEffects.charmingRoundsEffect.TintColor;

        // //Colours taken from items
        // public static Color cursedBulletsPurple = Gungeon.Game.Items["cursed_bullets"].GetComponent<ScalingStatBoostItem>().TintColor;
        // public static Color gildedBulletsGold = Gungeon.Game.Items["gilded_bullets"].GetComponent<ScalingStatBoostItem>().TintColor;
        // public static Color silvedBulletsSilver = Gungeon.Game.Items["silver_bullets"].GetComponent<SilverBulletsPassiveItem>().TintColor;
        // public static Color frostBulletsTint = Gungeon.Game.Items["frost_bullets"].GetComponent<BulletStatusEffectItem>().TintColor;
        // public static Color shadowBulletsBlue = new Color(0.35f, 0.25f, 0.65f, 1f);

    }

    public class EasyPlaceableObjects // stolen from NN
    {
        public static GameObject CoffinVert = LoadHelper.LoadAssetFromAnywhere<GameObject>("Coffin_Vertical");
        public static GameObject CoffinHoriz = LoadHelper.LoadAssetFromAnywhere<GameObject>("Coffin_Horizontal");
        public static GameObject Brazier = LoadHelper.LoadAssetFromAnywhere<DungeonPlaceable>("brazier").variantTiers[0].GetOrLoadPlaceableObject;
        public static GameObject CursedPot = LoadHelper.LoadAssetFromAnywhere<DungeonPlaceable>("Curse Pot").variantTiers[0].GetOrLoadPlaceableObject;
        public static GameObject GenericBarrel = LoadHelper.LoadAssetFromAnywhere<DungeonPlaceable>("Barrel_collection").variantTiers[0].nonDatabasePlaceable;
        public static DungeonPlaceable GenericBarrelDungeonPlaceable = LoadHelper.LoadAssetFromAnywhere<DungeonPlaceable>("Barrel_collection");
        public static GameObject PoisonBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Yellow Drum");
        public static GameObject MetalExplosiveBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Red Drum");
        public static GameObject ExplosiveBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Red Barrel");
        public static GameObject WaterBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Blue Drum");
        public static GameObject OilBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Purple Drum");
        public static GameObject IceBomb = LoadHelper.LoadAssetFromAnywhere<GameObject>("Ice Cube Bomb");
        public static GameObject TableHorizontal = LoadHelper.LoadAssetFromAnywhere<GameObject>("Table_Horizontal");
        public static GameObject TableVertical = LoadHelper.LoadAssetFromAnywhere<GameObject>("Table_Vertical");
        public static GameObject TableHorizontalStone = LoadHelper.LoadAssetFromAnywhere<GameObject>("Table_Horizontal_Stone");
        public static GameObject TableVerticalStone = LoadHelper.LoadAssetFromAnywhere<GameObject>("Table_Vertical_Stone");
        public static GameObject SpikeTrap = LoadHelper.LoadAssetFromAnywhere<GameObject>("trap_spike_gungeon_2x2");
        public static GameObject FlameTrap = LoadHelper.LoadAssetFromAnywhere<GameObject>("trap_flame_poofy_gungeon_1x1");
        public static GameObject HangingPot = LoadHelper.LoadAssetFromAnywhere<GameObject>("Hanging_Pot");
        public static GameObject DeadBlow = LoadHelper.LoadAssetFromAnywhere<GameObject>("Forge_Hammer");
        public static GameObject ChestTruth = LoadHelper.LoadAssetFromAnywhere<GameObject>("TruthChest");
        public static GameObject ChestRat = LoadHelper.LoadAssetFromAnywhere<GameObject>("Chest_Rat");
        public static GameObject Mirror = LoadHelper.LoadAssetFromAnywhere<GameObject>("Shrine_Mirror");
        public static GameObject FoldingTable = PickupObjectDatabase.GetById(644).GetComponent<FoldingTableItem>().TableToSpawn.gameObject;
        public static GameObject BabyDragunNPC = LoadHelper.LoadAssetFromAnywhere<GameObject>("BabyDragunJail");
    }

    public static class EasyGoopDefinitions  // mostly stolen from NN
    {
        //Basegame Goops
        public static GoopDefinition FireDef;
        public static GoopDefinition OilDef;
        public static GoopDefinition PoisonDef;
        public static GoopDefinition BlobulonGoopDef;
        public static GoopDefinition WebGoop;
        public static GoopDefinition WaterGoop;
        public static GoopDefinition CharmGoopDef = PickupObjectDatabase.GetById(310)?.GetComponent<WingsItem>()?.RollGoop;
        public static GoopDefinition GreenFireDef = (PickupObjectDatabase.GetById(698) as Gun).DefaultModule.projectiles[0].GetComponent<GoopModifier>().goopDefinition;
        public static GoopDefinition CheeseDef    = (PickupObjectDatabase.GetById(808) as Gun).DefaultModule.projectiles[0].GetComponent<GoopModifier>().goopDefinition;

        public static List<GoopDefinition> ColorGoops = new List<GoopDefinition>();
        public static List<Color> ColorGoopColors     = new List<Color>
        {
            ExtendedColours.pink,
            Color.red,
            ExtendedColours.orange,
            Color.yellow,
            Color.green,
            Color.blue,
            ExtendedColours.purple,
            Color.cyan,
        };
        private static string[] baseGoopAssets = new string[]
        {
            "assets/data/goops/napalmgoopthatworks.asset",
            "assets/data/goops/oil goop.asset",
            "assets/data/goops/poison goop.asset",
            "assets/data/goops/blobulongoop.asset",
            "assets/data/goops/phasewebgoop.asset",
            "assets/data/goops/water goop.asset",
        };

        public static void DefineDefaultGoops()
        {
            //Sets up the goops that have to be extracted from asset bundles
            AssetBundle assetBundle = ResourceManager.LoadAssetBundle("shared_auto_001");
            List<GoopDefinition> baseGoops = new List<GoopDefinition>();
            foreach (string text in EasyGoopDefinitions.baseGoopAssets)
            {
                GoopDefinition goopDefinition;
                try
                {
                    GameObject gameObject = assetBundle.LoadAsset(text) as GameObject;
                    goopDefinition = gameObject.GetComponent<GoopDefinition>();
                }
                catch
                {
                    goopDefinition = (assetBundle.LoadAsset(text) as GoopDefinition);
                }
                goopDefinition.name = text.Replace("assets/data/goops/", "").Replace(".asset", "");
                baseGoops.Add(goopDefinition);
            }

            //Define the asset bundle goops
            FireDef         = baseGoops[0];
            OilDef          = baseGoops[1];
            PoisonDef       = baseGoops[2];
            BlobulonGoopDef = baseGoops[3];
            WebGoop         = baseGoops[4];
            WaterGoop       = baseGoops[5];

            //Define colored water goop for paintball gun
            for (int i = 0; i < ColorGoopColors.Count; i++)
            {
                GoopDefinition g   = UnityEngine.Object.Instantiate<GoopDefinition>(WaterGoop);
                g.CanBeElectrified = false;
                g.baseColor32      = ColorGoopColors[i];
                ColorGoops.Add(g);
            }
        }
    }
}
