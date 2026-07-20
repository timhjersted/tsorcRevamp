using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Heart of Winter right tap: a friendly Rimefang spike erupting from the ground at the cursor.
    ///Same telegraph-then-rise presentation as the boss's GigasIceSpike (Deerclops spike texture,
    ///bottom-anchored rise clip). ai[0] = eruption delay, ai[1] = height scale.
    ///</summary>
    class TomeIceSpike : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeerclopsIceSpike;

        const int RiseTicks = 6;
        const int HoldTicks = 70;
        const int SinkTicks = 10;

        int TelegraphTicks => (int)Projectile.ai[0];
        float HeightScale => Projectile.ai[1] > 0f ? Projectile.ai[1] : 1f;
        int Timer => (int)Projectile.localAI[0];
        bool Erupted => Timer > TelegraphTicks;
        bool Sinking => Timer > TelegraphTicks + RiseTicks + HoldTicks;

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 22;
            Projectile.height = 54;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.height = (int)(54 * HeightScale);
            Projectile.timeLeft = TelegraphTicks + RiseTicks + HoldTicks + SinkTicks;
        }

        public override bool? CanDamage()
        {
            return Erupted && !Sinking;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float bottom = Projectile.position.Y + Projectile.height;

            if (!Erupted)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), bottom - 8f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, -1f, 100, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                }
                return;
            }
            if (Timer == TelegraphTicks + 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = -0.3f }, Projectile.Center);
                for (int i = 0; i < 8; i++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X, bottom - 12f), Projectile.width, 12, DustID.Ice, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f), 60, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                }
            }
            Lighting.AddLight(Projectile.Center, 0.15f, 0.3f, 0.45f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 2 * 60);
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
            float texScaleX = Projectile.width / (float)texture.Width * 1.4f;
            Vector2 bottom = new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height + 2f) - Main.screenPosition;
            SpriteEffects flip = Projectile.whoAmI % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(texture, bottom, null, lightColor, 0f, new Vector2(texture.Width / 2f, texture.Height), new Vector2(texScaleX, texScaleY), flip, 0);
            return false;
        }
    }
}
