using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// Grond's Toll's Blight mire: a 6-tile patch of Blight on the floor where the mace landed, for 240
    /// ticks, then a 30-tick fade. It deals no damage; a player standing in it gains 4 Blight every 30 ticks (GBK's
    /// persistent-hazard convention), applied by each client to its own player. Floor-denial in a cramped cave.
    /// Spawned centred on the impact point's floor. Dust-only visual (black wraith smoke + yellow Blight flecks).
    /// </summary>
    public class WitchkingBlightMire : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        private const float MireWidth = 6f * 16f;
        private const float MireHeight = 40f;
        private const int LifeTicks = 240;
        private const int FadeTicks = 30;
        private const int BuildupIntervalTicks = 30;
        private const int BuildupPerInterval = 4;

        // Local: this machine's own player's buildup clock (never shared).
        private int _localBuildupTimer;

        public override void SetDefaults()
        {
            Projectile.width = (int)MireWidth;
            Projectile.height = (int)MireHeight;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifeTicks + FadeTicks;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            float strength = 1f;
            if (Projectile.timeLeft < FadeTicks)
            {
                strength = Projectile.timeLeft / (float)FadeTicks;
            }

            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 position = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-MireWidth / 2f, MireWidth / 2f), Projectile.Bottom.Y - 2f);
                int dustType = Main.rand.NextBool(6) ? DustID.YellowTorch : DustID.Wraith;
                Dust mire = Dust.NewDustPerfect(position, dustType, new Vector2(0f, -Main.rand.NextFloat(0.4f, 1.4f)), 100, default, 1.5f * strength);
                mire.noGravity = true;
            }

            // Blight only while the mire is still mostly up; the fade is a safe tail.
            Player player = Main.LocalPlayer;
            bool standingIn = player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox) && Projectile.timeLeft > FadeTicks;
            if (!standingIn)
            {
                _localBuildupTimer = 0;
                return;
            }

            _localBuildupTimer++;
            if (_localBuildupTimer >= BuildupIntervalTicks)
            {
                _localBuildupTimer = 0;
                BlightBuildup.Apply(player, BuildupPerInterval);
            }
        }
    }
}
