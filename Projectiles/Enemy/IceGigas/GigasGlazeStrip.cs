using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Black Ice: a near-invisible glaze racing along the floor from under the player. For its
    ///duration, grounded players standing on the strip keep their momentum — every dodge overshoots,
    ///every stop takes forever. Deals no damage (Chilled tag on first touch only); it's a modifier
    ///that makes the rest of the kit land. ai[0] = duration in ticks.
    ///</summary>
    class GigasGlazeStrip : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const float MaxHalfWidth = 192f; //24-tile strip total
        const float ExpandSpeed = 12f;
        const float SlideCarry = 1.045f;  //per-tick momentum multiplier while on the glaze
        const float SlideSpeedCap = 8f;   //carry never pushes past this
        const float MinSlideSpeed = 0.4f; //below this the carry lets go (you CAN eventually stop)

        int Duration => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 960;
        float HalfWidth => MathHelper.Min(MaxHalfWidth, Projectile.localAI[0]);

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = (int)(MaxHalfWidth * 2f);
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 960;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.4f }, Projectile.Center);
        }

        public override bool? CanDamage()
        {
            return false; //pure floor hazard
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            bool expanding = Projectile.localAI[0] < MaxHalfWidth;
            Projectile.localAI[0] += ExpandSpeed;

            //Skate physics for grounded players on the strip (applied on all clients; the local
            //player's movement is what matters and is authoritative for them)
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || player.velocity.Y != 0f)
                {
                    continue;
                }
                if (System.Math.Abs(player.Center.X - Projectile.Center.X) > HalfWidth
                    || System.Math.Abs(player.Bottom.Y - (Projectile.position.Y + 8f)) > 48f)
                {
                    continue;
                }
                if (System.Math.Abs(player.velocity.X) > MinSlideSpeed)
                {
                    player.velocity.X *= SlideCarry;
                    if (System.Math.Abs(player.velocity.X) > SlideSpeedCap)
                    {
                        player.velocity.X = SlideSpeedCap * System.Math.Sign(player.velocity.X);
                    }
                }
                if (!player.HasBuff(BuffID.Chilled))
                {
                    player.AddBuff(BuffID.Chilled, 60); //first-touch tag only — the slide is the threat
                }
            }

            //Racing glint front while expanding — a few dark obsidian flecks mixed into the icy
            //blue, so it reads as BLACK ice rather than ordinary frost.
            if (expanding)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 front = Projectile.Center + new Vector2(side * HalfWidth, 0f);
                    int dust = Dust.NewDust(front - new Vector2(4f, 6f), 8, 8, DustID.IceTorch, side * 2f, -0.5f, 60, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                    if (Main.rand.NextBool(3))
                    {
                        int dark = Dust.NewDust(front - new Vector2(4f, 6f), 8, 8, DustID.Obsidian, side * 1.5f, -0.3f, 80, default, 1f);
                        Main.dust[dark].noGravity = true;
                    }
                }
            }
            //Sparse surface glints so the glaze stays readable without shouting. About a third are
            //black obsidian flecks instead of the icy IceRod glint — the same "black ice" mix.
            float remaining = Projectile.timeLeft / (float)Duration;
            if (Main.rand.NextFloat() < 0.35f + 0.3f * remaining)
            {
                float x = Projectile.Center.X + Main.rand.NextFloat(-HalfWidth, HalfWidth);
                int glintType = Main.rand.NextBool(3) ? DustID.Obsidian : DustID.IceRod;
                int glint = Dust.NewDust(new Vector2(x - 2f, Projectile.position.Y), 4, 6, glintType, 0f, 0f, 90, default, 0.85f);
                Main.dust[glint].noGravity = true;
                Main.dust[glint].velocity *= 0.1f;
            }
            Lighting.AddLight(Projectile.Center, 0.1f, 0.2f, 0.3f);
        }
    }
}
