using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class FirelinkHelm : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Armors/FirelinkHelm";

        public const float GenericDamage = 10f;
        public const float GenericCrit = 8f;
        public const int MaxMana = 120;
        public const float ManaCost = 10f;
        public const float SetGenericDamage = 10f;
        public const float SetGenericCrit = 8f;
        public const float SetEndurance = 8f;
        public const int SetLifeRegen = 2;
        public const float SetStaminaRegen = 15f;
        public const int SoulCost = 80000;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(GenericDamage, GenericCrit, MaxMana, ManaCost);

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.defense = 20;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += GenericDamage / 100f;
            player.GetCritChance(DamageClass.Generic) += GenericCrit;
            player.statManaMax2 += MaxMana;
            player.manaCost -= ManaCost / 100f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<FirelinkArmor>() && legs.type == ModContent.ItemType<FirelinkLeggings>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Language.GetTextValue("Mods.tsorcRevamp.Items.FirelinkHelm.SetBonus", SetGenericDamage, SetGenericCrit, SetEndurance, SetLifeRegen, SetStaminaRegen);
            player.GetDamage(DamageClass.Generic) += SetGenericDamage / 100f;
            player.GetCritChance(DamageClass.Generic) += SetGenericCrit;
            player.endurance += SetEndurance / 100f;
            player.lifeRegen += SetLifeRegen;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.OnFire3] = true;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + SetStaminaRegen / 100f;
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfCinder>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}