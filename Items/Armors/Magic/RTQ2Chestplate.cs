using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using Terraria.Localization;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Body)]
    public class RTQ2Chestplate : ModItem
    {
        public static float CritChance = 7f;
        public static float AtkSpeed = 10f;
        public static float LifeThreshold = 40f;
        public static int Defense = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance, AtkSpeed, LifeThreshold, Defense);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += CritChance;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<RTQ2Helmet>() && legs.type == ModContent.ItemType<RTQ2Leggings>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Magic) += AtkSpeed;
            player.spaceGun = true;

            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Red, 1.0f);
            Main.dust[dust].noGravity = true;

            if ((player.statLife <= (player.statLifeMax2 * LifeThreshold / 100f)))
            {
                player.statDefense += Defense;
                int dust2 = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 3), player.velocity.Y, 100, Color.Red, 3.0f);
                Main.dust[dust2].noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MeteorSuit, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
