using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Ice Gigas crown icicle: forms in an arc above its shoulders with a glint telegraph, then fires
    ///at the player. The crown launches them SEQUENTIALLY (each spawned with a longer ai[0] fire
    ///delay) so the volley is a rolling rhythm to walk out of, not one wall. Aim locks 10 ticks
    ///before each launch. ai[0] = fire delay in ticks.
    ///</summary>
    class GigasCrownIcicle : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSpike;

        const float FireSpeed = 13f;
        const int LockLead = 10; //aim locks this many ticks before firing

        int FireDelay => (int)Projectile.ai[0];
        int Timer => (int)Projectile.localAI[0];
        bool Fired => Timer > FireDelay;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 600;
            Projectile.light = 0.3f;
        }

        public override bool? CanDamage()
        {
            return Fired;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (!Fired)
            {
                Projectile.velocity = Vector2.Zero;
                Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                Vector2 aim = target != null && !target.dead ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) : Vector2.UnitY;

                if (Timer <= FireDelay - LockLead)
                {
                    //Forming: track the player and glimmer
                    Projectile.rotation = aim.ToRotation() + MathHelper.PiOver2;
                    if (Main.rand.NextBool(3))
                    {
                        int glint = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceRod, 0f, 0f, 100, default, 0.9f);
                        Main.dust[glint].noGravity = true;
                        Main.dust[glint].velocity *= 0.2f;
                    }
                }
                else
                {
                    //Locked: aim frozen, bright flash — this one fires next
                    if (Timer == FireDelay - LockLead + 1)
                    {
                        Projectile.velocity = aim * 0.001f; //store the locked direction in velocity (synced)
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.35f, Pitch = 0.6f }, Projectile.Center);
                    }
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                    int flash = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch, 0f, 0f, 60, default, 1.2f);
                    Main.dust[flash].noGravity = true;
                    Main.dust[flash].velocity *= 0.2f;
                }

                if (Timer >= FireDelay)
                {
                    Vector2 dir = Projectile.velocity.SafeNormalize(aim);
                    Projectile.velocity = dir * FireSpeed;
                    Projectile.tileCollide = true;
                    Projectile.timeLeft = 240;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.5f, Pitch = 0.3f }, Projectile.Center);
                }
                Lighting.AddLight(Projectile.Center, 0.25f, 0.4f, 0.55f);
                return;
            }

            //In flight
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Frost, 0f, 0f, 100, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 2 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ice, vel.X, vel.Y, 60, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
