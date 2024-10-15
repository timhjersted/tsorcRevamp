using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    public class AncientDemonArmor : ModItem
    {
        public static float WhipDmg = 16f;
        public static float TagDuration = 12f;
        public static float WhipRange = 30f;
        public static float AtkSpeed = 16f;
        public static float LifeThreshold = 50f;
        public const int ExplosionBaseDmg = 20;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(WhipDmg, TagDuration, WhipRange, AtkSpeed, LifeThreshold);
        public override void SetStaticDefaults()
        {
            ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.SummonMeleeSpeed) += WhipDmg / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration += TagDuration / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<AncientDemonHelmet>() && legs.type == ModContent.ItemType<AncientDemonGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.whipRangeMultiplier += WhipRange / 100f;
            player.GetAttackSpeed(DamageClass.Summon) += AtkSpeed / 100f;

            if (player.statLife <= (player.statLifeMax2 * LifeThreshold / 100f))
            {
                player.GetModPlayer<tsorcRevampPlayer>().DemonPower = true;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 6, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Green, 1.0f);
                Main.dust[dust].noGravity = true;

            }
        }

        public override void DrawArmorColor(Player drawPlayer, float shadow, ref Color color, ref int glowMask, ref Color glowMaskColor)
        {
            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(new Vector2((float)drawPlayer.position.X, (float)drawPlayer.position.Y), drawPlayer.width, drawPlayer.height, 6, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, Color.Yellow, 1.0f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = false;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ObsidianShirt);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
