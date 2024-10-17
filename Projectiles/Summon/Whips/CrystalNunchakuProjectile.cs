using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Summon.Whips
{
    public class CrystalNunchakuProjectile : ModdedWhipProjectile
    {
        public override int WhipWidth => 10;
        public override int WhipHeight => 32;
        public override int WhipSegments => 20;
        public override float WhipRangeMult => 1.4f;
        public override int DustId => DustID.PurpleCrystalShard;
        public override int DustWidth => 10;
        public override int DustHeight => 10;
        public override Color DustColor => default;
        public override float DustScale => 1f;
        public override float MaxChargeTime => 0;
        public override Vector2 WhipTipBase => new Vector2(10, 32);
        public override float MaxChargeDmgMultiplier => 1f;
        public override float ChargeRangeBonus => 0;
        public override int WhipDebuffId => ModContent.BuffType<CrystalNunchakuDebuff>();
        public override int WhipDebuffDuration => 0; //custom tag so the duration can't be refreshed after a proc
        public override float WhipMultihitPenalty => 0.7f;
        public override Color WhipLineColor => Color.BlueViolet;
        public override void CustomOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!target.HasBuff(WhipDebuffId))
            {
                var globalNPC = target.GetGlobalNPC<tsorcRevampGlobalNPC>();
                Player player = Main.player[Projectile.owner];
                globalNPC.CrystalNunchakuWielder = player;
                target.AddBuff(WhipDebuffId, (int)(CrystalNunchaku.BuffDuration * 60 * player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration));
            }
        }
        public override void CustomAIDustAndTipEffects(List<Vector2> points)
        {
            Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], 10, 10, DustID.HallowedTorch, 0f, 0f, 150, default, 0.75f);
            Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], 10, 10, DustID.PinkCrystalShard, 0f, 0f, 150, default, 1f);
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
                Rectangle frame = new Rectangle(0, 0, 10, 32);
                Vector2 origin = new Vector2(5, 19);
                float scale = 1;

                // These statements determine what part of the spritesheet to draw for the current segment.
                // They can also be changed to suit your sprite.
                if (i == list.Count - 2)
                {
                    frame.Y = 80;
                    frame.Height = 32;
                    // For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i > 14)
                {
                    frame.Y = 64;
                    frame.Height = 16;
                }
                else if (i > 7)
                {
                    frame.Y = 48;
                    frame.Height = 16;
                }
                else if (i > 0)
                {
                    frame.Y = 32;
                    frame.Height = 16;
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
