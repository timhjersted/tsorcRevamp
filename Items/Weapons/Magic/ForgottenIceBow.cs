using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class ForgottenIceBow : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Casts magic shards of ice from your bow." + 
                                "\nAttuned with the greatest powers when wielded by mages." + 
                                "\nEach shot can be channeled with the powers of your mind once in the air." + 
                                "\nChanneling is useful for directing the shot directly above your enemies for maximum damage");
        }

        public override void SetDefaults() {
            Item.damage = 110;
            Item.height = 58;
            Item.knockBack = 4;
            Item.noMelee = true;
            Item.magic = true;
            Item.rare = ItemRarityID.Red;
            Item.mana = 40;
            Item.channel = true;
            Item.autoReuse = true;
            Item.scale = 0.9f;
            Item.shootSpeed = 34;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 15;
            Item.value = PriceByRarity.Red_10;
            Item.width = 16;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ice5Ball>();
        }

        public override void AddRecipes() {
            //todo add ingredients
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("ForgottenIceBowScroll"), 1);
            recipe.AddIngredient(Mod.GetItem("Ice4Tome"), 1);
            recipe.AddIngredient(Mod.GetItem("SoulOfArtorias"), 1);
            recipe.AddIngredient(Mod.GetItem("Humanity"), 30);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 200000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
