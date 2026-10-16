using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;

namespace BetterGrappleWorms
{
    //better grapple worms remix menu
    public class BGWConfig : OptionInterface
    {

        public static Configurable<bool> corridorDropTongueDisable;
        public static Configurable<bool> zeroGTongue;
        public static Configurable<bool> vineTongue;
        public static Configurable<bool> vineTongueZeroGOnly;

        OpCheckBox vineTongueBox;
        OpCheckBox vineTongueZeroGBox;
        OpLabel vineTongueZeroGLabel;

        List<UIelement> elements;

        public BGWConfig()
        {
            zeroGTongue = this.config.Bind<bool>("zeroGTongue", true);
            corridorDropTongueDisable = this.config.Bind<bool>("corridorDropTongueDisable", true);
            vineTongue = this.config.Bind<bool>("vineTongue", true);
            vineTongueZeroGOnly = this.config.Bind<bool>("vineTongueZeroGOnly", false);

            elements = new List<UIelement>();
        }

        void Add(UIelement element, string description)
        {
            elements.Add(element);
            if (description != "")
            {
                element.description = description;
            }
            this.Tabs[0]._AddItem(element);
        }

        public override void Initialize()
        {
            base.Initialize();

            this.Tabs = new OpTab[]
                {
            new OpTab(this, "Options")
                };

            //big title text
            Add(new OpLabel(new UnityEngine.Vector2(0f, 500f), new UnityEngine.Vector2(600f, 100f), "Coral Grapple", FLabelAlignment.Center, true), "");

            //this is kinda sloppy but you only have to modify this one set of numbers to adjust the layout so who cares
            Vector2 pos = new Vector2(50, 500f);
            Vector2 posIncrement = new Vector2(0, -30f);
            Vector2 labelOffset = new Vector2(35f, -2f);

            string text = "Enable overhauled grappling worm controls in zero gravity";
            Add(new OpCheckBox(zeroGTongue, pos), text);
            Add(new OpLabel(pos + labelOffset, new Vector2(400f, 30f), "Zero Gravity Grapple Controls", FLabelAlignment.Left, false), text);
            pos += posIncrement;

            text = "Disable grappling worm tongue when dropping down a tunnel with jump button controls";
            Add(new OpCheckBox(corridorDropTongueDisable, pos), text);
            Add(new OpLabel(pos + labelOffset, new Vector2(400f, 30f), "Disable Tongue During Corridor Drop", FLabelAlignment.Left, false), text);
            pos += posIncrement;

            text = "Allow grappling worm tongues to stick to coral neurons, corruption tubes, and other vine-like objects";
            vineTongueBox = new OpCheckBox(vineTongue, pos);
            Add(vineTongueBox, text);
            Add(new OpLabel(pos + labelOffset, new Vector2(400f, 30f), "Grapple Vines", FLabelAlignment.Left, false), text);
            pos += posIncrement;

            text = "If the Grapple Vines setting is enabled, only allow them to be grappled in zero gravity";
            //save these for later so they can be grayed out
            vineTongueZeroGBox = new OpCheckBox(vineTongueZeroGOnly, pos);
            vineTongueZeroGLabel = new OpLabel(pos + labelOffset, new Vector2(400f, 30f), "Grapple Vines - Zero Gravity Only", FLabelAlignment.Left, false);
            Add(vineTongueZeroGBox, text);
            Add(vineTongueZeroGLabel, text);
            pos += posIncrement;
        }

        public override void Update()
        {
            base.Update();
            UnityEngine.Debug.Log(vineTongueBox.value);
            vineTongueZeroGBox.greyedOut = vineTongueBox.value == "false";
        }
    }
}