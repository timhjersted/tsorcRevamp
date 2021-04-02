using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class FlamesOfTheNight : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A full sprectral array of green flame will light up the skies");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 20;
            item.useTime = 2;
            item.damage = 40;
            item.knockBack = 1;
            item.autoReuse = true;
            item.UseSound = SoundID.Item20;
            item.rare = ItemRarityID.Pink;
            item.shootSpeed = 21;
            item.mana = 12;
            item.value = 400000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.CursedFlames>();
        }
    }
}
