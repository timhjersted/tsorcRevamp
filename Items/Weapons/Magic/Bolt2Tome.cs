using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class Bolt2Tome : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Bolt 2 Tome");
            Tooltip.SetDefault("A lost tome for artisans." +
                                "\nDrops a lightning strike upon collision" +
                                "\nHas a chance to electrify enemies" +
                                "\nCan be upgraded with 25,000 Dark Souls and 15 Soul of Light.");

        }

        public override void SetDefaults() {

            item.damage = 17;
            item.height = 10;
            item.knockBack = 0f;
            item.autoReuse = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.Orange;
            item.shootSpeed = 7f;
            item.magic = true;
            item.noMelee = true;
            item.mana = 20;
            item.useAnimation = 30;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 30;
            item.value = PriceByRarity.Orange_3;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Bolt2Ball>();
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
            recipe.AddIngredient(mod.GetItem("Bolt1Tome"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 8000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
