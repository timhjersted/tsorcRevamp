using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Ice4Icicle : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 84;
            Projectile.friendly = true;
            Projectile.penetrate = 8;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 400;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!target.boss)
            {
                target.AddBuff(Terraria.ID.BuffID.Slow, 360);
                target.AddBuff(Terraria.ID.BuffID.Frozen, 5);
                if (Main.rand.NextBool(30))
                {
                    target.AddBuff(Terraria.ID.BuffID.Frozen, 120);
                }
            }
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
            Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * .75f);


            if (Projectile.ai[0] >= 1)
            {
                Projectile.hostile = true;
                Projectile.friendly = false;
                Projectile.timeLeft = 1400;
                Projectile.ai[0] = 0;
            }
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
