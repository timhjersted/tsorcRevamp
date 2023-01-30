using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.Items
{
    class TheEnd : ModItem
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
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())))
                    {
                        tooltips[i].Text = "As the burning remains of the Mindflayer King lay before you, you look up and out to the horizon." +
                               "\nYou feel relieved, but notice your heart is still not at peace. Attraidies was known for his games." +
                               "\nEven in death you suspect his sway over the world has not ended." +
                               "\nYou remember the magic spell that he put on Aaron and wonder if he cast the same spell on himself..." +
                               "\n[c/00ffd4:Looking down, you notice a drill lying in the ashes, still hot to the touch...]" +
                               "\n[c/ffbf00:Congratulations on beating the game! We hope you enjoyed it!- Tim Hjersted & the Revamp Team]";
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
