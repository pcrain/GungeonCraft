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

            //Make Wolf pettable
            ETGModConsole.Log("Making wolfyboi pettable");
            CompanionItem wolfyboi = Gungeon.Game.Items["wolf"].GetComponent<CompanionItem>();
            AIActor wolfyboiai = EnemyDatabase.GetOrLoadByGuid(wolfyboi.CompanionGuid);
            wolfyboiai.gameObject.AddComponent<PettableCompanionController>();
            // wolfyboiai.aiActor = doggoai.aiActor;
        }
    }
    public class PettableCompanionController : CompanionController
    {
        private void Start()
        {
            this.CanBePet = true;
            // CompanionItem doggo = Gungeon.Game.Items["dog"].GetComponent<CompanionItem>();
            // AIActor doggoai = EnemyDatabase.GetOrLoadByGuid(doggo.CompanionGuid);
            // wolfyboiai.animationAudioEvents = doggoai.animationAudioEvents;
        }
        public override void Update()
        {
            if(!(this.IsBeingPet && this.m_owner.IsPetting))
                base.Update();
            else
                this.DoPet(this.m_owner);
        }
    }
}


