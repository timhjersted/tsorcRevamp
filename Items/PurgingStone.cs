using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class PurgingStone : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Ash-colored stone encasing a skull" +
                                "\nSecret treasure of Arstor, the Earl of Carim" +
                                "\nAbsorbs curse build-up and breaks curse, restoring your max HP to 500" +
                                "\nCan not be used while potion sickness is active, and inflicts it" +
                                "\nHumans are helpless against curses, and can only redirect their influence" +
                                "\nThe Purging Stone does not dispel curses, but receives them as a surrogate" +
                                "\nThe stone itself was once a person or some other being" +
                                "\nWon't increase max HP over the maximum achieved " +
                                "\nvia Life Crystals or Life Fruit");
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
            recipe.AddIngredient(Mod.Find<ModItem>("RedTitanite").Type, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("WhiteTitanite").Type, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("BlueTitanite").Type, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("CursedSoul").Type, 30);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool CanUseItem(Player player)
        {

            if ((player.statLifeMax < player.GetModPlayer<tsorcRevampPlayer>().MaxAcquiredHP) || player.HasBuff(ModContent.BuffType<Buffs.CurseBuildup>()) || player.HasBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>()))
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
            if (player.statLifeMax < player.GetModPlayer<tsorcRevampPlayer>().MaxAcquiredHP)
            {
                player.statLifeMax = player.GetModPlayer<tsorcRevampPlayer>().MaxAcquiredHP;
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

                if (buffType == ModContent.BuffType<Buffs.CurseBuildup>() || buffType == ModContent.BuffType<Buffs.PowerfulCurseBuildup>())
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

            tooltips.Insert(11, new TooltipLine(Mod, "", $"Current max: {(player.GetModPlayer<tsorcRevampPlayer>().MaxAcquiredHP)}"));

        }
    }
}
