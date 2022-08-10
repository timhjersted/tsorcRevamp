using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    class DwarvenCrusaderHelmet : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases whip damage by 12%\nSet Bonus: Increases whip range by 20% and speed by 25%" +
                "\nWhen health is above 333, gain 7% minion damage + 7% whip damage, 17% whip speed and 6 life regen");
        }

        public override void SetDefaults()
        {
            Item.height = Item.width = 18;
            Item.defense = 9;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DwarvenArmor>() && legs.type == ModContent.ItemType<DwarvenGreaves>();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.12f;
        }
        public override void UpdateArmorSet(Player player)
        {
            player.whipRangeMultiplier += 0.2f;
            player.GetAttackSpeed(DamageClass.Summon) += 0.25f;

            if (player.statLife > 333)
            {
                player.GetDamage(DamageClass.Summon) += 0.07f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.07f;
                player.GetAttackSpeed(DamageClass.Summon) += 0.17f;
                player.lifeRegen += 6;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 42, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 105, Color.Gold, 1.0f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedHood, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6600);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
