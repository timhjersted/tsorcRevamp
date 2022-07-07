using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    class DwarvenCrusaderHelmet : ModItem
    {

        public override void SetStaticDefaults() //To be reworked
        {
            Tooltip.SetDefault("Set bonus grants +8 defense, +9% melee damage, and +9% melee speed. \n+6 life regen when health falls below 80, +3 otherwise.");
        }

        public override void SetDefaults()
        {
            Item.height = Item.width = 18;
            Item.defense = 4;
            Item.value = 15000;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<Items.Armors.DwarvenArmor>() && legs.type == ModContent.ItemType<Items.Armors.DwarvenGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.statDefense += 8;
            player.GetDamage(DamageClass.Melee) += 0.09f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.09f;
            if (player.statLife < 80) player.lifeRegen += 6;
            else player.lifeRegen += 3;
        }
    }
}
