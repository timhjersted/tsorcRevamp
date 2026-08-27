using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Ice Gigas ground spike (Deerclops homage, reusing its vanilla spike texture). Spawned with its
    ///bottom on the ground; frost-patch telegraph for ai[0] ticks, then erupts over a few ticks and
    ///persists ~2s as damaging terrain before sinking away. ai[1] = height scale (1 = normal).
    ///Shared by Glacial Slam's marching waves, Rimefang Spikes and the Stampede's foot-trail.
    ///</summary>
    class GigasIceSpike : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeerclopsIceSpike;

        const int RiseTicks = 6;
        const int HoldTicks = 110;
        const int SinkTicks = 10;
        //Vanilla's own draw code special-cases type 961 (see Main.DrawProj) and crops ONE frame out
        //of a 1x5 vertical strip — the sheet is 5 growth-stage frames, not a single tall image. We
        //were drawing the whole sheet as one sprite, which squashed it to 1/5 height: the "spike"
        //came out as a wide horizontal smear (reads as sideways) and warped unpredictably as the
        //rise/sink scale animated across all 5 overlapping frames at once.
        const int SheetFrames = 5;
        const int ArtFrame = 4; //tallest/most-detailed growth stage; our own RiseProgress handles the animation

        int TelegraphTicks => (int)Projectile.ai[0];
        float HeightScale => Projectile.ai[1] > 0f ? Projectile.ai[1] : 1f;
        int Timer => (int)Projectile.localAI[0];
        bool Erupted => Timer > TelegraphTicks;
        bool Sinking => Timer > TelegraphTicks + RiseTicks + HoldTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 22;
            Projectile.height = 54;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
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
                //Frost patch simmering on the ground where it will erupt
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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.3f }, Projectile.Center);
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X, bottom - 12f), Projectile.width, 12, DustID.Ice, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f), 60, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                }
            }
            //Held: occasional cold glints
            if (!Sinking && Main.rand.NextBool(6))
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), bottom - Main.rand.NextFloat(Projectile.height * RiseProgress()));
                int glint = Dust.NewDust(pos, 4, 4, DustID.IceRod, 0f, 0f, 100, default, 0.8f);
                Main.dust[glint].noGravity = true;
                Main.dust[glint].velocity *= 0.2f;
            }
            Lighting.AddLight(Projectile.Center, 0.2f, 0.35f, 0.5f);
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

        //Draw the vanilla Deerclops spike rising out of the ground (bottom-anchored, vertically clipped)
        public override bool PreDraw(ref Color lightColor)
        {
            float progress = RiseProgress();
            if (progress <= 0f)
            {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[ProjectileID.DeerclopsIceSpike].Value;
            int frameHeight = texture.Height / SheetFrames;
            Rectangle frame = new Rectangle(0, ArtFrame * frameHeight, texture.Width, frameHeight);

            float drawHeight = Projectile.height * progress;
            float texScaleY = drawHeight / frameHeight;
            float texScaleX = Projectile.width / (float)texture.Width * 1.4f;
            Vector2 bottom = new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height + 2f) - Main.screenPosition;
            SpriteEffects flip = Projectile.whoAmI % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(texture, bottom, frame, lightColor, 0f, new Vector2(frame.Width / 2f, frame.Height), new Vector2(texScaleX, texScaleY), flip, 0);
            return false;
        }
    }
}
