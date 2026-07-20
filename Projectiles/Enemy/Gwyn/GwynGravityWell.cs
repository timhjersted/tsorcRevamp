using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gravity of the Sun: a golden singularity at Gwyn's chest that RADIALLY drags every player
    ///toward him — the keystone anti-kite. Resistible by holding away or dodge-rolling, but strong
    ///enough to reel in a stationary caster, and it usually delivers you straight into his melee.
    ///Deals no damage itself; the sword waiting at the center is the payload. Sold by long light
    ///streams flowing into him. ai[0] = parent NPC whoAmI, ai[1] = duration in ticks.
    ///</summary>
    class GwynGravityWell : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float PullRadius = 760f;
        const float PullPerTick = 0.28f;
        const float PullSpeedCap = 6.5f;  //never accelerates a player beyond this toward him
        const float InnerDeadzone = 90f;  //no pull once you're already delivered

        int ParentIndex => (int)Projectile.ai[0];
        int Duration => (int)Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : 120;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 120;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
        }

        public override bool? CanDamage()
        {
            return false; //pure force — the greatsword at the center does the harming
        }

        public override void AI()
        {
            NPC parent = ParentIndex >= 0 && ParentIndex < Main.maxNPCs ? Main.npc[ParentIndex] : null;
            if (parent == null || !parent.active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = parent.Center;
            Projectile.velocity = Vector2.Zero;

            //Radial drag on every client (each client is authoritative for its own player)
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }
                Vector2 toCenter = parent.Center - player.Center;
                float dist = toCenter.Length();
                if (dist > PullRadius || dist < InnerDeadzone)
                {
                    continue;
                }
                Vector2 dir = toCenter.SafeNormalize(Vector2.Zero);
                //Only add pull while below the cap toward him — resisting stays possible
                float towardSpeed = Vector2.Dot(player.velocity, dir);
                if (towardSpeed < PullSpeedCap)
                {
                    player.velocity += dir * PullPerTick;
                }
            }

            //Long light streams flowing into the singularity + the golden core
            for (int i = 0; i < 5; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = parent.Center + ang.ToRotationVector2() * Main.rand.NextFloat(120f, PullRadius * 0.85f);
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.GoldCoin;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (parent.Center - pos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(6f, 10f);
                Main.dust[dust].fadeIn = 0.3f;
            }
            int core = Dust.NewDust(parent.Center - new Vector2(10f), 20, 20, DustID.GoldFlame, 0f, 0f, 30, default, 1.7f);
            Main.dust[core].noGravity = true;
            Main.dust[core].velocity *= 0.1f;
            Lighting.AddLight(parent.Center, 1.2f, 1f, 0.4f);
        }
    }
}
