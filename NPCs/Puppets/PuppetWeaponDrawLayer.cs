using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Puppets
{
    [Autoload(Side = ModSide.Client)]
    public sealed class PuppetSpectralDrawPlayer : ModPlayer
    {
        public override void TransformDrawData(ref PlayerDrawSet drawInfo)
        {
            PuppetNPC.DrawingPuppetFor?.TransformSpectralDrawData(ref drawInfo);
        }
    }

    /// <summary>
    /// Draws the puppet's held weapon at the correct layer depth:
    ///   body / legs → [weapon here] → front arm (grips the weapon) → head
    ///
    /// This layer is injected just after the vanilla HeldItem layer (which is skipped
    /// for combat puppets because noUseGraphic = true). The front arm is drawn AFTER
    /// this layer by the vanilla pipeline, so it lands on top of the weapon handle —
    /// giving the appearance that the arm is physically gripping the sword.
    ///
    /// The layer is invisible for all normal players; it only activates during the
    /// brief window when an PuppetNPC calls Main.PlayerRenderer.DrawPlayer on its puppet
    /// (PuppetNPC.DrawingPuppetFor is set to non-null for exactly that window).
    /// </summary>
    /// <summary>
    /// Sprite-registration aid, off by default — toggle with <c>/swingarm pivot</c>.
    ///
    /// Draws a crosshair on the exact point vanilla rotates the composite front arm around, plus a
    /// second marker on the front hand. The pivot is fixed by vanilla (bodyVect, shifted by
    /// GetCompositeOffset_FrontArm) and cannot be moved per-armor, so the arm art in a body sheet's
    /// composite column has to be drawn with its shoulder joint sitting on it. Eyeballing that offset
    /// from a swing is guesswork; this puts the target on screen so it can be measured directly.
    ///
    /// It runs as a layer rather than a PostDraw so it can reuse drawInfo's own values and reproduce
    /// vanilla's position math exactly, in the same coordinate space and under the same puppet scale.
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public class PuppetArmPivotDebugLayer : PlayerDrawLayer
    {
        /// <summary>Runtime toggle owned by <c>/swingarm pivot</c>. Never on in a normal session.</summary>
        internal static bool ShowArmPivot;

        private const int MarkerArmLength = 9;

        public override Position GetDefaultPosition()
            => new AfterParent(PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => ShowArmPivot && PuppetNPC.DrawingPuppetFor != null;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player puppet = drawInfo.drawPlayer;

            // Reproduced from DrawPlayer_28_ArmOverItem. Any drift here makes the marker lie, so it
            // deliberately mirrors the decompiled expression term for term instead of simplifying.
            Vector2 armPosition = new Vector2(
                (int)(drawInfo.Position.X - Main.screenPosition.X
                    - (puppet.bodyFrame.Width / 2) + (puppet.width / 2)),
                (int)(drawInfo.Position.Y - Main.screenPosition.Y
                    + puppet.height - puppet.bodyFrame.Height + 4f))
                + puppet.bodyPosition
                + new Vector2(puppet.bodyFrame.Width / 2, puppet.bodyFrame.Height / 2);

            Vector2 headgearOffset = Main.OffsetsPlayerHeadgear[puppet.bodyFrame.Y / puppet.bodyFrame.Height];
            headgearOffset.Y -= 2f;

            bool flippedVertically = drawInfo.playerEffect.HasFlag(SpriteEffects.FlipVertically);
            armPosition += headgearOffset * (flippedVertically ? 1f : -1f);

            // GetCompositeOffset_FrontArm: 5px toward the puppet's back, mirrored with the sprite.
            int horizontalSign = drawInfo.playerEffect.HasFlag(SpriteEffects.FlipHorizontally) ? -1 : 1;
            Vector2 pivot = armPosition + new Vector2(-5 * horizontalSign, 0f);

            DrawCrosshair(ref drawInfo, pivot, Color.Lime);

            // The far end of the same arm. If the pivot lines up but this does not, the arm art is
            // the right length in the wrong place; if both are off, it is registered wrong.
            Vector2 hand = puppet.GetFrontHandPosition(
                puppet.compositeFrontArm.stretch, puppet.compositeFrontArm.rotation);
            DrawCrosshair(ref drawInfo, hand - Main.screenPosition, Color.Magenta);
        }

        private static void DrawCrosshair(ref PlayerDrawSet drawInfo, Vector2 center, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle source = new Rectangle(0, 0, 1, 1);

            drawInfo.DrawDataCache.Add(new DrawData(pixel, center, source, color, 0f,
                new Vector2(0.5f, 0.5f), new Vector2(MarkerArmLength, 1f), SpriteEffects.None));
            drawInfo.DrawDataCache.Add(new DrawData(pixel, center, source, color, 0f,
                new Vector2(0.5f, 0.5f), new Vector2(1f, MarkerArmLength), SpriteEffects.None));
        }
    }

    /// <summary>
    /// Fixes composite shoulder drawing for puppets: pins the front arm behind the shoulder cap for
    /// every frame of a swing, and optionally turns the two static caps off entirely for puppets
    /// whose sheet draws the shoulder into the torso cell
    /// (see <see cref="PuppetNPC.SuppressCompositeShoulderCaps"/>).
    ///
    /// Runs before the Skin layer because that is where the BACK cap is emitted; the FRONT cap comes
    /// later in ArmOverItem. PlayerDrawSet is threaded through the layer chain by ref and both flags
    /// are only read at draw time, so setting them here covers both caps.
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public class PuppetShoulderCapLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
            => new BeforeParent(PlayerDrawLayers.Skin);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => PuppetNPC.DrawingPuppetFor != null;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            PuppetNPC puppet = PuppetNPC.DrawingPuppetFor;

            if (puppet == null)
            {
                return;
            }

            // Vanilla picks this from the BODY FRAME ROW, not the armour (PlayerDrawSet's composite
            // setup): rows 1, 2 and 5 draw the front arm OVER the shoulder cap, every other row
            // draws it behind. A swinging puppet changes body row with the weapon angle, so the arm
            // pops in front of the pauldron partway through the arc and back behind it again.
            // Forcing it true keeps the shoulder joint covered for the whole swing.
            drawInfo.compShoulderOverFrontArm = true;

            if (!puppet.SuppressCompositeShoulderCaps)
            {
                return;
            }

            drawInfo.hideCompositeShoulders = true;
        }
    }

    [Autoload(Side = ModSide.Client)]
    public class PuppetWeaponDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
            => new AfterParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => PuppetNPC.DrawingPuppetFor != null;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            PuppetNPC puppet = PuppetNPC.DrawingPuppetFor;
            if (puppet == null)
                return;

            // Shield first (off-hand, behind everything), then the slash swoosh (behind the
            // weapon sprite), then the weapon itself.
            puppet.DrawShieldToLayer(ref drawInfo);
            puppet.DrawSlashToLayer(ref drawInfo);
            puppet.DrawWeaponToLayer(ref drawInfo);
        }
    }
}
