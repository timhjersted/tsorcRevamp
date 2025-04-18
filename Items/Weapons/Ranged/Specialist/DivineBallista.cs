using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Weapons.Ranged.Specialist
{
    class DivineBallista : ModItem
    {
        private int shotCount = 0;
        private int burstCounter = 0;
        private float burstTimer = 0;
        private bool inBurst = false;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 78;
            Item.knockBack = 4;
            Item.crit = 10;
            Item.shootSpeed = 11;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.width = 76;
            Item.height = 32;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.useAmmo = ModContent.ItemType<Bolt>();
            Item.UseSound = SoundID.Item98;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Lime_7;
            Item.rare = ItemRarityID.Lime;
            Item.autoReuse = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SniperCrossbow>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(ItemID.HallowedBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            shotCount++;

            if (shotCount >= 3)
            {
                shotCount = 0;
                inBurst = true;
                burstCounter = 0;
                burstTimer = 0;
                return false;
            }
            else
            {
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                Vector2 higherPosition = position + new Vector2(0, -10);
                Projectile.NewProjectile(source, higherPosition, velocity, type, damage, knockback, player.whoAmI);
                return false;
            }
        }

        public override void HoldItem(Player player)
        {
            if (player.HeldItem.type != Item.type)
            {
                shotCount = 0;
                inBurst = false;
                burstCounter = 0;
                burstTimer = 0;
            }

            if (inBurst)
            {
                burstTimer++;
                if (burstTimer >= 4)
                {
                    if (burstCounter < 4)
                    {
                        Vector2 position = player.Center;
                        Vector2 mousePos = Main.MouseWorld;
                        Vector2 direction = mousePos - position;
                        float angle = direction.ToRotation();
                        Vector2 velocity = new Vector2(Item.shootSpeed, 0).RotatedBy(angle);
                        Vector2 offset = new Vector2(0, -10 + burstCounter * 5);

                        Projectile.NewProjectile(player.GetSource_ItemUse(Item), position + offset, velocity, ModContent.ProjectileType<DivineBoltProjectile>(), Item.damage, Item.knockBack, player.whoAmI);

                        if (direction.X > 0)
                        {
                            player.direction = 1;
                            player.itemRotation = angle;
                        }
                        else
                        {
                            player.direction = -1;
                            player.itemRotation = angle + MathHelper.Pi;
                        }

                        burstCounter++;
                        burstTimer = 0;
                    }
                    else
                    {
                        inBurst = false;
                    }
                }
            }
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(2f, -2f);
        }
    }
}