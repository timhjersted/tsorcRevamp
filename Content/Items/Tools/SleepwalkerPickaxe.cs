using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Tools
{
    public class SleepwalkerPickaxe : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.damage = 0;
            Item.pick = 99;
            Item.damage = 1;
            Item.DamageType = DamageClass.Default;
        }

        public override bool? CanHitNPC(Player player, NPC target)
        {
            return false;
        }

        public override bool CanHitPvp(Player player, Player target)
        {
            return false;
        }

        public override bool AllowPrefix(int pre)
        {
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Damage");
            if (ttindex != -1)
            {
                tooltips.RemoveRange(ttindex, 3);
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DiamondPickaxe>(), 1);
            recipe.AddIngredient(ItemID.ShadowScale, 6);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
            
            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<DiamondPickaxe>(), 1);
            recipe2.AddIngredient(ItemID.TissueSample, 6);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
