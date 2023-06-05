using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Pets
{
    class MiakodaCrescent : ModItem
    {
        public static float DamageMultiplier1 = 3f;
        public static float DamageMultiplier2 = 7f;
        public static float BoostDuration = 2.5f;
        public static int BoostCooldown = 12;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageMultiplier1, DamageMultiplier2, BoostDuration, BoostCooldown);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DD2PetGhost);
            Item.shoot = ModContent.ProjectileType<Projectiles.Pets.MiakodaCrescent>();
            Item.buffType = ModContent.BuffType<Buffs.MiakodaCrescent>();
        }

        public override void UseStyle(Player player, Rectangle rectangle)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
            {
                player.AddBuff(Item.buffType, 3600, true);
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
                recipe.AddIngredient(ModContent.ItemType<MiakodaNew>());
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
                recipe.AddTile(TileID.DemonAltar);

                recipe.Register();
            }
        }
    }
}
