using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class EmmisiveTrail : MonoBehaviour
    {
        public EmmisiveTrail()
        {
            this.EmissivePower = 75;
            this.EmissiveColorPower = 1.55f;
            debugLogging = false;
        }
        public void Start()
        {
            Shader glowshader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");

            foreach (Transform transform in base.transform)
            {
                
                    tk2dBaseSprite sproot = transform.GetComponent<tk2dBaseSprite>();
                    if (sproot != null)
                    {
                        if (debugLogging) Debug.Log($"Checks were passed for transform; {transform.name}");
                        sproot.usesOverrideMaterial = true;
                        sproot.renderer.material.shader = glowshader;
                        sproot.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_ON");
                        sproot.renderer.material.SetFloat("_EmissivePower", EmissivePower);
                        sproot.renderer.material.SetFloat("_EmissiveColorPower", EmissiveColorPower);
                    }
                    else
                    {
                        if (debugLogging) Debug.Log("Sprite was null");
                    }
            }
        }
        private List<string> TransformList = new List<string>()
        {
            "trailObject",
        };
        public float EmissivePower;
        public float EmissiveColorPower;
        public bool debugLogging;
    }
}
