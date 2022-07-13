using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    [LegacyName("ForgottenMurakumo")]
    class Murakumo : ModItem
    {

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Pink;
            Item.damage = 45;
            Item.height = 72;
            Item.knockBack = 7;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 16;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 48;
        }
    }
}
