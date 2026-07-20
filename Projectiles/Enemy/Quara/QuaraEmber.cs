using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Quara Pincher ember: an arcing gob of fire spit — the IGNITER of the Goo->Ignite loop. On landing it
    ///ignites nearby ichor patches into fire walls AND sets any doused player in radius alight (which
    ///detonates their ichor coating). A direct hit just applies OnFire3. ai[0] = gravity per tick.
    ///</summary>
    class QuaraEmber : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const float IgniteRadius = 96f;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
            Projectile.light = 0.5f;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 13f)
            {
                Projectile.velocity.Y = 13f;
            }
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 80, default, 1.4f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.2f;
            Lighting.AddLight(Projectile.Center, 0.6f, 0.4f, 0.15f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire3, 3 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = 0.3f }, Projectile.Center);
            for (int i = 0; i < 18; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, vel.X, vel.Y, 80, default, 1.5f);
                Main.dust[d].noGravity = true;
            }

            //Ignite ichor patches in radius (deterministic across clients, so no netUpdate needed).
            int patchType = ModContent.ProjectileType<QuaraIchorPatch>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == patchType && p.Center.Distance(Projectile.Center) < IgniteRadius + p.width / 2f)
                {
                    (p.ModProjectile as QuaraIchorPatch)?.Ignite();
                }
            }

            //Set the local doused player alight if they're in the blast (their own client handles the buff).
            if (Main.netMode != NetmodeID.Server)
            {
                Player local = Main.LocalPlayer;
                if (local.active && !local.dead && local.HasBuff(ModContent.BuffType<Buffs.Doused>())
                    && local.Distance(Projectile.Center) < IgniteRadius)
                {
                    local.AddBuff(BuffID.OnFire3, 3 * 60);
                }
            }
        }
    }
}
