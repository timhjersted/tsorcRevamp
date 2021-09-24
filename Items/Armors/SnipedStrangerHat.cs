using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class SnipedStrangerHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to ChromaEquinox");
        }

        public override void SetDefaults()
        {
            item.vanity = true;
            item.width = 30;
            item.height = 28;
            item.value = 50000;
            item.rare = ItemRarityID.Purple;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<Items.Armors.SnarbolaxArmor>() && legs.type == ModContent.ItemType<Items.Armors.SnarbolaxGreaves>();
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
            player.armorEffectDrawOutlinesForbidden = true;
        }

        public override bool DrawHead()
        {
            return false;
        }
    }
}