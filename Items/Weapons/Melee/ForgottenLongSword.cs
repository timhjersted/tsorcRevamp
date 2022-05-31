using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenLongSword : ModItem {

        public override void SetDefaults() {
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Green;
            Item.damage = 24;
            Item.height = 34;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4;
            Item.melee = true;
            Item.useAnimation = 22;
            Item.useTime = 22;
            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.Green_2;
            Item.width = 34;
        }
    }
}
