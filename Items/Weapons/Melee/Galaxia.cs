using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class Galaxia : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Forged from the stars of a distant galaxy");
        }
        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.damage = 75;
            Item.knockBack = 6;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.LightPurple_6;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useTurn = true;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FallenStar, 50);
            recipe.AddIngredient(ItemID.SoulofSight, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 50000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }

        public override void MeleeEffects(Player player, Rectangle hitbox) {
            int dust = Dust.NewDust(new Vector2((float)hitbox.X, (float)hitbox.Y), hitbox.Width, hitbox.Height, 57, (player.velocity.X), player.velocity.Y, 200, default, 1f);
            Main.dust[dust].noGravity = false;
        }
    }
}
