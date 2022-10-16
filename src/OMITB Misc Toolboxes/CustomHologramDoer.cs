using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoMod;
using UnityEngine;

namespace NevernamedsItems
{
    public class CustomHologramDoer : BraveBehaviour
    {
        public CustomHologramDoer()
        {
            hologramIsGreen = false;
        }


        public bool hologramIsGreen;
        public GameObject extantSprite;
        private tk2dSprite m_ItemSprite;

        public void ShowSprite(tk2dSpriteCollectionData encounterIconCollection, int spriteID)
        {
            if (!encounterIconCollection)
            {
                return;
            }
            if (base.gameActor)
            {
                if (extantSprite) { Destroy(extantSprite); }
                extantSprite = new GameObject("Item Hologram", new Type[] { typeof(tk2dSprite) }) { layer = 0 };
                extantSprite.transform.position = (transform.position + new Vector3(0.5f, 2));
                m_ItemSprite = extantSprite.AddComponent<tk2dSprite>();
                // CloningAndDuplication.DuplicateSprite(m_ItemSprite, sprite as tk2dSprite);
                m_ItemSprite.SetSprite(encounterIconCollection, spriteID);
                m_ItemSprite.PlaceAtPositionByAnchor(extantSprite.transform.position, tk2dBaseSprite.Anchor.LowerCenter);
                m_ItemSprite.transform.localPosition = m_ItemSprite.transform.localPosition.Quantize(0.0625f);
                if (base.gameActor != null) { extantSprite.transform.parent = base.gameActor.transform; }

                if (m_ItemSprite)
                {
                    sprite.AttachRenderer(m_ItemSprite);
                    m_ItemSprite.depthUsesTrimmedBounds = true;
                    m_ItemSprite.UpdateZDepth();
                }
                sprite.UpdateZDepth();

                ApplyHologramShader(m_ItemSprite, hologramIsGreen);

            }
        }

        public void ApplyHologramShader(tk2dBaseSprite targetSprite, bool isGreen = false, bool usesOverrideMaterial = true)
        {
            Shader m_cachedShader = Shader.Find("Brave/Internal/HologramShader");
            Material m_cachedMaterial = new Material(Shader.Find("Brave/Internal/HologramShader"));
            m_cachedMaterial.name = "HologramMaterial";
            Material m_cachedSharedMaterial = m_cachedMaterial;

            m_cachedMaterial.SetTexture("_MainTex", targetSprite.renderer.material.GetTexture("_MainTex"));
            m_cachedSharedMaterial.SetTexture("_MainTex", targetSprite.renderer.sharedMaterial.GetTexture("_MainTex"));
            if (isGreen)
            {
                m_cachedMaterial.SetFloat("_IsGreen", 1f);
                m_cachedSharedMaterial.SetFloat("_IsGreen", 1f);
            }
            targetSprite.renderer.material.shader = m_cachedShader;
            targetSprite.renderer.material = m_cachedMaterial;
            targetSprite.renderer.sharedMaterial = m_cachedSharedMaterial;
            targetSprite.usesOverrideMaterial = usesOverrideMaterial;
        }

        public void HideSprite()
        {
            if (base.gameActor && extantSprite)
            {
                Destroy(extantSprite);
            }
        }
    }
}
