using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;

namespace tsorcRevamp.Projectiles.Summon.Whips.EnchantedWhip
{
    public class EnchantedWhipProjectile : ModdedWhipProjectile
    {
        public override int WhipWidth => 18;
        public override int WhipHeight => 18;
        public override int WhipSegments => 16;
        public override float WhipRangeMult => 0.75f;
        public override int DustId => 15;
        public override int DustWidth => 5;
        public override int DustHeight => 5;
        public override Color DustColor => default;
        public override float DustScale => 1f;
        public override float MaxChargeTime => 0;
        public override Vector2 WhipTipBase => new Vector2(24, 34);
        public override float MaxChargeDmgDivisor => 1f;
        public override float ChargeRangeBonus => 0;
        public override int WhipDebuffId => ModContent.BuffType<EnchantedWhipDebuff>();
        public override int WhipDebuffDuration => DefaultWhipDebuffDuration;
        public override float WhipMultihitPenalty => 0.55f;
        public override Color WhipLineColor => Color.DeepSkyBlue;
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
                Rectangle frame = new Rectangle(0, 0, 34, 24);
                Vector2 origin = new Vector2(16, 7);
                float scale = 1;

                // These statements determine what part of the spritesheet to draw for the current segment.
                // They can also be changed to suit your sprite.
                if (i == list.Count - 2)
                {
                    frame.Y = 96;
                    frame.Height = 24;

                    // For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i == 16)
                {
                    frame.Y = 84;
                    frame.Height = 12;
                }
                else if (i == 15)
                {
                    frame.Y = 78;
                    frame.Height = 6;
                }
                else if (i == 14) //loops below
                {
                    frame.Y = 60;
                    frame.Height = 18;
                }
                else if (i == 13)
                {
                    frame.Y = 42;
                    frame.Height = 18;
                }
                else if (i == 12)
                {
                    frame.Y = 60;
                    frame.Height = 18;
                }
                else if (i == 11)
                {
                    frame.Y = 42;
                    frame.Height = 18;
                }
                else if (i == 10)
                {
                    frame.Y = 60;
                    frame.Height = 18;
                }
                else if (i == 9)
                {
                    frame.Y = 42;
                    frame.Height = 18;
                }
                else if (i == 8)
                {
                    frame.Y = 60;
                    frame.Height = 18;
                }
                else if (i == 7)
                {
                    frame.Y = 42;
                    frame.Height = 18;
                }
                else if (i == 6)
                {
                    frame.Y = 60;
                    frame.Height = 18;
                }
                else if (i == 5)
                {
                    frame.Y = 42;
                    frame.Height = 18;
                }
                else if (i == 4)
                {
                    frame.Y = 60;
                    frame.Height = 18;
                }
                else if (i == 3)
                {
                    frame.Y = 42;
                    frame.Height = 18;
                }
                else if (i == 2)
                {
                    frame.Y = 60;
                    frame.Height = 18;
                }
                else if (i == 1)
                {
                    frame.Y = 42;
                    frame.Height = 18;
                } //end of loop
                else if (i == 0)
                {
                    frame.Y = 24;
                    frame.Height = 18;
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
