using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///Void Singularity: a tear Chaos opens in the arena that drags everything toward it for six seconds.
    ///
    ///Deliberately outlives the attack that spawned it — Chaos casts it in ~70 ticks and then goes back to
    ///fighting, so this is a hazard the player navigates around while dodging everything else. Only the core
    ///hurts; the pull itself never deals damage.
    ///
    ///The grip tightens as you close: out at the rim the inward cap is a fraction of run speed and you simply
    ///walk out, but near the core it exceeds a walk, so the last stretch has to be beaten with a dash, a mount
    ///or wings rather than by holding a direction. That near field is deliberately a commitment.
    ///
    ///Chaos's own cast telegraphs the spot before this ever spawns, so there is no separate warning phase here
    ///— it phases in on its first tick with a dust burst. The sprite is a 7-frame GROWTH strip (94x94 each,
    ///vertical), not a loop, so frames run forward to open, hold on the last, then run backward to close.
    ///That is where the fade in and out comes from.
    ///</summary>
    class ChaosSingularity : ModProjectile
    {
        public const int FrameCount = 7;
        public const int TicksPerFrame = 6;
        public const int OpenTicks = FrameCount * TicksPerFrame;   // 42
        public const int CloseTicks = OpenTicks;
        public const int HoldTicks = 276;                          // total life = 360 ticks = 6 seconds
        public const int TotalTicks = OpenTicks + HoldTicks + CloseTicks;

        const float PullRadius = 1900f;     // the reach of the pull itself
        //Infalling dust is drawn from HALF the pull radius: a tight vortex reads better than a field of motes
        //at extreme range. The pull therefore starts slightly outside the visible spiral — deliberate, and
        //harmless, because out at that range it is a fraction of a pixel per tick.
        const float DustRadius = PullRadius * 0.5f;
        //Baseline pull strength. The SPEED CAP scales with proximity off this (see AI), which is what makes the
        //tear grip harder the closer you get: a flat cap meant the drag felt identical at 100px and 900px,
        //because the acceleration curve hit the ceiling almost immediately either way.
        const float MaxPullSpeed = 5.85f;
        const float NearPullCapMult = 1.25f;  // ceiling right at the core
        const float FarPullCapMult = 0.35f;   // ceiling out at the rim
        const float CoreRadius = 47f;       // half the 94px frame: the only part that can actually hit
        const int ShadowWeightTicks = 360;  // 6 seconds, applied when the core actually catches someone

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.width = 94;
            Projectile.height = 94;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = TotalTicks;
            Projectile.light = 0.5f;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        int Age => TotalTicks - Projectile.timeLeft;
        bool Opening => Age < OpenTicks;
        bool Closing => Age >= OpenTicks + HoldTicks;

        ///<summary>0 while opening or closed, 1 at full size. Scales the pull, the core hitbox and the dust, so
        ///a tear that is still forming cannot grab or kill anyone.</summary>
        float Openness
        {
            get
            {
                if (Opening)
                {
                    return Age / (float)OpenTicks;
                }

                if (Closing)
                {
                    return 1f - (Age - OpenTicks - HoldTicks) / (float)CloseTicks;
                }

                return 1f;
            }
        }

        public override bool? CanDamage()
        {
            //Only a fully-formed core is lethal; the opening and closing animations are safe to stand in.
            return Openness > 0.85f;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation += 0.02f;

            AnimateFrames();

            float openness = Openness;

            //Pull only THIS machine's player. Each client owns its own position, so pulling remote players here
            //would just fight their incoming position syncs and jitter them; their own client does the work.
            if (!Main.dedServ)
            {
                Player player = Main.LocalPlayer;
                Vector2 toCore = Projectile.Center - player.Center;
                float distance = toCore.Length();

                if (player.active && !player.dead && distance < PullRadius && distance > 1f)
                {
                    Vector2 inward = toCore / distance;

                    //Strongest at the core, tapering to nothing at the edge of the field.
                    float falloff = 1f - distance / PullRadius;
                    float pullSpeed = MaxPullSpeed * falloff * falloff * openness;
                    player.velocity += inward * pullSpeed * 0.12f;

                    //Clamp the INWARD component only; movement across or away from the tear is untouched.
                    //The ceiling itself rides proximity, so the far field is a nuisance you walk out of and
                    //the near field genuinely drags — out at the rim it is well under run speed, at the core
                    //it is a little over it.
                    float speedCap = MaxPullSpeed * MathHelper.Lerp(FarPullCapMult, NearPullCapMult, falloff) * openness;
                    float inwardSpeed = Vector2.Dot(player.velocity, inward);

                    if (inwardSpeed > speedCap)
                    {
                        player.velocity -= inward * (inwardSpeed - speedCap);
                    }
                }
            }

            if (!Main.dedServ)
            {
                if (Age == 0)
                {
                    PlayPhaseIn();
                }

                SpawnInfallingDust(openness);
            }

            Lighting.AddLight(Projectile.Center, 0.55f * openness, 0.15f * openness, 0.75f * openness);
        }

        ///<summary>Forward through the strip while opening, held on the last frame, then backward to close.</summary>
        void AnimateFrames()
        {
            int frame = FrameCount - 1;

            if (Opening)
            {
                frame = Age / TicksPerFrame;
            }
            else if (Closing)
            {
                frame = FrameCount - 1 - (Age - OpenTicks - HoldTicks) / TicksPerFrame;
            }

            Projectile.frame = (int)MathHelper.Clamp(frame, 0, FrameCount - 1);
        }

        ///<summary>The moment the tear starts existing: a hard purple burst so it never simply appears.</summary>
        void PlayPhaseIn()
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.5f }, Projectile.Center);

            for (int i = 0; i < 90; i++)
            {
                Vector2 outward = Main.rand.NextVector2CircularEdge(1f, 1f);
                float speed = Main.rand.NextFloat(3f, 11f);

                int dustType = DustID.Shadowflame;
                if (Main.rand.NextBool(2))
                {
                    dustType = DustID.DemonTorch;
                }

                Dust mote = Dust.NewDustPerfect(Projectile.Center + outward * 30f, dustType, outward * speed, 60, default, Main.rand.NextFloat(1.5f, 2.6f));
                mote.noGravity = true;
            }
        }

        ///<summary>Motes born inside half the pull radius and drawn inward on a spiral, so the tear reads as
        ///something actively swallowing matter rather than a static sprite.</summary>
        void SpawnInfallingDust(float openness)
        {
            for (int i = 0; i < 9; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float distance = Main.rand.NextFloat(DustRadius * 0.35f, DustRadius);
                Vector2 spawn = Projectile.Center + angle.ToRotationVector2() * distance;

                //Spiral rather than fall straight in: the tangential component makes it read as orbiting matter.
                Vector2 inward = (Projectile.Center - spawn).SafeNormalize(Vector2.Zero);
                Vector2 tangent = new Vector2(-inward.Y, inward.X);
                Vector2 velocity = (inward * 5.5f + tangent * 2.6f) * openness;

                int dustType = DustID.Shadowflame;
                if (Main.rand.NextBool(3))
                {
                    dustType = DustID.DemonTorch;
                }

                Dust mote = Dust.NewDustPerfect(spawn, dustType, velocity, 80, default, Main.rand.NextFloat(1.2f, 1.9f));
                mote.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 240);
            //Dragged into the tear and it stays on you: heavy, barely able to climb out of where it dropped you.
            target.AddBuff(ModContent.BuffType<WeightOfShadow>(), ShadowWeightTicks);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //Only the black core, not the pull field the broadphase would otherwise imply.
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));

            return Vector2.Distance(Projectile.Center, closest) <= CoreRadius * Openness;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.8f }, Projectile.Center);

            for (int i = 0; i < 70; i++)
            {
                Vector2 burst = Main.rand.NextVector2Circular(7f, 7f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, burst, 70, default, Main.rand.NextFloat(1.4f, 2.4f));
                mote.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle source = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);

            //The sheet's rim is blue; tint it toward Chaos's violet so it belongs to this boss.
            Color tint = new Color(190, 120, 255);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, tint,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
