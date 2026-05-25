using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Draws the invader's held weapon at the correct layer depth:
    ///   body / legs → [weapon here] → front arm (grips the weapon) → head
    ///
    /// This layer is injected just after the vanilla HeldItem layer (which is skipped
    /// for invader puppets because noUseGraphic = true).  The front arm is drawn AFTER
    /// this layer by the vanilla pipeline, so it lands on top of the weapon handle —
    /// giving the appearance that the arm is physically gripping the sword.
    ///
    /// The layer is invisible for all normal players; it only activates during the
    /// brief window when an InvaderNPC calls Main.PlayerRenderer.DrawPlayer on its puppet
    /// (InvaderNPC.DrawingPuppetFor is set to non-null for exactly that window).
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public class InvaderWeaponDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
            => new AfterParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => InvaderNPC.DrawingPuppetFor != null;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            InvaderNPC invader = InvaderNPC.DrawingPuppetFor;
            if (invader == null)
                return;

            invader.DrawWeaponToLayer(ref drawInfo);
        }
    }
}
