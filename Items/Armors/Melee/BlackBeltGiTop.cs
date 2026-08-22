using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class BlackBeltGiTop : ModItem
    {
        public const float Dmg = 20f;
        public const float AtkSpeed = 16f;
        public const int MaxDefense = 30;
        public const float LifeRegen = 13f;
        public const float ResistanceToMoveSpeedRatio = 1.25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, AtkSpeed, MaxDefense, LifeRegen / 2f, ResistanceToMoveSpeedRatio);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 2;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += Dmg / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<BlackBeltHairStyle>() && legs.type == ModContent.ItemType<BlackBeltGiPants>();
        }

        public override void UpdateArmorSet(Player player)
        {
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

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.NinjaShirt);
            recipe.AddIngredient(ItemID.PalladiumBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
