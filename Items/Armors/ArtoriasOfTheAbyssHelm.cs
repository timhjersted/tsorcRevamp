using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class ArtoriasOfTheAbyssHelm : ModItem
    {
        public static float MeleeDmg = 12f;
        public static float MagicDmg = 12f;
        public static float StaminaRegen = 10f;
        public static float PoiseDamage = 35f;
        public static float PoiseStaminaRestore = 8f;
        public const int SoulCost = 70000;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeDmg, MagicDmg, StaminaRegen, PoiseDamage, PoiseStaminaRestore);

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.value = 10000;
            Item.rare = ItemRarityID.Yellow;
            Item.defense = 9;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += MeleeDmg / 100f;
            player.GetDamage(DamageClass.Magic) += MagicDmg / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += StaminaRegen / 100f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ArtoriasOfTheAbyssArmor>() && legs.type == ModContent.ItemType<ArtoriasOfTheAbyssGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Language.GetTextValue("Mods.tsorcRevamp.Items.ArtoriasOfTheAbyssHelm.SetBonus");
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ArtoriasAbysswalker = true;
            modPlayer.CanUseItemsWhileDodging = true;
            player.GetDamage(DamageClass.Melee) += 0.10f;
            player.GetDamage(DamageClass.Magic) += 0.10f;
            player.moveSpeed += 0.12f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
