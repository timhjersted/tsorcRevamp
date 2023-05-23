using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic
{
    [LegacyName("AncientDragonScaleMail")]
    [AutoloadEquip(EquipType.Body)]
    public class DragonScaleMail : ModItem
    {
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
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<DragonScaleHelmet>() && legs.type == ModContent.ItemType<DragonScaleGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.manaRegenBonus += 6;
            if (player.statLife <= (player.statLifeMax2 / 4))
            {
                player.GetCritChance(DamageClass.Magic) += 11;
                player.GetDamage(DamageClass.Magic) += 0.11f;
                player.manaRegenBonus += 6;
                player.starCloakItem = new Item(ItemID.StarCloak); ;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 65, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Blue, 2.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += 0.22f;
            player.thorns += 1f;
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
