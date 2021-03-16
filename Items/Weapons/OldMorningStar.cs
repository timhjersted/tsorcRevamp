using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {

    public class OldMorningStar : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Does random damage from 0 to 24" +  
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.channel = true;
            item.scale = 0.8f;
            item.useAnimation = 60;
            item.useTime = 60;
            item.damage = 24;
            item.knockBack = 7f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.White;
            item.shootSpeed = 10;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = 12000;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.OldMorningStar>();
        }
    }
}
