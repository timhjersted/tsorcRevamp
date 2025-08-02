using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    public class BloomShards : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Bloom Shards");
            /* Tooltip.SetDefault("Evokes blooming shards of radiant light\n" +
                                "Close range"); */

        }

        public override void SetDefaults()
        {

            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 6;
            Item.useTime = 6;
            Item.damage = 165;
            Item.autoReuse = true;
            Item.scale = (float)1;
            Item.UseSound = SoundID.Item34;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 20f;
            Item.mana = 6;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SetAuraState(tsorcAuraState.Light);
        }

    }
}
