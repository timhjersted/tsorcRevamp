using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class ToxicCatalyzer : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Toxic Catalyzer");
            Tooltip.SetDefault("Left-click shots tag and poison enemies"
                                + "\nRight-click shots detonate tags"
                                +"\nThe more tags, the greater the explosion and damage");

        }

        public override void SetDefaults()
        {
            item.damage = 16;
            item.ranged = true;
            item.crit = 0;
            item.width = 38;
            item.height = 28;
            item.useTime = 18;
            item.useAnimation = 18;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 2.5f;
            item.value = 20000;
            item.scale = 0.8f;
            item.rare = ItemRarityID.Green;
            item.shoot = mod.ProjectileType("ToxicCatShot");
            item.shootSpeed = 6.6f;
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
                item.useTime = 26;
                item.useAnimation = 26;
                item.shoot = ModContent.ProjectileType<Projectiles.ToxicCatDetonator>();
            }
            else
            {
                item.useTime = 18;
                item.useAnimation = 18;
                item.shoot = ModContent.ProjectileType<Projectiles.ToxicCatShot>();
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

            if (Main.rand.Next(15) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 34, item.position.Y), 8, 28, 75, 1f, 0, 100, default(Color), .8f)];
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = .4f;
            }

            if (Main.rand.Next(10) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 34, item.position.Y), 8, 28, 75, 0, 0, 100, default(Color), .8f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = .4f;
            }

            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ToxicCatalyzerGlowmask];
            spriteBatch.Draw(texture, new Vector2(item.position.X - Main.screenPosition.X + item.width * 0.5f, item.position.Y - Main.screenPosition.Y + item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
