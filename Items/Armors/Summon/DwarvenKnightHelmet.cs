using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    class DwarvenKnightHelmet : ModItem
    {
        public static float Dmg = 7f;
        public static float TagStrength = 18f;
        public static float CritChance = 7f;
        public static int MinionSlots = 1;
        public static int SentrySlots = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, TagStrength, MinionSlots, SentrySlots, CritChance);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.defense = 8;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            player.GetCritChance(DamageClass.Summon) += CritChance;
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagStrength += TagStrength / 100f;
            player.maxMinions += MinionSlots;
            player.maxTurrets += SentrySlots;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetDamage(DamageClass.Summon) += Dmg / 100f;
                player.GetCritChance(DamageClass.Summon) += CritChance;
                player.GetModPlayer<tsorcRevampPlayer>().SummonTagStrength += TagStrength / 100f;
            }
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DwarvenArmor>() && legs.type == ModContent.ItemType<DwarvenGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.onHitDodge = true;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
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
            recipe2.AddIngredient(ItemID.AncientHallowedHelmet, 1);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
