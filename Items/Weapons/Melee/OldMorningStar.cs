using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {

    public class OldMorningStar : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Does random damage from 0 to 24" +  
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.scale = 0.8f;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.damage = 24;
            Item.knockBack = 7f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.White;
            Item.shootSpeed = 10;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = 10000;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.OldMorningStar>();
        }

        public override void HoldItem(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
