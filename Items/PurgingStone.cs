using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items
{
    class PurgingStone : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 38;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.maxStack = 10;
            //item.healLife = 500;
            Item.consumable = true;
            Item.scale = 1;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.Pink_5;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool CanUseItem(Player player)
        {

            if ((player.GetModPlayer<tsorcRevampPlayer>().cursePoints > 0) || player.HasBuff(ModContent.BuffType<CurseBuildup>()) || player.HasBuff(ModContent.BuffType<PowerfulCurseBuildup>()))
            {
                if (!player.HasBuff(BuffID.PotionSickness))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool? UseItem(Player player)
        {
            int restore = player.GetModPlayer<tsorcRevampPlayer>().cursePoints;
            player.GetModPlayer<tsorcRevampPlayer>().cursePoints = 0;
            if (Main.myPlayer == player.whoAmI)
            {
                player.HealEffect(restore, true);
            }

            player.AddBuff(BuffID.PotionSickness, 10800);
            if (Main.myPlayer == player.whoAmI)
            {
                player.HealEffect(500, true);
            }
            player.statLife += 500;

            int buffIndex = 0;

            foreach (int buffType in player.buffType)
            {

                if (buffType == ModContent.BuffType<CurseBuildup>() || buffType == ModContent.BuffType<PowerfulCurseBuildup>())
                {
                    player.DelBuff(buffIndex);
                    player.GetModPlayer<tsorcRevampPlayer>().CurseLevel = 0;
                    player.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel = 0;
                }
                buffIndex++;
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            int totalLife = player.statLifeMax + player.GetModPlayer<tsorcRevampPlayer>().cursePoints;
            tooltips.Insert(11, new TooltipLine(Mod, "CurrentMax", Language.GetTextValue("Mods.tsorcRevamp.Items.PurgingStone.Record") + totalLife));

        }
    }
}
