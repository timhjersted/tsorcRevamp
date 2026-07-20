using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Hero of Lumelia's thrown alchemist bomb: a lobbed flask that bursts into a purple cloud,
    // dealing damage and inflicting Potion Sickness (denies the player their healing).  A recolored
    // sibling of EnemySmokebomb — kept as its own class so the Hero's bomb reads distinct (purple,
    // anti-heal) from the generic green smoke bomb.
    class HeroPotionBomb : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemySmokebomb";

        private static readonly Color BombTint = new Color(180, 90, 220);

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.width = 14;
            Projectile.height = 15;
            Projectile.hostile = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
            Projectile.knockBack = 18;
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.light = 0.6f;
            Projectile.scale = 1f;

            DrawOffsetX = -5;
            DrawOriginOffsetY = -5;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            Projectile.timeLeft = 2; // detonate on the first player it touches
            Projectile.netUpdate = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.PotionSickness, 660);
            Projectile.timeLeft = 2;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.timeLeft = 2;
            return true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.9f;
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0, 0, 50, BombTint, 1.0f);
                Main.dust[dust].noGravity = false;
            }
            Lighting.AddLight(Projectile.Center, 0.35f, 0.15f, 0.45f);

            if (Projectile.timeLeft <= 2)
            {
                Projectile.tileCollide = false;
                Projectile.alpha = 255;
                // Expand the hitbox about the flask center so the burst itself damages.
                Projectile.position += new Vector2(Projectile.width / 2f, Projectile.height / 2f);
                Projectile.width = 110;
                Projectile.height = 110;
                Projectile.position -= new Vector2(Projectile.width / 2f, Projectile.height / 2f);
                Projectile.knockBack = 12f;
                Projectile.DamageType = DamageClass.Throwing;
            }
            else if (Main.rand.NextBool(4))
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, BombTint, 2f);
                Main.dust[dustIndex].scale = 0.1f + Main.rand.Next(5) * 0.1f;
                Main.dust[dustIndex].fadeIn = 0.5f + Main.rand.Next(5) * 0.1f;
                Main.dust[dustIndex].noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { PitchVariance = 0.5f }, Projectile.Center);

            for (int i = 0; i < 60; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.Center, 0, 0, DustID.Torch, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, BombTint, 2f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 3f;
            }
        }
    }
}
