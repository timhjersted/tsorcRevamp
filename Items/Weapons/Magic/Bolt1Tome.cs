using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class Bolt1Tome : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bolt 1 Tome");
            Tooltip.SetDefault("A lost beginner's tome" +
                                "\nDrops a small lightning strike upon collision" +
                                "\nHas a chance to electrify enemies" +
                                "\nCan be upgraded");

        }

        public override void SetDefaults()
        {
            Item.damage = 12;
            Item.height = 10;
            Item.knockBack = 0.1f;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Blue;
            Item.shootSpeed = 6f;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 9;
            Item.useAnimation = 27;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 27;
            Item.value = PriceByRarity.Blue_1;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt1Revamped>();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position += velocity * 6;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (player.wet)
            {
                player.AddBuff(BuffID.Electrified, 90);
            }
            Projectile.NewProjectile(source, position, speed, type, damage, knockBack, player.whoAmI, 0, 0);

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
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
