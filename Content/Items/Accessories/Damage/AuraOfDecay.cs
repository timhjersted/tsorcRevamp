using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Damage
{
    public class AuraOfDecay : ModItem
    {
        public const int WaveDamage = 265;
        public const int WaveIntervalTicks = 180;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(WaveDamage, WaveIntervalTicks / 60);

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

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            // The glowing aura is purely cosmetic, so the accessory's "visible" toggle hides it (the wave still fires).
            if (!hideVisual)
            {
                modPlayer.SetAuraState(tsorcAuraState.Poison);
                modPlayer.effectRadius = 250f;
            }

            if (Main.GameUpdateCount % WaveIntervalTicks == 0 && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.AuraOfDecay>(), WaveDamage, 0, Main.myPlayer, player.whoAmI);
            }
        }
        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return base.CanEquipAccessory(player, slot, modded);
        }

    }
}
