using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    class ShadowNinjaMask2 : ModItem
    {
        public static float CritChance = 30f;
        public static float Dmg = 30f;
        public static float AtkSpeed = 30f;
        public static int MaxDefense = 60;
        public static int LifeRegen = 30;
        public static float DRToMoveSpeedRatio = 2f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance, Dmg, AtkSpeed, MaxDefense, LifeRegen, DRToMoveSpeedRatio);
        public override string Texture => "tsorcRevamp/Items/Armors/Melee/ShadowNinjaMask";
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
            Item.defense = 6;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += CritChance;
            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<Melee.ShadowNinjaTop>() && legs.type == ModContent.ItemType<Melee.ShadowNinjaBottoms>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += AtkSpeed / 100f;
            if(player.statDefense > MaxDefense)
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
            recipe.AddIngredient(ModContent.ItemType<Melee.ShadowNinjaMask>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
