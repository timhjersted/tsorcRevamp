using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class Sanguinis : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 13;
            Item.useTime = 13;
            Item.damage = 130;
            Item.knockBack = 4f;
            Item.mana = 8;
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.Item171;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.value = PriceByRarity.Pink_5;
            Item.shootSpeed = 6.5f;
            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.SanguinisProj>();   
            Item.noUseGraphic = true;                 
        }

        private void UpdateStats(Player player)
        {
            float life = (float)player.statLife / player.statLifeMax2;

            if (life <= 0.33f) 
            {
                Item.damage = 170;  
                Item.useTime = 10;
                Item.useAnimation = 10;
            }
            else if (life <= 0.66f) 
            {
                Item.damage = 140;  
                Item.useTime = 12;
                Item.useAnimation = 12;
            }
            else
            {
                Item.damage = 150;  
                Item.useTime = 13;
                Item.useAnimation = 13;
            }
        }

        public override void HoldItem(Player player)
        {
            UpdateStats(player); 
        }

        public override bool? UseItem(Player player)
        {
            if (player.statLife > 5)
            {
                player.statLife -= 2;
            }

            if (player.whoAmI == Main.myPlayer)
            {
                Vector2 center = player.Center;
                for (int i = 0; i < Main.rand.Next(13, 16); i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                    Dust dust = Dust.NewDustPerfect(
                        center + Main.rand.NextVector2Circular(14f, 14f),
                        DustID.Blood,
                        velocity,
                        0,
                        default,
                        Main.rand.NextFloat(1.5f, 1.8f)
                    );
                    dust.noGravity = true;
                }
            }
            return true;    
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            UpdateStats(player);
            velocity = velocity.RotatedByRandom(MathHelper.ToRadians(20));
            float randomSpeed = Main.rand.NextFloat(3.4f, 8.9f);  
            velocity = Vector2.Normalize(velocity) * randomSpeed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 3);
            recipe.AddIngredient(ItemID.SoulofFright, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
