using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Pets
{
    class MiakodaNew : ModItem
    {
        public static float MoveSpeed1 = 5f;
        public static float MoveSpeed2 = 90f;
        public static float DamageReduction = 50f;
        public static float BoostDuration = 2.5f;
        public static int BoostCooldown = 12;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed1, MoveSpeed2, DamageReduction, BoostDuration, BoostCooldown);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DD2PetGhost);
            Item.shoot = ModContent.ProjectileType<Projectiles.Pets.MiakodaNew>();
            Item.buffType = ModContent.BuffType<Buffs.MiakodaNew>();
        }

        public override void UseStyle(Player player, Rectangle rectangle)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
            {
                player.AddBuff(Item.buffType, 60 * 60, true);
            }
        }

        public override void AddRecipes()
        {
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ModContent.ItemType<MiakodaFull>());
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
                recipe.AddTile(TileID.DemonAltar);

                recipe.Register();
            }
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ModContent.ItemType<MiakodaCrescent>());
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
                recipe.AddTile(TileID.DemonAltar);

                recipe.Register();
            }
        }
    }
}
