using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Content.Items.Armor.SetBonuses;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Armor.Melee.FighterSet
{
    [AutoloadEquip(EquipType.Head)]
    public class FighterHairStyle : ModItem
    {
        public const float CritChance = 17f;
        public const float Dmg = 24f;
        public const float Resistance = 13f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, CritChance);
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 2;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += CritChance;
            player.GetDamage(DamageClass.Melee) += Dmg / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<FighterBreastplate>() && legs.type == ModContent.ItemType<FighterGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            var modPlayer = player.GetModPlayer<ShunpoPlayer>();
            modPlayer.HasShunpo = true;
            player.endurance += Resistance / 100f;
            player.setBonus = Language.GetTextValue(Tooltip.Key + "SetBonus", Resistance) + modPlayer.ShunpoSetBonusTip(ShunpoBlink.Cooldown);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AdamantiteHelmet);
            recipe2.AddIngredient(ItemID.TitaniumMask);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
