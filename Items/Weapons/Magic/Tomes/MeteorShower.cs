using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    public class MeteorShower : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Causes meteorites to rain from the sky.");
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item20;
            Item.rare = ItemRarityID.Orange;
            Item.knockBack = 3;
            Item.mana = 10;
            Item.damage = 45;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Magic;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.MeteoriteBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        
        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                Vector2 target = Main.MouseWorld;
                Vector2 spawnPosition = new Vector2(
                    target.X + Main.rand.NextFloat(-190f, 190f),
                    player.position.Y - 600f
                );

                Vector2 direction = target - spawnPosition;
                direction.Normalize();
                direction *= 14f;

                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    spawnPosition,
                    direction,
                    ModContent.ProjectileType<Projectiles.MeteorShower>(),
                    (int)(player.GetTotalDamage(DamageClass.Magic).ApplyTo(Item.damage)),
                    2.0f,
                    player.whoAmI
                );
            }
            return true;
        }
    }
}
