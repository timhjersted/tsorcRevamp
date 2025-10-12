using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Lore
{
    public abstract class Crest : ModItem
    { //crests are using an abstract so we dont have 6 different .cs files with essentially nothing in them
        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.rare = ItemRarityID.Blue;
        }
    }

    public class CrestOfFire : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
        }
    }

    public class CrestOfWater : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
        }
    }
    public class CrestOfEarth : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && tsorcRevampWorld.RemixMap)
            {
                tooltips.Add(new TooltipLine(Mod, "DarkMirrorAdventure", LangUtils.GetTextValue("Items.CrestOfEarth.RemixMode")));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "DarkMirrorDefault", LangUtils.GetTextValue("Items.CrestOfEarth.AdvMode")));
            }
        }

        public override void SetStaticDefaults()
        {
        }
    }

    public class CrestOfCorruption : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
        }
    }

    public class CrestOfSky : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.maxStack = 3;
        }

        public override void SetStaticDefaults()
        {
        }
    }

    public class CrestOfSteel : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        } 
        
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && tsorcRevampWorld.RemixMap)
            {
                tooltips.Add(new TooltipLine(Mod, "DarkMirrorAdventure", LangUtils.GetTextValue("Items.CrestOfSteel.RemixMode")));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "DarkMirrorDefault", LangUtils.GetTextValue("Items.CrestOfSteel.AdvMode")));
            }
        }

        public override void SetStaticDefaults()
        {
        }
    }
    public class CrestOfLife : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
        }
    }
    public class CrestOfStone : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
        }
    }
}
