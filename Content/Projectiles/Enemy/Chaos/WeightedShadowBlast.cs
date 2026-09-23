using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///The bolt Chaos throws in rings during Scythe Lunge: a knot of shadow that leaves whatever it touches
    ///too heavy to climb, on top of its damage.
    ///
    ///The sheet is a HORIZONTAL 4-frame strip (48x28 a frame), which Main.projFrames cannot express — that
    ///machinery assumes a vertical strip and offsets frame.Y — so the framing is done by hand in PreDraw.
    ///The art's leading edge points LEFT, which is why the rotation carries an extra half turn.
    ///</summary>
    class WeightedShadowBlast : ModProjectile
    {
        const int FrameCount = 4;
        const int FrameWidth = 48;
        const int FrameHeight = 28;
        const int TicksPerFrame = 5;
        const int ShadowWeightTicks = 240;   // 4 seconds

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            //Sized to the dense head rather than the whole frame; the rest of the sprite is trailing tail.
            Projectile.width = 30;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 360;
            Projectile.light = 0.4f;
        }

        public override void AI()
        {
            //Leading edge of the art points left, so the sprite sits a half turn past its heading.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame * FrameCount)
            {
                Projectile.frameCounter = 0;
            }
            Projectile.frame = Projectile.frameCounter / TicksPerFrame;

            Lighting.AddLight(Projectile.Center, 0.45f, 0.1f, 0.65f);

            //Sparse trail: a ring is twenty of these, so anything denser buries the arena.
            if (Main.dedServ || Projectile.timeLeft % 3 != 0)
            {
                return;
            }

            int dustType = DustID.Shadowflame;
            if (Main.rand.NextBool(3))
            {
                dustType = DustID.DemonTorch;
            }

            Dust mote = Dust.NewDustPerfect(Projectile.Center, dustType, -Projectile.velocity * 0.1f, 90, default, Main.rand.NextFloat(1f, 1.5f));
            mote.noGravity = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            //On top of the damage, not instead of it.
            target.AddBuff(ModContent.BuffType<WeightOfShadow>(), ShadowWeightTicks);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 burst = Main.rand.NextVector2Circular(3f, 3f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, burst, 80, default, Main.rand.NextFloat(1.2f, 1.8f));
                mote.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Rectangle source = new Rectangle(Projectile.frame * FrameWidth, 0, FrameWidth, FrameHeight);
            Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

            //Drawn at full brightness rather than lightColor: these are thrown into unlit cave air, and the
            //sickles they replace were unreadable precisely because ambient shading swallowed them.
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Color.White,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
