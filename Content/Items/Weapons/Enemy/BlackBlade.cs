using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only sword the Black Knight fights with. BlackBlade.png is 62x62, hilt in the bottom-left
    /// corner (pommel 0,61; tip 61,0), so ~79px grip-to-tip at scale 1.</summary>
    public class BlackBlade : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 62;
            Item.damage = 1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 22;
            Item.useTime = 22;
            Item.DamageType = DamageClass.Melee;
        }
    }
}
