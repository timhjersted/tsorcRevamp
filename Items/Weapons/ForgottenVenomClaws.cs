using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class ForgottenVenomClaws : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("May poison the enemy.");
        }

        public override void SetDefaults() {
            item.autoReuse = true;
            item.useTurn = true;
            item.rare = ItemRarityID.Pink;
            item.damage = 35;
            item.height = 18;
            item.knockBack = 3;
            item.melee = true;
            item.useAnimation = 7;
            item.useTime = 7;
            item.UseSound = SoundID.Item1;
            item.value = 200000;
            item.width = 22;
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(30) == 0) {
                player.AddBuff(BuffID.Poisoned, 360);
            }
        }
    }
}
