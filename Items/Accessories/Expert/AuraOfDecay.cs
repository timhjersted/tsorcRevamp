using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class AuraOfDecay : ModItem
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
            modPlayer.SetAuraState(tsorcAuraState.Poison);
            modPlayer.effectRadius = 250f;

            if (Main.GameUpdateCount % 180 == 0 && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.AuraOfDecay>(), 5 * 60, 0, Main.myPlayer, player.whoAmI);
            }
        }
        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return base.CanEquipAccessory(player, slot, modded);
        }

    }
}
