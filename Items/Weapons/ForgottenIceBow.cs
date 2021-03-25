using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class ForgottenIceBow : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Casts magic shards of ice from your bow." + 
                                "\nAttuned with the greatest powers when wielded by mages." + 
                                "\nEach shot can be channeled with the powers of your mind once in the air." + 
                                "\nChanneling is useful for directing the shot directly above your enemies for maximum damage");
        }

        public override void SetDefaults() {
            item.damage = 130;
            item.height = 58;
            item.knockBack = 4;
            item.noMelee = true;
            item.magic = true;
            item.rare = ItemRarityID.LightRed;
            item.mana = 40;
            item.channel = true;
            item.autoReuse = true;
            item.scale = 0.9f;
            item.shootSpeed = 14;
            item.useAnimation = 15;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 15;
            item.value = 30000000;
            item.width = 16;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice5Ball>();
        }

        public override void AddRecipes() {
            //todo add ingredients
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("ForgottenIceBowScroll"), 1);
            recipe.AddIngredient(mod.GetItem("Ice4Tome"), 1);
            recipe.AddIngredient(mod.GetItem("SoulOfArtorias"), 1);
            recipe.AddIngredient(mod.GetItem("Humanity"), 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 200000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
