using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    public class WitchkingTop : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("+20% minion damage\nIncreases your max number of minions and turrets by 1" +
                "\nSet Bonus: Increases your max number of minions and turrets by 1\n+30% whip range, +25% summon attack speed"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 22;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<WitchkingHelmet>() && legs.type == ModContent.ItemType<WitchkingBottoms>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.maxMinions += 1;
            player.maxTurrets += 1;
            player.GetAttackSpeed(DamageClass.Summon) += 0.25f;
            player.whipRangeMultiplier += 0.3f;

            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.92f, 0.8f, 0.65f);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.2f;
            player.maxMinions += 1;
            player.maxTurrets += 1;
        }
    }
}
