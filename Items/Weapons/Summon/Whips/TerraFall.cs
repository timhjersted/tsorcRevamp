using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class TerraFall : ModItem
    {
        public const int BaseDamage = 115;
        public const int MaxStacks = 5; //must be adjusted manually in the whip projectile
        public static float MinSummonTagDamage = 5;
        public static float MaxSummonTagDamage = MinSummonTagDamage * MaxStacks;
        public static float MinSummonTagCrit = 2;
        public static float MaxSummonTagCrit = MinSummonTagCrit * MaxStacks;
        public static float MinSummonTagAttackSpeed = 7;
        public static float MaxSummonTagAttackSpeed = MinSummonTagAttackSpeed * MaxStacks;
        public static float CritDamage = 33;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MinSummonTagDamage, MaxSummonTagDamage, MinSummonTagCrit, MaxSummonTagCrit, MinSummonTagAttackSpeed, MaxSummonTagAttackSpeed, SearingLash.CritMult, CritDamage);
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        }

        public override void SetDefaults()
        {
            Item.height = 80;
            Item.width = 90;

            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = BaseDamage;
            Item.knockBack = 5;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(3, 33, 33, 33);


            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.TerraFallProjectile>();
            Item.shootSpeed = 4;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<NightsCracker>());
            recipe.AddIngredient(ItemID.SwordWhip);
            recipe.AddIngredient(ItemID.RainbowWhip);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 115000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}