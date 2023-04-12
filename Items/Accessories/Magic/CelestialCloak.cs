using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Accessories.Magic
{
    [AutoloadEquip(new EquipType[]
    {
        EquipType.Back,
        EquipType.Front
    })]
    public class CelestialCloak : ModItem
    {
        public static int hitchances = 0;

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Increases max mana by 40" +
                "\nReduces mana usage by 16%" +
                "\nAutomatically use mana potions when needed" +
                "\nIncreases pickup range for mana stars" +
                "\nRestores mana when damaged" +
                "\nGrants a thorns effects scaling with max mana" +
                "\nSmall chance to spawn falling mana stars when hitting an enemy" +
                "\nTheir damage scales with max mana" +
                "\nMana stars restore mana when collected"); */

        }

        public override void SetDefaults()
        {
            Item.width = 10;
            Item.height = 12;
            Item.accessory = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.Yellow_8;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CelestialCuffs, 1);
            recipe.AddIngredient(ItemID.ManaCloak, 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void UpdateEquip(Player player)
        {
            Vector2 starvector1 = new Vector2(-40, -200) + player.Center;
            Vector2 starvector2 = new Vector2(40, -200) + player.Center;
            Vector2 starvector3 = new Vector2(0, -200) + player.Center;
            Vector2 starmove1 = new Vector2(+4, 20);
            Vector2 starmove2 = new Vector2(-4, 20);
            Vector2 starmove3 = new Vector2(0, 20);

            player.statManaMax2 += 40;
            player.manaCost -= 0.16f;
            player.manaFlower = true;
            player.manaMagnet = true;
            player.magicCuffs = true;
            player.thorns += 0.1f + (player.statManaMax2 / 50f);
            if (hitchances >= 1)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector1, starmove1, ProjectileID.ManaCloakStar, player.statManaMax2 / 5, 2f, Main.myPlayer);
                Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector2, starmove2, ProjectileID.ManaCloakStar, player.statManaMax2 / 5, 2f, Main.myPlayer);
                Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector3, starmove3, ProjectileID.ManaCloakStar, player.statManaMax2 / 5, 2f, Main.myPlayer);
                hitchances -= 1;
            }
        }

    }
}
