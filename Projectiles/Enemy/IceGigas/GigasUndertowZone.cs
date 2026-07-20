using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Undertow of Winter: the blizzard pull. A wide band anchored to the Ice Gigas that drags players
    ///toward it — resistible by holding away, escapable by dodging through. Deals no damage itself
    ///(the point-blank exhale the NPC fires at the center is the payoff). Sold by long streaming dust
    ///lines flowing into the giant. ai[0] = parent NPC whoAmI, ai[1] = duration in ticks.
    ///</summary>
    class GigasUndertowZone : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const float PullPerTick = 0.22f;
        const float PullSpeedCap = 6f;   //never accelerates a player beyond this toward the giant
        const float InnerDeadzone = 60f; //no pull once basically on top of it

        int ParentIndex => (int)Projectile.ai[0];
        int Duration => (int)Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : 90;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 1280; //40 tiles
            Projectile.height = 192; //12 tiles
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 90;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
        }

        public override bool? CanDamage()
        {
            return false; //pure force zone
        }

        public override void AI()
        {
            NPC parent = ParentIndex >= 0 && ParentIndex < Main.maxNPCs ? Main.npc[ParentIndex] : null;
            if (parent == null || !parent.active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = parent.Center + new Vector2(0f, -20f);
            Projectile.velocity = Vector2.Zero;

            //The pull: applied on every client so the local player's movement (which is authoritative
            //for them) feels it; remote players' positions come from their own clients.
            Rectangle band = Projectile.Hitbox;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || !player.Hitbox.Intersects(band))
                {
                    continue;
                }
                float dx = parent.Center.X - player.Center.X;
                if (System.Math.Abs(dx) < InnerDeadzone)
                {
                    continue;
                }
                int sign = System.Math.Sign(dx);
                //Only add pull while below the cap in the pull direction — resisting stays possible
                if (System.Math.Sign(player.velocity.X) != sign || System.Math.Abs(player.velocity.X) < PullSpeedCap)
                {
                    player.velocity.X += sign * PullPerTick;
                }
            }

            //Wind read: long streaming lines flowing toward the giant + hail glints whipping past
            for (int i = 0; i < 4; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                Vector2 toParent = (parent.Center - pos).SafeNormalize(Vector2.UnitX);
                int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, 130, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = toParent * Main.rand.NextFloat(6f, 11f);
                Main.dust[dust].fadeIn = 0.3f;
            }
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                Vector2 toParent = (parent.Center - pos).SafeNormalize(Vector2.UnitX);
                int glint = Dust.NewDust(pos, 4, 4, DustID.Snow, 0f, 0f, 80, default, 1.2f);
                Main.dust[glint].noGravity = true;
                Main.dust[glint].velocity = toParent * 9f;
            }
        }
    }
}
