using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.EnemySpriteRendering
{
    [Autoload(Side = ModSide.Client)]
    public class EnemyWeaponDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
            => new AfterParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => EnemySpriteRenderer.DrawingRendererFor != null;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            EnemySpriteRenderer.DrawingRendererFor?.DrawWeaponToLayer(ref drawInfo);
        }
    }
}
