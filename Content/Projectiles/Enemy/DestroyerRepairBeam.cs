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
        private const int ChunkTicks = 60;
        private const int HealPerChunk = 2000;
        private const float BeamDrawWidth = 46f;
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
                if (!Main.dedServ)
                {
                    Vector2 beam = endpoint - Projectile.Center;
                    float length = beam.Length();
                    Vector2 direction = beam.SafeNormalize(Vector2.Zero);
                    Vector2 lateral = direction.RotatedBy(MathHelper.PiOver2);
                    Vector2 muzzle = BeamMuzzle(direction, length);
                    Lighting.AddLight(muzzle, 0.08f, 0.58f, 0.20f);

                    if (age == 1 || age % 2 == 0)
                    {
                        int count = age == 1 ? 7 : 3;
                        for (int i = 0; i < count; i++)
                        {
                            Vector2 plume = direction * Main.rand.NextFloat(1.6f, 5.5f);
                            plume += lateral * Main.rand.NextFloat(-2.3f, 2.3f);
                            Vector2 origin = muzzle + Main.rand.NextVector2Circular(3f, 3f);
                            Dust spark = Dust.NewDustPerfect(origin, DustID.GreenTorch, plume, Scale: Main.rand.NextFloat(0.75f, 1.25f));
                            spark.noGravity = true;
                        }
                    }

                    if (age == 1 || age % 4 == 0)
                    {
                        Vector2 position = Vector2.Lerp(muzzle, endpoint, Main.rand.NextFloat());
                        Dust.NewDustPerfect(position, DustID.GreenTorch, direction * 4f, Scale: 1.35f).noGravity = true;

                        // Scatter sparks at the first contact with the incoming face of the hull.
                        float inset = Math.Min(length * 0.5f, Math.Min(segment.width, segment.height) * 0.4f);
                        Vector2 impact = endpoint - direction * inset;
                        for (int side = -1; side <= 1; side += 2)
                        {
                            float spread = Main.rand.NextFloat(1.2f, 2.7f) * side;
                            Vector2 sparkVelocity = lateral * spread - direction * Main.rand.NextFloat(0.6f, 1.6f);
                            Dust.NewDustPerfect(impact, DustID.GreenTorch, sparkVelocity, Scale: 1.2f).noGravity = true;
                        }
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
                Vector2 beam = endpoint - Projectile.Center;
                Vector2 direction = beam.SafeNormalize(Vector2.Zero);
                Vector2 muzzle = BeamMuzzle(direction, beam.Length());
                Color green = new Color(38, 255, 82);
                DestroyerHullLaser.DrawLaser(muzzle, endpoint, BeamDrawWidth, green, opacity * 0.85f, 1.1f * opacity,
                    -10f * age, "DestroyerRepairBeam");
            }

            return false;
        }

        private Vector2 BeamMuzzle(Vector2 direction, float length)
        {
            return Projectile.Center + direction * Math.Min(10f, length * 0.2f);
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
