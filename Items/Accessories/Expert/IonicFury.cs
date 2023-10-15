using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class IonicFury : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Lime_7;
            Item.expert = true;
        }

        public override void UpdateEquip(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.SetAuraState(tsorcAuraState.Ion);

            if (Main.GameUpdateCount % 180 == 0 && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, Main.rand.NextVector2CircularEdge(7, 7), ModContent.ProjectileType<Projectiles.Accessories.FriendlyIonBomb>(), 200, 0, Main.myPlayer, player.whoAmI);
            }
        }

    }
}
