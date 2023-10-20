using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.BossItems
{
    public class BossRematchTome : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 1;
            Item.useAnimation = 1;
            Item.UseSound = SoundID.Item11;
            Item.useTurn = true;
            Item.noMelee = true;
            Item.value = 10000;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 24f;
            Item.shoot = ModContent.ProjectileType<Projectiles.BlackFirelet>();
        }






        public override bool CanUseItem(Player player)
        {
            return !UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.VFX.BossSelectVisuals>());
        }

        //TODO: Make it work like the Grand Design
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
            {
                return false;
            }
            if (tsorcRevampWorld.NewSlain == null || tsorcRevampWorld.NewSlain.Keys.Count == 0)
            {
                UsefulFunctions.BroadcastText(Language.GetTextValue("Mods.tsorcRevamp.Items.BossRematchTome.None"));
                return false;
            }
            if (tsorcRevampWorld.BossAlive)
            {
                UsefulFunctions.BroadcastText(Language.GetTextValue("Mods.tsorcRevamp.Items.BossRematchTome.Forbidden"));
                return false;
            }

            Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossSelectVisuals>(), 0, 0, player.whoAmI);
            return true;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25);
            recipe.AddIngredient(ItemID.Book, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 25);
            recipe2.AddIngredient(ItemID.SpellTome, 1);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();
        }
    }
}