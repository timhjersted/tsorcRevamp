using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items
{
    class FairyInABottle : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("A fairy can be seen trapped in the bottle.\n" + "Using this will free the fairy.");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.consumable = true;
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
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.FairyInABottle.Guide"), Color.HotPink);
            return true;
        }
    }
}