using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class GreyWolfRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("One of the rings worn by Artorias." +
                                "\nInherits Ring of Clarity immunities" +
                                "\nPress the Wolf Ring key to increase life regen and damage taken temporarily" +
                                "\nRemoves the life regen if hit during the effect and puts it on a long cooldown" +
                                "\n+12 defense within the Abyss" +
                                "\nGrants Magma Stone and Acid Venom imbue effect" +
                                "\nIncreases life regeneration by 4 and maximum mana by 100" +
                                "\nImbue effect can be toggled by hiding the accessory."); */
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 19;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<WolfRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BandOfPhenomenalCosmicPower>(), 1);
            recipe.AddIngredient(ModContent.ItemType<RingOfClarity>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().WolfRing = true;

            //Band of Phenomenal Cosmic Power inheritance
            player.statManaMax2 += 100;
            player.lifeRegen += 4;
            player.manaRegenBonus += 35;
            player.manaRegenDelayBonus += 1.3f;

            //Ring of Clarity inheritance
            player.GetDamage(DamageClass.Generic) += 0.03f;
            player.noKnockback = true;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Burning] = true;
            player.buffImmune[BuffID.Chilled] = true;
            player.buffImmune[BuffID.CursedInferno] = true;
            player.buffImmune[BuffID.Ichor] = true;
            player.buffImmune[BuffID.Gravitation] = true;
            player.buffImmune[ModContent.BuffType<Chilled>()] = true;

            //Wolf Ring inheritance
            if (Main.bloodMoon)
            { // Apparently this is the flag used in the Abyss?
                player.statDefense += 12;
            }

            player.magmaStone = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            if (!hideVisual) player.AddBuff(BuffID.WeaponImbueVenom, 1, false);
        }
    }
}