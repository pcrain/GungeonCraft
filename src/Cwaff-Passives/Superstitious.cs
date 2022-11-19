using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

using ETGGUI;
using SGUI;

namespace CwaffingTheGungy
{
    public class Superstitious : PassiveItem
    {
        public static string passiveName      = "Superstitious";
        public static string spritePath       = "CwaffingTheGungy/Resources/NeoItemSprites/88888888_icon";
        public static string shortDescription = "Writings on the HUD";
        public static string longDescription  = "(6s and 7s)";

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<Superstitious>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.C;

            ETGModMainBehaviour.Instance.gameObject.AddComponent<MyHUDController>();
        }

        // public override void Pickup(PlayerController player)
        // {
        //     base.Pickup(player);
        // }

        // public override DebrisObject Drop(PlayerController player)
        // {
        //     return base.Drop(player);
        // }
    }

    public class MyHUDController : MonoBehaviour
    {
        public static MyHUDController Instance;

        SGroup statsContainer;
        Dictionary<string, Stat> stats = new Dictionary<string, Stat>();

        void SpawnLabels()
        {
            SGroup statsContainer = new SGroup() { Background = Color.clear, Size = new Vector2(400, 2), AutoGrowDirection = SGroup.EDirection.Vertical };

            var stat = new Stat();
            statsContainer.Children.Add(stat.container);
            stats["test"] = stat;

            statsContainer.Children.Add(new SRect(Color.clear) { Size = Vector2.zero }); // add empty element to the beginning due to a bug in SGUI's code TT
            // weaponStats.ForEach((s) => GenerateStatLabel(s));
            statsContainer.Children.Add(new SRect(Color.clear) { Size = Vector2.zero.WithY(32) });
            // playerStats.ForEach((s) => GenerateStatLabel(s));

            statsContainer.AutoLayout = (SGroup g) => new Action<int, SElement>(g.AutoLayoutVertical);
            SGUIRoot.Main.Children.Add(statsContainer);
        }

        void Awake()
        {
            // singleton
            Instance = this;
            SpawnLabels();
        }

        void LateUpdate()
        {
            // update stats
            foreach (var stat in stats.Values)
                stat.Update();

            // update hud
            statsContainer.ContentSize = statsContainer.Size.WithY(0);
            statsContainer.UpdateStyle();
            statsContainer.Position.y = statsContainer.Root.Size.y / 2 - statsContainer.Size.y / 2 + 5;
        }
    }

    class Stat
    {
        public float value;
        public float multiplier;
        public float previousValue;
        public float previousMultiplier;
        public bool visible;

        public float baseAlpha; // currently always 1

        public bool lessIsBetter;
        public bool announceBaseChange; // either stat has no multiplier, or is already a multiplier

        public SGroup container; // houses layout group & button
        public SGroup layout; // icon + value + multiplier + difference
        public SButton button; // for toggling visibility
        public SImage icon;
        public SImage infinity;
        public SImage visibilityIcon;
        public SLabel label; // base stat
        public SLabel label2; // stat's multiplier
        public SLabel labelChange; // difference

        float multiplierBeforeChanges;
        float valueBeforeChanges;
        bool doingChange;

        float changeAppearTime;

        public Stat()
        {
            label = new SLabel("STAT");

            container = new SGroup() { Background = Color.clear, Size = new Vector2(300, 50) };

            layout = new SGroup() { Background = Color.clear, Size = container.Size, AutoLayoutVerticalStretch = false };
            layout.Children.Add(new SRect(Color.clear) { Size = Vector2.zero.WithX(8) });
            layout.Children.Add(label);
            layout.AutoLayout = (SGroup g) => new Action<int, SElement>(g.AutoLayoutHorizontal);
            container.Children.Add(layout);

            multiplier = 1f;
            baseAlpha = 1f;
            visible = true;
        }

        public void Update()
        {
            label.Text = "Hello!";
            label.Update();
        }
    }
}

