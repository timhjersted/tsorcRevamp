using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class Bolt2Tome : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bolt 2 Tome");
            Tooltip.SetDefault("A lost tome for artisans." +
                                "\nDrops a lightning strike upon collision" +
                                "\nElectrifies enemies" +
                                "\nCan be upgraded with 25,000 Dark Souls and 15 Soul of Light.");

        }

        public override void SetDefaults()
        {

            Item.damage = 17;
            Item.height = 10;
            Item.knockBack = 0.1f;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Orange;
            Item.shootSpeed = 12f;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 20;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.value = PriceByRarity.Orange_3;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt1Revamped>();
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position += velocity * 3;
        }


        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (player.wet)
            {
                player.AddBuff(BuffID.Electrified, 90);
            }
            Projectile.NewProjectile(source, position, speed, type, damage, knockBack, player.whoAmI, 0, 1);

            //Every time the player shoots, clear the "bolt chain immunity" from every NPC so they can be hit again
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].HasBuff(ModContent.BuffType<Buffs.BoltChainImmunity>()))
                {
                    Main.npc[i].DelBuff(Main.npc[i].FindBuffIndex(ModContent.BuffType<Buffs.BoltChainImmunity>()));
                }
            }
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("Bolt1Tome").Type, 1);
            recipe.AddIngredient(ItemID.JungleSpores, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
