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
        public static float MeleeCrit = 30f;
        public static float MeleeSpeed = 30f;
        public static int MaxDefense = 40;
        public static int LifeRegen = 30;
        public static float DRToMoveSpeedRatio = 1.5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeCrit, MeleeSpeed, MaxDefense, LifeRegen, DRToMoveSpeedRatio);
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
            player.lifeRegen += LifeRegen;
            player.moveSpeed += player.endurance * DRToMoveSpeedRatio;
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
