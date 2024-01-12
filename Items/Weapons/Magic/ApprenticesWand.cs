using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Magic;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Magic
{
    [LegacyName("WoodenFlute")]
    class ApprenticesWand : ModItem
    {
        public const int BaseDmg = 12;
        public override void SetDefaults()
        {
            Item.damage = BaseDmg;
            Item.mana = 3;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.knockBack = 2.5f;
            Item.width = 34;
            Item.height = 10;
            Item.shootSpeed = 10;
            Item.rare = ItemRarityID.White;
            Item.DamageType = DamageClass.Magic;
            Item.UseSound = SoundID.Item8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.White_0;
            Item.shoot = ModContent.ProjectileType<ApprenticesWandFireball>();
            Item.noMelee = true;
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (Item.prefix == PrefixID.Ignorant)
            {
                damage.Flat -= 5;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Item.prefix == PrefixID.Ignorant)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "CanBeBlessed", LangUtils.GetTextValue("CommonItemTooltip.CanBeBlessed")));
                }
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Wood, 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
