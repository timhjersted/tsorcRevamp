using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class WoodenFlute : ModItem {
        public override void SetDefaults() {
            item.damage = 10;
            item.height = 10;
            item.knockBack = 4;
            item.maxStack = 1;
            item.rare = ItemRarityID.Blue;
            item.scale = 1;
            item.shootSpeed = 10;
            item.magic = true;
            item.mana = 2;
            item.useAnimation = 45;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 45;
            item.value = 20000;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.MusicalNote>();
        }
    }
}
