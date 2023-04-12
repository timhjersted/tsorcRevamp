using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class Biohazard : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Biohazard");
            /* Tooltip.SetDefault("The addition of the propulsion field allows detonating shots to pierce twice," +
                                "\nand for sticking shots to home in on their target"
                                + "\nWarning - perfectly capable of destroying entire ecosystems"); */
        }

        public override void SetDefaults()
        {
            Item.damage = 170;
            Item.DamageType = DamageClass.Ranged;
            Item.crit = 0;
            Item.width = 52;
            Item.height = 28;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = PriceByRarity.Yellow_8;
            Item.scale = 0.8f;
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<BiohazardShot>();
            Item.shootSpeed = 15f;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-5, 4);
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }



        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useTime = 22;
                Item.useAnimation = 22;
                Item.shootSpeed = 24.05f;
                Item.shoot = ModContent.ProjectileType<BiohazardDetonator>();
            }
            else
            {
                Item.useTime = 14;
                Item.useAnimation = 14;
                Item.shootSpeed = 16.12f;
                Item.shoot = ModContent.ProjectileType<BiohazardShot>();
            }

            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarShot") with { Volume = 0.6f, PitchVariance = 0.3f }, player.Center);
            }

            {
                Vector2 muzzleOffset = Vector2.Normalize(speed) * 1f;
                if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0))
                {
                    position += muzzleOffset;
                    position.Y += 3;
                }
            }
            return true;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Right, 0.2496f, 0.4584f, 0.130f);

            if (Main.rand.NextBool(10))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 34, Item.position.Y), 8, 28, 75, 1f, 0, 100, default(Color), .8f)];
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = .4f;
            }

            if (Main.rand.NextBool(5))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 34, Item.position.Y), 8, 28, 75, 0, 0, 100, default(Color), .8f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = .4f;
            }

            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BiohazardGlowmask];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<VirulentCatalyzer>());
            recipe.AddIngredient(ItemID.ShroomiteBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
