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
            item.vanity = true;
            item.width = 34;
            item.height = 24;
            item.value = 50000;
            item.rare = ItemRarityID.Purple;
        }

        public override void UpdateVanity(Player player, EquipType type)
        {
            if (Main.rand.Next(10) == 0)
            {
                int idx = Dust.NewDust(player.position, player.width, player.height, 96);
                Main.dust[idx].noGravity = true;
            }
        }
    }
}