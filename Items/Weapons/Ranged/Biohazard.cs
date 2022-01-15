using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class Biohazard : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Biohazard");
            Tooltip.SetDefault("The addition of the propulsion field allows detonating shots to pierce twice"
                                + "\nWarning - perfectly capable of destroying entire ecosystems");
        }

        public override void SetDefaults()
        {
            item.damage = 60;
            item.ranged = true;
            item.crit = 0;
            item.width = 52;
            item.height = 28;
            item.useTime = 14;
            item.useAnimation = 14;
            item.autoReuse = true;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 3f;
            item.value = PriceByRarity.Yellow_8;
            item.scale = 0.8f;
            item.rare = ItemRarityID.Yellow;
            item.shoot = mod.ProjectileType("BiohazardShot");
            item.shootSpeed = 10f;
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
                item.useTime = 22;
                item.useAnimation = 22;
                item.shootSpeed = 24.05f;
                item.shoot = ModContent.ProjectileType<Projectiles.BiohazardDetonator>();
            }
            else
            {
                item.useTime = 14;
                item.useAnimation = 14;
                item.shootSpeed = 16.12f;
                item.shoot = ModContent.ProjectileType<Projectiles.BiohazardShot>();
            }

            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarShot").WithVolume(.6f).WithPitchVariance(.3f), player.Center);
            }

            {
                Vector2 muzzleOffset = Vector2.Normalize(new Vector2(speedX, speedY)) * 1f;
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
            Lighting.AddLight(item.Right, 0.2496f, 0.4584f, 0.130f);

            if (Main.rand.Next(10) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 34, item.position.Y), 8, 28, 75, 1f, 0, 100, default(Color), .8f)];
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = .4f;
            }

            if (Main.rand.Next(5) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 34, item.position.Y), 8, 28, 75, 0, 0, 100, default(Color), .8f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = .4f;
            }

            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BiohazardGlowmask];
            spriteBatch.Draw(texture, new Vector2(item.position.X - Main.screenPosition.X + item.width * 0.5f, item.position.Y - Main.screenPosition.Y + item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("VirulentCatalyzer"));
            recipe.AddIngredient(ItemID.ShroomiteBar, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 150000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
