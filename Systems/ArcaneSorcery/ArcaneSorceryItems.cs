using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Magic;
using tsorcRevamp.Items.Armors.Magic;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems.ArcaneSorcery;

public class ArcaneSorceryItems : GlobalItem
{
    public const float ManaRestorationCuffsPercentage = 120;
    public const float ManaStarMaxManaPercentage = 5;
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        Player player = Main.LocalPlayer;
        var arcanePlayer = player.GetModPlayer<ArcaneSorceryPlayer>();

        if (arcanePlayer.ArcaneSorcerer && player.whoAmI == Main.myPlayer && (item.type == ItemID.CelestialMagnet || item.type == ItemID.ManaCloak || item.type == ItemID.CelestialEmblem))
        {
            TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips, LangUtils.GetTextValue("Items.VanillaItems.CelestialMagnetBotC", ManaStarMaxManaPercentage));
        }

        if (arcanePlayer.ArcaneSorcerer && player.whoAmI == Main.myPlayer && item.type == ItemID.MagicCuffs)
        {
            TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MagicCuffsBotC", ManaRestorationCuffsPercentage));
        }

        if (arcanePlayer.ArcaneSorcerer && player.whoAmI == Main.myPlayer && (item.type == ItemID.CelestialCuffs | item.type == ModContent.ItemType<CelestialCloak>()))
        {
            TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.CelestialMagnetBotC", ManaStarMaxManaPercentage) + "\n" + Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MagicCuffsBotC", ManaRestorationCuffsPercentage));
        }
    }

    public override void Load()
    {
        On_Player.OnHurt_Part2 += ManaStarRestorationEdit;
        On_Player.PickupItem += ManaStarRestorationTextEdit;
    }
    
    private static void ManaStarRestorationEdit(On_Player.orig_OnHurt_Part2 orig, Player self, Player.HurtInfo info)
    {
        if (self.magicCuffs && self.GetModPlayer<ArcaneSorceryPlayer>().ArcaneSorcerer)
        {
            int ManaGain = (int)(info.SourceDamage * (ManaRestorationCuffsPercentage / 100f));
            self.statMana += ManaGain;
            if (self.statMana > self.statManaMax2)
            {
                self.statMana = self.statManaMax2;
            }
            self.ManaEffect(ManaGain);
            return;
        }
        orig(self, info);
    }
    private static Item ManaStarRestorationTextEdit(On_Player.orig_PickupItem orig, Player self, int playerIndex, int worldItemArrayIndex, Item itemToPickUp)
    {
        if ((itemToPickUp.type == ItemID.Star || itemToPickUp.type == ItemID.SugarPlum || itemToPickUp.type == ItemID.SoulCake || itemToPickUp.type == ItemID.ManaCloakStar) && self.GetModPlayer<ArcaneSorceryPlayer>().ArcaneSorcerer)
        {
            int ManaGain = (int)(self.statManaMax2 * (ArcaneSorceryItems.ManaStarMaxManaPercentage / 100f));
            SoundEngine.PlaySound(SoundID.Grab, new Vector2((int)self.position.X, (int)self.position.Y));
            self.statMana += ManaGain;
            self.ManaEffect(ManaGain);
            if (self.statMana > self.statManaMax2)
            {
                self.statMana = self.statManaMax2;
            }
            itemToPickUp.type = ItemID.None;
            return itemToPickUp;
        }
        return orig(self, playerIndex, worldItemArrayIndex, itemToPickUp);
    }
}