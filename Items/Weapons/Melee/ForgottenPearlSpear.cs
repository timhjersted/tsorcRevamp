using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class ForgottenPearlSpear : ModItem {

        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            item.damage = 52;
            item.knockBack = 7f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 15;
            item.useTime = 7;
            item.shootSpeed = 7;
            //item.shoot = ProjectileID.DarkLance;

            item.height = 50;
            item.width = 50;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.value = 9000000;
            item.rare = ItemRarityID.LightRed;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.ForgottenPearlSpear>();

        }

    }
}
