using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Ammo
{
    public class TeslaBolt : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 14;
            Item.height = 20;
            Item.maxStack = Item.CommonMaxStack;
            Item.scale = 1f;
            Item.value = 3;
            Item.ammo = Item.type;
            Item.rare = ItemRarityID.Red;
            Item.damage = 50;
        }
    }
}
