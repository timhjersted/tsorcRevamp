using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///One 3-tile segment of the Ice Gigas's Hoarfrost Creep (IceWave.png: 4 frames of three shards
    ///GROWING out of the surface — the animation is the eruption, no draw tricks needed). Segments
    ///spawn in a chain with staggered ai[0] delays so the carpet dominoes outward from the giant.
    ///FLOOR variant: stands ~6 seconds of zone denial, then shatters in place. CEILING variant
    ///(ai[1] = 1, flipped, shards down): hangs 4 seconds, then each segment individually WOBBLES for
    ///30 ticks (a few pixels side to side — the "it's coming loose" read) and FALLS, shattering on
    ///ground contact. The staggered spawn delays make the wobble-and-fall ripple down the line.
    ///</summary>
    class GigasIceWaveSegment : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/IceWave";

        const int GrowFrameTicks = 4;      //ticks per growth frame (4 frames = 16t eruption)
        const int FloorHoldTicks = 360;    //~6s of standing zone denial
        const int CeilingHoldTicks = 240;  //~4s hanging, then it comes loose
        const int WobbleTicks = 30;        //the coming-loose shake before the drop
        const float WobbleAmplitude = 2f;  //px of side-to-side
        const float WobblePeriod = 10f;    //ticks per full shake
        const int GrowTicks = GrowFrameTicks * 4;

        int TelegraphTicks => (int)Projectile.ai[0];
        bool Ceiling => Projectile.ai[1] == 1f;
        int Timer => (int)Projectile.localAI[0];
        bool Erupted => Timer > TelegraphTicks;
        int WobbleStart => TelegraphTicks + GrowTicks + CeilingHoldTicks;
        bool Wobbling => Ceiling && !Falling && Timer > WobbleStart;
        bool Falling { get => Projectile.localAI[1] == 1f; set => Projectile.localAI[1] = value ? 1f : 0f; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 44;
            Projectile.height = 34; //shards are shorter than the 44px frame — tight hitbox
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 400;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Ceiling
                ? TelegraphTicks + GrowTicks + CeilingHoldTicks + WobbleTicks + 300 //fall allowance; tiles end it
                : TelegraphTicks + GrowTicks + FloorHoldTicks;
        }

        public override bool? CanDamage()
        {
            return Erupted;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            //Ceiling segment falling: a dropping shard cluster, shatters on the ground (tileCollide → OnKill)
            if (Falling)
            {
                Projectile.velocity.Y += 0.4f;
                if (Projectile.velocity.Y > 12f)
                {
                    Projectile.velocity.Y = 12f;
                }
                if (Main.rand.NextBool(3))
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Frost, 0f, 0f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.2f;
                }
                Lighting.AddLight(Projectile.Center, 0.15f, 0.28f, 0.42f);
                return;
            }
            Projectile.velocity = Vector2.Zero;

            //Coming loose: ice chips sprinkle down while it shakes, then it drops
            if (Wobbling)
            {
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Projectile.height - 4f);
                    int chip = Dust.NewDust(pos, 4, 4, DustID.Ice, 0f, 1f, 80, default, 0.9f);
                    Main.dust[chip].velocity *= 0.4f;
                }
                if (Timer >= WobbleStart + WobbleTicks)
                {
                    Falling = true;
                    Projectile.tileCollide = true;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.45f, Pitch = 0.5f }, Projectile.Center);
                    Projectile.netUpdate = true;
                }
                return;
            }
            float surfaceY = Ceiling ? Projectile.position.Y : Projectile.position.Y + Projectile.height;

            if (!Erupted)
            {
                Projectile.frame = 0;
                //Frost simmering along the surface where this segment will erupt
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), surfaceY + (Ceiling ? 2f : -8f));
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, Ceiling ? 1f : -1f, 110, default, 1f);
                    Main.dust[dust].noGravity = true;
                }
                return;
            }

            //Growth: the four frames ARE the eruption
            int grown = Timer - TelegraphTicks;
            Projectile.frame = System.Math.Min(3, grown / GrowFrameTicks);
            if (grown == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = -0.2f + Main.rand.NextFloat(0.2f) }, Projectile.Center);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), surfaceY);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Ice, Main.rand.NextFloat(-2f, 2f), Ceiling ? Main.rand.NextFloat(1f, 3f) : Main.rand.NextFloat(-3f, -1f), 60, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                }
            }
            //Standing: cold glints on the shard tips
            if (Main.rand.NextBool(8))
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int glint = Dust.NewDust(pos, 4, 4, DustID.IceRod, 0f, 0f, 100, default, 0.8f);
                Main.dust[glint].noGravity = true;
                Main.dust[glint].velocity *= 0.15f;
            }
            Lighting.AddLight(Projectile.Center, 0.15f, 0.28f, 0.42f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Chilled, 2 * 60);
        }

        ///<summary>The IceWave frame, anchored to its surface (flipped for the ceiling variant), with
        ///the coming-loose side-to-side shake while wobbling.</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (!Erupted)
            {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            SpriteEffects flip = Ceiling ? SpriteEffects.FlipVertically : SpriteEffects.None;
            float wobbleX = Wobbling
                ? (float)System.Math.Sin((Timer - WobbleStart) * MathHelper.TwoPi / WobblePeriod) * WobbleAmplitude
                : 0f;
            Vector2 anchor = Ceiling
                ? new Vector2(Projectile.Center.X + wobbleX, Projectile.position.Y)                     //hangs from the ceiling line
                : new Vector2(Projectile.Center.X + wobbleX, Projectile.position.Y + Projectile.height); //stands on the floor line
            Vector2 origin = Ceiling ? new Vector2(texture.Width / 2f, 0f) : new Vector2(texture.Width / 2f, frameHeight);
            Main.EntitySpriteDraw(texture, anchor - Main.screenPosition, frame, lightColor, 0f, origin, 1f, flip, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = 0.3f }, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ice, vel.X, vel.Y, 60, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
