using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class SnarbolaxArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to ChromaEquinox");
        }

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 34;
            Item.height = 24;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateVanity(Player player)
        {
            if (Main.rand.NextBool(10))
            {
                int idx = Dust.NewDust(player.position, player.width, player.height, 96);
                Main.dust[idx].noGravity = true;
            }
        }
    }
}