using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class EasyTrailBullet : BraveBehaviour //----------------------------------------------------------------------------------------------
    {
        public EasyTrailBullet()
        {
            //=====
            this.TrailPos = new Vector3(0, 0, 0);
            //======
            this.BaseColor = Color.red;
            this.StartColor = Color.red;
            this.EndColor = Color.white;
            //======
            this.LifeTime = 1f;
            //======
            this.StartWidth = 1;
            this.EndWidth = 0;

        }
        /// <summary>
        /// Lets you add a trail to your projectile.    
        /// </summary>
        /// <param name="TrailPos">Where the trail attaches its center-point to. You can input a custom Vector3 but its best to use the base preset. (Namely"projectile.transform.position;").</param>
        /// <param name="BaseColor">The Base Color of your trail.</param>
        /// <param name="StartColor">The Starting color of your trail.</param>
        /// <param name="EndColor">The End color of your trail. Having it different to the StartColor will make it transition from the Starting/Base Color to its End Color during its lifetime.</param>
        /// <param name="LifeTime">How long your trail lives for.</param>
        /// <param name="StartWidth">The Starting Width of your Trail.</param>
        /// <param name="EndWidth">The Ending Width of your Trail. Not sure why youd want it to be something other than 0, but the options there.</param>
        public void Start()
        {
            proj = base.projectile;
            {
                TrailRenderer tr;
                var tro = base.projectile.gameObject.AddChild("trail object");
                tro.transform.position = base.projectile.transform.position;
                tro.transform.localPosition = TrailPos;

                tr = tro.AddComponent<TrailRenderer>();
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.mainTexture = _gradTexture;
                tr.material = mat;
                tr.minVertexDistance = 0.1f;
                //======
                mat.SetColor("_Color", BaseColor);
                tr.startColor = StartColor;
                tr.endColor = EndColor;
                //======
                tr.time = LifeTime;
                //======
                tr.startWidth = StartWidth;
                tr.endWidth = EndWidth;
            }

        }

        public Texture _gradTexture;
        private Projectile proj;

        public Vector2 TrailPos;
        public Color BaseColor;
        public Color StartColor;
        public Color EndColor;
        public float LifeTime;
        public float StartWidth;
        public float EndWidth;

    }
    public class EasyTrailMisc : BraveBehaviour //----------------------------------------------------------------------------------------------
    {
        public EasyTrailMisc()
        {
            //=====
            this.TrailPos = new Vector3(0, 0, 0);
            //======
            this.BaseColor = Color.red;
            this.StartColor = Color.red;
            this.EndColor = Color.white;
            //======
            this.LifeTime = 1f;
            //======
            this.StartWidth = 1;
            this.EndWidth = 0;

        }
        /// <summary>
        /// Lets you add a trail to your projectile.    
        /// </summary>
        /// <param name="TrailPos">Where the trail attaches its center-point to. You can input a custom Vector3 but its best to use the base preset. (Namely"projectile.transform.position;").</param>
        /// <param name="BaseColor">The Base Color of your trail.</param>
        /// <param name="StartColor">The Starting color of your trail.</param>
        /// <param name="EndColor">The End color of your trail. Having it different to the StartColor will make it transition from the Starting/Base Color to its End Color during its lifetime.</param>
        /// <param name="LifeTime">How long your trail lives for.</param>
        /// <param name="StartWidth">The Starting Width of your Trail.</param>
        /// <param name="EndWidth">The Ending Width of your Trail. Not sure why youd want it to be something other than 0, but the options there.</param>
        public void Start()
        {
            gameobject = base.gameObject;
            {
                TrailRenderer tr;
                var tro = base.gameObject.AddChild("trail object");
                tro.transform.position = base.transform.position;
                tro.transform.localPosition = TrailPos;
                
                tr = tro.AddComponent<TrailRenderer>();
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.mainTexture = _gradTexture;
                tr.material = mat;
                tr.minVertexDistance = 0.1f;
                //======
                mat.SetColor("_Color", BaseColor);
                tr.startColor = StartColor;
                tr.endColor = EndColor;
                //======
                tr.time = LifeTime;
                //======
                tr.startWidth = StartWidth;
                tr.endWidth = EndWidth;
            }

        }

        public Texture _gradTexture;
        private GameObject gameobject;

        public Vector2 TrailPos;
        public Color BaseColor;
        public Color StartColor;
        public Color EndColor;
        public float LifeTime;
        public float StartWidth;
        public float EndWidth;

    }
}
