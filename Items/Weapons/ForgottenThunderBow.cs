using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class ForgottenThunderBow : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Casts a bolt of lightning from your bow, doing massive damage over time. ");
        }
        public override void SetDefaults() {
            item.damage = 160;
            item.height = 58;
            item.knockBack = 4;
            item.autoReuse = true;
            item.noMelee = true ;
            item.magic = true;
            item.rare = ItemRarityID.LightRed;
            item.mana = 100;
            item.shootSpeed = 13; ;
            item.useAnimation = 40;
            item.UseSound = SoundID.Item5;
            item.useTime = 40;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = 35000000;
            item.width = 16;
            item.shoot = ModContent.ProjectileType<Projectiles.Bolt4Ball>();
        }
        public override void AddRecipes() {
            base.AddRecipes();
        }
    }
}
