using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenSwordbreaker : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Striking an enemy may temporarily make you deflect attacks.");
        }

        public override void SetDefaults() {
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.autoReuse = true;
            item.useTurn = true;
            item.rare = ItemRarityID.Pink;
            item.damage = 93;
            item.height = 28;
            item.knockBack = 3.5f;
            item.melee = true;
            item.useAnimation = 15;
            item.useTime = 15;
            item.UseSound = SoundID.Item1;
            item.value = PriceByRarity.Pink_5;
            item.width = 28;
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(20) == 0) {
                player.AddBuff(ModContent.BuffType<Buffs.Invincible>(), 30);
            }
        }
    }
}
