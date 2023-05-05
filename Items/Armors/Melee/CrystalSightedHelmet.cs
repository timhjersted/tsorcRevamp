using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Head)]
    public class CrystalSightedHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Dazzling armor cut from crystal" +
                "\nIncreases melee damage by 6%" +
                "\nSet Bonus: Increases critical strike damage by 25%" +
                "\nIncreases melee speed by 18%, doubled when under 40% life"); */
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
            player.GetDamage(DamageClass.Melee) += 0.06f;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MythrilOrichalcumCritDamage = true;
            player.GetAttackSpeed(DamageClass.Melee) += 0.18f;

            if (player.statLife <= (player.statLifeMax2 / 5 * 2))
            {
                player.GetAttackSpeed(DamageClass.Melee) += 0.18f;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 42, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 105, Color.Aqua, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.MythrilHelmet);
            recipe2.AddIngredient(ItemID.OrichalcumMask);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
