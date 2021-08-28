using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class Bolt1Tome : ModItem {

        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Bolt 1 Tome");
            Tooltip.SetDefault("A lost beginner's tome" +
                                "\nDrops a small lightning strike upon collision" +
                                "\nHas a chance to electrify enemies" +
                                "\nCan be upgraded");

        }

        public override void SetDefaults() {
            item.damage = LegacyMode ? 9 : 11;
            item.height = 10;
            item.knockBack = 0f;
            item.autoReuse = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 6f;
            item.magic = true;
            item.noMelee = true;
            item.mana = LegacyMode ? 5 : 9;
            item.useAnimation = LegacyMode ? 15 : 27;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = LegacyMode ? 15 : 27;
            item.value = 140;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Bolt1Ball>();
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
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
