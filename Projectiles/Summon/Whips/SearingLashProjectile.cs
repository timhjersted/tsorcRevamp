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
    public class SearingLashProjectile : ModdedWhipProjectile
    {
        public override int WhipWidth => 12;
        public override int WhipHeight => 38;
        public override int WhipSegments => 20;
        public override float WhipRangeMult => 1.32f;
        public override int DustId => DustID.Flare;
        public override int DustWidth => 10;
        public override int DustHeight => 10;
        public override Color DustColor => default;
        public override float DustScale => 1f;
        public override float MaxChargeTime => 210;
        public override Vector2 WhipTipBase => new Vector2(10, 12);
        public override float MaxChargeDmgDivisor => 4.3f;
        public override float ChargeRangeBonus => 0.1f;
        public override int WhipDebuffId => ModContent.BuffType<SearingLashDebuff>();
        public override int WhipDebuffDuration => 0; //set to 0 so it does nothing and I can make a custom calculation
        public override float WhipMultihitPenalty => 0.65f;
        public override Color WhipLineColor => Color.OrangeRed;
        public override void CustomDust(List<Vector2> points)
        {
            Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], 10, 10, 25, 0f, 0f, 150, default, 1f);
        }
        public override void CustomOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>();
            modPlayer.SearingLashStacks = ChargeTime / (MaxChargeTime / 4) + 1;
            target.AddBuff(ModContent.BuffType<SearingLashDebuff>(), (int)(modPlayer.SearingLashStacks * 120 * modPlayer.SummonTagDuration));
            target.AddBuff(BuffID.OnFire, (int)(modPlayer.SearingLashStacks * 120 * modPlayer.SummonTagDuration));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<Vector2> list = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, list);

            DrawLine(list);

            //Main.DrawWhip_WhipBland(Projectile, list);
            // The code below is for custom drawing.
            // If you don't want that, you can remove it all and instead call one of vanilla's DrawWhip methods, like above.
            // However, you must adhere to how they draw if you do.

            SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.instance.LoadProjectile(Type);
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Vector2 pos = list[0];

            for (int i = 0; i < list.Count - 1; i++)
            {
                // These two values are set to suit this projectile's sprite, but won't necessarily work for your own.
                // You can change them if they don't!
                Rectangle frame = new Rectangle(0, 0, 12, 40);
                Vector2 origin = new Vector2(4, 15);
                float scale = 1;

                // These statements determine what part of the spritesheet to draw for the current segment.
                // They can also be changed to suit your sprite.
                if (i == list.Count - 2)
                {
                    frame.Y = 60;
                    frame.Height = 12;

                    // For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i > 0)
                {
                    frame.Y = 40;
                    frame.Height = 20;
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
