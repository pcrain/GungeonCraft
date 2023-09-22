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
    public class VoodooDoll : PassiveItem
    {
        public static string ItemName         = "Voodoo Doll";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/voodoo_doll_icon";
        public static string ShortDescription = "PewPew Unto Others";
        public static string LongDescription  = "(When a player-owned projectile hits an enemy, all other enemies of the same type in that room take damage)";

        private static bool _VoodooDollEffectHappening = false;

        internal static GameObject _VoodooGhostVFX;

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<VoodooDoll>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.A;
            _VoodooGhostVFX   = VFX.animations["VoodooGhost"];
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnDealtDamageContext += this.OnDealtDamage;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.OnDealtDamageContext -= this.OnDealtDamage;
            return base.Drop(player);
        }

        private void OnDealtDamage(PlayerController source, float damage, bool fatal, HealthHaver enemy)
        {
            if (_VoodooDollEffectHappening || !enemy)
                return; // avoid recursive damage

            _VoodooDollEffectHappening = true;
            DoVoodooDollEffect(damage, enemy);
            _VoodooDollEffectHappening = false;
        }

        private void DoVoodooDollEffect(float damage, HealthHaver enemy)
        {
            string myGuid = enemy.aiActor.EnemyGuid;
            List<AIActor> activeEnemies = enemy.aiActor.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            foreach (AIActor other in activeEnemies)
            {
                if (!other || !other.specRigidbody || other.IsGone || !other.healthHaver || other.healthHaver.IsDead)
                    continue; // don't care about inactive or dead enemies
                if (other.aiActor.EnemyGuid != myGuid)
                    continue; // don't care about non-matching enemies
                if (other == enemy.aiActor)
                    continue; // don't care about matching ourself

                other.healthHaver.ApplyDamage(damage, Vector2.zero, "Voodoo Doll", CoreDamageTypes.Magic, DamageCategory.Unstoppable,
                    ignoreInvulnerabilityFrames: true, ignoreDamageCaps: false);

                bool flip = Lazy.CoinFlip();
                Vector2 ppos = flip ? other.sprite.WorldTopRight : other.sprite.WorldTopLeft;
                GameObject v = SpawnManager.SpawnVFX(_VoodooGhostVFX, ppos, 0f.EulerZ());
                    v.GetComponent<tk2dSprite>().FlipX = flip;
                FancyVFX f = v.AddComponent<FancyVFX>();
                    f.Setup(velocity: Vector2.zero, lifetime: 0.4f, fadeOutTime: 0.4f);
            }
        }

    }
}
