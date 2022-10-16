using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class MagicCircleDoer
    {
        public static HeatIndicatorController DoMagicCircle(Vector2 position, float radius, float lengthOfStay, Color colour, bool isFire)
        {
            HeatIndicatorController m_radialIndicator = ((GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/HeatIndicator"), position, Quaternion.identity)).GetComponent<HeatIndicatorController>();
            m_radialIndicator.CurrentColor = colour;
            m_radialIndicator.IsFire = isFire;
            m_radialIndicator.CurrentRadius = radius;
            GameManager.Instance.StartCoroutine(HandleTime(lengthOfStay, m_radialIndicator));
            return m_radialIndicator;
        }
        private static IEnumerator HandleTime(float time, HeatIndicatorController circle)
        {
            yield return new WaitForSeconds(time);
            circle.EndEffect();
            yield break;
        }
    }
}
