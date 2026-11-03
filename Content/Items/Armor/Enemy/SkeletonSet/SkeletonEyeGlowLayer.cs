using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Items.Armor.Enemy.SkeletonSet
{
    internal static class SkeletonEyeGlow
    {
        internal static bool IsVisible(PlayerDrawSet drawInfo)
            => SkeletonHeadpiece.HeadSlot >= 0
                && drawInfo.drawPlayer.head == SkeletonHeadpiece.HeadSlot
                && drawInfo.shadow == 0f;

        internal static void Draw(ref PlayerDrawSet drawInfo)
        {
            if (Main.dedServ || !IsVisible(drawInfo))
                return;

            int slot = drawInfo.drawPlayer.head;
            if (slot >= TextureAssets.ArmorHead.Length)
                return;

            Texture2D head = TextureAssets.ArmorHead[slot].Value;
            // Copy the real head entry, not a spectral history or shadow copy.
            // Source frame, origin, position, flip, rotation and scale stay exact.
            for (int i = drawInfo.DrawDataCache.Count - 1; i >= 0; i--)
            {
                DrawData eye = drawInfo.DrawDataCache[i];
                if (eye.texture != head || !eye.sourceRect.HasValue)
                    continue;

                eye.texture = ModContent.Request<Texture2D>(
                    "tsorcRevamp/Content/Items/Armor/Enemy/SkeletonSet/Skeleton_Head_Glow",
                    AssetRequestMode.ImmediateLoad).Value;
                float opacity = eye.color.A / 255f;
                eye.shader = drawInfo.cHead;
                // White input lets the actual head dye color the glow. Undyed
                // skeletons use red; full-bright RGB avoids world-light dimming.
                eye.color = (eye.shader == 0 ? new Color(255, 48, 32) : Color.White) * opacity;
                drawInfo.DrawDataCache.Insert(i + 1, eye);
                return;
            }
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class SkeletonEyeGlowLayer : PlayerDrawLayer
    {
        public override bool IsHeadLayer => true;
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => PuppetNPC.DrawingPuppetFor == null && SkeletonEyeGlow.IsVisible(drawInfo);
        protected override void Draw(ref PlayerDrawSet drawInfo) => SkeletonEyeGlow.Draw(ref drawInfo);
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class SkeletonPuppetEyeGlowLayer : PlayerDrawLayer
    {
        public override bool IsHeadLayer => true;
        public override Position GetDefaultPosition()
            => new AfterParent(ModContent.GetInstance<PuppetFinalCompositeTransformLayer>());
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => PuppetNPC.DrawingPuppetFor != null && SkeletonEyeGlow.IsVisible(drawInfo);
        protected override void Draw(ref PlayerDrawSet drawInfo) => SkeletonEyeGlow.Draw(ref drawInfo);
    }
}
