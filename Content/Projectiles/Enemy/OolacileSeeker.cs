using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class OolacileSeeker : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Oolacile Seeker");
        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.height = 34;
            Projectile.tileCollide = false;
            Projectile.width = 34;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 750;
            Main.projFrames[Projectile.type] = 4;
            Projectile.light = 1;
            Projectile.scale = 1.22f;
        }

        // ai[0] = target player. ai[1] = 1 selects the Acolyte's PAIR mode (ai[2] = launch side, +1 / -1); 0 keeps the
        // legacy Oolacile Sorcerer seeker that the Grand Occultist still fires.
        private bool PairMode => Projectile.ai[1] > 0.5f;

        // Pair mode: forms at the staff tip for 12t (0.4 -> 1.22 scale, harmless), launches 70 degrees off the aim line at
        // 6 px/t, swings wide for 30t, then turns toward the target at up to 0.05 rad/t at 7 px/t; homing stops 90t later.
        public const int PairFormTicks = 12;
        private const int PairSwingTicks = 30;
        private const int PairHomingTicks = 90;
        private const int PairLifetime = PairFormTicks + PairSwingTicks + PairHomingTicks + 120;
        private const int PairFadeTicks = 15;
        private const float PairLaunchAngle = 70f;
        private const float PairLaunchSpeed = 6f;
        private const float PairHomingSpeed = 7f;
        private const float PairTurnRate = 0.05f;
        private const float FullScale = 1.22f;

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override bool? CanDamage()
        {
            bool forming = PairMode && Age < PairFormTicks;
            bool fading = PairMode && Projectile.timeLeft < PairFadeTicks;
            if (forming || fading)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Age++;

            // Every peer sets the pair lifetime on its first tick (OnSpawn would only run on the spawning machine).
            if (Age == 1 && PairMode)
            {
                Projectile.timeLeft = PairLifetime;
            }

            if (PairMode)
            {
                PairAI();
            }
            else
            {
                LegacyAI();
            }

            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Wraith, 0, 0, 100, Color.Black, 1.0f);
            Main.dust[dust].noGravity = true;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frame++;
                Projectile.frameCounter = 3;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }
        }

        private void LegacyAI()
        {
            this.Projectile.rotation = (float)Math.Atan2((double)this.Projectile.velocity.Y, (double)this.Projectile.velocity.X);

            if (this.Projectile.timeLeft < 100)
            {
                this.Projectile.scale *= 0.9f;
                this.Projectile.damage = 0;
            }

            if (this.Projectile.timeLeft > 200 && this.Projectile.timeLeft < 500)
            {
                this.Projectile.velocity.X -= (this.Projectile.position.X - Main.player[(int)this.Projectile.ai[0]].position.X) / 1000f;
                this.Projectile.velocity.Y -= (this.Projectile.position.Y - Main.player[(int)this.Projectile.ai[0]].position.Y) / 1000f;

                this.Projectile.rotation = (float)Math.Atan2((double)this.Projectile.velocity.Y, (double)this.Projectile.velocity.X);
                this.Projectile.velocity.Y = (float)Math.Sin(this.Projectile.rotation) * 8;
                this.Projectile.velocity.X = (float)Math.Cos(this.Projectile.rotation) * 8;
            }
        }

        private void PairAI()
        {
            // tileCollide is not synced, so every peer derives it from the synced mode + its own age: walls kill it once launched.
            Projectile.tileCollide = Age > PairFormTicks;

            if (Age <= PairFormTicks)
            {
                // Hold at the tip and grow; the launch direction rides in velocity at 1/100th magnitude so it syncs.
                float formProgress = Age / (float)PairFormTicks;
                Vector2 aimDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.scale = MathHelper.Lerp(0.4f, FullScale, formProgress);
                Projectile.rotation = aimDirection.ToRotation();
                Projectile.velocity = aimDirection * 0.01f;

                if (Age == PairFormTicks)
                {
                    float side = Math.Sign(Projectile.ai[2]);
                    if (side == 0f)
                    {
                        side = 1f;
                    }

                    Vector2 launch = aimDirection.RotatedBy(MathHelper.ToRadians(PairLaunchAngle) * side);
                    Projectile.velocity = launch * PairLaunchSpeed;
                }

                return;
            }

            int homingStart = PairFormTicks + PairSwingTicks;
            bool homing = Age > homingStart && Age <= homingStart + PairHomingTicks;
            Player target = Main.player[(int)Projectile.ai[0]];

            if (homing && target.active && !target.dead)
            {
                float currentAngle = Projectile.velocity.ToRotation();
                float wantedAngle = (target.Center - Projectile.Center).ToRotation();
                float turned = currentAngle.AngleTowards(wantedAngle, PairTurnRate);
                Projectile.velocity = turned.ToRotationVector2() * PairHomingSpeed;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Projectile.timeLeft < PairFadeTicks)
            {
                Projectile.scale = FullScale * (Projectile.timeLeft / (float)PairFadeTicks);
                Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / (float)PairFadeTicks));
            }
        }

        // A real death event. This replaces the old PreKill, which reassigned Projectile.type to vanilla 41
        // so the engine would play THAT projectile's death instead — the seeker had no death of its own.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.2f }, Projectile.Center);
            int shardCount = 70;
            if (PairMode)
            {
                shardCount = 30;
            }

            for (int i = 0; i < shardCount; i++)
            {
                Vector2 outward = Main.rand.NextVector2Circular(5f, 5f);
                Dust shard = Dust.NewDustPerfect(Projectile.Center, DustID.Wraith, outward, 90, Color.Black,
                    Main.rand.NextFloat(1f, 1.8f));
                shard.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (PairMode)
            {
                target.AddBuff(BuffID.Darkness, 180);
                return;
            }

            if (Main.expertMode)
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Darkness, 9000, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.Poisoned, 1800, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.Bleeding, 9000, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.BrokenArmor, 300, false);
            }
            else
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Darkness, 18000, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.Poisoned, 3600, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.Bleeding, 18000, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.BrokenArmor, 600, false);
            }
        }
    }
}