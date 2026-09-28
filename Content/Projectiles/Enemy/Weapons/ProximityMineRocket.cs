using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class ProximityMineRocket : ModProjectile
    {
        public const float Speed = 6f;
        private const float ReleaseDistance = 150f;
        private const float TurnStrength = 0.07f;
        private const float Seeking = 0f;
        private const float Committed = 1f;

        public override void SetDefaults()
        {
            // ProximityMineRocket.png: 16x30, nose at the top of the sprite.
            Projectile.width = 12;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.aiStyle = -1;
        }

        // The blast is the only damage event, including on player contact.
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Give the rocket six ticks to leave its parent explosion before proximity arms.
                if (Projectile.localAI[0] >= 6f && PlayerWithinBlast())
                {
                    Projectile.Kill();
                    return;
                }
                if (Projectile.ai[0] == Seeking)
                {
                    Player target = FindNearestPlayer();
                    if (target != null)
                    {
                        if (Projectile.Distance(target.Center) <= ReleaseDistance)
                        {
                            Projectile.ai[0] = Committed;
                            Projectile.netUpdate = true;
                        }
                        else
                        {
                            Vector2 wanted = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity);
                            Projectile.velocity = Vector2.Lerp(
                                Projectile.velocity.SafeNormalize(wanted), wanted, TurnStrength)
                                .SafeNormalize(wanted) * Speed;
                            if (Projectile.timeLeft % 6 == 0)
                                Projectile.netUpdate = true;
                        }
                    }
                }
            }
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.5f, 0.25f, 0.04f);
                if (Main.GameUpdateCount % 2 == 0)
                {
                    Vector2 back = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 13f;
                    Dust smoke = Dust.NewDustPerfect(back, DustID.Smoke,
                        -Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                        150, default, 0.8f);
                    smoke.noGravity = true;
                    if (Main.rand.NextBool(3))
                        Dust.NewDustPerfect(back, DustID.OrangeTorch, -Projectile.velocity * 0.1f,
                            80, default, 0.6f).noGravity = true;
                }
            }
        }

        private bool PlayerWithinBlast()
        {
            foreach (Player player in Main.ActivePlayers)
                if (!player.dead && !player.ghost
                    && ProximityMineExplosion.IsWithinDamageRadius(Projectile.Center, player.Hitbox))
                    return true;
            return false;
        }

        private Player FindNearestPlayer()
        {
            Player nearest = null;
            float bestDistance = float.MaxValue;
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.dead || player.ghost)
                    continue;
                float distance = Vector2.DistanceSquared(Projectile.Center, player.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = player;
                }
            }
            return nearest;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                Projectile.velocity = Vector2.Zero;
                return false; // Wait for the server's kill and explosion at the authoritative impact.
            }
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.localAI[1] == 1f)
                return; // Ulhan's death or party-wipe cleanup disarmed this rocket.
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<ProximityMineExplosion>(), Projectile.damage,
                    Projectile.knockBack, Main.myPlayer);
        }

        public void Disarm()
        {
            Projectile.localAI[1] = 1f;
            Projectile.Kill();
        }
    }
}
