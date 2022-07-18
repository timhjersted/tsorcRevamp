using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class CrystalSightedHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Dazzling armor cut from crystal\nIncreases whip damage by 10%\nSet Bonus: Increases whip range by 50% and speed by 35%" +
                "\nSet Bonus: When health falls below 166, gain 10% minion damage + 10% whip damage on top and 20% whip speed");
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 7;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<CrystalArmor>() && legs.type == ModContent.ItemType<CrystalGreaves>();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.1f;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.whipRangeMultiplier += 0.5f;
            player.GetAttackSpeed(DamageClass.Summon) += 0.35f;
            if (player.statLife < 166)
            {
                player.GetDamage(DamageClass.Summon) += 0.1f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.1f;
                player.GetAttackSpeed(DamageClass.Summon) += 0.2f;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 42, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 105, Color.Aqua, 1.0f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
            }
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpiderMask, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 4400);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
