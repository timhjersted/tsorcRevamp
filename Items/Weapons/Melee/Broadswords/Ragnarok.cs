using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class Ragnarok : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ragnarok");

        }

        public override void SetDefaults()
        {
            Item.stack = 1;
            Item.rare = ItemRarityID.Cyan;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.damage = 170;
            Item.knockBack = 10;
            Item.scale = 0.8f;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = (float)1.3;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.Cyan_9;
        }
    }
}
