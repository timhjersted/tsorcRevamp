using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Crystal Canopy stalactite: crystallizes in the air above the player's area, hangs, then drops
    ///straight down. The canopy spawns six with ACCELERATING drop delays (gaps shrinking 30t → 10t),
    ///so the rhythm closes in like fingers. Glint flash 10 ticks before its own drop.
    ///ai[0] = drop delay in ticks. Splits into two small shards on ground impact.
    ///</summary>
    class GigasCanopyIcicle : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSpike;

        const float FallSpeed = 16f;
        const int GlintLead = 10;

        int DropDelay => (int)Projectile.ai[0];
        int Timer => (int)Projectile.localAI[0];
        bool Falling => Timer > DropDelay;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 600;
            Projectile.scale = 1.4f;
            Projectile.light = 0.3f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = DropDelay + 300;
        }

        public override bool? CanDamage()
        {
            return Falling;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = MathHelper.Pi; //always points down

            if (!Falling)
            {
                Projectile.velocity = Vector2.Zero;
                //Crystallizing: frost condensing onto the stalactite
                if (Main.rand.NextBool(2))
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(10f, 30f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, 110, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (Projectile.Center - pos) * 0.1f;
                }
                //Glint: this one drops next
                if (Timer > DropDelay - GlintLead)
                {
                    int flash = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch, 0f, 0f, 60, default, 1.2f);
                    Main.dust[flash].noGravity = true;
                    Main.dust[flash].velocity *= 0.2f;
                    if (Timer == DropDelay - GlintLead + 1)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.35f, Pitch = 0.6f }, Projectile.Center);
                    }
                }
                if (Timer >= DropDelay)
                {
                    Projectile.velocity = new Vector2(0f, FallSpeed);
                    Projectile.tileCollide = true;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.5f, Pitch = 0.5f }, Projectile.Center);
                }
                return;
            }

            //Falling: frost streak behind it
            for (int seg = 0; seg < 2; seg++)
            {
                Vector2 pos = Projectile.Center - Projectile.velocity * (seg * 0.5f);
                int dust = Dust.NewDust(pos - new Vector2(4, 4), 8, 8, DustID.Frost, 0f, 0f, 100, default, 1.2f - seg * 0.3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = -0.1f }, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ice, vel.X, vel.Y, 60, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
            //Ground impact splits into two low shards
            if (Falling && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 vel = new Vector2(side * Main.rand.NextFloat(3.5f, 5f), Main.rand.NextFloat(-3f, -1.5f));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
                        ModContent.ProjectileType<GigasIceShard>(), (int)(Projectile.damage * 0.5f), 1f, Main.myPlayer, 0.2f);
                }
            }
        }
    }
}
