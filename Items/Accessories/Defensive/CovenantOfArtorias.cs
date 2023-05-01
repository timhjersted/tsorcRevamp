using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class CovenantOfArtorias : ModItem
    {
        public override void SetStaticDefaults()
        {
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
            player.GetDamage(DamageClass.Generic) *= 1.07f;
            player.GetCritChance(DamageClass.Generic) *= 1.07f;
            player.GetCritChance(DamageClass.Generic) += 7;
            player.moveSpeed *= 1.07f;
            player.GetAttackSpeed(DamageClass.Generic) *= 1.07f;
            player.GetAttackSpeed(DamageClass.Melee) *= 1.07f;
            player.lavaImmune = true;
            player.noKnockback = true;
            player.fireWalk = true;
            player.enemySpawns = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Oiled] = true;
            player.buffImmune[ModContent.BuffType<Crippled>()] = true;
            player.buffImmune[ModContent.BuffType<DarkInferno>()] = true;
        }


    }
}
