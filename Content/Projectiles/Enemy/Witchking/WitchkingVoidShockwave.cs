using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WitchkingBoss = tsorcRevamp.NPCs.Bosses.SuperHardMode.Witchking;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// Grond's Toll's floor wave: a low void shockwave that runs along the floor from the mace's impact,
    /// 6 px/t for up to 30 tiles or until a wall/pit (the fire wall's ground-crawler snap). Only 32px tall: JUMPED, not
    /// rolled. Armed after a 4-tick delay so the impact dust leads the hitbox. Hits apply the void set.
    /// ai[0] = direction (+1 / -1), ai[1] = floor Y in world px. Dust-only visual.
    /// </summary>
    public class WitchkingVoidShockwave : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        private const float WaveHeight = 32f;
        private const float WaveWidth = 30f;
        private const float Speed = 6f;
        private const float TravelDistance = 30f * 16f;
        private const int ArmDelayTicks = 4;
        private const int FadeTicks = 12;

        private int Age => (int)Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = (int)WaveWidth;
            Projectile.height = (int)WaveHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = (int)(TravelDistance / Speed) + FadeTicks;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            float direction = Projectile.ai[0] >= 0f ? 1f : -1f;
            float floorY = Projectile.ai[1];

            bool travelling = Projectile.timeLeft > FadeTicks;
            if (travelling)
            {
                float centerX = Projectile.Center.X + direction * Speed;
                bool onFloor = WitchkingFireWall.TrySnapToFloor(centerX, ref floorY);
                if (!onFloor)
                {
                    Projectile.timeLeft = FadeTicks;
                }
                else
                {
                    Projectile.Center = new Vector2(centerX, Projectile.Center.Y);
                    Projectile.ai[1] = floorY;
                }
            }

            Projectile.Center = new Vector2(Projectile.Center.X, floorY - WaveHeight / 2f);

            if (!Main.dedServ)
            {
                float fade = 1f;
                if (!travelling)
                {
                    fade = Projectile.timeLeft / (float)FadeTicks;
                }
                for (int i = 0; i < 3; i++)
                {
                    Vector2 position = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-WaveWidth / 2f, WaveWidth / 2f),
                        floorY - Main.rand.NextFloat(0f, WaveHeight * fade));
                    int dustType = i == 0 ? DustID.Wraith : DustID.Shadowflame;
                    Dust wave = Dust.NewDustPerfect(position, dustType, new Vector2(direction * 1.5f, -Main.rand.NextFloat(0.5f, 2f)), 60, default, 1.4f * fade);
                    wave.noGravity = true;
                }
                Lighting.AddLight(Projectile.Center, 0.3f * fade, 0.1f * fade, 0.45f * fade);
            }
        }

        public override bool? CanDamage() => Age > ArmDelayTicks && Projectile.timeLeft > FadeTicks ? null : false;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            WitchkingBoss.ApplyVoidHit(target);
        }
    }
}
