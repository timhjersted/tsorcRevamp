using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Body)]
    public class SmoughArmor : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/SmoughArmor";

        public const int StaminaShieldCost = 40;
        public const float BadAtkSpeedManaCost = 50f;
        public const float BadCritDmg = 25f;
        public const int MaxLife = 60;
        public const float BlockStaminaDiscount = 20f;
        public const int CrushingBlowHitCount = 4;
        public const float CrushingBlowPoiseBonus = 50f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BadAtkSpeedManaCost, BadCritDmg, MaxLife, BlockStaminaDiscount, CrushingBlowHitCount, CrushingBlowPoiseBonus);
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 30;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.statLifeMax2 += MaxLife;
            player.GetModPlayer<tsorcRevampPlayer>().SmoughShieldSkills = true;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<SmoughHelmet>() && legs.type == ModContent.ItemType<SmoughGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SmoughAttackSpeedReduction = true;
            player.GetAttackSpeed(DamageClass.Generic) *= 1f - BadAtkSpeedManaCost / 100f;
            player.manaCost += BadAtkSpeedManaCost / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfSmough>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
