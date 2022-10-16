using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemAPI;
namespace ItemAPI
{
    class ExampleMod : ETGModule
    {
        public override void Exit()
        {
        }

        public override void Init()
        {
        }

        public override void Start()
        {
            ItemBuilder.Init();
            ExampleActive.Init();
            ExamplePassive.Init();
            //ExampleGun.Init(); //Not implemented yet
        }
    }
}
