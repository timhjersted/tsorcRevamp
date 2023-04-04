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
            Tooltip.SetDefault("Increases whip damage and summon attack speed by 18%" +
                "\nSet Bonus: Grants Holy Dodge, stats provided by this armor set are doubled while Holy Dodge is active" +
                "\nDefense, minion slots and whip range are not affected by this" +
                "\nIncreases whip range by 30% and life regen by 3");
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
            player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.18f;
            player.GetAttackSpeed(DamageClass.Summon) += 0.18f;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.18f;
                player.GetAttackSpeed(DamageClass.Summon) += 0.18f;
            }
        }
        public override void UpdateArmorSet(Player player)
        {
            player.onHitDodge = true;

            player.whipRangeMultiplier += 0.3f;

            player.lifeRegen += 3;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.lifeRegen += 3;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 42, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 105, Color.Gold, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedHood, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
