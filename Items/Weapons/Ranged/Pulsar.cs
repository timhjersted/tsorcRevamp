using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class Pulsar : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Pulsar");
            Tooltip.SetDefault("Keep your enemies close, but not too close!"
                                + "\nEnlarged projectile does x1.5 damage"
                                + "\nElectrocutes enemies"
                                + "\nPowerful, but hard to master");

        }

        public override void SetDefaults()
        {
            Item.damage = 34;
            Item.DamageType = DamageClass.Ranged;
            Item.crit = 0;
            Item.width = 38;
            Item.height = 28;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2f;
            Item.value = 20000;
            Item.scale = 0.8f;
            Item.rare = ItemRarityID.Green;
            Item.shoot = Mod.Find<ModProjectile>("PulsarShot").Type;
            Item.shootSpeed = 5f;
            //item.glowMask = TransparentTextureType.PulsarGlowmask;
        }


        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-5, 4);
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("Sounds/Item/PulsarShot") with { Volume = 0.6f, PitchVariance = 0.3f }, player.Center);
            }

            if (player.wet)
            {
                player.AddBuff(BuffID.Electrified, 90);
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
            Lighting.AddLight(Item.Right, 0.0452f, 0.24f, 0.24f);

            if (Main.rand.Next(15) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 32, Item.position.Y), 8, 28, 226, 1f, 0, 100, default(Color), .4f)];
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = .4f;
            }

            if (Main.rand.Next(10) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 32, Item.position.Y), 8, 28, 226, 0, 0, 100, default(Color), .4f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = .4f;
            }

            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PulsarGlowmask];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }

    }
}
