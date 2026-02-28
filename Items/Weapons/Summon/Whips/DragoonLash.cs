using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class DragoonLash : ModItem
    {
        public const int BaseDamage = 72;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        }

        public override void SetDefaults()
        {
            Item.height = 48;
            Item.width = 60;

            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = BaseDamage;
            Item.knockBack = 3.5f;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(0, 70, 0, 0);

            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.DragoonLashProjectile>();
            Item.shootSpeed = 4;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 25;
            Item.useAnimation = 25;
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
            recipe.AddIngredient(ItemID.ChlorophyteBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ItemID.SoulofFright, 1);
            recipe.AddIngredient(ItemID.SoulofSight, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}