using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    [LegacyName("SearingLash")]
    public class SearingLashItem : ModItem
    {
        public const int BaseDamage = 30;
        public const int BatDmgScaling = 33;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BatDmgScaling);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        }

        public override void SetDefaults()
        {
            Item.height = 84;
            Item.width = 88;

            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = BaseDamage;
            Item.knockBack = 3;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(0, 8, 50, 0);

            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.SearingLash.SearingLashProjectile>();
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
        public override bool AltFunctionUse(Player player)
        {
            if (!Main.mouseLeft)
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HellstoneBar, 3);
            recipe.AddIngredient(ItemID.MeteoriteBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}