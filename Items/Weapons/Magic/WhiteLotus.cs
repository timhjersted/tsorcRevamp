using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Magic;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class WhiteLotus : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Shoots flowers. Each petal hits separately \n'You can hear the cries of angels when you close your eyes'");
        }
        public override void SetDefaults()
        {
            Item.damage = 26;
            Item.mana = 14;
            Item.knockBack = 1;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = Item.useAnimation = 15;
            Item.UseSound = SoundID.Item11;
            Item.useTurn = false;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.autoReuse = true;
            Item.value = PriceByRarity.Yellow_8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Yellow;
            Item.shootSpeed = 16f;
            Item.shoot = 1; // ignore
        }
        public override Vector2? HoldoutOffset() => new Vector2(-6, 0);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            Projectile.NewProjectile(source, player.Center, speed, ModContent.ProjectileType<WhiteLotusCore>(), damage, knockBack, Main.myPlayer, 0, Main.rand.Next(2));
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FlowerPacketWhite);
            recipe.AddIngredient(ItemID.ShroomiteBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
