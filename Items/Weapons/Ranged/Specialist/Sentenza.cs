using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged;

namespace tsorcRevamp.Items.Weapons.Ranged.Specialist
{
    public class Sentenza : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 70;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 52;
            Item.height = 42;
            Item.useTime = 13;
            Item.useAnimation = 13;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true; //so the item's animation doesn't do damage
            Item.knockBack = 4;
            Item.value = 250000;
            Item.scale = 0.9f;
            Item.rare = ItemRarityID.Pink;
            Item.crit = 5;
            Item.UseSound = SoundID.Item40;
            Item.shoot = ModContent.ProjectileType<SentenzaShot>();
            Item.shootSpeed = 26f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Blackhawk>());
            recipe.AddIngredient(ItemID.HallowedBar, 3);
            recipe.AddIngredient(ItemID.SoulofNight, 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, -2);
        }

        int ammoleft = 6;
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            ammoleft--;
            if (ammoleft > 0)
            {
                return true;
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    if (Main.rand.NextBool(2))
                    {
                        if (Main.myPlayer == player.whoAmI)
                        {
                            if (player.direction == 1)
                            {
                                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, new Vector2(Main.rand.NextFloat(-0.2f, -1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<ShulletBellDark>(), 0, 0, Main.myPlayer);
                            }
                            if (player.direction == -1)
                            {
                                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, new Vector2(Main.rand.NextFloat(0.2f, 1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<ShulletBellDark>(), 0, 0, Main.myPlayer);
                            }
                        }
                    }
                }

                if (Main.myPlayer == player.whoAmI)
                {
                    if (player.direction == 1)
                    {
                        Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, new Vector2(Main.rand.NextFloat(-0.2f, -1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<ShulletBellDark>(), 0, 0, Main.myPlayer);
                    }
                    if (player.direction == -1)
                    {
                        Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, new Vector2(Main.rand.NextFloat(0.2f, 1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<ShulletBellDark>(), 0, 0, Main.myPlayer);
                    }
                }

                ammoleft = 6;
                return true;
            }
        }
    }
}
