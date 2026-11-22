using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.SentientSet;

[AutoloadEquip(EquipType.Legs)]
public class SentientLegs : ModItem
{
    public float DistanceTraveled;
    public float Level = 0;
    public const float Divisor = 2000f;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs();

    public override void SetStaticDefaults()
    {
        int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
        ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
        ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 18;
        Item.defense = 9;
        Item.rare = ItemRarityID.Orange;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        var modPlayer = player.GetModPlayer<SentientSetPlayer>();
        modPlayer.EquippedLegs = this;
        Level = MathF.Round(MathF.Sqrt(DistanceTraveled / Divisor), 0);
        player.moveSpeed += (Level / 100f);
    }
    public override void SaveData(TagCompound tag)
    {
        tag.Add("DistTrav", DistanceTraveled);
    }
    public override void LoadData(TagCompound tag)
    {
        DistanceTraveled = tag.GetFloat("DistTrav");
        Level = MathF.Round(MathF.Sqrt(DistanceTraveled / Divisor), 0);
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
        if (ttindex != -1)
        {
            tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Lvl", Language.GetTextValue(Tooltip.Key + "0", MathF.Round(DistanceTraveled, 2), Level)));
        }
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.CrimsonGreaves);
        recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2600);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
