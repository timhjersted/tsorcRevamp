using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    [LegacyName("NightsCracker")]
    public class NightsCrackerItem : ModItem
    {
        public const int BaseDamage = 56;
        public const int MaxStacks = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxStacks);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
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

            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.NightsCracker.NightsCrackerProjectile>();
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
            recipe.AddIngredient(ModContent.ItemType<DominatrixItem>());
            recipe.AddIngredient(ItemID.ThornWhip);
            recipe.AddIngredient(ItemID.BoneWhip);
            recipe.AddIngredient(ModContent.ItemType<SearingLashItem>());
            recipe.AddIngredient(ItemID.SoulofNight, 7);
            recipe.AddIngredient(ItemID.SoulofFright, 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 26000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}