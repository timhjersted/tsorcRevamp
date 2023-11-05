using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    class Crossbow : ModItem
    {
        public const int BaseDmg = 13;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = BaseDmg;
            Item.knockBack = 4;
            Item.crit = 16;
            Item.shootSpeed = 10;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.width = 12;
            Item.height = 28;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.useAmmo = ModContent.ItemType<Bolt>();
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.White_0;
        }
        public override void UpdateInventory(Player player)
        {
            if (Item.prefix == PrefixID.Awful)
            {
                Item.damage = BaseDmg - 7;
            }
            else
            {
                Item.damage = BaseDmg;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Item.prefix == PrefixID.Awful)
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
            recipe.AddIngredient(ItemID.Wood, 5);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
