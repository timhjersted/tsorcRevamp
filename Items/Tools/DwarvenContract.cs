using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Tools
{
    class DwarvenContract : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Materials/ForgottenIceBowScroll";
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("A contract for a dwarf guard.\n" + "Will summon a dwarf to guard a piece of property.");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.consumable = true;
            Item.value = 10000;
            Item.rare = ItemRarityID.Pink;
            Item.useTime = 5;
            Item.useAnimation = 5;
            Item.scale = 1f;
            Item.useStyle = ItemUseStyleID.HoldUp;
        }
        public override bool? UseItem(Player player)
        {
            NPC.SpawnOnPlayer(Main.myPlayer, ModContent.NPCType<NPCs.Friendly.DwarvenGuard>());
            return true;
        }
    }
}