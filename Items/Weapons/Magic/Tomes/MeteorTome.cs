using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class MeteorTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("A legendary spell tome that calls down a meteor storm");
        }
        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.height = 10;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.Pink;
            Item.shootSpeed = 3f;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 12;
            Item.UseSound = SoundID.Item88;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 11;
            Item.useAnimation = 11;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 34;
            Item.autoReuse = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<MeteorShower>());
            recipe.AddIngredient(ItemID.MeteorStaff);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                Vector2 target = Main.MouseWorld;
                Vector2 spawnPosition = new Vector2(
                    target.X + Main.rand.NextFloat(-250f, 250f), 
                    player.position.Y - 600f 
                );

                Vector2 direction = target - spawnPosition;
                direction.Normalize();
                direction *= 14f; 

                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    spawnPosition,
                    direction,
                    ModContent.ProjectileType<Projectiles.Meteor>(),
                    (int)(player.GetTotalDamage(DamageClass.Magic).ApplyTo(Item.damage)),
                    2.0f,
                    player.whoAmI
                );
            }
            return true;
        }
    }
}
