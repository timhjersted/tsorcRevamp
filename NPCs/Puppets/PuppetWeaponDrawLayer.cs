using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Puppets
{
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
