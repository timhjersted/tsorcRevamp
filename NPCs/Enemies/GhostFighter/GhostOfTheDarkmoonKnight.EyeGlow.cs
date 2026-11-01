using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.NPCs.Enemies.GhostFighter
{
    // The mask shares the helmet's twenty 40x56 cells. Reuse the final solid helmet draw
    // after the spectral pass, so the eyes inherit its bob, flip, rotation and scale exactly.
    [Autoload(Side = ModSide.Client)]
    public sealed class DarkmoonEyeGlowLayer : PlayerDrawLayer
    {
        Asset<Texture2D> eyeGlow;

        public override Position GetDefaultPosition()
            => new AfterParent(ModContent.GetInstance<PuppetFinalCompositeTransformLayer>());

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => PuppetNPC.DrawingPuppetFor is GhostOfTheDarkmoonKnight && drawInfo.shadow == 0f;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (Main.dedServ || PuppetNPC.DrawingPuppetFor is not GhostOfTheDarkmoonKnight knight)
                return;

            int head = drawInfo.drawPlayer.head;
            if (head <= 0 || head >= TextureAssets.ArmorHead.Length)
                return;

            Texture2D helmet = TextureAssets.ArmorHead[head].Value;
            // Spectral halos and history precede the solid core. Search backwards so only
            // the real helmet receives a full-bright eye, never a floating trail copy.
            for (int i = drawInfo.DrawDataCache.Count - 1; i >= 0; i--)
            {
                DrawData glow = drawInfo.DrawDataCache[i];
                if (glow.texture != helmet)
                    continue;

                eyeGlow ??= ModContent.Request<Texture2D>(
                    "tsorcRevamp/Content/Items/Armor/Enemy/DarkmoonKnightSet/DarkmoonKnightHelmet_Head_Glow",
                    AssetRequestMode.ImmediateLoad);
                glow.texture = eyeGlow.Value;
                float opacity = knight.NPC.Opacity;
                if (drawInfo.drawPlayer.isFirstFractalAfterImage)
                    opacity *= drawInfo.drawPlayer.firstFractalAfterImageOpacity;
                glow.color = Color.White * opacity;
                glow.shader = 0; // Keep the authored cyan luminous through spectral tint and armor dye.
                drawInfo.DrawDataCache.Insert(i + 1, glow);
                return;
            }
        }
    }
}
