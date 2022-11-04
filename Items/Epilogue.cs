using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class Epilogue : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/DeathStrikeScroll";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("This item is from the future, and its text is obscured. You will have to progress further in your adventure to reveal it...");
        }

        public override void SetDefaults()
        {
            Item.maxStack = 1;
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Pink;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Text.Contains("obscured"))
                {
                    if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()))
                    {
                        tooltips[i].Text = "[c/ffbf00:Congratulations! You have unlocked the true ending of the game!]" +
                               "\nHaving defeated Gwyn, the portal that Attraidies opened at the moment of his death has closed." +
                               "\nAt last, you can finally feel the energies of the corruption losing their hold on the world," +
                               "\nthe darkness receding back into the earth, growing in harmony with the light once again." +
                               "\nYou think of Asha, waiting for you, and rejoice upon the thought of your return home." +
                               "\nYou take a moment to think about all that you have been through... Forever you shall be known" +
                               "\nas the hero of the age. Centuries from now, elders will be telling your story. The story of Red Cloud.";
                    }
                    else
                    {
                        tooltips[i].OverrideColor = Main.DiscoColor;
                    }
                }
            }
        }
    }
}
