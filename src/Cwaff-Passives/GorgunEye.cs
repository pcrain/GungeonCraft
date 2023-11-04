namespace CwaffingTheGungy
{
    public class GorgunEye : PassiveItem
    {
        public static string ItemName         = "Gorgun Eye";
        public static string SpritePath       = "gorgun_eye_icon";
        public static string ShortDescription = "Staredown";
        public static string LongDescription  = "Stuns enemies while looking directly at them.\n\nContrary to popular belief, Gorgun eyes have no intrinsic petrification properties. Gungeon archaeologists have discovered that Gorgun shamans traditionally imbue the eyes of their peers with petrification magic during infancy. More recently, it appears that Gorguns have begun to prefer enchanting synthetic eyes and disposable contact lenses, presumably after collectively realizing that petrifying everyone you meet was mildly inconvenient during social gatherings.";

        private const float _CONE_RADIUS      = 10f;
        private const float _STUN_LINGER_TIME = 0.1f;
        private const string _EFFECT_NAME     = "Gorgun Eyed";

        private static readonly Color _StoneColor        = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        private static GameActorHealthEffect _GorgunTint = null;

        private AIActor _afflictedEnemy = null;

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<GorgunEye>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            _GorgunTint = new GameActorHealthEffect()
            {
                AppliesTint              = true,
                TintColor                = _StoneColor,
                AppliesDeathTint         = true,
                DeathTintColor           = _StoneColor,
                AffectsEnemies           = true,
                DamagePerSecondToEnemies = 0f,
                duration                 = 10000000,
                effectIdentifier         = _EFFECT_NAME,
            };
        }

        public override void Update()
        {
            base.Update();
            if (this.Owner is not PlayerController player)
                return;

            Vector2 ppos         = player.sprite.WorldCenter;
            float gunAngle       = player.m_currentGunAngle;
            float minAngle       = gunAngle;
            AIActor closestEnemy = null;
            float closestDist    = 999999f;
            foreach(AIActor enemy in this.Owner.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
            {
                if (!enemy.IsHostileAndNotABoss())
                    continue; // enemy is not one we should be targeting

                Vector2 epos  = enemy.sprite.WorldCenter;
                Vector2 delta = epos - ppos;
                if (!delta.IsNearAngle(gunAngle, _CONE_RADIUS))
                    continue; // enemy is not within our vision range

                float dist = delta.sqrMagnitude;
                if (dist >= closestDist)
                    continue; // enemy is not the closest enemy we've encountered

                // Make sure we're not going through walls
                Vector2 target = Raycast.ToNearestWallOrObject(ppos, gunAngle, minDistance: 0);
                if ((target-ppos).sqrMagnitude < dist)
                    continue; // wall obstructs our view of the enemy

                closestEnemy = enemy;
                closestDist  = dist;
            }

            if (closestEnemy != this._afflictedEnemy)
            {
                this._afflictedEnemy?.RemoveEffect(_EFFECT_NAME);
                closestEnemy?.ApplyEffect(_GorgunTint);
                this._afflictedEnemy = closestEnemy;
                if (closestEnemy)
                {
                    AkSoundEngine.PostEvent("gorgun_eye_activate_stop_all", closestEnemy.gameObject);
                    AkSoundEngine.PostEvent("gorgun_eye_activate", closestEnemy.gameObject);
                }
            }
            closestEnemy?.behaviorSpeculator?.Stun(_STUN_LINGER_TIME, createVFX: false);
        }
    }
}
