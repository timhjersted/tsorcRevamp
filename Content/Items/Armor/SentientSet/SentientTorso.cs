using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.SentientSet;

[AutoloadEquip(EquipType.Body)]
public class SentientTorso : ModItem
{
    public int DamageTaken;
    public float BaseRegen = 3;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs();

    public override void SetStaticDefaults()
    {
        int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
        ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
        ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
        ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;
        // Keep the separate pauldron cells visible in the airborne pose, as on Forgotten Warrior.
        ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 26;
        Item.defense = 10;
        Item.rare = ItemRarityID.Orange;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        var modPlayer = player.GetModPlayer<SentientSetPlayer>();
        modPlayer.EquippedTorso = this;
        float level = MathF.Sqrt(DamageTaken / 100f);
        player.endurance += (level / 100f);
    }
    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        return head.type == ModContent.ItemType<SentientHead>()
            && legs.type == ModContent.ItemType<SentientLegs>();
    }

    public override void UpdateArmorSet(Player player)
    {
        player.setBonus = Language.GetTextValue("Mods.tsorcRevamp.Items.SentientTorso.SetBonus");
        player.crimsonRegen = true;
        player.lifeRegen += (int)BaseRegen;
        player.GetModPlayer<SentientSetPlayer>().SetBonus = true;
    }
    public override void SaveData(TagCompound tag)
    {
        tag.Add("DmgTaken", DamageTaken);
    }
    public override void LoadData(TagCompound tag)
    {
        DamageTaken = tag.GetInt("DmgTaken");
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
        if (ttindex != -1)
        {
            tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Lvl", "Damage absorbed: " + DamageTaken));
        }
    }
    
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.CrimsonScalemail);
        recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
