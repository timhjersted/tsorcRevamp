using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Melee.BlackBeltSet
{
    [AutoloadEquip(EquipType.Head)]
    public class BlackBeltHairStyle : ModItem
    {
        public const float CritChance = 5f;
        public const float AtkSpeed = 16f;
        public const int MaxDefense = 30;
        public const float LifeRegen = 13f;
        public const float ResistanceToMoveSpeedRatio = 1.25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 2;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += CritChance;
        }
        
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<BlackBeltGiTop>() && legs.type == ModContent.ItemType<BlackBeltGiPants>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Language.GetTextValue(Tooltip.Key + "SetBonus", AtkSpeed, LifeRegen / 2f, MaxDefense,
                ResistanceToMoveSpeedRatio);
            player.GetAttackSpeed(DamageClass.Melee) += AtkSpeed / 100f;
            if (player.statDefense > MaxDefense)
            {
                player.statDefense *= 0;
                player.statDefense += MaxDefense;
            }
            player.lifeRegen += (int)LifeRegen;
            player.moveSpeed += player.endurance * ResistanceToMoveSpeedRatio;
            player.endurance = 0f;
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.NinjaHood);
            recipe.AddIngredient(ItemID.PalladiumBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
