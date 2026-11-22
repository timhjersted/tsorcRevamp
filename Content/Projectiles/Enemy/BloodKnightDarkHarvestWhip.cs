using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Enemy-owned Dark Harvest lash: a 27-tile, 30-segment whip that latches and tows on hit. The rope simulation,
    /// telegraph, lash curve, tether and sync live in <see cref="EnemyWhipProjectileBase"/> (extracted from this class
    /// unchanged); this keeps the Dark Blood Knight's numbers, palette and 5-frame sheet.
    /// ai[1] > 0.5 = blackfire phase (shadowflame lash dust).
    /// </summary>
    public class BloodKnightDarkHarvestWhip : EnemyWhipProjectileBase
    {
        public const float MaximumReachPixels = 27f * 16f;

        protected override int SegmentCount => 30;
        protected override float MaximumReach => MaximumReachPixels;
        protected override int LashDustType => Projectile.ai[1] > 0.5f ? DustID.Shadowflame : DustID.Blood;

        protected override void OnWhipHitPlayer(Player target)
        {
            DarkBloodKnight.ApplyBloodArrowDebuffs(target, blackfire: false);
        }

        // Five vertical frames: handle, three alternating links, tip (the last two segments).
        protected override Rectangle GetSegmentSource(Texture2D texture, int segment)
        {
            int frame = 1 + segment % 3;
            if (segment == 0)
            {
                frame = 0;
            }
            else if (segment >= SegmentCount - 2)
            {
                frame = 4;
            }
            return texture.Frame(1, 5, 0, frame);
        }
    }
}
