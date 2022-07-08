using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("RedMageHat")]
    [AutoloadEquip(EquipType.Head)]
    public class RedClothHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
            Tooltip.SetDefault("Increases magic crit by 7%");
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 16;
            Item.defense = 2;
            Item.value = 6000;
            Item.rare = ItemRarityID.Blue;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += 7;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<RedClothTunic>() && legs.type == ModContent.ItemType<RedClothPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.statManaMax2 += 50;
            player.manaCost += 0.1f;
        }
        //Can be dropped by many enemies
    }
}
