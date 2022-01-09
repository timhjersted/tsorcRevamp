using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class ElfinBow : ModItem {
        public override void SetDefaults() {
            item.autoReuse = true;
            item.damage = 385;
            item.height = 58;
            item.knockBack = 5;
            item.noMelee = true;
            item.ranged = true;
            item.rare = ItemRarityID.Orange;
            item.scale = 0.9f;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 12;
            item.useAmmo = AmmoID.Arrow;
            item.useAnimation = 25;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 25;
            item.value = 20000000;
            item.width = 14;
        }
    }
}
