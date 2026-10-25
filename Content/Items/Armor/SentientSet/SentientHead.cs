using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.SentientSet;

[Autoload(false)]
[AutoloadEquip(EquipType.Head)]
public class SentientHead : ModItem
{
    public int DamageDealt;
    public const float BaseDmg = 9;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs();
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 8;
        Item.rare = ItemRarityID.Orange;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        var modPlayer = player.GetModPlayer<SentientSetPlayer>();
        modPlayer.EquippedHead = this;
        float level = MathF.Sqrt(DamageDealt / 1000f);
        player.GetDamage(DamageClass.Generic) += (level / 100f) + (BaseDmg / 100f);
    }
    public override void SaveData(TagCompound tag)
    {
        tag.Add("DmgDealt", DamageDealt);
    }
    public override void LoadData(TagCompound tag)
    {
        DamageDealt = tag.GetInt("DmgDealt");
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
        if (ttindex != -1)
        {
            tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Lvl", "Damage studied: " + DamageDealt));
        }
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