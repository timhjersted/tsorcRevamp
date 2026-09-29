using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class ProximityMine : ModProjectile
    {
        private const float Gravity = 0.22f;
        private const float ThrowSpeed = 12.5f;
        private const int SpreadCount = 6;
        private const float SpreadDegrees = 42f;
        private const int DormantTicks = 6 * 60;
        private const int PopTicks = 40;
        private const float Flying = 0f;
        private const float Grounded = 1f;
        private const float Popping = 2f;
        private const float Detonating = 3f;
        private const float RocketPopping = 4f;
        private const float LaunchingRocket = 5f;

        public override void SetDefaults()
        {
            // ProximityMine.png: 24x24, circular body with an orange centre.
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = DormantTicks + 60;
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => false;

        // ai[2] carries the spawning NPC's slot + 1 for encounter cleanup and the rocket child.
        public static int[] ThrowSpread(IEntitySource source, Vector2 origin, Vector2 target,
            int damage, float knockback, int owner, int ownerNpcIndex)
        {
            Vector2 baseVelocity = UsefulFunctions.BallisticTrajectory(origin, target,
                ThrowSpeed, Gravity, highAngle: false, fallback: true);
            if (baseVelocity == Vector2.Zero)
                baseVelocity = (target - origin).SafeNormalize(Vector2.UnitX) * ThrowSpeed;

            int[] slots = new int[SpreadCount];
            for (int i = 0; i < SpreadCount; i++)
            {
                float angle = MathHelper.ToRadians(-SpreadDegrees * 0.5f
                    + SpreadDegrees * i / (SpreadCount - 1));
                Vector2 velocity = baseVelocity.RotatedBy(angle)
                    * Main.rand.NextFloat(0.92f, 1.08f);
                slots[i] = Projectile.NewProjectile(source, origin, velocity,
                    ModContent.ProjectileType<ProximityMine>(), damage, knockback, owner,
                    0f, 0f, ownerNpcIndex + 1);
            }
            return slots;
        }

        public override void AI()
        {
            if ((Projectile.ai[0] == Popping || Projectile.ai[0] == RocketPopping)
                && Projectile.localAI[1] != Projectile.ai[0] && !Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.55f, Pitch = 0.3f }, Projectile.Center);
            Projectile.localAI[1] = Projectile.ai[0];
            if (Projectile.ai[0] == Flying)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && PlayerWithinBlast())
                {
                    Detonate(false);
                    return;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft <= 60)
                {
                    // A mine thrown into a bottomless void still completes its lifecycle.
                    Detonate(true);
                    return;
                }
                Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + Gravity, 16f);
                Projectile.rotation += Projectile.velocity.X * 0.08f;
                EmitTrail();
                return;
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            if (Projectile.ai[0] == Grounded)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.ai[1]++;
                    if (PlayerWithinBlast())
                        BeginPop(false);
                    else if (Projectile.ai[1] >= DormantTicks)
                        BeginPop(true);
                }
                if (!Main.dedServ && Main.GameUpdateCount % 12 == 0)
                    Dust.NewDustPerfect(Projectile.Center - new Vector2(0f, 7f), DustID.OrangeTorch,
                        new Vector2(0f, -0.25f), 100, default, 0.55f).noGravity = true;
                return;
            }

            if (Projectile.ai[0] == Popping || Projectile.ai[0] == RocketPopping)
            {
                Projectile.ai[1]++;
                if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center - new Vector2(0f, 7f)
                        + Main.rand.NextVector2Circular(5f, 2f),
                        Main.rand.NextBool() ? DustID.OrangeTorch : DustID.Torch,
                        new Vector2(0f, 0.8f), 80, default, 0.7f);
                    dust.noGravity = true;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.ai[1] >= PopTicks)
                    Detonate(Projectile.ai[0] == RocketPopping);
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

        private void BeginPop(bool launchRocket)
        {
            Projectile.ai[0] = launchRocket ? RocketPopping : Popping;
            Projectile.ai[1] = 0f;
            Projectile.timeLeft = PopTicks + 30;
            Projectile.netUpdate = true;
        }

        private void Detonate(bool launchRocket)
        {
            Projectile.ai[0] = launchRocket ? LaunchingRocket : Detonating;
            Projectile.Kill();
        }

        private void EmitTrail()
        {
            if (Main.dedServ || Main.GameUpdateCount % 4 != 0)
                return;
            Dust dust = Dust.NewDustPerfect(Projectile.Center,
                Main.rand.NextBool(3) ? DustID.Smoke : DustID.OrangeTorch,
                -Projectile.velocity * 0.15f, 130, default, 0.6f);
            dust.noGravity = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.ai[0] != Flying)
                return false;
            if (oldVelocity.Y > 0f && Projectile.velocity.Y != oldVelocity.Y)
            {
                // The collision solver places the bottom at the tile top. Sink the 24px
                // sprite by 12px so the lower half stays buried throughout the fuse.
                Projectile.position.Y += Projectile.height * 0.5f;
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.ai[0] = Grounded;
                Projectile.ai[1] = 0f;
                Projectile.timeLeft = DormantTicks + PopTicks + 20;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.netUpdate = true;
                }
                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.35f }, Projectile.Center);
            }
            else
            {
                Projectile.velocity.X = 0f;
                if (Projectile.velocity.Y != oldVelocity.Y)
                    Projectile.velocity.Y = 0f;
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if ((Projectile.ai[0] == Detonating || Projectile.ai[0] == LaunchingRocket)
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Projectile.ai[0] == Detonating)
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<ProximityMineExplosion>(), Projectile.damage,
                        Projectile.knockBack, Main.myPlayer);
                else
                {
                    Player target = FindNearestPlayer();
                    Vector2 heading = target == null ? -Vector2.UnitY
                        : (target.Center - Projectile.Center).SafeNormalize(-Vector2.UnitY);
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center - new Vector2(0f, 12f),
                        heading * ProximityMineRocket.Speed,
                        ModContent.ProjectileType<ProximityMineRocket>(), Projectile.damage,
                        Projectile.knockBack, Main.myPlayer, 0f,
                        target == null ? 0f : target.whoAmI + 1f, Projectile.ai[2]);
                }
            }
            else if (Projectile.ai[0] != Detonating && Projectile.ai[0] != LaunchingRocket
                && !Main.dedServ)
            {
                for (int i = 0; i < 12; i++)
                    Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                        Main.rand.NextVector2Circular(1.5f, 1.5f), 170, default, 0.65f).noGravity = true;
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
            List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
            List<int> overWiresUI)
        {
            if (Projectile.ai[0] != Flying)
                behindNPCsAndTiles.Add(index);
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
    }
}
