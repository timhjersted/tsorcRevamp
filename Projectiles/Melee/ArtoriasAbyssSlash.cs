using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee
{
    public class ArtoriasAbyssSlash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssSlash";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 28;
            Projectile.scale = 1.15f;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.ai[1] == 1f)
            {
                Projectile.DamageType = DamageClass.Magic;
            }

            Projectile.rotation = Projectile.ai[0];
            if (Projectile.velocity.X < 0)
            {
                Projectile.spriteDirection = -1;
            }
            else if (Projectile.velocity.X > 0)
            {
                Projectile.spriteDirection = 1;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Lighting.AddLight(Projectile.Center, 0.32f, 0.08f, 0.48f);
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(28f, 18f), DustID.ShadowbeamStaff, -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.9f, 0.9f), 120, Color.MediumPurple, Main.rand.NextFloat(0.8f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color slashColor = new Color(185, 95, 255, 210);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, slashColor, Projectile.rotation, frame.Size() / 2f, Projectile.scale, effects, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition - Projectile.velocity * 0.75f, frame, slashColor * 0.45f, Projectile.rotation, frame.Size() / 2f, Projectile.scale * 1.08f, effects, 0);
            return false;
        }
    }
}