using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Items.Armor.Enemy.BasiliskSet
{
    internal static class BasiliskHeadAnimation
    {
        internal const int FrameWidth = 64;
        internal const int SidePadding = 12;
        private const string AtlasPath = "tsorcRevamp/Content/Items/Armor/Enemy/BasiliskSet/BasiliskHeadpiece_Head_Animated";

        internal static bool IsEquipped(PlayerDrawSet drawInfo)
            => BasiliskHeadpiece.HeadSlot >= 0 && drawInfo.drawPlayer.head == BasiliskHeadpiece.HeadSlot;

        internal static void Apply(ref PlayerDrawSet drawInfo)
        {
            if (Main.dedServ || !IsEquipped(drawInfo))
                return;

            int slot = drawInfo.drawPlayer.head;
            if (slot >= TextureAssets.ArmorHead.Length)
                return;

            Texture2D original = TextureAssets.ArmorHead[slot].Value;
            Texture2D atlas = null;
            int mouth = (int)((Main.GameUpdateCount / BasiliskHeadpiece.MouthFrameTicks) % BasiliskHeadpiece.MouthFrames);

            for (int i = 0; i < drawInfo.DrawDataCache.Count; i++)
            {
                DrawData data = drawInfo.DrawDataCache[i];
                if (data.texture != original || !data.sourceRect.HasValue)
                    continue;

                Rectangle source = data.sourceRect.Value;
                if (source.Width != 40 || source.Y < 0 || source.Y + source.Height > 1120)
                    continue;

                atlas ??= ModContent.Request<Texture2D>(AtlasPath, AssetRequestMode.ImmediateLoad).Value;
                source.X = mouth * FrameWidth;
                source.Width = FrameWidth;
                data.texture = atlas;
                data.sourceRect = source;
                // Match the original pivot in texture space for either facing,
                // retaining its position, head bob, rotation, dye and opacity.
                data.origin += new Vector2(SidePadding, 0f);
                drawInfo.DrawDataCache[i] = data;
            }
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class BasiliskHeadAnimationLayer : PlayerDrawLayer
    {
        public override bool IsHeadLayer => true;
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => PuppetNPC.DrawingPuppetFor == null && BasiliskHeadAnimation.IsEquipped(drawInfo);
        protected override void Draw(ref PlayerDrawSet drawInfo) => BasiliskHeadAnimation.Apply(ref drawInfo);
    }

    // Puppet transforms identify native armor textures. Swap afterward so their
    // scale, spectral echoes and composite placement are already accounted for.
    [Autoload(Side = ModSide.Client)]
    public sealed class BasiliskPuppetHeadAnimationLayer : PlayerDrawLayer
    {
        public override bool IsHeadLayer => true;
        public override Position GetDefaultPosition()
            => new AfterParent(ModContent.GetInstance<PuppetFinalCompositeTransformLayer>());
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => PuppetNPC.DrawingPuppetFor != null && BasiliskHeadAnimation.IsEquipped(drawInfo);
        protected override void Draw(ref PlayerDrawSet drawInfo) => BasiliskHeadAnimation.Apply(ref drawInfo);
    }
}
