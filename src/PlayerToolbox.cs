using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;
using MonoMod.RuntimeDetour;

using ItemAPI;
// using SaveAPI;
using Dungeonator;

namespace CwaffingTheGungy
{
    public static class PlayerToolsSetup  // hooks and stuff for PlayerControllers on game start
    {
        public static Hook playerStartHook;
        public static Hook enemySpawnHook;

        public static void Init()
        {
            playerStartHook = new Hook(
                typeof(PlayerController).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance),
                typeof(PlayerToolsSetup).GetMethod("DoSetup"));

            enemySpawnHook = new Hook(
                typeof(AIActor).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance),
                typeof(CwaffToolbox).GetMethod("OnEnemyPreSpawn"));
        }
        public static void DoSetup(Action<PlayerController> action, PlayerController player)
        {
            action(player);
            if (player.GetComponent<HatController>() == null) player.gameObject.AddComponent<HatController>();
            if (player.GetComponent<CwaffToolbox>() == null) player.gameObject.AddComponent<CwaffToolbox>();
        }
    }

    class CwaffToolbox : MonoBehaviour
    {
        private PlayerController m_attachedPlayer;
        private bool isSecondaryPlayer;

        // public static string enemyWithoutAFuture = "01972dee89fc4404a5c408d50007dad5"; // bullet kin for testing
        public static string enemyWithoutAFuture = "";

        public static Texture2D eeveeTexture;

        private void Start()
        {
            m_attachedPlayer = base.GetComponent<PlayerController>();
            if (m_attachedPlayer)
                isSecondaryPlayer = (GameManager.Instance.SecondaryPlayer == m_attachedPlayer);

            eeveeTexture = ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<Texture2D>("nebula_reducednoise");

            enemyWithoutAFuture = ""; //reset so enemies don't stay dead between runs
        }

        public static void OnEnemyPreSpawn(Action<AIActor> action, AIActor enemy)
        {
            // ETGModConsole.Log("spawning "+enemy.GetActorName() + " ("+enemy.EnemyGuid+")");
            // if (true || enemy.EnemyGuid == enemyWithoutAFuture)
            if (string.IsNullOrEmpty(enemyWithoutAFuture) || enemy.EnemyGuid != enemyWithoutAFuture)
            {
                action(enemy);
                return;
            }

            // ETGModConsole.Log("  ded o.o");
            Memorialize(enemy);
            UnityEngine.Object.Destroy(enemy.gameObject);
        }

        public static int GetIdForBestIdleAnimation(AIActor enemy)
        {
            int bestMatchStrength = 0;
            int bestSpriteId = -1;

            tk2dSpriteDefinition[] defs = enemy.sprite.collection.spriteDefinitions;
            for (int i = 0; i < defs.Length; ++i)
            {
                tk2dSpriteDefinition sd = defs[i];
                int matchStrength = 0;
                if (sd.name.Contains("001"))
                {
                    if (sd.name.Contains("idle_f"))
                        matchStrength = 4;
                    else if (sd.name.Contains("idle_right") || sd.name.Contains("idle_l") || sd.name.Contains("idle_r"))
                        matchStrength = 3;
                    else if (sd.name.Contains("idle") || sd.name.Contains("fire") || sd.name.Contains("run_right") || sd.name.Contains("right_run"))
                        matchStrength = 2;
                    else if (sd.name.Contains("death") || sd.name.Contains("left") || sd.name.Contains("right"))
                        matchStrength = 1;
                    if (matchStrength > bestMatchStrength)
                    {
                      bestMatchStrength = matchStrength;
                      bestSpriteId = i;
                      if (bestMatchStrength == 4)
                        break;
                    }
                }
            }
            if (bestSpriteId == -1)
            {
                bestSpriteId = enemy.sprite.spriteId;
                // ETGModConsole.Log("  no matches, options: ");
                for (int i = 0; i < defs.Length; ++i)
                {
                    tk2dSpriteDefinition sd = defs[i];
                    // if (sd.name.Contains("001"))
                    //     ETGModConsole.Log("    "+sd.name);
                }
            }
            else
            {
                // ETGModConsole.Log("  found "+defs[bestSpriteId].name);
            }

            return bestSpriteId;
        }

        public static void Memorialize(AIActor enemy)
        {
            GameObject obj = new GameObject();
            obj.SetActive(false);
            FakePrefab.MarkAsFakePrefab(obj);
            GameObject g = UnityEngine.Object.Instantiate(obj, enemy.sprite.WorldBottomCenter, Quaternion.identity);

            int bestSpriteId = GetIdForBestIdleAnimation(enemy);

            tk2dBaseSprite sprite = g.AddComponent<tk2dSprite>();
                sprite.SetSprite(enemy.sprite.collection, bestSpriteId);
                // sprite.SetSprite(enemy.sprite.collection, enemy.sprite.collection.GetSpriteIdByName("idle"));
                // ETGModConsole.Log(sprite.name);
                sprite.FlipX = enemy.sprite.FlipX;
                sprite.depthUsesTrimmedBounds = true;
                sprite.PlaceAtPositionByAnchor(
                    enemy.sprite.transform.position,
                    sprite.FlipX ? tk2dBaseSprite.Anchor.LowerRight : tk2dBaseSprite.Anchor.LowerLeft);
                // sprite.allowDefaultLayer
            g.GetComponent<BraveBehaviour>().sprite = sprite;

            g.GetComponent<BraveBehaviour>().StartCoroutine(Flicker(g));
        }

        public static IEnumerator Flicker(GameObject g)
        {
            tk2dSprite gsprite = g.GetComponent<tk2dSprite>();
            gsprite.renderer.enabled = true;
            gsprite.OverrideMaterialMode = tk2dBaseSprite.SpriteMaterialOverrideMode.OVERRIDE_MATERIAL_COMPLEX;
            // gsprite.usesOverrideMaterial = false;
            gsprite.usesOverrideMaterial = true;
            // gsprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitBlendUber");
            // gsprite.renderer.material.SetFloat("_VertexColor", 1f);
            // gsprite.renderer.sharedMaterial.shader = ShaderCache.Acquire("Brave/LitBlendUber");
            // gsprite.renderer.sharedMaterial.SetFloat("_VertexColor", 1f);
            // gsprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
            // gsprite.renderer.sharedMaterial.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");

            gsprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/GlitchEevee");
                gsprite.renderer.material.SetTexture("_EeveeTex", eeveeTexture);
                gsprite.renderer.material.SetFloat("_WaveIntensity", 0.9f);
                gsprite.renderer.material.SetFloat("_ColorIntensity", 0.95f);
            gsprite.renderer.sharedMaterial.shader = ShaderCache.Acquire("Brave/Internal/GlitchEevee");
                gsprite.renderer.sharedMaterial.SetTexture("_EeveeTex", eeveeTexture);
                gsprite.renderer.sharedMaterial.SetFloat("_WaveIntensity", 0.9f);
                gsprite.renderer.sharedMaterial.SetFloat("_ColorIntensity", 0.95f);

            gsprite.color = AfterImageHelpers.afterImageGray.WithAlpha(0.5f);
            gsprite.enabled = true;
            gsprite.UpdateZDepth();
            while (true)
            {
                yield return new WaitForSeconds(0.05f);
                gsprite.renderer.enabled = true;
                // gsprite.color = AfterImageHelpers.afterImageGray.WithAlpha(0.5f);
                yield return null;
                gsprite.renderer.enabled = false;
                // gsprite.color = AfterImageHelpers.afterImageGray.WithAlpha(0.15f);
            }
        }
    }
}
