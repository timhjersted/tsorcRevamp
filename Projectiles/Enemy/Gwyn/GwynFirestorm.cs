using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gwyn's Firestorm controller: an invisible node that hangs over the arena and rains fireballs
    ///across a wide band tracking the player, for a set duration — the "you can't just stand and
    ///heal" attack. A brief darkening telegraph precedes the first drop. ai[0] = damage passed to the
    ///fireballs, ai[1] = duration ticks.
    ///</summary>
    class GwynFirestorm : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int TelegraphTicks = 40;
        const int DropInterval = 7;   // a fireball wave every N ticks
        const float BandHalfWidth = 380f;

        int Duration => (int)Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : 150;
        int Timer => (int)Projectile.localAI[0];
        bool Raining => Timer > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 400;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + Duration;
        }

        public override bool? CanDamage()
        {
            return false; // the falling fireballs deal the damage, not the controller
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            if (target == null || target.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!Raining)
            {
                //Telegraph: embers drifting down over the player's area, sky reddening
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = target.Center + new Vector2(Main.rand.NextFloat(-BandHalfWidth, BandHalfWidth), -400f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Torch, 0f, 2f, 120, default, 1f);
                    Main.dust[dust].noGravity = true;
                }
                if (Timer == 1)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.7f, Pitch = -0.4f }, target.Center);
                }
                return;
            }

            //Rain: fireballs drop across the band above the player
            if (Main.netMode != NetmodeID.MultiplayerClient && (Timer - TelegraphTicks) % DropInterval == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    float x = target.Center.X + Main.rand.NextFloat(-BandHalfWidth, BandHalfWidth) + target.velocity.X * 20f;
                    Vector2 spawn = new Vector2(x, target.Center.Y - 460f - Main.rand.NextFloat(80f));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), 6f),
                        ModContent.ProjectileType<GwynFireball>(), (int)Projectile.ai[0], 2f, Projectile.owner, 0.2f);
                }
            }
        }
    }
}
