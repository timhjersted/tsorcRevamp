using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.TheDestroyer;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    internal sealed class DestroyerRepairBeam : ModProjectile
    {
        private const int FadeTicks = 12;
        private const int ChunkTicks = 30;
        // 500 health every 30 ticks = 1000 health per second.
        private const int HealPerChunk = 500;
        private Vector2 endpoint;
        private Vector2 cutPosition;
        private int age;
        private int fadeRemaining;
        private bool visible;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyLingeringLaser));

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = TheDestroyer.RepairTicks + FadeTicks + 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (fadeRemaining > 0)
            {
                if (--fadeRemaining == 0)
                {
                    Projectile.Kill();
                }

                return;
            }

            NPC probe = Main.npc[(int)Projectile.ai[0]];
            NPC segment = Main.npc[(int)Projectile.ai[1]];
            NPC head = Main.npc[(int)Projectile.ai[2]];
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (TheDestroyer.CanRepair(probe, segment, head))
            {
                Projectile.Center = probe.Center;
                endpoint = segment.Center;
                if (!visible && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.55f }, probe.Center);
                }

                visible = true;
                age++;
                if (!Main.dedServ && (age == 1 || age % 4 == 0))
                {
                    Vector2 beam = endpoint - Projectile.Center;
                    Vector2 direction = beam.SafeNormalize(Vector2.Zero);
                    Vector2 lateral = direction.RotatedBy(MathHelper.PiOver2);
                    Vector2 position = Vector2.Lerp(Projectile.Center, endpoint, Main.rand.NextFloat());
                    Dust.NewDustPerfect(position, DustID.GreenTorch, direction * 4f, Scale: 1.35f).noGravity = true;

                    Vector2 emitterVelocity = direction * 2.2f + lateral * Main.rand.NextFloat(-0.8f, 0.8f);
                    Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, emitterVelocity, Scale: 1.15f).noGravity = true;

                    // Show the spray on the incoming face of the hull, where the beam first meets the piece.
                    float inset = Math.Min(beam.Length() * 0.5f, Math.Min(segment.width, segment.height) * 0.4f);
                    Vector2 impact = endpoint - direction * inset;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float spread = Main.rand.NextFloat(1.2f, 2.7f) * side;
                        Vector2 sparkVelocity = lateral * spread - direction * Main.rand.NextFloat(0.6f, 1.6f);
                        Dust.NewDustPerfect(impact, DustID.GreenTorch, sparkVelocity, Scale: 1.2f).noGravity = true;
                    }
                }

                if (authority)
                {
                    foreach (Player player in Main.ActivePlayers)
                    {
                        float point = 0f;
                        if (!player.dead && Collision.CheckAABBvLineCollision(player.Hitbox.TopLeft(), player.Hitbox.Size(),
                            Projectile.Center, endpoint, 10f, ref point))
                        {
                            cutPosition = player.Center;
                            break;
                        }
                    }

                    if (cutPosition == Vector2.Zero && age % ChunkTicks == 0)
                    {
                        int heal = Math.Min(HealPerChunk, head.lifeMax - head.life);
                        if (heal > 0)
                        {
                            head.life += heal;
                            head.HealEffect(heal, broadcast: true);
                            head.netUpdate = true;
                        }
                    }
                }
            }

            if (authority && (cutPosition != Vector2.Zero || age >= TheDestroyer.RepairTicks
                || !TheDestroyer.CanRepair(probe, segment, head)))
            {
                fadeRemaining = FadeTicks;
                TheDestroyer.StopRepair(probe);
                Projectile.netUpdate = true;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(visible);
            writer.Write((short)age);
            writer.Write((byte)fadeRemaining);
            writer.WriteVector2(endpoint);
            writer.WriteVector2(cutPosition);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            visible = reader.ReadBoolean();
            age = reader.ReadInt16();
            fadeRemaining = reader.ReadByte();
            endpoint = reader.ReadVector2();
            cutPosition = reader.ReadVector2();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (visible)
            {
                float opacity = fadeRemaining > 0 ? fadeRemaining / (float)FadeTicks : Math.Min(age / 6f, 1f);
                Vector2 start = Projectile.Center;
                DestroyerHullLaser.DrawBand(Main.spriteBatch, start, endpoint, Color.LimeGreen * (0.7f * opacity), 12f);
                DestroyerHullLaser.DrawBand(Main.spriteBatch, start, endpoint, Color.PaleGreen * opacity, 3f);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || !visible)
            {
                return;
            }

            Vector2 burstPosition = cutPosition == Vector2.Zero ? endpoint : cutPosition;
            for (int i = 0; i < 16; i++)
            {
                Dust.NewDustPerfect(burstPosition, DustID.GreenTorch, Main.rand.NextVector2Circular(4f, 4f), Scale: 1.4f).noGravity = true;
            }
        }
    }
}
