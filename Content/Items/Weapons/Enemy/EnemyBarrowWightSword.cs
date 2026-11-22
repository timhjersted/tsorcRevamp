using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only sword the Barrow Wight Phantom fights with (the Witchking's summon). 52x52, the
    /// standard broadsword diagonal: pommel at the bottom-left corner (0,51), tip at (51,0), ~73px long.</summary>
    public class EnemyBarrowWightSword : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 52;
            Item.damage = 1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 24;
            Item.useTime = 24;
            Item.DamageType = DamageClass.Melee;
        }
    }
}
