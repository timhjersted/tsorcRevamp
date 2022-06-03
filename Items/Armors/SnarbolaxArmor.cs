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
            Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to ChromaEquinox");
        }

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 34;
            Item.height = 24;
            Item.value = 50000;
            Item.rare = ItemRarityID.Purple;
        }

        public override void UpdateVanity(Player player)
        {
            if (Main.rand.Next(10) == 0)
            {
                int idx = Dust.NewDust(player.position, player.width, player.height, 96);
                Main.dust[idx].noGravity = true;
            }
        }
    }
}