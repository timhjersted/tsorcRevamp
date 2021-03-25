using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class Bolt3Tome : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Bolt 3 Tome");
            Tooltip.SetDefault("A lost tome fabled to deal great damage.\n" +
                                "Only mages will be able to realize this tome's full potential. \n" + 
                                "Can be upgraded with 85,000 Dark Souls");

        }

        public override void SetDefaults() {

            item.damage = 35;
            item.height = 10;
            item.width = 34;
            item.knockBack = 0.1f;
            item.autoReuse = true;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 6f;
            item.magic = true;
            item.noMelee = true;
            item.mana = 70;
            item.useAnimation = 25;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 25;
            item.value = 20000;
            item.shoot = ModContent.ProjectileType<Projectiles.Bolt3Ball>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofLight, 15);
            recipe.AddIngredient(mod.GetItem("Bolt2Tome"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 25000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
