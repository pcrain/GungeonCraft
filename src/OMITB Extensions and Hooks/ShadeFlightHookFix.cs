using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Alexandria;
using Alexandria.CharacterAPI;

namespace NevernamedsItems
{
    public static class ShadeFlightHookFix
    {
        public static void Init()
        {
            shadefixhook = new Hook(
                    typeof(PlayerController).GetMethod("GetBaseAnimationName", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(ShadeFlightHookFix).GetMethod("GetBaseAnimationNameHook", BindingFlags.Static | BindingFlags.NonPublic));

            shadehandfix = new Hook(
                    typeof(PlayerController).GetMethod("HandleGunAttachPointInternal", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(ShadeFlightHookFix).GetMethod("HandleGunAttachPointInternalHook", BindingFlags.Static | BindingFlags.NonPublic));
        }
        public static Hook shadefixhook;
        public static Hook shadehandfix;
        public delegate TResult FuncC<T, T2, T3, T4, T5, TResult>(T arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        private static string GetBaseAnimationNameHook(FuncC<PlayerController, Vector2, float, bool, bool, string> orig, PlayerController self, Vector2 v, float gunAngle, bool invertThresholds = false, bool forceTwoHands = false)
        {
            
            if (self.characterIdentity == OMITBChars.Shade)
            {
                string text = string.Empty;
                bool flag = self.CurrentGun != null;
                if (flag && self.CurrentGun.Handedness == GunHandedness.NoHanded)
                {
                    forceTwoHands = true;
                }
                if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.END_TIMES)
                {
                    flag = false;
                }
                float num = 155f;
                float num2 = 25f;
                if (invertThresholds)
                {
                    num = -155f;
                    num2 -= 50f;
                }
                float num3 = 120f;
                float num4 = 60f;
                float num5 = -60f;
                float num6 = -120f;
                bool flag2 = gunAngle <= num && gunAngle >= num2;
                if (invertThresholds)
                {
                    flag2 = (gunAngle <= num || gunAngle >= num2);
                }

                var renderBodyHand = !self.ForceHandless && self.CurrentSecondaryGun == null && (self.CurrentGun == null || self.CurrentGun.Handedness != GunHandedness.TwoHanded);



                if (!self.IsGhost && self.IsFlying && !self.IsPetting && (v == Vector2.zero || self.IsStationary))
                {
                    if (flag2)
                    {
                        if (gunAngle < num3 && gunAngle >= num4)
                        {
                            string text2 = ((!forceTwoHands && flag) || self.ForceHandless) ? ((!renderBodyHand) ? "idle_backward" : "idle_backward_hand") : "idle_backward_twohands";
                            text = text2;
                        }
                        else
                        {
                            string text3 = ((!forceTwoHands && flag) || self.ForceHandless) ? "idle_bw" : "idle_bw_twohands";
                            text = text3;
                        }
                    }
                    else if (gunAngle <= num5 && gunAngle >= num6)
                    {
                        string text4 = ((!forceTwoHands && flag) || self.ForceHandless) ? ((!renderBodyHand) ? "idle_forward" : "idle_forward_hand") : "idle_forward_twohands";
                        text = text4;
                    }
                    else
                    {
                        string text5 = ((!forceTwoHands && flag) || self.ForceHandless) ? ((!renderBodyHand) ? "idle" : "idle_hand") : "idle_twohands";
                        text = text5;
                    }
                }
                else if (flag2 && !self.IsGhost && self.IsFlying)
                {
                    string text6 = ((!forceTwoHands && flag) || self.ForceHandless) ? "run_right_bw" : "run_right_bw_twohands";
                    if (gunAngle < num3 && gunAngle >= num4)
                    {
                        text6 = (((!forceTwoHands && flag) || self.ForceHandless) ? ((!renderBodyHand) ? "run_up" : "run_up_hand") : "run_up_twohands");
                    }
                    text = text6;
                }
                else if (!self.IsGhost && self.IsFlying)
                {
                    string text7 = "run_right";
                    if (gunAngle <= num5 && gunAngle >= num6)
                    {
                        text7 = "run_down";
                    }
                    if ((forceTwoHands || !flag) && !self.ForceHandless)
                    {
                        text7 += "_twohands";
                    }
                    else if (renderBodyHand)
                    {
                        text7 += "_hand";
                    }
                    text = text7;
                }

                return string.IsNullOrEmpty(text) ? orig(self, v, gunAngle, invertThresholds, forceTwoHands) : text;
            }
            return orig(self, v, gunAngle, invertThresholds, forceTwoHands);
        }
        private static void HandleGunAttachPointInternalHook(Action<PlayerController, Gun, bool> orig, PlayerController self, Gun targetGun, bool isSecondary = false)
        {
            if (self.characterIdentity == (PlayableCharacters)274131)
            {
                FieldInfo _startingAttachPointPosition = typeof(PlayerController).GetField("m_startingAttachPointPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo _spriteDimensions = typeof(PlayerController).GetField("m_spriteDimensions", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo _currentGunAngle = typeof(PlayerController).GetField("m_currentGunAngle", BindingFlags.NonPublic | BindingFlags.Instance);

                if (targetGun == null)
                {
                    return;
                }



                Vector3 vector = ((Vector3)_startingAttachPointPosition.GetValue(self));
                Vector3 vector2 = self.downwardAttachPointPosition;
                if (targetGun.IsForwardPosition)
                {
                    vector = vector.WithX(((Vector3)_spriteDimensions.GetValue(self)).x - vector.x);
                    vector2 = vector2.WithX(((Vector3)_spriteDimensions.GetValue(self)).x - vector2.x);
                }
                if (self.SpriteFlipped)
                {
                    vector = vector.WithX(((Vector3)_spriteDimensions.GetValue(self)).x - vector.x);
                    vector2 = vector2.WithX(((Vector3)_spriteDimensions.GetValue(self)).x - vector2.x);
                }
                float num = (float)((!self.SpriteFlipped) ? 1 : -1);
                Vector3 a = targetGun.GetCarryPixelOffset(self.characterIdentity).ToVector3();
                vector += Vector3.Scale(a, new Vector3(num, 1f, 1f)) * 0.0625f;
                vector2 += Vector3.Scale(a, new Vector3(num, 1f, 1f)) * 0.0625f;
                if (targetGun.Handedness == GunHandedness.NoHanded && self.SpriteFlipped)
                {
                    vector += Vector3.Scale(targetGun.leftFacingPixelOffset.ToVector3(), new Vector3(num, 1f, 1f)) * 0.0625f;
                    vector2 += Vector3.Scale(targetGun.leftFacingPixelOffset.ToVector3(), new Vector3(num, 1f, 1f)) * 0.0625f;
                }
                if (isSecondary)
                {
                    if (targetGun.transform.parent != self.SecondaryGunPivot)
                    {
                        targetGun.transform.parent = self.SecondaryGunPivot;
                        targetGun.transform.localRotation = Quaternion.identity;
                        targetGun.HandleSpriteFlip(self.SpriteFlipped);
                        targetGun.UpdateAttachTransform();
                    }
                    self.SecondaryGunPivot.position = self.gunAttachPoint.position + num * new Vector3(-0.75f, 0f, 0f);
                }
                else
                {
                    if (targetGun.transform.parent != self.gunAttachPoint)
                    {
                        targetGun.transform.parent = self.gunAttachPoint;
                        targetGun.transform.localRotation = Quaternion.identity;
                        targetGun.HandleSpriteFlip(self.SpriteFlipped);
                        targetGun.UpdateAttachTransform();
                    }
                    if (targetGun.IsHeroSword)
                    {
                        float t = 1f - Mathf.Abs((float)_currentGunAngle.GetValue(self) - 90f) / 90f;
                        self.gunAttachPoint.localPosition = BraveUtility.QuantizeVector(Vector3.Slerp(vector, vector2, t), 16f);
                    }
                    else if (targetGun.Handedness == GunHandedness.TwoHanded)
                    {
                        float t2 = Mathf.PingPong(Mathf.Abs(1f - Mathf.Abs((float)_currentGunAngle.GetValue(self) + 90f) / 90f), 1f);
                        Vector2 vector3 = Vector2.zero;
                        if ((float)_currentGunAngle.GetValue(self) > 0f)
                        {
                            vector3 = Vector2.Scale(targetGun.carryPixelUpOffset.ToVector2(), new Vector2(num, 1f)) * 0.0625f;
                        }
                        else
                        {
                            vector3 = Vector2.Scale(targetGun.carryPixelDownOffset.ToVector2(), new Vector2(num, 1f)) * 0.0625f;
                        }
                        if (targetGun.LockedHorizontalOnCharge)
                        {
                            vector3 = Vector3.Slerp(vector3, Vector2.zero, targetGun.GetChargeFraction());
                        }
                        if ((float)_currentGunAngle.GetValue(self) < 0f)
                        {
                            self.gunAttachPoint.localPosition = BraveUtility.QuantizeVector(Vector3.Slerp(vector, vector2 + vector3.ToVector3ZUp(0f), t2), 16f);
                        }
                        else
                        {
                            self.gunAttachPoint.localPosition = BraveUtility.QuantizeVector(Vector3.Slerp(vector, vector + vector3.ToVector3ZUp(0f), t2), 16f);
                        }
                    }
                    else
                    {
                        self.gunAttachPoint.localPosition = BraveUtility.QuantizeVector(vector, 16f);
                    }
                }
            }
            else
            {
                orig(self, targetGun, isSecondary);

            }
        }
    }
}
