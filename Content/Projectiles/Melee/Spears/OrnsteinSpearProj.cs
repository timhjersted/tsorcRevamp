using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace tsorcRevamp.Content.Projectiles.Melee.Spears
{
    public class OrnsteinSpearProj : ModdedSpearProjectile
    {
        // The 104x104 frame points up-right. Draw from the tip to keep the hitbox on the visible point.
        internal static readonly Vector2 TipOrigin = new Vector2(103f, 1f);
        internal const float SpriteScale = 0.82f;

        public override float HoldoutRangeMin => 58f;
        public override float HoldoutRangeMax => 107f;
        public override float HitboxSize => 1f;
        public override float Scale => SpriteScale;
        public override int dustID => DustID.GoldFlame;

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool PreAI()
        {
            bool runVanillaAi = base.PreAI();
            float aimRotation = Projectile.velocity.ToRotation();
            Projectile.rotation = aimRotation + MathHelper.PiOver4;
            return runVanillaAi;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation,
                TipOrigin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
