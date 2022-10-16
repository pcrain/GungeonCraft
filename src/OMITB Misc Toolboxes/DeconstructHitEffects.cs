using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    public static class DeconstructHitEffectsClass
    {
        public static void DeconstructHitEffects(this ProjectileImpactVFXPool pool)
        {
            ETGModConsole.Log("<color=#09b022>-------------------------------------</color>");
            ETGModConsole.Log("<color=#09b022>Bools:</color>");
            ETGModConsole.Log("<color=#ff0000ff>AlwaysUseMidair:</color> " + pool.alwaysUseMidair);
            ETGModConsole.Log("<color=#ff0000ff>CenterDeathVFXOnProj:</color> " + pool.CenterDeathVFXOnProjectile);
            ETGModConsole.Log("<color=#ff0000ff>HasProjectileDeathVFX:</color> " + pool.HasProjectileDeathVFX);
            ETGModConsole.Log("<color=#ff0000ff>SuppressMidairDeathVFX:</color> " + pool.suppressMidairDeathVfx);
            ETGModConsole.Log("<color=#ff0000ff>SuppressHitEffectsIfOffscreen:</color> " + pool.suppressHitEffectsIfOffscreen);
            ETGModConsole.Log("<color=#09b022>Effects:</color>");
            if (pool.overrideEarlyDeathVfx) ETGModConsole.Log("<color=#ff0000ff>    OverrideEarlyDeathVFX: </color>" + pool.overrideEarlyDeathVfx.name);
            else ETGModConsole.Log("<color=#ff0000ff>   OverrideEarlyDeathVFX: </color>" + "NULL");
            if (pool.overrideMidairDeathVFX) ETGModConsole.Log("<color=#ff0000ff>    OverrideMidairDeathVFX: </color>" + pool.overrideMidairDeathVFX.name);
            else ETGModConsole.Log("<color=#ff0000ff>   OverrideMidairDeathVFX: </color>" + "NULL");

            if (pool.deathAny != null)
            {
                ETGModConsole.Log("<color=#ff0000ff>   DeathAny: </color>");
                DeconstructVFXPool(pool.deathAny);
            }
            else ETGModConsole.Log("<color=#ff0000ff>   DeathAny: </color>" + "NULL");

            if (pool.deathEnemy != null)
            {
                ETGModConsole.Log("<color=#ff0000ff>   DeathEnemy: </color>");
                DeconstructVFXPool(pool.deathEnemy);
            }
            else ETGModConsole.Log("<color=#ff0000ff>   DeathEnemy: </color>" + "NULL");

            if (pool.deathTileMapHorizontal != null)
            {
                ETGModConsole.Log("<color=#ff0000ff>   DeathTilemapHorizontal: </color>");
                DeconstructVFXPool(pool.deathTileMapHorizontal);
            }
            else ETGModConsole.Log("<color=#ff0000ff>   DeathTilemapHorizontal: </color>" + "NULL");

            if (pool.deathTileMapVertical != null)
            {
                ETGModConsole.Log("<color=#ff0000ff>   DeathTilemapVertical: </color>");
                DeconstructVFXPool(pool.deathTileMapVertical);
            }
            else ETGModConsole.Log("<color=#ff0000ff>   DeathTilemapVertical: </color>" + "NULL");

            if (pool.enemy != null)
            {
                ETGModConsole.Log("<color=#ff0000ff>   Enemy(?): </color>");
                DeconstructVFXPool(pool.enemy);
            }
            else ETGModConsole.Log("<color=#ff0000ff>   Enemy(?): </color>" + "NULL");

            if (pool.tileMapHorizontal != null)
            {
                ETGModConsole.Log("<color=#ff0000ff>   TilemapHorizontal: </color>");
                DeconstructVFXPool(pool.tileMapHorizontal);
            }
            else ETGModConsole.Log("<color=#ff0000ff>   TilemapHorizontal: </color>" + "NULL");

            if (pool.tileMapVertical != null)
            {
                ETGModConsole.Log("<color=#ff0000ff>   TilemapVertical: </color>");
                DeconstructVFXPool(pool.tileMapVertical);
            }
            else ETGModConsole.Log("<color=#ff0000ff>   TilemapVertical: </color>" + "NULL");
        }
        private static void DeconstructVFXPool(VFXPool pool)
        {
            int iterator = 0;
            if (pool.effects != null && pool.effects.Length > 0)
            {

                foreach (VFXComplex comp in pool.effects)
                {
                    ETGModConsole.Log("<color=#ff0000ff>      VFXPoolEffect: </color>" + iterator);
                    if (comp.effects != null && comp.effects.Length > 0)
                    {
                        foreach (VFXObject obj in comp.effects)
                        {
                            ETGModConsole.Log("<color=#ff0000ff>      VFXObject: </color>" + iterator);
                            if (obj.effect != null)
                            {
                                ETGModConsole.Log("<color=#ff0000ff>           Effect: </color>" + obj.effect.name);
                            }
                            else ETGModConsole.Log("<color=#ff0000ff>           Effect: </color>" + "NULL");
                        }

                    }
                    else ETGModConsole.Log("<color=#ff0000ff>        VFXObject: </color>" + "NULL");
                }

            }
            else ETGModConsole.Log("<color=#ff0000ff>      VFXPoolEffects: </color>" + "NULL");
        }
    }
}
