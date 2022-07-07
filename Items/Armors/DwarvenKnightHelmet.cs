using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("AncientDwarvenHelmet")]
    [AutoloadEquip(EquipType.Head)]
    class DwarvenKnightHelmet : ModItem //To be reworked
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Set bonus grants +4 defense, +7% melee damage, and +7% melee speed. \n+4 life regen when health falls below 80, +1 otherwise.");
        }

        public override void SetDefaults()
        {
            Item.height = Item.width = 18;
            Item.defense = 4;
            Item.value = 10000;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<Items.Armors.DwarvenArmor>() && legs.type == ModContent.ItemType<Items.Armors.DwarvenGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.statDefense += 4;
            player.GetDamage(DamageClass.Melee) += 0.07f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.07f;
            if (player.statLife < 80) player.lifeRegen += 4;
            else player.lifeRegen += 1;
        }
    }
}
