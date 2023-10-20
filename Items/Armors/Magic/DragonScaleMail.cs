using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [LegacyName("AncientDragonScaleMail")]
    [AutoloadEquip(EquipType.Body)]
    public class DragonScaleMail : ModItem
    {
        public static float Dmg = 22f;
        public static float Thorns = 100f;
        public static int ManaRegen = 6;
        public static float LifeThreshold = 25f;
        public static float SetBonusDmgCritChance = 11f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, Thorns, ManaRegen, LifeThreshold, SetBonusDmgCritChance, tsorcRevampPlayer.MythrilOcrichalcumCritDmg);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += Dmg / 100f;
            player.thorns += Thorns / 100f;
            player.manaRegenBonus += ManaRegen;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<DragonScaleHelmet>() && legs.type == ModContent.ItemType<DragonScaleGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MythrilOrichalcumCritDamage = true;
            if (player.statLife <= (player.statLifeMax2 * LifeThreshold / 100f))
            {
                player.GetCritChance(DamageClass.Magic) += SetBonusDmgCritChance;
                player.GetDamage(DamageClass.Magic) += SetBonusDmgCritChance / 100f;
                player.manaRegenBonus += ManaRegen;
                player.starCloakItem = new Item(ItemID.StarCloak); ;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 65, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Blue, 2.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilChainmail);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.MythrilChainmail);
            recipe2.AddIngredient(ItemID.OrichalcumBreastplate);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
