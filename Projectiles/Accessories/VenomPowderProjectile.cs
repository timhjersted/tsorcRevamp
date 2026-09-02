using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Accessories
{
    public class VenomPowderProjectile : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.height = 270;         
            Projectile.width = 270;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.penetrate = 35;
            Projectile.ArmorPenetration = 30;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Vector2 center = Projectile.Center;
            float radius = 165f;

            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                int dust1 = Dust.NewDust(center + offset, 1, 1, 171, 0f, 0f, 75, default, 2.1f);
                Main.dust[dust1].velocity = offset.SafeNormalize(Vector2.Zero) * 1.5f;
                Main.dust[dust1].noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                int dust2 = Dust.NewDust(center + offset, 1, 1, 205, 0f, 0f, 75, default, 1.5f);
                Main.dust[dust2].velocity = offset.SafeNormalize(Vector2.Zero) * 1.5f;
                Main.dust[dust2].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 300);
        }
    }
}