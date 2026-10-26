using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Systems.CeruleanFlask;
using tsorcRevamp.UI;

namespace tsorcRevamp.Systems.EstusFlask;

public class EstusFlaskQuickHealOverride : ModSystem
{
    public override void Load()
    {
        Terraria.On_Player.QuickHeal += CustomQuickHeal;
    }
        private static void CustomQuickHeal(Terraria.On_Player.orig_QuickHeal orig, Player player)
        {
            if (MethodSwaps.HasGuardActionLock(player))
            {
                return;
            }

            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            EstusFlaskPlayer estusPlayer = player.GetModPlayer<EstusFlaskPlayer>();
            CeruleanFlaskPlayer ceruleanFlaskPlayer = player.GetModPlayer<CeruleanFlaskPlayer>();

            if (modPlayer.SoulsMode && player.statLife < player.statLifeMax2)
            {
                if (player == Main.LocalPlayer && !player.mouseInterface && estusPlayer.EstusChargesCurrent > 0 && player.itemAnimation == 0
                && !modPlayer.isDodging && !estusPlayer.IsDrinking && !player.CCed && !ceruleanFlaskPlayer.IsDrinking)
                {
                    estusPlayer.IsDrinking = true;
                    estusPlayer.EstusDrinkTimer = 0;
                }
                return;
            }

            if (player.noItems || player.statLife == player.statLifeMax2 || player.potionDelay > 0)
                return;
            Item selectedItem = player.QuickHeal_GetItemToUse();
            if (selectedItem == null)
            {
                selectedItem = new Item();
                selectedItem.SetDefaults(0);
            }
            Item[] PotionBagItems = modPlayer.PotionBagItems;
            if (!player.HasBuff(BuffID.PotionSickness))
            {
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                {
                    if (PotionBagItems[i] != null && PotionBagItems[i].potion && ItemLoader.CanUseItem(PotionBagItems[i], player) && (selectedItem == null || player.GetHealLife(PotionBagItems[i], true) > player.GetHealLife(selectedItem, true)))
                    {
                        selectedItem = PotionBagItems[i];
                    }
                }

                MethodSwaps.UsePotion(selectedItem, player);
            }
        }
}