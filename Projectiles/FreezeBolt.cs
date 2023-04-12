using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles
{
    class FreezeBolt : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.alpha = 0;
            Projectile.timeLeft = 1800;
            Projectile.friendly = true;
            Projectile.penetrate = 10;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 100;
        }

        public override void AI()
        {
            if (Projectile.type == 96 && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            }
            int num40 = Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 100, default(Color), 3f);
            Main.dust[num40].noGravity = true;
            if (Main.rand.NextBool(10))
            {
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 100, default(Color), 1.4f);
            }
            if (Projectile.ai[1] >= 20f)
            {
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
            }
            Projectile.rotation += 0.3f * (float)Projectile.direction;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
                return;
            }
        }

        public override bool OnTileCollide(Vector2 CollideVel)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] >= 6f)
            {
                Projectile.position += Projectile.velocity;
                Projectile.Kill();
            }
            else
            {
                if (Projectile.velocity.Y > 4f)
                {
                    if (Projectile.velocity.Y != CollideVel.Y)
                    {
                        Projectile.velocity.Y = -CollideVel.Y * 0.8f;
                    }
                }
                else
                {
                    if (Projectile.velocity.Y != CollideVel.Y)
                    {
                        Projectile.velocity.Y = -CollideVel.Y;
                    }
                }
                if (Projectile.velocity.X != CollideVel.X)
                {
                    Projectile.velocity.X = -CollideVel.X;
                }
            }
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(4))
            {
                target.AddBuff(BuffID.Frozen, 240);
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(4) && info.PvP)
            {
                target.AddBuff(BuffID.Frozen, 4 * 60);
            }
        }
    }
}
