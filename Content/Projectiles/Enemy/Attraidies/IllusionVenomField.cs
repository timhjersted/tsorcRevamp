using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Attraidies
{
    // The lingering poison patch left where an IllusionVenomOrb splashes.
    // Art: 40x200 sheet, 5 vertical 40x40 frames of a green cloud; drawn as three overlapping columns so the
    // 96px-wide hitbox reads as one patch. Sits on the floor and does not move.
    // Life: 10t fade-in (harmless), then active, then a 20t fade-out (harmless) at the end of LifetimeTicks.
    public class IllusionVenomField : ModProjectile
    {
        public const int LifetimeTicks = 260;
        public const int PoisonTicks = 360;
        private const int FadeInTicks = 10;
        private const int FadeOutTicks = 20;
        private const int FrameCount = 5;
        private const int Width = 96;
        private const int Height = 40;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = Width;
            Projectile.height = Height;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        /// <summary>Center for a field that rests on the first floor at or below the splash point (up to 6 tiles down), so a
        /// splash on a wall or the player's body still leaves the patch on the ground.</summary>
        public static Vector2 FindFloorCenter(Vector2 splashPoint)
        {
            int tileX = (int)(splashPoint.X / 16f);
            int startTileY = (int)(splashPoint.Y / 16f);

            for (int tileY = startTileY; tileY <= startTileY + 6; tileY++)
            {
                Tile tile = Framing.GetTileSafely(tileX, tileY);
                bool isFloor = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);

                if (isFloor)
                {
                    float floorY = tileY * 16f;
                    return new Vector2(splashPoint.X, floorY - Height * 0.5f);
                }
            }

            return splashPoint;
        }

        private int Age => LifetimeTicks - Projectile.timeLeft;

        // Harmless while it fades in or out, so its edges in time are honest.
        public override bool? CanDamage()
        {
            if (Age < FadeInTicks || Projectile.timeLeft <= FadeOutTicks)
            {
                return false;
            }

            return null;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, PoisonTicks);
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, IllusionVenomOrb.VenomColor.ToVector3() * 0.3f * Opacity);

            // Rising bubbles so the patch reads as alive and as harmful.
            if (Main.rand.NextBool(3))
            {
                Vector2 bubblePosition = new Vector2(Main.rand.NextFloat(Projectile.Left.X, Projectile.Right.X), Projectile.Bottom.Y - 4f);
                Dust bubble = Dust.NewDustPerfect(bubblePosition, DustID.TintableDustLighted,
                    new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.6f)), 90, IllusionVenomOrb.VenomColor, Main.rand.NextFloat(0.6f, 1f) * Opacity);
                bubble.noGravity = true;
            }
        }

        private float Opacity
        {
            get
            {
                float fadeIn = MathHelper.Clamp(Age / (float)FadeInTicks, 0f, 1f);
                float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeOutTicks, 0f, 1f);
                return fadeIn * fadeOut;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / FrameCount;
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight * 0.5f);
            Color drawColor = Color.White * 0.85f * Opacity;

            // Three columns 32px apart, each a frame out of step, so the patch doesn't pulse in unison.
            for (int column = -1; column <= 1; column++)
            {
                int frame = (Projectile.frame + column + FrameCount) % FrameCount;
                Rectangle source = new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);
                Vector2 position = Projectile.Center + new Vector2(column * 32f, 0f) - Main.screenPosition;
                Main.EntitySpriteDraw(texture, position, source, drawColor, 0f, origin, 1f, SpriteEffects.None, 0);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 20; i++)
            {
                Vector2 puffPosition = new Vector2(Main.rand.NextFloat(Projectile.Left.X, Projectile.Right.X), Projectile.Center.Y);
                Dust puff = Dust.NewDustPerfect(puffPosition, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(1.2f, 1.2f), 120, IllusionVenomOrb.VenomColor, 0.7f);
                puff.noGravity = true;
            }
        }
    }
}
