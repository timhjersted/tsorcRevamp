using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items
{
    class AdventurersCard : ModItem
    {
        public int ClassCounter = 1;
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
        }
        public override bool CanRightClick()
        {
            return true;
        }
        public override void RightClick(Player player)
        {
            Item.stack++;
            ClassCounter++;
            if (ClassCounter > 4)
            {
                ClassCounter = 1;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");

            tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "GenericStats", LangUtils.GetTextValue("Items.AdventurersCard.Generic",
                (int)(player.endurance * 100), 100 - (int)(100f / (100f + (player.endurance * 100f)) * 100f), (int)(player.moveSpeed * 100), player.lifeRegen / 2f, (int)(player.manaCost * 100), player.manaRegen / 2f, player.manaRegenDelayBonus,
                (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGain * 100f), (float)System.Math.Round(60f / (player.GetModPlayer<tsorcRevampPlayer>().spawnRate) * 100, 1))));

            switch (ClassCounter)
            {
                case 1:
                    {
                        tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "MeleeStats", LangUtils.GetTextValue("Items.AdventurersCard.Melee",
                            (int)(player.GetTotalDamage(DamageClass.Melee).ApplyTo(100)), (int)player.GetTotalCritChance(DamageClass.Melee), (int)(player.GetTotalAttackSpeed(DamageClass.Melee) * 100), player.GetTotalArmorPenetration(DamageClass.Melee))));
                        break;
                    }
                case 2:
                    {
                        tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "RangedStats", LangUtils.GetTextValue("Items.AdventurersCard.Ranged",
                            (int)(player.GetTotalDamage(DamageClass.Ranged).ApplyTo(100)), (float)System.Math.Round(player.GetTotalCritChance(DamageClass.Ranged), 1), (int)(player.GetTotalAttackSpeed(DamageClass.Ranged) * 100), player.GetTotalArmorPenetration(DamageClass.Ranged))));
                        break;
                    }
                case 3:
                    {
                        tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "MagicStats", LangUtils.GetTextValue("Items.AdventurersCard.Magic",
                            (int)(player.GetTotalDamage(DamageClass.Magic).ApplyTo(100)), (int)player.GetTotalCritChance(DamageClass.Magic), (int)(player.GetTotalAttackSpeed(DamageClass.Magic) * 100), player.GetTotalArmorPenetration(DamageClass.Magic))));
                        break;
                    }
                case 4:
                    {
                        tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "SummonStats", LangUtils.GetTextValue("Items.AdventurersCard.Summon",
                            (int)(player.GetTotalDamage(DamageClass.Summon).ApplyTo(100)), (int)player.GetTotalCritChance(DamageClass.Summon), (int)(player.GetTotalAttackSpeed(DamageClass.Summon) * 100), player.GetTotalArmorPenetration(DamageClass.Summon),
                            (int)(player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(100)), player.maxMinions, player.maxTurrets, (int)(player.whipRangeMultiplier * 100f))));
                        break;
                    }
                case 5:
                    {
                        tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "ThrowingStats", LangUtils.GetTextValue("Items.AdventurersCard.Throwing",
                            (int)(player.GetTotalDamage(DamageClass.Throwing).ApplyTo(100)), (int)player.GetTotalCritChance(DamageClass.Throwing), (int)(player.GetTotalAttackSpeed(DamageClass.Throwing) * 100), player.GetTotalArmorPenetration(DamageClass.Throwing), player.ThrownVelocity)));
                        break;
                    }
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25);
            recipe.AddIngredient(ItemID.Book, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register(); 

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 25);
            recipe2.AddIngredient(ItemID.SpellTome, 1);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();
        }
    }
}