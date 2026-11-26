using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Tools
{
    public class DiamondPickaxe : ModItem
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
            Item.pick = 54;
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
            recipe.AddIngredient(ItemID.CopperPickaxe, 1);
            recipe.AddIngredient(ItemID.Diamond, 6);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
