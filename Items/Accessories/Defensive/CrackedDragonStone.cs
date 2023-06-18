using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class CrackedDragonStone : ModItem
    {
        public static float Dmg = 3f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
            player.noKnockback = true;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Burning] = true;
            player.buffImmune[BuffID.Chilled] = true;
            player.buffImmune[ModContent.BuffType<Frostbite>()] = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 9000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
