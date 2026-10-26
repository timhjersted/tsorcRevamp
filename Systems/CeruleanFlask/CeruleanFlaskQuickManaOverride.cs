using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Systems.EstusFlask;
using tsorcRevamp.UI;

namespace tsorcRevamp.Systems.CeruleanFlask;

public class CeruleanFlaskQuickManaOverride : ModSystem
{
    public override void Load()
    {
        Terraria.On_Player.QuickMana += CustomQuickMana;
    }
    
        public static void TryUseQuickMana(Player player)
        {
            if (MethodSwaps.HasGuardActionLock(player))
            {
                return;
            }

            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            EstusFlaskPlayer estusPlayer = player.GetModPlayer<EstusFlaskPlayer>();
            CeruleanFlaskPlayer ceruleanFlaskPlayer = player.GetModPlayer<CeruleanFlaskPlayer>();
            if (modPlayer.SoulsMode && player.statMana < player.statManaMax2 && ceruleanFlaskPlayer.CeruleanChargesCurrent > 0)
            {
                if (player == Main.LocalPlayer && !player.mouseInterface && ceruleanFlaskPlayer.CeruleanChargesCurrent > 0 && player.itemAnimation == 0
                && !modPlayer.isDodging && !ceruleanFlaskPlayer.IsDrinking && !player.CCed && !estusPlayer.IsDrinking && !ceruleanFlaskPlayer.IsCeruleanRestoring)
                {
                    ceruleanFlaskPlayer.IsDrinking = true;
                    ceruleanFlaskPlayer.CeruleanDrinkTimer = 0;

                    // Cerulean drinks intentionally apply NO mobility/defense debuffs — drinking for
                    // mana shouldn't lock the player down. The Chloranthy ring's "trade slowdown for
                    // vulnerability" mechanic was moved to the Estus drink (see CustomQuickHeal below)
                    // since healing drinks happen more often in active combat.
                }
                return;
            }
            if (player.noItems || player.statMana == player.statManaMax2)
            {
                return;
            }
            Item selectedItem = player.QuickMana_GetItemToUse();
            if (player.HasItemInInventoryOrOpenVoidBag(ModContent.ItemType<SupremeManaPotion>()))
            {
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    if (player.inventory[i] != null && player.inventory[i].type == ModContent.ItemType<SupremeManaPotion>())
                    {
                        selectedItem = player.inventory[i];
                    }
                }
            }
            if (selectedItem == null)
            {
                selectedItem = new Item();
                selectedItem.SetDefaults(0);
            }

            Item[] PotionBagItems = player.GetModPlayer<tsorcRevampPlayer>().PotionBagItems;

            if (PotionBagItems != null)
            {
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                {
                    if (PotionBagItems[i] != null)
                    {
                        if (PotionBagItems[i].type != 0 && (player.potionDelay == 0 || !PotionBagItems[i].potion) && ItemLoader.CanUseItem(PotionBagItems[i], player) && (player.GetHealMana(PotionBagItems[i], true) > player.GetHealMana(selectedItem, true)) || PotionBagItems[i].type == ModContent.ItemType<SupremeManaPotion>())
                        {
                            selectedItem = PotionBagItems[i];
                        }
                    }
                }
            }

            MethodSwaps.UsePotion(selectedItem, player);
        }

        private static void CustomQuickMana(Terraria.On_Player.orig_QuickMana orig, Player player)
        {
            TryUseQuickMana(player);            
        }
}