using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class AuraOfDecay : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Aura of Decay");
            Tooltip.SetDefault("Periodically releases a wave of poison");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateEquip(Player player)
        {
            if(player.whoAmI != Main.myPlayer)
            {
                return;
            }

            if (Main.GameUpdateCount % 180 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.AuraOfDecay>(), 300, 0, Main.myPlayer, player.whoAmI);
                }
            }
        }

    }
}
