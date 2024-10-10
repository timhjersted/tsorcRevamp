using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;

namespace tsorcRevamp.Projectiles.Summon.Whips
{
    public class PyromethaneProjectile : ModdedWhipProjectile
    {
        public override int WhipWidth => 26;
        public override int WhipHeight => 36;
        public override int WhipSegments => 20;
        public override float WhipRangeMult => 1.44f;
        public override int DustId => DustID.SilverFlame;
        public override int DustWidth => 10;
        public override int DustHeight => 10;
        public override Color DustColor => Color.Yellow;
        public override float DustScale => 1f;
        public override float MaxChargeTime => 0;
        public override Vector2 WhipTipBase => new Vector2(10, 12);
        public override float MaxChargeDmgDivisor => 1f;
        public override float ChargeRangeBonus => 0;
        public override int WhipDebuffId => ModContent.BuffType<PyromethaneDebuff>();
        public override int WhipDebuffDuration => DefaultWhipDebuffDuration;
        public override float WhipMultihitPenalty => 0.65f;
        public override Color WhipLineColor => Color.White;
        public override void CustomDustAndTipEffects(List<Vector2> points)
        {
            Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], DustWidth, DustHeight, DustID.IchorTorch, 0f, 0f, 150, default, DustScale);
        }
        public override void CustomOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Ichor, WhipDebuffDuration * 60);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<Vector2> list = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, list);

            DrawLine(list);

            SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.instance.LoadProjectile(Type);
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Vector2 pos = list[0];

            for (int i = 0; i < list.Count - 1; i++)
            {
                // These two values are set to suit this projectile's sprite, but won't necessarily work for your own.
                // You can change them if they don't!
                Rectangle frame = new Rectangle(0, 0, WhipWidth, WhipHeight);
                Vector2 origin = new Vector2(12, 10);
                float scale = 1;

                // These statements determine what part of the spritesheet to draw for the current segment.
                // They can also be changed to suit your sprite.
                if (i == list.Count - 2)
                {
                    frame.Y = 58;
                    frame.Height = 12;
                    // For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i > 0)
                {
                    frame.Y = 36;
                    frame.Height = 22;
                }

                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;

                float rotation = diff.ToRotation() - MathHelper.PiOver2; // This projectile's sprite faces down, so PiOver2 is used to correct rotation.
                Color color = Lighting.GetColor(element.ToTileCoordinates());

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, flip, 0);

                pos += diff;
            }
            return false;
        }
    }
}
