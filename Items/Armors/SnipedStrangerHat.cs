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
            // Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to ChromaEquinox");
            ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 30;
            Item.height = 28;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
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
    }
}