using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;

namespace tsorcRevamp.Projectiles.Summon.Whips
{
    public class SupremeDragoonLashProjectile : ModdedWhipProjectile
    {
        public override int WhipWidth => 10;
        public override int WhipHeight => 20;
        public override int WhipSegments => 26;
        public override float WhipRangeMult => 2.48f;
        public override int DustId => DustID.Sandnado;
        public override int DustWidth => 10;
        public override int DustHeight => 10;
        public override Color DustColor => default;
        public override float DustScale => 0.9f;
        public override float MaxChargeTime => 0;
        public override Vector2 WhipTipBase => new Vector2(10, 12);
        public override float MaxChargeDmgMultiplier => 1f;
        public override float ChargeRangeBonus => 0;
        public override int WhipDebuffId => ModContent.BuffType<SupremeDragoonLashDebuff>();
        public override int WhipDebuffDuration => DefaultWhipDebuffDuration;
        public override float WhipMultihitPenalty => 0.8f;
        public override Color WhipLineColor => Color.Indigo;
        public bool Hit = false;
        public override void CustomAIDustAndTipEffects(List<Vector2> points)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], 10, 10, DustID.WitherLightning, 0f, 0f, 150, default, 1f);
            if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[ModContent.ProjectileType<SupremeDragoonLashTrail>()] == 0)
            {
                Projectile Trail = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), player.Center, Projectile.WhipPointsForCollision[points.Count - 1], ModContent.ProjectileType<SupremeDragoonLashTrail>(), 0, Projectile.knockBack, Main.myPlayer, 0, player.itemAnimationMax, ChargeTime);
                player.ownedProjectileCounts[ModContent.ProjectileType<SupremeDragoonLashTrail>()]++; //without this it'd spawn two trails because of extraupdate spawning them in the same tick, before their owned number increases
                Trail.netUpdate = true;
            }
        }
        public override void CustomOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            var owner = Main.player[Projectile.owner];
            owner.AddBuff(ModContent.BuffType<DragoonLashBuff>(), (int)(WhipDebuffDuration * 60 * Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().SummonTagDuration));
            if (!Hit)
            {
                owner.GetModPlayer<tsorcRevampPlayer>().SupremeDragoonLashFireBreathTimer += 0.7f;
                Hit = true;
            }
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
                Rectangle frame = new Rectangle(0, 0, 10, 20);
                Vector2 origin = new Vector2(4, 12);
                float scale = 1;

                // These statements determine what part of the spritesheet to draw for the current segment.
                // They can also be changed to suit your sprite.
                if (i == list.Count - 2)
                {
                    frame.Y = 48;
                    frame.Height = 12;

                    // For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i > 10)
                {
                    frame.Y = 34;
                    frame.Height = 10;
                }
                else if (i > 0)
                {
                    frame.Y = 20;
                    frame.Height = 10;
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
