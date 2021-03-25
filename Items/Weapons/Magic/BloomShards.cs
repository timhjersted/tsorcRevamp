using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class BloomShards : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Bloom Shards");
            Tooltip.SetDefault("Evokes blooming shards of radiant light\n" +
                                "Close range");

        }

        public override void SetDefaults() {

            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 5;
            item.useTime = 5;
            item.maxStack = 1;
            item.damage = 77;
            item.autoReuse = true;
            item.scale = (float)1;
            item.UseSound = SoundID.Item34;
            item.rare = ItemRarityID.LightRed;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 11f;
            item.mana = 5;
            item.noMelee = true;
            item.magic = true;
            item.value = 20000;
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            int healEffect = damage / 10;

            if (crit) {
                healEffect *= 2;
            }

            player.statLife += healEffect;
            player.HealEffect(healEffect);
        }

        public override void OnHitPvp(Player player, Player target, int damage, bool crit) {
            int healEffect = damage / 20;

            if (crit) {
                healEffect *= 2;
            }

            player.statLife += healEffect;
            player.HealEffect(healEffect);
        }

    }
}
