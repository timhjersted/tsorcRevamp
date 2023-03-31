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
            Tooltip.SetDefault("One of the rings worn by Artorias." +
                                "\nInherits Ring of Clarity effects" +
                                "\nPress Shift and Special Ability to increase life regen and damage taken temporarily" +
                                "\n+12 defense within the Abyss" +
                                "\nGrants Magma Stone and Acid Venom imbue effect" +
                                "\n+6 life Regen. +120 Mana." +
                                "\nImbue effect can be toggled by hiding the accessory.");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 19;
            Item.lifeRegen = 4;
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
            player.statManaMax2 += 120;
            player.lifeRegen += 6; //3 from band 3 from ring

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