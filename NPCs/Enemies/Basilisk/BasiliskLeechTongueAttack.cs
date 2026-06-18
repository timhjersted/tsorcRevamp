using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    internal static class BasiliskLeechTongueAttack
    {
        private const int TelegraphTicks = 45;
        private const float MinRange = 120f;
        private const float MaxRange = 560f;

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
            if (Main.rand.NextBool(2))
            {
                Vector2 mouth = Projectiles.Enemy.BasiliskLeechTongue.GetMouthPosition(npc);
                Dust dust = Dust.NewDustPerfect(mouth + Main.rand.NextVector2Circular(10f, 8f), DustID.PinkTorch, Main.rand.NextVector2Circular(1.5f, 1.5f), 80, Color.HotPink, 1.2f);
                dust.noGravity = true;
            }
        }

        private static void Fire(NPC npc, Player player, int damage)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 mouth = Projectiles.Enemy.BasiliskLeechTongue.GetMouthPosition(npc);
            Vector2 target = player.Center + new Vector2(0f, -120f);
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(mouth, target, 8.5f, Projectiles.Enemy.BasiliskLeechTongue.Gravity, true, true);
            Projectile.NewProjectile(npc.GetSource_FromThis(), mouth, velocity, ModContent.ProjectileType<Projectiles.Enemy.BasiliskLeechTongue>(), damage, 0f, Main.myPlayer, npc.whoAmI);
            SoundEngine.PlaySound(SoundID.NPCHit8 with { Volume = 0.45f, Pitch = 0.35f }, npc.Center);
        }

        private static bool HasActiveTongue(NPC npc)
        {
            int tongueType = ModContent.ProjectileType<Projectiles.Enemy.BasiliskLeechTongue>();
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
