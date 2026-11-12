using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class EnemySpellSuddenDeathStrike : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 40;
            Main.projFrames[Projectile.type] = 12;
            DrawOriginOffsetX = 15;
            DrawOriginOffsetY = 10;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.5f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        #region AI
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 12)
            {
                Projectile.Kill();
                return;
            }

            // Fade out over the last 12 ticks of the 12-frame (4 ticks/frame = 48t) life instead of blinking out at full
            // brightness: alpha 0 -> 200, scale 1 -> 0.8. Lifetime, speed and damage are unchanged for every caster.
            int ticksLeft = (12 - Projectile.frame) * 4 - Projectile.frameCounter;
            if (ticksLeft <= 12)
            {
                float fade = 1f - ticksLeft / 12f;
                int fadeAlpha = (int)(fade * 200f);
                Projectile.alpha = Math.Max(Projectile.alpha, fadeAlpha);
                Projectile.scale = MathHelper.Lerp(1f, 0.8f, fade);
            }

            //It flies through walls, so it must be LOUD: shadowflame wisp trail + purple glow
            for (int i = 0; !Main.dedServ && i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, BlightBuildup.FromGreatBlackKnight(Projectile) ? (i == 0 && Main.rand.NextBool(6) ? DustID.YellowTorch : DustID.Wraith) : DustID.Shadowflame, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
            if (!Main.dedServ) Lighting.AddLight(Projectile.Center, 0.35f, 0.15f, 0.5f);

            // Blight accent: every caster's strike builds Blight now, so the non-GBK shadowflame sigils carry black
            // wraith wisps too (the GBK's palette above is already wraith-black and gold).
            bool blightPalette = BlightBuildup.FromGreatBlackKnight(Projectile);
            if (!Main.dedServ && !blightPalette && Main.rand.NextBool(2))
            {
                Dust wraith = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Wraith, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.3f);
                wraith.noGravity = true;
                wraith.velocity *= 0.3f;
            }
        }
        #endregion

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[0] == 1f)
            {
                EnemyVFX.DrawBlackKnightDeathTrail(Projectile.Center, Projectile.velocity, new Vector2(92f, 34f), 0.76f,
                    BlightBuildup.FromGreatBlackKnight(Projectile));
            }
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            //Expiry puff so a whiffed sigil still reads as a near miss
            for (int i = 0; !Main.dedServ && i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, BlightBuildup.FromGreatBlackKnight(Projectile) ? (i % 8 == 0 ? DustID.YellowTorch : DustID.Wraith) : DustID.Shadowflame, vel.X, vel.Y, 80, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Blight from every caster (was powerful curse unless the Great Black Knight cast it).
            BlightBuildup.Apply(target);
            target.AddBuff(BuffID.Silenced, 120);
            target.AddBuff(BuffID.Weak, 600);
        }

    }
}
