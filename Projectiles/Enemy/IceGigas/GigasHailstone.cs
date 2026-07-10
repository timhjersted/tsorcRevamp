using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Ice Gigas hailstone (vanilla hostile-snowball texture). ai[0] = gravity per tick — the volley
    ///mixes slow high lobs and fast flat throws. ai[1] = 1 for the giant finisher, which is much
    ///bigger and shatters into a radial spray of ice shards on impact.
    ///</summary>
    class GigasHailstone : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SnowBallHostile;

        bool Giant => Projectile.ai[1] == 1f;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 600;
            Projectile.light = 0.3f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Giant)
            {
                Projectile.Resize(34, 34);
                Projectile.scale = 2.2f;
            }
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }
            Projectile.rotation += 0.15f * (Projectile.velocity.X >= 0f ? 1f : -1f);

            if (Main.rand.NextBool(Giant ? 1 : 3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Snow, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, Giant ? 1.4f : 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
            if (Giant && Main.rand.NextBool(3))
            {
                int glow = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch, 0f, 0f, 100, default, 1.3f);
                Main.dust[glow].noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 2 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = Giant ? 0.9f : 0.5f, Pitch = Giant ? -0.4f : 0f }, Projectile.Center);
            int puffs = Giant ? 26 : 10;
            for (int i = 0; i < puffs; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(Giant ? 5f : 3f, Giant ? 5f : 3f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ice, vel.X, vel.Y, 60, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
            if (Giant && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi * i / 6f + Main.rand.NextFloat(0.3f)).ToRotationVector2() * 6f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
                        ModContent.ProjectileType<GigasIceShard>(), (int)(Projectile.damage * 0.6f), 1f, Main.myPlayer, 0.15f);
                }
            }
        }
    }
}
