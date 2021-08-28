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
            item.damage = 100;
            item.height = 10;
            item.width = 34;
            item.knockBack = 0;
            item.autoReuse = true;
            item.rare = ItemRarityID.LightRed;
            item.shootSpeed = (float)6;
            item.magic = true;
            item.noMelee = true;
            item.mana = 350;
            item.useAnimation = 30;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 30;
            item.value = 100000;
            item.shoot = ModContent.ProjectileType<Projectiles.Bolt4Ball>();
        }
        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if (player.wet)
            {
                player.AddBuff(BuffID.Electrified, 90);
            }

            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("Bolt3Tome"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 85000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
