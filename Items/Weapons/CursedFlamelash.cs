using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class CursedFlamelash : ModItem {

        public override void SetDefaults() {
            item.width = 26;
            item.height = 26;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 19;
            item.useTime = 19;
            item.channel = true;
            item.damage = 47;
            item.knockBack = 4;
            item.UseSound = SoundID.Item20;
            item.rare = ItemRarityID.LightRed;
            item.crit = 4;
            item.mana = 17;
            item.noMelee = true;
            item.value = 250000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.CursedFlamelash>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Flamelash, 1);
            recipe.AddIngredient(ItemID.CursedFlame, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
