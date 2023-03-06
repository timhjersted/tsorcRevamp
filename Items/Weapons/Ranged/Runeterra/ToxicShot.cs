using tsorcRevamp.Projectiles.Ranged.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    public class ToxicShot : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Toxic Shot");
            /* Tooltip.SetDefault("Converts seeds into Toxic Shots, these scale with magic damage too" +
                "\nAlso uses all darts as ammo"); */
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 8;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item64;//63
            Item.DamageType = DamageClass.Ranged; 
            Item.damage = 17;
            Item.knockBack = 1f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.Seed;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Dart;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.Seed)
            {
                type = ModContent.ProjectileType<ToxicShotProj>();
            }
        }
        /*
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ItemID.Blowpipe);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }*/

    }
}