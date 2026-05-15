using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Buffs.Debuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class ShadowSparkle : ModProjectile
    {

        public override void SetDefaults()
        {
			Projectile.width = 8; 
			Projectile.height = 8; 
			Projectile.aiStyle = 1; 
			Projectile.friendly = true; 
			Projectile.hostile = false; 
			Projectile.DamageType = DamageClass.Ranged; 
			Projectile.timeLeft = 80; 
			Projectile.ignoreWater = true; 
			Projectile.tileCollide = false; 
			Projectile.extraUpdates = 1;
            Projectile.scale = 1.15f;
			AIType = ProjectileID.Bullet; 
        }
        public override void AI()
        {
            Projectile.velocity.Y = Projectile.velocity.Y;

            if (Main.rand.NextBool(15))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    27,
                    Projectile.velocity.X * 0.2f,
                    Projectile.velocity.Y * 0.2f,
                    100,
                    default,
                    0.85f
                );

                dust.noGravity = false;
                dust.velocity *= 0.5f;
            }

            Projectile.velocity *= 0.97f;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 8 * 60);
        }
        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White, 
                Projectile.rotation,
                frame.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}