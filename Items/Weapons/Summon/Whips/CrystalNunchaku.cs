using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class CrystalNunchaku : ModItem
    {
        public static float MinSummonTagScalingDamage = 0;
        public static float MaxSummonTagScalingDamage = 33;
        public static int MinSummonTagDefense = 0;
        public static float MaxSummonTagDefense = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxSummonTagScalingDamage, MinSummonTagScalingDamage, MaxSummonTagDefense, MinSummonTagDefense);
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        }

        public override void SetDefaults()
        {
            Item.height = 36;
            Item.width = 32;

            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = 49;
            Item.knockBack = 5f;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.buyPrice(0, 20, 0, 0);

            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.CrystalNunchakuProjectile>();
            Item.shootSpeed = 4;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CrystalShard, 5);
            recipe.AddIngredient(ItemID.SoulofLight, 4);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 11000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}