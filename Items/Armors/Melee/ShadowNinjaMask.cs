using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Head)]
    class ShadowNinjaMask : ModItem
    {
        public const float MeleeCrit = 30f;
        public const float MeleeSpeed = 30f;
        public const int MaxDefense = 40;
        public const float LifeRegen = 18f;
        public const float ResistanceToMoveSpeedRatio = 1.5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeCrit, MeleeSpeed, MaxDefense, LifeRegen / 2f, ResistanceToMoveSpeedRatio);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
            Item.defense = 5;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ShadowNinjaTop>() && legs.type == ModContent.ItemType<ShadowNinjaBottoms>();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += MeleeCrit;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;
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
            recipe.AddIngredient(ModContent.ItemType<BlackBeltHairStyle>());
            recipe.AddIngredient(ItemID.SoulofFright, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
