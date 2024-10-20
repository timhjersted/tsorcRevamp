using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Summon.Whips.TerraFall;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    [LegacyName("TerraFall")]
    public class TerraFallItem : ModItem
    {
        public const int BaseDamage = 85;
        public const int MaxStacks = 4;
        public const int TagDmg = 20;
        public const int TagCrit = 12;
        //public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDmg, TagCrit, MaxStacks);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
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


            Item.shoot = ModContent.ProjectileType<TerraFallProjectile>();
            Item.shootSpeed = 4;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "TagDmg", Language.GetTextValue(Tooltip.Key + "0", (int)(TagDmg * modPlayer.SummonTagStrength), (int)(TagCrit * modPlayer.SummonTagStrength), MaxStacks)));
            }
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
            recipe.AddIngredient(ModContent.ItemType<NightsCrackerItem>());
            recipe.AddIngredient(ItemID.SwordWhip);
            recipe.AddIngredient(ItemID.RainbowWhip);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 115000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}