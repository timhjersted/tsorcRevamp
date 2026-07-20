using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Quara Pincher / clutch-crab ichor glob: an arcing blob of flammable black goo. A direct hit DOUSES
    ///the player (slow + bleed + explosive-prime). A miss splats a QuaraIchorPatch on the ground — harmless
    ///until a Pincher ember ignites it. ai[0] = gravity per tick.
    ///</summary>
    class QuaraIchorGlob : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int DousedSeconds = 6;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
            Projectile.light = 0.15f;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 12f)
            {
                Projectile.velocity.Y = 12f;
            }
            Projectile.rotation += 0.2f * Projectile.direction;

            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ichor, 0f, 0f, 120, default, 1.1f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.2f;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Doused>(), DousedSeconds * 60);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //Splat a lingering ichor patch on the surface it hit (server-authoritative, synced).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                    new Vector2(Projectile.Center.X, Projectile.Bottom.Y), Vector2.Zero,
                    ModContent.ProjectileType<QuaraIchorPatch>(), Projectile.damage, 0f, Main.myPlayer);
            }
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit20 with { Volume = 0.4f, Pitch = 0.3f }, Projectile.Center);
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ichor, vel.X, vel.Y, 120, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
