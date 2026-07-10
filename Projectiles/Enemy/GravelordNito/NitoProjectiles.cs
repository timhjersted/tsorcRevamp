using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class NitoSwordSlash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNitoSword";

        int OwnerIndex => (int)Projectile.ai[0];
        int Kind => (int)Projectile.ai[1];
        int Dir => Projectile.velocity.X >= 0f ? 1 : -1;
        int Timer => (int)Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 330;
            Projectile.height = 260;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC owner = OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs ? Main.npc[OwnerIndex] : null;
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            Vector2 offset = Kind switch
            {
                1 => new Vector2(Dir * 70f, -118f),
                2 => new Vector2(Dir * (135f + Timer * 5f), -78f),
                3 => new Vector2(Dir * 78f, -72f),
                _ => new Vector2(Dir * 100f, -82f),
            };
            Projectile.Center = owner.Center + offset;
            Projectile.rotation = RotationForKind();

            for (int i = 0; i < 3; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(90f, 45f), DustID.BoneTorch, Main.rand.NextVector2Circular(2f, 2f), 90, default, 0.95f);
                d.noGravity = true;
            }
        }

        float RotationForKind()
        {
            float progress = Timer / 18f;
            return Kind switch
            {
                1 => Dir * MathHelper.Lerp(-1.65f, 0.85f, progress),
                2 => Dir > 0 ? 0.02f : MathHelper.Pi - 0.02f,
                3 => Dir * MathHelper.Lerp(0.45f, -0.95f, progress),
                _ => Dir * MathHelper.Lerp(-0.85f, 0.55f, progress),
            };
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 hilt = Projectile.Center - Projectile.rotation.ToRotationVector2() * 60f;
            Vector2 tip = Projectile.Center + Projectile.rotation.ToRotationVector2() * 170f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(new Vector2(targetHitbox.X, targetHitbox.Y), new Vector2(targetHitbox.Width, targetHitbox.Height), hilt, tip, Kind == 2 ? 34f : 48f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            SpriteEffects effects = Dir < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(texture.Width / 2f, texture.Height / 2f), 1f, effects, 0);
            return false;
        }
    }

    class NitoBoneShard : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bone;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            Projectile.rotation += Projectile.velocity.X * 0.04f;
            Projectile.velocity.Y += 0.08f;
            Lighting.AddLight(Projectile.Center, 0.16f, 0.16f, 0.22f);
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, -Projectile.velocity.X * 0.1f, -Projectile.velocity.Y * 0.1f, 110, default, 0.75f);
            }
        }
    }

    class NitoDeathNova : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float ExpandSpeed = 8f;
        const float RingHalfThickness = 24f;
        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 260f;
        float Radius => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 620;
            Projectile.height = 620;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = (int)(MaxRadius / ExpandSpeed) + 3;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] += ExpandSpeed;
            for (int i = 0; i < 34; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * (Radius + Main.rand.NextFloat(-7f, 7f));
                Dust d = Dust.NewDustPerfect(pos, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.BoneTorch, angle.ToRotationVector2() * 2.2f, 75, default, Main.rand.NextFloat(1f, 1.5f));
                d.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.45f, 0.35f, 0.65f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float distClosest = Vector2.Distance(Projectile.Center, closest);
            float distFarthest = 0f;
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Bottom)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Bottom)));
            return distClosest <= Radius + RingHalfThickness && distFarthest >= Radius - RingHalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Darkness, 5 * 60);
            target.AddBuff(BuffID.Slow, 2 * 60);
        }
    }

    class NitoGraveSpike : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeerclopsIceSpike;

        const int RiseTicks = 7;
        const int HoldTicks = 80;
        const int SinkTicks = 12;
        int TelegraphTicks => (int)Projectile.ai[0];
        float HeightScale => Projectile.ai[1] > 0f ? Projectile.ai[1] : 1f;
        int Timer => (int)Projectile.localAI[0];
        bool Erupted => Timer > TelegraphTicks;
        bool Sinking => Timer > TelegraphTicks + RiseTicks + HoldTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 26;
            Projectile.height = 54;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.height = (int)(54 * HeightScale);
            Projectile.timeLeft = TelegraphTicks + RiseTicks + HoldTicks + SinkTicks;
        }

        public override bool? CanDamage() => Erupted && !Sinking;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float bottom = Projectile.position.Y + Projectile.height;
            if (!Erupted)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust.NewDust(new Vector2(Projectile.position.X, bottom - 10f), Projectile.width, 8, DustID.BoneTorch, 0f, -0.8f, 100, default, 0.9f);
                }
                return;
            }
            if (Timer == TelegraphTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.55f, Pitch = -0.5f }, Projectile.Center);
            }
            if (!Sinking && Main.rand.NextBool(4))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, 0f, -0.6f, 100, default, 0.8f);
            }
        }

        float RiseProgress()
        {
            if (!Erupted)
            {
                return 0f;
            }
            if (Sinking)
            {
                return 1f - (Timer - TelegraphTicks - RiseTicks - HoldTicks) / (float)SinkTicks;
            }
            return MathHelper.Min(1f, (Timer - TelegraphTicks) / (float)RiseTicks);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = RiseProgress();
            if (progress <= 0f)
            {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[ProjectileID.DeerclopsIceSpike].Value;
            float drawHeight = Projectile.height * progress;
            float texScaleY = drawHeight / texture.Height;
            float texScaleX = Projectile.width / (float)texture.Width * 1.5f;
            Vector2 bottom = new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height + 2f) - Main.screenPosition;
            Main.EntitySpriteDraw(texture, bottom, null, new Color(150, 150, 165, 230), 0f, new Vector2(texture.Width / 2f, texture.Height), new Vector2(texScaleX, texScaleY), SpriteEffects.None, 0);
            return false;
        }
    }

    class NitoGraveHand : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        int TelegraphTicks => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : 18;
        int Timer => (int)Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 86;
            Projectile.height = 78;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;
            Projectile.aiStyle = 0;
        }

        public override bool? CanDamage() => Timer > TelegraphTicks && Timer < TelegraphTicks + 16;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Timer <= TelegraphTicks)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust.NewDust(Projectile.BottomLeft - new Vector2(0f, 8f), Projectile.width, 12, DustID.BoneTorch, 0f, -1f, 100, default, 0.9f);
                }
            }
            else if (Timer == TelegraphTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.4f }, Projectile.Center);
                for (int i = 0; i < 22; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f), 80, default, 1.1f);
                }
            }
        }
    }

    class NitoMiasmaCloud : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 58;
            Projectile.height = 58;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 95;
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.98f;
            Projectile.rotation += 0.03f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(28f, 28f), DustID.Poisoned, Main.rand.NextVector2Circular(1f, 1f), 120, default, Main.rand.NextFloat(1.1f, 1.8f));
                d.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 4 * 60);
            target.AddBuff(BuffID.Darkness, 3 * 60);
        }
    }
}
