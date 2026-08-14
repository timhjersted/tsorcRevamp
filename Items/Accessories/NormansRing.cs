using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class NormansRing : ModItem
    {
        public const float DamageBonus = 3f;
        public const float ManaCostReduction = 3f;
        public const float AmmoSaveChance = 5f;
        public const float WhipRangeBonus = 5f;
        public const int DefenseBonus = 4;
        public const float DeprivedMoveSpeedBonus = 5f;
        public const float DeprivedStaminaRegenBonus = 5f;

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = 0;
            Item.rare = ItemRarityID.White;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            switch (modPlayer.startingClass)
            {
                case StartingClass.Melee:
                    player.GetDamage(DamageClass.Melee) += DamageBonus / 100f;
                    player.statDefense += DefenseBonus;
                    break;
                case StartingClass.Ranged:
                    player.GetDamage(DamageClass.Ranged) += DamageBonus / 100f;
                    modPlayer.normansRingAmmoSave = true;
                    break;
                case StartingClass.Magic:
                    player.GetDamage(DamageClass.Magic) += DamageBonus / 100f;
                    player.manaCost *= 1f - ManaCostReduction / 100f;
                    break;
                case StartingClass.Summoner:
                    player.GetDamage(DamageClass.Summon) += DamageBonus / 100f;
                    player.whipRangeMultiplier += WhipRangeBonus / 100f;
                    break;
                case StartingClass.Deprived:
                    // No weapon bias to boost, so Deprived's ring skips damage/defense entirely and leans into
                    // mobility instead - "unarmored but nimble" rather than a smaller version of Melee's ring.
                    player.moveSpeed += DeprivedMoveSpeedBonus / 100f;
                    player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += DeprivedStaminaRegenBonus / 100f;
                    break;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            StartingClass startingClass = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().startingClass;
            tooltips.Add(new TooltipLine(Mod, "StartingClassStats", GetBonusDescription(startingClass)));
        }

        /// <summary>Localized description of the class-specific bonus this ring grants. Shared by the item's
        /// own tooltip and the starting-class selection screen so the two can never drift apart.</summary>
        internal static string GetBonusDescription(StartingClass startingClass)
        {
            string statKey = startingClass switch
            {
                StartingClass.Ranged => "Mods.tsorcRevamp.Items.NormansRing.Stats.Ranged",
                StartingClass.Magic => "Mods.tsorcRevamp.Items.NormansRing.Stats.Magic",
                StartingClass.Summoner => "Mods.tsorcRevamp.Items.NormansRing.Stats.Summoner",
                StartingClass.Melee => "Mods.tsorcRevamp.Items.NormansRing.Stats.Melee",
                StartingClass.Deprived => "Mods.tsorcRevamp.Items.NormansRing.Stats.Deprived",
                _ => "Mods.tsorcRevamp.Items.NormansRing.Stats.None"
            };

            return Language.GetTextValue(statKey, DamageBonus, DefenseBonus, AmmoSaveChance, ManaCostReduction, WhipRangeBonus, DeprivedMoveSpeedBonus, DeprivedStaminaRegenBonus);
        }
    }
}
