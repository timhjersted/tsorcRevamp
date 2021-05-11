using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class PhazonRifle : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Three round burst" +
                                "\nBeware; Phazon is EXTREMELY toxic.");
        }
        public override void SetDefaults() {
            item.width = 50;
            item.height = 18;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 12;
            item.useTime = 4;
            item.maxStack = 1;
            item.damage = 25;
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.knockBack = 1f; //It deserves at least a little KB
            if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.knockBack = 0f;
            item.autoReuse = true;
            item.UseSound = SoundID.Item31;
            item.rare = ItemRarityID.LightRed;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 7;
            item.useAmmo = 14;
            item.noMelee = true;
            item.value = 300000;
            item.ranged = true;
            item.reuseDelay = 11;
            item.useAmmo = AmmoID.Bullet;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
            {
                tooltips.Insert(4, new TooltipLine(mod, "", "Only the first shot consumes ammo"));
            }

        }

        public override bool ConsumeAmmo(Player player)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
            {
                return !(player.itemAnimation < item.useAnimation - 2); //consume 1 ammo instead of 3
            }

            return true;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-8, 0);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ClockworkAssaultRifle);
            recipe.AddIngredient(ItemID.MeteoriteBar, 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
