using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;

namespace GungeonAPI
{
    public static class HitboxMonitor
    {

        static float pixelsPerUnit = 16;

        public static void DisplayHitbox(SpeculativeRigidbody speculativeRigidbody)
        {
            PixelCollider collider = speculativeRigidbody.HitboxPixelCollider;
            Tools.Log("Collider Found...");
            if (!speculativeRigidbody.gameObject.GetComponent<HitBoxDisplay>())
                speculativeRigidbody.gameObject.AddComponent<HitBoxDisplay>();
            Tools.Log("Displaying...");
            LogHitboxInfo(collider);
        }

        public static void DeleteHitboxDisplays()
        {
            foreach (var comp in GameObject.FindObjectsOfType<HitBoxDisplay>())
            {
                GameObject.Destroy(comp);
            }
        }

        public class HitBoxDisplay : BraveBehaviour
        {
            GameObject hitboxDisplay = null;
            PixelCollider collider;
            void Start()
            {
                CreateDisplay();
            }

            public void CreateDisplay()
            {
                collider = base.specRigidbody.HitboxPixelCollider;
                string displayerName = "HitboxDisplay";

                if (hitboxDisplay == null)
                    hitboxDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);

                hitboxDisplay.GetComponent<Renderer>().material.color = new Color(1, 0, 1, .75f);
                hitboxDisplay.name = displayerName;
                hitboxDisplay.transform.SetParent(specRigidbody.transform);
            }

            void FixedUpdate()
            {
                hitboxDisplay.transform.localScale = new Vector3(
                    collider.Dimensions.x / pixelsPerUnit,
                    collider.Dimensions.y / pixelsPerUnit,
                    1f
                );
                Vector3 offset = new Vector3(
                    collider.Offset.x + collider.Dimensions.x * 0.5f,
                    collider.Offset.y + collider.Dimensions.y * 0.5f,
                    -pixelsPerUnit
                );
                offset /= pixelsPerUnit;
                hitboxDisplay.transform.localPosition = offset;
            }

            public override void OnDestroy()
            {
                if (hitboxDisplay)
                    GameObject.DestroyImmediate(hitboxDisplay);
            }
        }

        private static void LogHitboxInfo(PixelCollider collider)
        {
            Tools.Print($"Dimensions: ({collider.Dimensions.x},{collider.Dimensions.y})");
            Tools.Print($"Offset: ({collider.Offset.x},{collider.Offset.y})");
        }
    }
}