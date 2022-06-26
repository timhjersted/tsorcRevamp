using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ForgottenKaiserKnuckles : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Great spiked knuckles.");
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Green;
            Item.damage = 17;
            Item.height = 23;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 3;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.Green_2;
            Item.width = 21;
        }
    }
}
