using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ForgottenAxe : ModItem
    {

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Blue;
            Item.damage = 18;
            Item.height = 30;
            Item.knockBack = 6;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 4500;
            Item.width = 30;
        }
    }
}
