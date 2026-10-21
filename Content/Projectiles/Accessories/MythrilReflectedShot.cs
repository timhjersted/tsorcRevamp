using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Accessories
{
    /// <summary>
    /// Mythril Bulwark's perfect-parry deflect. A blocked hostile shot is removed and replaced by this friendly copy,
    /// owned by the blocking player, instead of flipping the original's hostile/friendly flags in place: those flags
    /// are not networked, so a flipped shot stayed hostile on every other machine. This copy also flies straight with
    /// its own AI, so a homing or splitting enemy projectile can't turn back on players.
    /// ai[0] = the original projectile type (drawn as this shot's sprite); ai[1] = the original's rotation offset
    /// from its velocity, so sprites that lead with an edge other than "right" still point the way they travel.
    /// </summary>
    public class MythrilReflectedShot : ModProjectile
    {
        public const int LifetimeTicks = 120;
        private const int FadeTicks = 15;

        // Never drawn (PreDraw draws the ORIGINAL projectile's sprite from ai[0]); any loaded texture satisfies autoload.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

        private int OriginalType => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Default;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = LifetimeTicks;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + Projectile.ai[1];

            // Fade over the last FadeTicks so a shot that outlives its flight doesn't vanish at full opacity.
            if (Projectile.timeLeft < FadeTicks)
            {
                Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / (float)FadeTicks));
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust trail = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Mythril, 0f, 0f, 100, default, 1.1f);
                trail.noGravity = true;
                trail.velocity *= 0.3f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int originalType = OriginalType;
            if (originalType <= 0 || originalType >= TextureAssets.Projectile.Length)
            {
                return false;
            }

            Main.instance.LoadProjectile(originalType);
            Texture2D texture = TextureAssets.Projectile[originalType].Value;
            int frameCount = Main.projFrames[originalType];
            if (frameCount < 1)
            {
                frameCount = 1;
            }

            int frameHeight = texture.Height / frameCount;
            Rectangle frame = new Rectangle(0, 0, texture.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Mythril-tinted, so a reflected shot reads as the player's, not the enemy's.
            Color tint = Color.Lerp(Color.White, new Color(110, 230, 200), 0.45f) * Projectile.Opacity;
            Main.EntitySpriteDraw(texture, drawPosition, frame, tint, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 15; i++)
            {
                Dust burst = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Mythril, 0f, 0f, 100, default, 1.2f);
                burst.noGravity = true;
                burst.velocity *= 2f;
            }
        }
    }
}
