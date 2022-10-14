using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class GreyWolfRing : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Accessories/Defensive/WolfRing";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("One of the rings worn by Artorias." +
                                "\nInherits Ring of Clarity effects" +
                                "\n+22 defense within the Abyss, +10 defense otherwise" +
                                "\nGrants Magma Stone and Fire Flask effect" +
                                "\n+4 HP Regen. +120 Mana.");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 10;
            Item.lifeRegen = 4;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("WolfRing").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("BandOfSupremeCosmicPower").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<RingOfClarity>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.magmaStone = true;
            player.statManaMax2 += 120;
            player.AddBuff(BuffID.WeaponImbueFire, 60, false);

            player.GetDamage(DamageClass.Generic) += 0.03f;
            player.noKnockback = true;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Burning] = true;
            player.buffImmune[BuffID.Chilled] = true;
            player.buffImmune[BuffID.CursedInferno] = true;
            player.buffImmune[BuffID.BrokenArmor] = true;
            player.buffImmune[BuffID.Ichor] = true;
            player.buffImmune[BuffID.Slow] = true;
            player.buffImmune[BuffID.Confused] = true;
            player.buffImmune[BuffID.Gravitation] = true;
            player.buffImmune[ModContent.BuffType<Buffs.Chilled>()] = true;

            player.lifeRegen += 4;
            player.statDefense += 9;

            if (Main.bloodMoon)
            { // Apparently this is the flag used in the Abyss?
                player.statDefense += 12;
            }
        }

    }
}