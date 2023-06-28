using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    class DwarvenCrusaderHelmet : ModItem
    {
        public static float WhipDmg = 16f;
        public static float AtkSpeed = 8f;
        public static float TagDuration = 18f;
        public static float WhipRange = 30f;
        public static int LifeRegen = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(WhipDmg, AtkSpeed, TagDuration, WhipRange, LifeRegen);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 28;
            Item.defense = 13;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DwarvenArmor>() && legs.type == ModContent.ItemType<DwarvenGreaves>();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.SummonMeleeSpeed) += WhipDmg / 100f;
            player.GetAttackSpeed(DamageClass.Summon) += AtkSpeed / 100f;
            player.whipRangeMultiplier += WhipRange;
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration += TagDuration / 100f;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetDamage(DamageClass.SummonMeleeSpeed) += WhipDmg / 100f;
                player.GetAttackSpeed(DamageClass.Summon) += AtkSpeed / 100f;
                player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration += TagDuration / 100f;
            }
        }
        public override void UpdateArmorSet(Player player)
        {
            player.onHitDodge = true;

            player.lifeRegen += LifeRegen;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.lifeRegen += LifeRegen;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 42, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 105, Color.Gold, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedHood, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
