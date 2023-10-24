using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items.Weapons.Summon.Whips;

namespace tsorcRevamp.Projectiles.Summon.Whips
{
    public class TerraFallProjectile : ModdedWhipProjectile
    {
        public override int WhipWidth => 11;
        public override int WhipHeight => 14;
        public override int WhipSegments => 20;
        public override float WhipRangeMult => 2f;
        public override int DustId => DustID.TerraBlade;
        public override int DustWidth => 10;
        public override int DustHeight => 10;
        public override Color DustColor => default;
        public override float DustScale => 1f;
        public override float MaxChargeTime => 210;
        public override Vector2 WhipTipBase => new Vector2(11, 14);
        public override float MaxChargeDmgDivisor => 4f;
        public override int WhipDebuffId => ModContent.BuffType<TerraFallDebuff>();
        public override int WhipDebuffDuration => 0; //set to 0 so it does nothing and I can make a custom calculation
        public override float WhipMultihitPenalty => 1f; //set to 1 so it does nothing and I can make a custom calculation
        public override Color WhipLineColor => Color.Red;
        public override void CustomModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.CritDamage *= 1f + (TerraFall.CritDamage / 100f);
        }
        public override void CustomOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.TerraFallStacks = ChargeTime / (MaxChargeTime / 4) + 1;
            player.AddBuff(ModContent.BuffType<TerraFallBuff>(), (int)(modPlayer.TerraFallStacks * 120 * modPlayer.SummonTagDuration));
            target.AddBuff(ModContent.BuffType<TerraFallDebuff>(), (int)(modPlayer.TerraFallStacks * 120 * modPlayer.SummonTagDuration));
            Projectile.damage = (int)(Projectile.damage * (modPlayer.TerraFallStacks / 20.5f + 0.75f));
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
                Rectangle frame = new Rectangle(0, 0, 11, 14);
                Vector2 origin = new Vector2(5, 5);
                float scale = 2;

                // These statements determine what part of the spritesheet to draw for the current segment.
                // They can also be changed to suit your sprite.
                if (i == list.Count - 2)
                {
                    frame.Y = 39;
                    frame.Height = 14;

                    // For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i > 20)
                {
                    frame.Y = 30;
                    frame.Height = 9;
                }
                else if (i > 10)
                {
                    frame.Y = 21;
                    frame.Height = 9;
                }
                else if (i > 0)
                {
                    frame.Y = 14;
                    frame.Height = 7;
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
