using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
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

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.dedServ)
                return;
            // The mine vents smoke as this rocket lifts off; there is no launch blast.
            for (int i = 0; i < 16; i++)
            {
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 4f),
                    DustID.Smoke, Main.rand.NextVector2Circular(1.8f, 1.2f), 95,
                    new Color(175, 180, 180), Main.rand.NextFloat(0.7f, 1.1f));
                smoke.noGravity = true;
                smoke.noLight = true;
                smoke.fadeIn = 1.3f;
            }
        }

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
                        if (Projectile.ai[1] != target.whoAmI + 1f)
                        {
                            Projectile.ai[1] = target.whoAmI + 1f;
                            Projectile.netUpdate = true;
                        }
                        if (Projectile.Distance(target.Center) <= ReleaseDistance)
                        {
                            Projectile.ai[0] = Committed;
                            Projectile.netUpdate = true;
                        }
                    }
                }
            }
            // Both peers steer towards the synchronized target, so the displayed rocket stays
            // close to the server's blast center between position updates.
            if (Projectile.ai[0] == Seeking && Projectile.ai[1] > 0f)
            {
                int targetIndex = (int)Projectile.ai[1] - 1;
                if (targetIndex >= 0 && targetIndex < Main.maxPlayers)
                {
                    Player target = Main.player[targetIndex];
                    if (target.active && !target.dead && !target.ghost)
                    {
                        Vector2 wanted = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity);
                        Projectile.velocity = Vector2.Lerp(
                            Projectile.velocity.SafeNormalize(wanted), wanted, TurnStrength)
                            .SafeNormalize(wanted) * Speed;
                        if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft % 6 == 0)
                            Projectile.netUpdate = true;
                    }
                }
            }
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.5f, 0.25f, 0.04f);
                Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
                Vector2 sideways = new Vector2(-backward.Y, backward.X);
                Vector2 exhaust = Projectile.Center + backward * 15f;
                for (int i = 0; i < 2; i++)
                {
                    Dust smoke = Dust.NewDustPerfect(exhaust + backward * (i * 5f)
                        + sideways * Main.rand.NextFloat(-2f, 2f), DustID.Smoke,
                        backward * Main.rand.NextFloat(0.7f, 1.4f)
                        + sideways * Main.rand.NextFloat(-0.5f, 0.5f), 105,
                        new Color(175, 180, 180), Main.rand.NextFloat(0.8f, 1.1f));
                    smoke.noGravity = true;
                    smoke.noLight = true;
                    smoke.fadeIn = 1.3f;
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
