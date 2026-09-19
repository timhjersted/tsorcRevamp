using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Melee.Broadswords;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords
{
    public class AncientFireSword : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.damage = 21;
            Item.width = 34;
            Item.height = 38;
            Item.knockBack = 4f;
            Item.scale = 1.1f;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.shootSpeed = 65;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Green_2;
            Item.shoot = ModContent.ProjectileType<AncientFireSwordSlash>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.GoldBroadsword);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }

    }
}
