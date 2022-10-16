using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class CustomDarknessHandler : MonoBehaviour
    {
        public CustomDarknessHandler()
        {
        }
        public static OverridableBool shouldBeDark = new OverridableBool(false);
        public static OverridableBool shouldBeLightOverride = new OverridableBool(false);
        private void Start()
        {
            GameObject ChallengeManagerReference = LoadHelper.LoadAssetFromAnywhere<GameObject>("_ChallengeManager");
            DarknessEffectShader = (ChallengeManagerReference.GetComponent<ChallengeManager>().PossibleChallenges[5].challenge as DarknessChallengeModifier).DarknessEffectShader;
        }
        private bool ReturnShouldBeDark()
        {
            if (shouldBeDark.Value && !shouldBeLightOverride.Value)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void Update()
        {
            if (ReturnShouldBeDark() && !isDark)
            {
                EnableDarkness();
            }
            else if (!ReturnShouldBeDark() && isDark)
            {
                DisableDarkness();
            }

            if (isDark)
            {
                if (Pixelator.Instance.AdditionalCoreStackRenderPass == null)
                {
                    m_material = new Material(DarknessEffectShader);
                    Pixelator.Instance.AdditionalCoreStackRenderPass = m_material;
                }
                if (m_material != null)
                {
                    float num = GameManager.Instance.PrimaryPlayer.FacingDirection;
                    if (num > 270f)
                    {
                        num -= 360f;
                    }
                    if (num < -270f)
                    {
                        num += 360f;
                    }
                    m_material.SetFloat("_ConeAngle", this.FlashlightAngle);
                    Vector4 centerPointInScreenUV = GetCenterPointInScreenUV(GameManager.Instance.PrimaryPlayer.CenterPosition);
                    centerPointInScreenUV.z = num;
                    Vector4 vector = centerPointInScreenUV;
                    if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
                    {
                        num = GameManager.Instance.SecondaryPlayer.FacingDirection;
                        if (num > 270f)
                        {
                            num -= 360f;
                        }
                        if (num < -270f)
                        {
                            num += 360f;
                        }
                        vector = GetCenterPointInScreenUV(GameManager.Instance.SecondaryPlayer.CenterPosition);
                        vector.z = num;
                    }
                    m_material.SetVector("_Player1ScreenPosition", centerPointInScreenUV);
                    m_material.SetVector("_Player2ScreenPosition", vector);
                }
            }
        }
        private static Vector4 GetCenterPointInScreenUV(Vector2 centerPoint)
        {
            Vector3 vector = GameManager.Instance.MainCameraController.Camera.WorldToViewportPoint(centerPoint.ToVector3ZUp(0f));
            return new Vector4(vector.x, vector.y, 0f, 0f);
        }
       private void EnableDarkness()
        {
            if (isDark) return;

            m_material = new Material(DarknessEffectShader);
            Pixelator.Instance.AdditionalCoreStackRenderPass = m_material;

            isDark = true;
        }
        private void DisableDarkness()
        {
            if (!isDark) return;
            if (Pixelator.Instance)
            {
                Pixelator.Instance.AdditionalCoreStackRenderPass = null;
            }
            isDark = false;
        }
        public static bool isDark = false;
        public static Shader DarknessEffectShader;
        public float FlashlightAngle = 25f;
        //private AdditionalBraveLight[] flashlights;
        //private int m_valueMinID;
        //private RoomHandler m_room;
        private static Material m_material;
    }
}
