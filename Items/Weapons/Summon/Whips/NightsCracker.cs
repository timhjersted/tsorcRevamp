using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class NightsCracker : ModItem
    {
        public const int BaseDamage = 42;
        public const int MaxStacks = 5; //must be adjusted manually in the whip projectile
        public static float MinSummonTagDamage = 3;
        public static float MaxSummonTagDamage = MinSummonTagDamage * MaxStacks;
        public static float MinSummonTagCrit = 1;
        public static float MaxSummonTagCrit = MinSummonTagCrit * MaxStacks;
        public static float MinSummonTagAttackSpeed = 5;
        public static float MaxSummonTagAttackSpeed = MinSummonTagAttackSpeed * MaxStacks;
        public static float CritDamage = 33;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MinSummonTagDamage, MaxSummonTagDamage, MinSummonTagCrit, MaxSummonTagCrit, MinSummonTagAttackSpeed, MaxSummonTagAttackSpeed, SearingLash.CritMult, CritDamage);
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        }

        public override void SetDefaults()
        {
            Item.height = 52;
            Item.width = 62;

            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = BaseDamage;
            Item.knockBack = 2.5f;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(0, 30, 0, 0);

            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.NightsCrackerProjectile>();
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
            recipe.AddIngredient(ModContent.ItemType<Dominatrix>());
            recipe.AddIngredient(ItemID.ThornWhip);
            recipe.AddIngredient(ItemID.BoneWhip);
            recipe.AddIngredient(ModContent.ItemType<SearingLash>());
            recipe.AddIngredient(ItemID.SoulofNight, 7);
            recipe.AddIngredient(ItemID.SoulofFright, 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 26000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}