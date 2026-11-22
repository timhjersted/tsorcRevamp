using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.SentientSet;

[AutoloadEquip(EquipType.Head)]
public class SentientHead : ModItem
{
    public int DamageDealt;
    public float Level = 0;
    public const float Divisor = 1000;
    
    public const float BaseRegen = 6;
    public const float BaseDmg = 9;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs();

    public override void SetStaticDefaults()
    {
        int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
        ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
    }

    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 24;
        Item.defense = 8;
        Item.rare = ItemRarityID.Orange;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        var modPlayer = player.GetModPlayer<SentientSetPlayer>();
        modPlayer.EquippedHead = this;
        Level = MathF.Round(MathF.Sqrt(DamageDealt / Divisor), 0);
        player.GetDamage(DamageClass.Generic) += (Level / 100f);
    }
    public override void SaveData(TagCompound tag)
    {
        tag.Add("DmgDealt", DamageDealt);
    }
    public override void LoadData(TagCompound tag)
    {
        DamageDealt = tag.GetInt("DmgDealt");
        Level = MathF.Round(MathF.Sqrt(DamageDealt / Divisor), 0);
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
        if (ttindex != -1)
        {
            tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Lvl", Language.GetTextValue(Tooltip.Key + "0", DamageDealt, Level)));
        }
    }
    
    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        return body.type == ModContent.ItemType<SentientTorso>()
               && legs.type == ModContent.ItemType<SentientLegs>();
    }

    public override void UpdateArmorSet(Player player)
    {
        player.setBonus = Language.GetTextValue( Tooltip.Key + "SetBonus", BaseDmg, BaseRegen / 2f);
        player.crimsonRegen = true;
        player.lifeRegen += (int)BaseRegen;
        player.GetModPlayer<SentientSetPlayer>().SetBonus = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.CrimsonHelmet);
        recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2400);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
