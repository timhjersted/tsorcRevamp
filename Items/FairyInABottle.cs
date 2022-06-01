using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class FairyInABottle : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A fairy can be seen trapped in the bottle.\n" + "Using this will free the fairy.");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.consumable = true;
            Item.maxStack = 1;
            Item.value = 0;
            Item.rare = ItemRarityID.Pink;
            Item.useTime = 5;
            Item.useAnimation = 5;
            Item.scale = 1f;
            Item.useStyle = ItemUseStyleID.HoldUp;
        }
        public override bool? UseItem(Player player)
        {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Friendly.FreedFairy>());
            Main.NewText("Check your minimap to find them!", Color.HotPink);
            return true;
        }
    }
}