using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Abyss Slash finisher: fans out, briefly homes, then gets a final four-second homing window.
    // Each homing pass commits to a straight heading within three tiles of the player.
    class ArtoriasFlameOrb : ModProjectile
    {
        const int FanOutTicks = 3 * 60;
        const int HomingTicks = 60;
        const int FinalTicks = 3 * 60;
        const int FinalHomingStart = FanOutTicks + HomingTicks + FinalTicks;
        const int FinalHomingTicks = 4 * 60;
        const int TotalTicks = FinalHomingStart + FinalHomingTicks;
        const float TrackingCutoff = 3f * 16f;
        const int FadeTicks = 15;
        // ai[2]: initial pass (0), initial heading locked (1), final homing (2), final heading locked (3).

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(AbyssLurkerFlameOrb));

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.scale = 1.1f;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // Deliberately cross terrain on both seeking passes.
            Projectile.ignoreWater = true;
            Projectile.light = 0.9f;
            Projectile.timeLeft = TotalTicks;
        }

        public override void AI()
        {
            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.1f);
            }

            Animate();

            Projectile.ai[0]++;
            float elapsed = Projectile.ai[0];

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (elapsed > FinalHomingStart && Projectile.ai[2] < 2f)
                {
                    Projectile.ai[2] = 2f;
                    Projectile.netUpdate = true;
                }

                bool initialHoming = elapsed > FanOutTicks && elapsed <= FanOutTicks + HomingTicks;
                bool homing = (initialHoming && Projectile.ai[2] == 0f) || Projectile.ai[2] == 2f;
                if (homing)
                {
                    Player target = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
                    if (target.active && !target.dead)
                    {
                        float targetDistance = Vector2.Distance(Projectile.Center, target.Center);
                        if (targetDistance <= TrackingCutoff)
                        {
                            Projectile.ai[2]++;
                            Projectile.netUpdate = true;
                        }
                        else
                        {
                            // Preserve travel speed through the turn; lerping opposite velocities
                            // shrank the old sphere's speed and could strand it far from the player.
                            float speed = Projectile.velocity.Length();
                            float currentAngle = Projectile.velocity.ToRotation();
                            float targetAngle = (target.Center - Projectile.Center).ToRotation();
                            float homingAngle = Utils.AngleTowards(currentAngle, targetAngle, 0.06f);
                            Projectile.velocity = homingAngle.ToRotationVector2() * speed;

                            if ((int)elapsed % 6 == 0)
                            {
                                Projectile.netUpdate = true;
                            }
                        }
                    }
                }
            }

            FaceVelocity();
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            Projectile.alpha = (int)(255f * (1f - fade));

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.4f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float elapsed = Projectile.ai[0];
            bool initialHoming = elapsed > FanOutTicks && elapsed <= FanOutTicks + HomingTicks;
            bool homing = (initialHoming && Projectile.ai[2] == 0f) || Projectile.ai[2] == 2f;
            float state = 0.12f;
            if (homing)
            {
                state = 1f;
            }
            else if (elapsed > FanOutTicks)
            {
                state = 0.55f;
            }

            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float trailOpacity = homing ? 0.78f : 0.54f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            ArtoriasVFX.DrawProjectileTrail(Projectile.Center - direction * 28f, direction.ToRotation(),
                new Vector2(homing ? 72f : 58f, 22f), state, trailOpacity * fade);
            ArtoriasVFX.DrawOrb(Projectile.Center, new Vector2(38f, 38f), Projectile.rotation,
                state, 0.90f * fade);
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
        }

        void FaceVelocity()
        {
            if (Projectile.velocity.LengthSquared() > 0.01f)
            {
                Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
                Projectile.rotation = MathHelper.PiOver2;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), 4 * 60, false);
            SpawnFlameDust(8, 4.6f, 1.35f);

            // ai[1] is the firing NPC's whoAmI, offset by +1 (0 = "no owner" - ArtoriasAbyssBlast's
            // own orb fan spawns this projectile anonymously) so the dodge-punish-chain system knows
            // an Abyss Slash orb finisher actually connected.
            int ownerIdx = (int)Projectile.ai[1] - 1;
            if (ownerIdx >= 0 && ownerIdx < Main.maxNPCs && Main.npc[ownerIdx].active
                && Main.npc[ownerIdx].ModNPC is PuppetNPC invader)
            {
                invader.ReportAttackHit();
            }
        }

        public override void OnKill(int timeLeft)
        {
            SpawnFlameDust(5, 3.2f, 1.2f);
        }

        void SpawnFlameDust(int count, float speed, float scale)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(speed, speed);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch,
                    velocity.X, velocity.Y, 100, new Color(158, 77, 220), scale);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
