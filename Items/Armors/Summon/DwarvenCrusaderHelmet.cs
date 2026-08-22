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
        public const float WhipDmg = 16f;
        public const float AtkSpeed = 8f;
        public const float TagDuration = 8f;
        public const float CritChance = 7f;
        public const float WhipRange = 30f;
        public const float LifeRegen = 3f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(WhipDmg, AtkSpeed, TagDuration, WhipRange, LifeRegen / 2f, CritChance);
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
            player.whipRangeMultiplier += WhipRange / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration += TagDuration / 100f;
            player.GetCritChance(DamageClass.Summon) += CritChance;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetDamage(DamageClass.SummonMeleeSpeed) += WhipDmg / 100f;
                player.GetAttackSpeed(DamageClass.Summon) += AtkSpeed / 100f;
                player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration += TagDuration / 100f;
                player.GetCritChance(DamageClass.Summon) += CritChance;
            }
        }
        public override void UpdateArmorSet(Player player)
        {
            player.onHitDodge = true;

            player.lifeRegen += (int)LifeRegen;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.lifeRegen += (int)LifeRegen;

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


            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AncientHallowedHood, 1);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
