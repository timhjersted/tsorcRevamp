using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Summon.Whips
{
    public class WitchkingMaceProjectile : ModdedWhipProjectile
    {
        public override int WhipWidth => 30;
        public override int WhipHeight => 36;
        public override int WhipSegments => 25;
        public override float WhipRangeMult => 1.2f;
        public override int DustId => DustID.CorruptGibs;
        public override int DustWidth => 10;
        public override int DustHeight => 10;
        public override Color DustColor => default;
        public override float DustScale => 1f;
        public override float MaxChargeTime => 0;
        public override Vector2 WhipTipBase => new Vector2(10, 12);
        public override float MaxChargeDmgMultiplier => 1f;
        public override float ChargeRangeBonus => 0;
        public override int WhipDebuffId => ModContent.BuffType<WitchkingMaceDebuff>();
        public override int WhipDebuffDuration => DefaultWhipDebuffDuration;
        public override float WhipMultihitPenalty => 0.85f;
        public override Color WhipLineColor => Color.CadetBlue;
        public bool Hit = false;
        public static int TagDuration = 4;
        public override void CustomAIDustAndTipEffects(List<Vector2> points)
        {
            Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], 10, 10, DustID.PurpleTorch, 0f, 0f, 150, default, 1f);
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
                Rectangle frame = new Rectangle(0, 0, 30, 36);
                Vector2 origin = new Vector2(15, 16);
                float scale = 1;

                // These statements determine what part of the spritesheet to draw for the current segment.
                // They can also be changed to suit your sprite.
                if (i == list.Count - 2)
                {
                    frame.Y = 70;
                    frame.Height = 50;

                    // For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i > 10)
                {
                    frame.Y = 48;
                    frame.Height = 18;
                }
                else if (i > 0)
                {
                    frame.Y = 48;
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner]; 

            int whipBuffSelection = Main.rand.Next(20); 
            switch (whipBuffSelection)
            {
                case 0:
                    target.AddBuff(ModContent.BuffType<LeatherWhipDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 1:
                    target.AddBuff(ModContent.BuffType<SnapthornDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 2:
                    target.AddBuff(ModContent.BuffType<SpinalTapDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 3:
                    target.AddBuff(ModContent.BuffType<FirecrackerDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 4:
                    target.AddBuff(ModContent.BuffType<CoolWhipDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    player.AddBuff(BuffID.CoolWhipPlayerBuff, (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    if (player.ownedProjectileCounts[ProjectileID.CoolWhipProj] == 0 && Main.myPlayer == player.whoAmI)
                    {
                        Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ProjectileID.CoolWhipProj, 15, 0, player.whoAmI, 1);
                    }
                    break;
                case 5:
                    target.AddBuff(ModContent.BuffType<DurendalDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 6:
                    target.AddBuff(ModContent.BuffType<DarkHarvestDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 7:
                    target.AddBuff(ModContent.BuffType<MorningStarDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 8:
                    target.AddBuff(ModContent.BuffType<KaleidoscopeDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 9:
                    target.AddBuff(ModContent.BuffType<EnchantedWhipDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 10:
                    target.AddBuff(ModContent.BuffType<DominatrixDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 11:
                    target.AddBuff(ModContent.BuffType<SearingLashDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 12:
                    var globalNPC = target.GetGlobalNPC<tsorcRevampGlobalNPC>();
                    globalNPC.CrystalNunchakuWielder = player;
                    target.AddBuff(ModContent.BuffType<CrystalNunchakuDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * CrystalNunchaku.BuffDuration * 60));
                    break;
                case 13:
                    target.AddBuff(ModContent.BuffType<PyrosulfateDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 14:
                    player.AddBuff(ModContent.BuffType<PolarisLeashBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * 5 * 60));
                    break;
                case 15:
                    target.AddBuff(ModContent.BuffType<DragoonLashDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    player.AddBuff(ModContent.BuffType<DragoonLashBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    player.GetModPlayer<tsorcRevampPlayer>().DragoonLashFireBreathTimer += 0.7f;
                    break;
                case 16:
                    player.AddBuff(ModContent.BuffType<TerraFallBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 17:
                    target.AddBuff(ModContent.BuffType<DetonationSignalDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 18:
                    target.AddBuff(ModContent.BuffType<RustedChainDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
                case 19:
                    target.AddBuff(ModContent.BuffType<PyromethaneDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                    break;
            }

            base.OnHitNPC(target, hit, damageDone);
        }
    }
}
