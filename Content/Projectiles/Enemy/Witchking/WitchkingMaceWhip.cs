using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.Summon.Whips;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// The Witchking's phase-2 mace: a hostile whip on the shared <see cref="EnemyWhipProjectileBase"/> (the Dark Blood
    /// Knight's lash), drawn with the player whip's own sheet (intentional sprite sharing). 20 tiles of reach — the
    /// knight's 27 is too long for the cramped cave. Every head hit adds 15 Blight.
    /// ai[0] = owner whoAmI. ai[1] = 1 for Morgul Lash (latch and tow on hit). ai[2] = telegraph ticks (0 = 40: the
    /// opening lash; a mid-combo lash passes its 20-tick pause).
    ///
    /// Sheet (WitchkingMaceProjectile.png, 30x120, sprite faces DOWN): handle (0,0,30,36), chain link (0,48,30,18),
    /// mace head (0,70,30,50). 19 rope points over 320px ≈ 17.8px per link, so the 18px links barely stretch.
    /// </summary>
    public class WitchkingMaceWhip : EnemyWhipProjectileBase
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(WitchkingMaceProjectile));

        public const float ReachPixels = 20f * 16f;
        public const int OpeningTelegraphTicks = 40;
        public const int BlightPerHit = 15;

        private static readonly Rectangle HandleFrame = new Rectangle(0, 0, 30, 36);
        private static readonly Rectangle LinkFrame = new Rectangle(0, 48, 30, 18);
        private static readonly Rectangle HeadFrame = new Rectangle(0, 70, 30, 50);

        protected override int SegmentCount => 19;
        protected override float MaximumReach => ReachPixels;
        protected override int TelegraphDuration => Projectile.ai[2] > 0f ? (int)Projectile.ai[2] : OpeningTelegraphTicks;
        protected override bool LatchOnHit => Projectile.ai[1] >= 1f;
        protected override float RopeThickness => 20f;
        protected override int LashDustType => DustID.Shadowflame;
        protected override int TetherDustType => DustID.Wraith;

        public override void AI()
        {
            base.AI();

            // The head smoulders with Blight while the mace is out.
            if (!Main.dedServ && Main.rand.NextBool(3) && ControlPoints.Count > 0)
            {
                Dust blight = Dust.NewDustPerfect(WhipTip + Main.rand.NextVector2Circular(8f, 8f), DustID.Wraith,
                    new Vector2(0f, -0.6f), 120, default, 1.1f);
                blight.noGravity = true;
            }
        }

        protected override void OnWhipHitPlayer(Player target)
        {
            BlightBuildup.Apply(target, BlightPerHit);

            if (!Main.dedServ)
            {
                for (int i = 0; i < 16; i++)
                {
                    Dust spark = Dust.NewDustDirect(target.position, target.width, target.height, DustID.Shadowflame, 0f, 0f, 60, default, 1.3f);
                    spark.velocity = Main.rand.NextVector2Circular(3f, 3f);
                    spark.noGravity = true;
                }
            }
        }

        protected override Rectangle GetSegmentSource(Texture2D texture, int segment)
        {
            if (segment == 0)
            {
                return HandleFrame;
            }
            if (segment >= SegmentCount - 2)
            {
                return HeadFrame;
            }
            return LinkFrame;
        }

        // Links stretch to their gap like the base; the mace head never stretches, hangs from its top edge, and swells
        // to 1.4x at full extension (the player whip's own read of a heavy head landing).
        protected override void DrawSegment(Texture2D texture, int segment, Vector2 from, Vector2 to)
        {
            if (segment < SegmentCount - 2)
            {
                base.DrawSegment(texture, segment, from, to);
                return;
            }

            Vector2 delta = to - from;
            float rotation = delta.ToRotation() - MathHelper.PiOver2;
            float extension = MathHelper.Clamp(Vector2.Distance(Projectile.Center, to) / ReachPixels, 0f, 1f);
            float scale = MathHelper.Lerp(1f, 1.4f, extension);
            Vector2 origin = new Vector2(HeadFrame.Width / 2f, 6f);
            Color color = Lighting.GetColor(from.ToTileCoordinates());

            Main.EntitySpriteDraw(texture, from - Main.screenPosition, HeadFrame, color, rotation, origin, scale, SpriteEffects.None, 0);
        }
    }
}
