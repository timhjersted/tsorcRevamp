using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class Bolt4Tome : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Bolt 4 Tome");
            Tooltip.SetDefault("A lost legendary tome. You command the forces of the sky.\n" +
                                "Only the most powerful mages will be able to cast this spell.");

        }

        public override void SetDefaults() {
            Item.damage = 100;
            Item.height = 10;
            Item.width = 34;
            Item.knockBack = 0;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Lime;
            Item.shootSpeed = (float)6;
            Item.magic = true;
            Item.noMelee = true;
            Item.mana = 350;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.value = PriceByRarity.Lime_7;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt4Ball>();
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (player.wet)
            {
                player.AddBuff(BuffID.Electrified, 90);
            }

            return true;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("Bolt3Tome"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 85000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
