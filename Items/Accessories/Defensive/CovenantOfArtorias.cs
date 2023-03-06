using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class CovenantOfArtorias : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Covenant of Artorias");
            /* Tooltip.SetDefault("[c/ffbf00:Allows you to safely enter The Abyss when worn. Remove the ring to escape from The Abyss!]" +
                                "\n[c/ffbf00:The Abyss pervades the entire world, like a mirror of our own, but dangerous foes are far more numerous in this realm.]" +
                                "\n[c/00ffd4:Grants immunity to particular dangers caused by the Abyss and foes empowered by it, such as Crippled, Poisoned, On Fire and Oiled]" +
                                "\nAlso grants protection from lava, knockback, and fire blocks, but increases enemy spawn rates." +
                                "\n+7% Melee speed" +
                                "\n+7% Move speed" +
                                "\n+7% Damage" +
                                "\n+7% Critical strike chance"); */
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Cyan_9;
            Item.rare = ItemRarityID.Cyan;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 17000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.07f;
            player.moveSpeed += 0.07f;
            player.GetCritChance(DamageClass.Magic) += 7;
            player.GetCritChance(DamageClass.Melee) += 7;
            player.GetCritChance(DamageClass.Ranged) += 7;
            player.lavaImmune = true;
            player.noKnockback = true;
            player.fireWalk = true;
            player.enemySpawns = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Oiled] = true;
            player.buffImmune[ModContent.BuffType<Buffs.Crippled>()] = true;
        }


    }
}
