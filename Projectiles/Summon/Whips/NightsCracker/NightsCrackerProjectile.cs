using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Projectiles.Summon.Whips.NightsCracker;

namespace tsorcRevamp.Projectiles.Summon.Whips.NightsCracker
{
    public class NightsCrackerProjectile : ModdedWhipProjectile
    {
        public override int WhipWidth => 30;
        public override int WhipHeight => 28;
        public override int WhipSegments => 20;
        public override float WhipRangeMult => 1.42f;
        public override int DustId => 27;
        public override int DustWidth => 10;
        public override int DustHeight => 10;
        public override Color DustColor => default;
        public override float DustScale => 1f;
        public override float MaxChargeTime => MaximumChargeTime;
        public const float MaximumChargeTime = 160;
        public override Vector2 WhipTipBase => new Vector2(11, 17);
        public override float MaxChargeDmgMultiplier => MaxChargeDmgMult;
        public const float MaxChargeDmgMult = 2.25f;
        public override float ChargeRangeBonus => 0.06f;
        public override int WhipDebuffId => ModContent.BuffType<NightsCrackerDebuff>();
        public override int WhipDebuffDuration => 0; //set to 0 so it does nothing and I can make a custom calculation
        public override float WhipMultihitPenalty => 1f; //set to 1 so it does nothing and I can make a custom calculation
        public override Color WhipLineColor => Color.Indigo;
        public override void CustomDustAndTipEffects(List<Vector2> points)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], 10, 10, DustID.PurpleTorch, 0f, 0f, 150, default, 1f);
            if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[ModContent.ProjectileType<NightsCrackerTrail>()] == 0)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Projectile.WhipPointsForCollision[points.Count - 1], ModContent.ProjectileType<NightsCrackerTrail>(), Projectile.damage, Projectile.knockBack, player.whoAmI, Projectile.whoAmI, (ChargeTime >= MaxChargeTime) ? 1 : 0, ChargeTime);
                player.ownedProjectileCounts[ModContent.ProjectileType<NightsCrackerTrail>()]++; //without this it'd spawn two trails because of extraupdate spawning them in the same tick, before their owned number increases
            }
        }
        public override void CustomModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }
        public override void CustomOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
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
                Vector2 origin = new Vector2(14, 9);
                float scale = 0.75f;

                // These statements determine what part of the spritesheet to draw for the current segment.
                // They can also be changed to suit your sprite.
                if (i == list.Count - 2)
                {
                    frame.Y = 44;
                    frame.Height = 26;

                    // For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(scale / 2f, scale * 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i > 0)
                {
                    frame.Y = 28;
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
