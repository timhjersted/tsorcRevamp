using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class MeteorShower : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Causes meteorites to rain from the sky.");
        }
        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 5;
            item.useTime = 5;
            item.autoReuse = true;
            item.UseSound = SoundID.Item8;
            item.rare = ItemRarityID.LightRed;
            item.knockBack = 3;
            item.mana = 10;
            item.damage = 40;
            item.autoReuse = true;
            item.noMelee = true;
            item.value = 100000;
            item.magic = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.MeteoriteBar, 20);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player)
        {
            Projectile.NewProjectile(
                (float)(Main.mouseX + Main.screenPosition.X) - 100 + Main.rand.Next(200),
                (float)player.position.Y - 800.0f,
                (float)(Main.rand.Next(-40, 40)) / 10,
                14.9f,
                ModContent.ProjectileType<Projectiles.MeteorShower>(),
                50,
                2.0f,
                player.whoAmI);
            return true;
        }
    }
}
