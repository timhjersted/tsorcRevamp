using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.Summon.Whips;
using WitchkingBoss = tsorcRevamp.NPCs.Bosses.SuperHardMode.Witchking;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// The Witchking's mace as a ball-and-chain: the shared authored flail patterns (EnemyFlailProjectileBase) on his
    /// own sheet. Used by Morgul Reaper (Ankle Reaper), Crown of Iron (Reverse Halo), Black Captain's Cross (Chain Cross),
    /// Backlash (Backlash Reversal) and Grond's March (Advancing Chainstorm). The lashes (Mace Lash, Morgul Lash) stay
    /// on WitchkingMaceWhip; he never has both out.
    ///   Reach: every pattern cap x1.25 (level lash 260px, Backlash 240px, Cross 250px; the owner passes Chainstorm's
    ///   300px in ai[2]). The 40t tell orbits keep the shared 60-96px so the tell stays close to his body.
    ///   Height: Morgul Reaper's lash runs 10px below the hand (head centre ~11px off the floor: jumped), Crown of
    ///   Iron's 24px above it (head height: rolled, a jump rises into it).
    ///   Hits: 15 Blight (Grond's March: 10, it can cross a player four times).
    /// ai[0] = owner whoAmI, ai[1] = pattern, ai[2] = reach (negative once released) — the base's wire format.
    /// Sheet (WitchkingMaceProjectile.png, 30x120, faces DOWN): handle, chain link and head frames from
    /// WitchkingMaceWhip; the head ball's centre sits 29px below its frame top and is drawn on Projectile.Center.
    /// </summary>
    public class WitchkingMaceFlail : EnemyFlailProjectileBase
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(WitchkingMaceProjectile));

        // The base only reads this for its default draw, which DrawFlailOverride replaces.
        protected override string ChainTexturePath => Texture;

        public const float ReachScale = 1.25f;
        public const float ReaperHeightOffset = 10f;
        public const float CrownHeightOffset = -24f;
        public const int BlightPerHit = 15;
        public const int MarchBlightPerHit = 10;

        private const float HeadCentreFromFrameTop = 29f;
        // Client decor: the first ticks of a new head are drawn sliding from where the resting mace lay on the floor.
        private const int BirthBlendTicks = 8;

        private Vector2 _birthPoint;
        private bool _birthPointCaptured;

        protected override float PatternReachScale => ReachScale;

        protected override float LevelLashHeightOffset(bool ankleReaper)
        {
            if (ankleReaper)
            {
                return ReaperHeightOffset;
            }
            return CrownHeightOffset;
        }

        private bool Marching => AuthoredPattern == EnemyFlailAttackPattern.AdvancingCyclone;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = EnemyFlailAttackPatterns.MaximumLifetime;
        }

        public override void SetStaticDefaults()
        {
            // The chain reaches 300px from the hand: keep drawing it while the head is off screen.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 700;
        }

        // Runs on the hit player's machine.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int blight = BlightPerHit;
            if (Marching)
            {
                blight = MarchBlightPerHit;
            }
            BlightBuildup.Apply(target, blight);

            if (!Main.dedServ)
            {
                for (int i = 0; i < 16; i++)
                {
                    Dust spark = Dust.NewDustDirect(target.position, target.width, target.height, DustID.Shadowflame, 0f, 0f, 40, default, 1.3f);
                    spark.velocity = Main.rand.NextVector2Circular(3f, 3f);
                    spark.noGravity = true;
                }
            }
        }

        // Every tick, every machine (decor): Blight smoulders off the head; while it is live, a dense wake follows it.
        // Live wake: 3 body motes/t spread back along this tick's travel (a 20 px/t lash → ~7px gap), 1 bright violet
        // glint, and floor sparks when the head is skimming the ground (Morgul Reaper).
        protected override void OnFlailTick(NPC owner, Vector2 hand)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (Main.rand.NextBool(2))
            {
                Dust smoulder = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Wraith,
                    new Vector2(0f, -0.6f), 90, default, Main.rand.NextFloat(1.0f, 1.25f));
                smoulder.noGravity = true;
            }

            bool live = CanDamage() != false;
            if (!live)
            {
                return;
            }

            Vector2 travel = Projectile.position - Projectile.oldPosition;
            for (int i = 0; i < 3; i++)
            {
                Vector2 along = travel * Main.rand.NextFloat(0f, 1f);
                int dustType = DustID.Wraith;
                if (i == 0)
                {
                    dustType = DustID.Shadowflame;
                }
                Dust wake = Dust.NewDustPerfect(Projectile.Center - along + Main.rand.NextVector2Circular(8f, 8f), dustType,
                    -travel * 0.08f, 40, default, Main.rand.NextFloat(1.0f, 1.4f));
                wake.noGravity = true;
            }
            Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, travel * 0.05f, 0, new Color(235, 210, 255), Main.rand.NextFloat(0.55f, 0.85f));
            glint.noGravity = true;

            Point below = (Projectile.Center + new Vector2(0f, 16f)).ToTileCoordinates();
            if (WorldGen.SolidTile(below.X, below.Y))
            {
                Vector2 contact = new Vector2(Projectile.Center.X, below.Y * 16f);
                Vector2 sparkVelocity = new Vector2(-travel.X * 0.15f + Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(2f, 4.5f));
                Dust spark = Dust.NewDustPerfect(contact, DustID.Torch, sparkVelocity, 0, new Color(255, 220, 170), Main.rand.NextFloat(0.55f, 0.85f));
                spark.noGravity = false;
            }
        }

        // Handle at the hand, links to the head frame's top, the head frame with its ball centred on the hitbox. For
        // the first ticks the drawn head slides out of the resting mace's floor position (that position is this
        // client's own decor, so the blend is decor too; the hitbox never moves with it).
        protected override bool DrawFlailOverride(NPC owner, Vector2 hand, Color lightColor)
        {
            Vector2 head = Projectile.Center;
            int age = (int)Projectile.localAI[0];
            if (!_birthPointCaptured)
            {
                _birthPointCaptured = true;
                _birthPoint = head;
                if (owner.ModNPC is WitchkingBoss witchking && witchking.RestingMaceHeadForDraw is Vector2 resting)
                {
                    _birthPoint = resting;
                }
            }
            if (age < BirthBlendTicks)
            {
                float blend = MathHelper.SmoothStep(0f, 1f, age / (float)BirthBlendTicks);
                head = Vector2.Lerp(_birthPoint, head, blend);
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 line = head - hand;
            float length = line.Length();
            Vector2 direction = line.SafeNormalize(Vector2.UnitY);
            float rotation = direction.ToRotation() - MathHelper.PiOver2;

            Rectangle handleFrame = WitchkingMaceWhip.HandleFrame;
            Rectangle linkFrame = WitchkingMaceWhip.LinkFrame;
            Rectangle headFrame = WitchkingMaceWhip.HeadFrame;

            Color handleColor = Lighting.GetColor(hand.ToTileCoordinates());
            Main.EntitySpriteDraw(texture, hand - Main.screenPosition, handleFrame, handleColor, rotation,
                new Vector2(handleFrame.Width / 2f, 16f), 1f, SpriteEffects.None, 0);

            float headFrameTop = length - HeadCentreFromFrameTop;
            for (float along = 18f; along + linkFrame.Height <= headFrameTop; along += linkFrame.Height)
            {
                Vector2 linkPosition = hand + direction * along;
                Color linkColor = Lighting.GetColor(linkPosition.ToTileCoordinates());
                Main.EntitySpriteDraw(texture, linkPosition - Main.screenPosition, linkFrame, linkColor, rotation,
                    new Vector2(linkFrame.Width / 2f, 0f), 1f, SpriteEffects.None, 0);
            }

            Color headColor = Lighting.GetColor(head.ToTileCoordinates());
            Main.EntitySpriteDraw(texture, head - Main.screenPosition, headFrame, headColor, rotation,
                new Vector2(headFrame.Width / 2f, HeadCentreFromFrameTop), 1f, SpriteEffects.None, 0);
            return true;
        }
    }
}
