using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using System.ComponentModel;  //debug
using System.Reflection;  //debug

namespace CwaffingTheGungy
{
    class CwaffTweaks
    {
        public static void Init()
        {
            //Get base companion controller from doggo
            CompanionItem doggo = Gungeon.Game.Items["dog"].GetComponent<CompanionItem>();
            AIActor doggoai = EnemyDatabase.GetOrLoadByGuid(doggo.CompanionGuid);
            CompanionController doggocontroller =
                doggoai.gameObject.GetOrAddComponent<CompanionController>();
            doggoai.gameObject.AddComponent<DebugCompanionController>();

            //Make Wolf pettable
            ETGModConsole.Log("Making wolfyboi pettable");
            CompanionItem wolfyboi = Gungeon.Game.Items["wolf"].GetComponent<CompanionItem>();
            AIActor wolfyboiai = EnemyDatabase.GetOrLoadByGuid(wolfyboi.CompanionGuid);
            wolfyboiai.gameObject.AddComponent<PettableCompanionController>();
        }
    }

    public class DebugCompanionController : CompanionController
    {
        private void Start()
        {
            ETGModConsole.Log("debug companion controller loaded");
            // CompanionBuilder.AnimationType
            AIAnimator a = this.sprite.aiAnimator;
            foreach(AIAnimator.NamedDirectionalAnimation n in a.OtherAnimations)
            {
                if (n.name == "pet")
                {
                    DirectionalAnimation d = n.anim;
                    d.WriteJSON("/tmp/jsonpath");
                    foreach(string s in d.AnimNames)
                    {
                        ETGModConsole.Log("aname: "+s);
                    }
                }
                // "pet" is the animation we're looking for
            }
        }
        public override void Update()
        {
            // ETGModConsole.Log(this.sprite.name);
            // ETGModConsole.Log("updated");
            // ETGModConsole.Log(this.spriteAnimator.m_queuedAnimationName);
            base.Update();
        }
    }

    public class PettableCompanionController : CompanionController
    {
        private void Start()
        {
            this.CanBePet = true;
            CompanionItem doggo = Gungeon.Game.Items["dog"].GetComponent<CompanionItem>();
            AIActor doggoai = EnemyDatabase.GetOrLoadByGuid(doggo.CompanionGuid);
            CompanionController doggocontroller =
                doggoai.gameObject.GetComponent<CompanionController>();

            foreach(AIAnimator.NamedDirectionalAnimation n in doggocontroller.sprite.aiAnimator.OtherAnimations)
            {
                if (n.name == "pet")
                {
                    this.sprite.aiAnimator.OtherAnimations.Add(new AIAnimator.NamedDirectionalAnimation
                    {
                        name = "pet",
                        anim = n.anim,
                    });
                }
            }

            // doggocontroller.sprite.aiAnimator.OtherAnimations;
            // wolfyboiai.animationAudioEvents = doggoai.animationAudioEvents;
        }
        // public override void Update()
        // {
        //     // if(!(this.IsBeingPet && this.m_owner.IsPetting))
        //         base.Update();
        //     // else
        //     //     this.DoPet(this.m_owner);
        // }
    }
}


