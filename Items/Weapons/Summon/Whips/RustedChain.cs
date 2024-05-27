using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class RustedChain : ModItem
    {
        public const int SummonTagArmorPen = 4;
        public const int SummonTagDmg = 3;
        public const int BaseDmg = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonTagArmorPen, SummonTagDmg);
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        }
        public override void SetDefaults()
        {
            Item.height = 66;
            Item.width = 60;

            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = BaseDmg;
            Item.knockBack = 0.5f;
            Item.rare = ItemRarityID.White;
            Item.value = PriceByRarity.White_0;

            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.RustedChainProjectile>();
            Item.shootSpeed = 4;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (Item.prefix == PrefixID.Terrible)
            {
                if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    damage.Flat -= 1;
                }
                else
                {
                    damage.Flat -= 8;
                }
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Item.prefix == PrefixID.Terrible)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "CanBeBlessed", LangUtils.GetTextValue("CommonItemTooltip.CanBeBlessed")));
                }
            }
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Chain, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe.AddCondition(Condition.NearWater);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.Chain, 3);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe2.AddIngredient(ItemID.BottledWater, 4);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.Chain, 3);
            recipe3.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe3.AddIngredient(ItemID.WaterBucket);
            recipe3.AddTile(TileID.DemonAltar);
            recipe3.Register();
        }
    }
}