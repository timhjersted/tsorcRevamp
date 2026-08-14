using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Tiles;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Placeable
{
    public class BonfirePlaceable : ModItem
    {
        // Doubling-cost system disabled for now (bonfires weren't showing as craftable with it active) -
        // reverted to a flat cost. The doubling logic below is commented out, not deleted, in case we want to
        // revisit it later.
        public const int FlatDarkSoulCost = 5000;

        /* --- Doubling-cost version (disabled) ---
        public const int BaseDarkSoulCost = 2000;
        */

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<BonfireCheckpoint>());
            Item.rare = ItemRarityID.Master;
            Item.value = Item.sellPrice(50);
        }

        /* --- Doubling-cost version (disabled) ---
        /// <summary>The Dark Soul price for this player's NEXT craft: BaseDarkSoulCost, doubled once per
        /// bonfire they've already crafted (2000, 4000, 8000, ...). Capped at 2^20 multiplier purely so the
        /// math can never overflow int - no one is realistically ever affording that many.</summary>
        public static int GetDarkSoulCost(Player player)
        {
            int crafted = player.GetModPlayer<tsorcRevampPlayer>().BonfiresCrafted;
            int cappedCrafted = Math.Min(crafted, 20);
            return BaseDarkSoulCost * (1 << cappedCrafted);
        }
        */

        public override void AddRecipes()
        {
            var recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Campfire);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), FlatDarkSoulCost);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            recipe.Register();

            /* --- Doubling-cost version (disabled) ---
            var recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Campfire);
            // tModLoader's recipe panel can't re-render a dynamic ingredient amount, so the badge here always
            // shows the BASE price (2000) - not the real, currently-doubled one. That real price is what
            // actually gates crafting (see the condition below) and what OnCraft actually charges; the
            // tooltip spells out the current number so it's never a surprise at the altar.
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), BaseDarkSoulCost);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            // Player.CountItem's default stopCountingAt is 0, which makes it return after the FIRST matching
            // stack it finds - not a true sum across stacks (any positive count already satisfies ">= 0"). Pass
            // the real cost explicitly so this actually sums stacks until it's confirmed there's enough, rather
            // than silently under-counting a Dark Soul total split across more than one inventory slot.
            recipe.AddCondition(new Condition("Mods.tsorcRevamp.Items.BonfirePlaceable.CanAffordCondition",
                () => Main.LocalPlayer.CountItem(ModContent.ItemType<DarkSoul>(), GetDarkSoulCost(Main.LocalPlayer)) >= GetDarkSoulCost(Main.LocalPlayer)));
            recipe.Register();
            */
        }

        /* --- Doubling-cost version (disabled) ---
        public override void OnCraft(Recipe recipe)
        {
            Player player = Main.LocalPlayer;
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            // The recipe's own ingredient list already consumed the base 2000 - only take the rest of the
            // real (doubled) price here.
            int totalCost = GetDarkSoulCost(player);
            int extraBeyondBase = totalCost - BaseDarkSoulCost;
            if (extraBeyondBase > 0)
            {
                ConsumeDarkSouls(player, extraBeyondBase);
            }

            modPlayer.BonfiresCrafted++;
        }

        /// <summary>Removes `amount` Dark Souls from the player's inventory in a single pass, rather than
        /// calling Player.ConsumeItem in a loop - the cost here can run into the thousands/millions, and
        /// ConsumeItem rescans the whole inventory per call.</summary>
        private static void ConsumeDarkSouls(Player player, int amount)
        {
            int darkSoulType = ModContent.ItemType<DarkSoul>();
            for (int i = 0; i < player.inventory.Length && amount > 0; i++)
            {
                Item invItem = player.inventory[i];
                if (invItem != null && !invItem.IsAir && invItem.type == darkSoulType)
                {
                    int take = Math.Min(invItem.stack, amount);
                    invItem.stack -= take;
                    amount -= take;
                    if (invItem.stack <= 0)
                    {
                        invItem.TurnToAir();
                    }
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int nextCost = GetDarkSoulCost(Main.LocalPlayer);
            tooltips.Add(new TooltipLine(Mod, "BonfireNextCost", LangUtils.GetTextValue("Items.BonfirePlaceable.NextCost", nextCost)));
        }
        */
    }
}
