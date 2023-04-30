using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class AuraOfIlluminance : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Aura of Illuminance");
            Tooltip.SetDefault("Creates a glowing trail of damaging hallowed light behind you");
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
            player.GetModPlayer<tsorcRevampPlayer>().AuraOfIlluminance = true;
            player.GetModPlayer<tsorcRevampPlayer>().SetAuraState(tsorcAuraState.Cataluminance);

            if (player.whoAmI == Main.myPlayer)
            {
                //If a projectile exists for this player, return as soon as it is found
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<Projectiles.AuraOfIlluminance>() && Main.projectile[i].ai[0] == player.whoAmI)
                    {
                        return;
                    }
                }

                //If not, spawn it
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.AuraOfIlluminance>(), 80, 0, Main.myPlayer, player.whoAmI);
            }
        }
    }
}
