using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    internal static class BasiliskLeechTongueAttack
    {
        private const int TelegraphTicks = 45;
        private const float MinRange = 120f;
        private const float MaxRange = BasiliskLeechTongue.MaxLaunchRange;
        private const float MinLureRiseSpeed = 10f;
        private const float MaxLureRiseSpeed = 14f;

        public static bool Update(NPC npc, ref int timer, int cooldown, int damage, bool canStart = true)
        {
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (HasActiveTongue(npc))
            {
                HoldBody(npc, globalNPC, committed: true);
                return true;
            }

            npc.TargetClosest(false);

            Player player = Main.player[npc.target];
            if (!player.active || player.dead)
            {
                timer = 0;
                return false;
            }

            if (!canStart)
            {
                timer = System.Math.Min(timer, cooldown - 30);
                return false;
            }

            timer++;
            bool targetInRange = npc.Distance(player.Center) >= MinRange
                && npc.Distance(player.Center) <= MaxRange
                && Collision.CanHitLine(npc.Center, 1, 1, player.Center, 1, 1);

            if (timer < cooldown)
            {
                return false;
            }

            if (!targetInRange)
            {
                timer = cooldown - 30;
                return false;
            }

            HoldBody(npc, globalNPC, committed: timer >= cooldown + TelegraphTicks / 2);
            Telegraph(npc);

            if (timer == cooldown)
            {
                SpawnTelegraphTip(npc);
            }

            if (timer >= cooldown + TelegraphTicks)
            {
                Fire(npc, player, damage);
                timer = 0;
            }

            return true;
        }

        private static void HoldBody(NPC npc, tsorcRevampGlobalNPC globalNPC, bool committed)
        {
            npc.velocity.X *= 0.75f;
            if (npc.velocity.Y == 0f)
            {
                npc.velocity.X = 0f;
            }

            globalNPC.AttackTelegraphing = !committed;
            globalNPC.AttackCommitted = committed;
            globalNPC.SuppressPreAttackJump = true;
            globalNPC.ProjectileTimer = 0f;
        }

        private static void Telegraph(NPC npc)
        {
            Lighting.AddLight(npc.Center, Color.DeepPink.ToVector3() * 0.7f);
            if (!Main.dedServ)
            {
                Vector2 mouth = BasiliskLeechTongue.GetMouthPosition(npc);
                for (int i = 0; i < 2; i++)
                {
                    if (Main.rand.NextBool(2))
                    {
                        Dust dust = Dust.NewDustPerfect(mouth + Main.rand.NextVector2Circular(10f, 8f), DustID.PinkTorch, Main.rand.NextVector2Circular(1.5f, 1.5f), 80, Color.HotPink, 1.2f);
                        dust.noGravity = true;
                    }
                }
            }
        }

        private static void SpawnTelegraphTip(NPC npc)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), BasiliskLeechTongue.GetMouthPosition(npc), Vector2.Zero,
                    ModContent.ProjectileType<BasiliskLeechTongueTelegraph>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
        }

        private static void Fire(NPC npc, Player player, int damage)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 mouth = BasiliskLeechTongue.GetMouthPosition(npc);
            Vector2 playerOffset = player.Center - mouth;
            Vector2 target = player.Center + playerOffset.SafeNormalize(new Vector2(npc.direction, 0f)) * BasiliskLeechTongue.LaunchOvershoot;
            float distanceFactor = MathHelper.Clamp(playerOffset.Length() / MaxRange, 0f, 1f);
            Vector2 velocity = CalculateLureVelocity(mouth, target, distanceFactor);
            Projectile.NewProjectile(npc.GetSource_FromThis(), mouth, velocity, ModContent.ProjectileType<BasiliskLeechTongue>(), damage, 0f, Main.myPlayer, npc.whoAmI);
            SoundEngine.PlaySound(SoundID.NPCHit8 with { Volume = 0.45f, Pitch = 0.35f }, npc.Center);
        }

        private static Vector2 CalculateLureVelocity(Vector2 mouth, Vector2 target, float distanceFactor)
        {
            float initialYVelocity = -MathHelper.Lerp(MinLureRiseSpeed, MaxLureRiseSpeed, distanceFactor);
            float verticalDistance = target.Y - mouth.Y;
            float b = initialYVelocity + BasiliskLeechTongue.Gravity * 0.5f;
            float discriminant = b * b + 2f * BasiliskLeechTongue.Gravity * verticalDistance;
            if (discriminant <= 0f)
            {
                return UsefulFunctions.Aim(mouth, target, MathHelper.Lerp(20f, 30f, distanceFactor));
            }

            // Use the later root: the tongue rises above its target, then falls onto the 7.5-tile
            // overshoot point like a fishing lure rather than taking a straight shot.
            float flightTicks = (-b + (float)System.Math.Sqrt(discriminant)) / BasiliskLeechTongue.Gravity;
            if (flightTicks <= 1f)
            {
                return UsefulFunctions.Aim(mouth, target, MathHelper.Lerp(20f, 30f, distanceFactor));
            }

            return new Vector2((target.X - mouth.X) / flightTicks, initialYVelocity);
        }

        private static bool HasActiveTongue(NPC npc)
        {
            int tongueType = ModContent.ProjectileType<BasiliskLeechTongue>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.type == tongueType && (int)projectile.ai[0] == npc.whoAmI)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
