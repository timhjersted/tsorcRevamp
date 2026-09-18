using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Gwyn
{
    ///<summary>
    ///A lightweight sunlight spear — the volley/storm projectile fired by GwynSolarSpearNode. Shares
    ///the LightningSpear art (GwynLightningSpear.png), but flies straight without the marquee
    ///spear's delayed judgment-bolt payload (that stays unique to Spear of the First Sun). Storm
    ///spears that strike terrain crackle at the impact for 90 ticks, then fire one equal-damage
    ///spear back along the incoming path. The rebound dies normally on its next player/tile hit.
    ///ai[0] = 0 regular Volley spear, 1 outbound Storm spear, 2 Storm rebound, 3 terrain charge.
    ///ai[1] = terrain-charge timer. ai[2] = rebound direction in radians.
    ///</summary>
    class GwynSunlightSpear : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(GwynLightningSpear));

        public const float RegularState = 0f;
        public const float StormOutboundState = 1f;
        const float StormReboundState = 2f;
        const float StormChargeState = 3f;
        const int ImpactChargeTicks = 90;
        const float SpearSpeed = 12f;

        bool IsCharging => Projectile.ai[0] == StormChargeState;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 26;
            Projectile.height = 18;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 240;
            Projectile.light = 0.6f;
        }

        public override void AI()
        {
            AdvanceAnimation();

            if (IsCharging)
            {
                TickImpactCharge();
                return;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, 0f, 0f, 80, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.5f, 0.2f);
        }

        void AdvanceAnimation()
        {
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }
        }

        void TickImpactCharge()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.ai[1]++;

            if (Main.netMode != NetmodeID.Server)
            {
                SpawnChargeDust();
            }
            Lighting.AddLight(Projectile.Center, 0.75f, 0.65f, 0.35f);

            if (Projectile.ai[1] < ImpactChargeTicks)
            {
                return;
            }

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Main.netMode != NetmodeID.Server)
                {
                    SpawnReleaseBurst();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.55f, Pitch = 0.35f }, Projectile.Center);
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 reboundDirection = Projectile.ai[2].ToRotationVector2();
                Vector2 spawnPosition = Projectile.Center + reboundDirection * 18f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition,
                    reboundDirection * SpearSpeed, Type, Projectile.damage, Projectile.knockBack,
                    Projectile.owner, StormReboundState);
                Projectile.Kill();
            }
        }

        void SpawnChargeDust()
        {
            Vector2 reboundDirection = Projectile.ai[2].ToRotationVector2();
            Vector2 normal = new Vector2(-reboundDirection.Y, reboundDirection.X);

            Dust blueSquiggle = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                DustID.Electric,
                normal * Main.rand.NextFloat(-1.4f, 1.4f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                55, Color.RoyalBlue, Main.rand.NextFloat(0.65f, 1.05f));
            blueSquiggle.noGravity = true;

            Dust yellowSpark = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                DustID.GoldFlame,
                reboundDirection * Main.rand.NextFloat(0.4f, 1.4f) + Main.rand.NextVector2Circular(0.7f, 0.7f),
                45, new Color(255, 225, 100), Main.rand.NextFloat(0.45f, 0.8f));
            yellowSpark.noGravity = true;
        }

        void SpawnReleaseBurst()
        {
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(6f, 6f) * Main.rand.NextFloat(0.45f, 1f);
                Dust blueSquiggle = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, velocity,
                    45, Color.RoyalBlue, Main.rand.NextFloat(0.65f, 1.05f));
                blueSquiggle.noGravity = true;
            }
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(8f, 8f) * Main.rand.NextFloat(0.55f, 1f);
                Dust yellowSpark = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, velocity,
                    35, new Color(255, 225, 100), Main.rand.NextFloat(0.45f, 0.85f));
                yellowSpark.noGravity = true;
            }

            //Gold sparks kicked out of the tile along the firing line, so the rebound's launch reads at a glance.
            //±0.6 rad (~35°) cone, faster and larger than the ring above; every other spark keeps gravity and falls like an ember.
            Vector2 reboundDirection = Projectile.ai[2].ToRotationVector2();
            for (int i = 0; i < 16; i++)
            {
                float spreadAngle = Main.rand.NextFloat(-0.6f, 0.6f);
                float sparkSpeed = Main.rand.NextFloat(4f, 11f);
                Vector2 velocity = reboundDirection.RotatedBy(spreadAngle) * sparkSpeed;

                Dust goldSpark = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, velocity,
                    20, new Color(255, 225, 100), Main.rand.NextFloat(0.9f, 1.4f));
                goldSpark.noGravity = i % 2 == 0;
            }
        }

        public override bool? CanDamage()
        {
            return IsCharging ? false : null;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.ai[0] != StormOutboundState)
            {
                return true;
            }

            Vector2 reboundDirection = -oldVelocity.SafeNormalize(Vector2.UnitY);
            Projectile.ai[0] = StormChargeState;
            Projectile.ai[1] = 0f;
            Projectile.ai[2] = reboundDirection.ToRotation();
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = ImpactChargeTicks + 5;
            Projectile.netUpdate = true;

            if (Main.netMode != NetmodeID.Server)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.45f, Pitch = 0.45f }, Projectile.Center);

                //Small gold flash where the spear buries itself, so the sprite hiding (see PreDraw) reads as an impact, not a pop-out.
                for (int i = 0; i < 8; i++)
                {
                    float spreadAngle = Main.rand.NextFloat(-1f, 1f);
                    Vector2 velocity = reboundDirection.RotatedBy(spreadAngle) * Main.rand.NextFloat(1.5f, 4.5f);

                    Dust impactSpark = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, velocity,
                        35, new Color(255, 225, 100), Main.rand.NextFloat(0.6f, 1f));
                    impactSpark.noGravity = true;
                }
            }
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }

            //Normal death language for both the outbound spear and its one allowed rebound:
            //blue electric squiggles underneath a faster foreground of small yellow sparks.
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                Dust blueSquiggle = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, velocity,
                    50, Color.RoyalBlue, Main.rand.NextFloat(0.65f, 1.05f));
                blueSquiggle.noGravity = true;
            }
            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(6.5f, 6.5f) * Main.rand.NextFloat(0.45f, 1f);
                Dust yellowSpark = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, velocity,
                    35, new Color(255, 225, 100), Main.rand.NextFloat(0.45f, 0.8f));
                yellowSpark.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Hidden while buried in the tile: the crackle dust is the whole telegraph. The spear only
            //reappears as the rebound projectile spawned when the 90t charge fires.
            if (IsCharging)
            {
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
            SpriteEffects fx = Projectile.velocity.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, 0.45f, fx, 0);
            return false;
        }
    }
}
