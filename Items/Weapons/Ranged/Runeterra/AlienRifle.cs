using tsorcRevamp.Projectiles.Ranged.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    [Autoload(true)]
    public class AlienRifle : ModItem
    {
        public const int BaseDamage = 60;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 24;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.buyPrice(0, 30, 0, 0);
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item157;
            Item.DamageType = DamageClass.Ranged; 
            Item.damage = BaseDamage;
            Item.knockBack = 5f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.Seed;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Dart;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0f, -9f);
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.Seed & player.altFunctionUse == 1)
            {
                type = ModContent.ProjectileType<AlienLaser>();
            }
            if (type == ProjectileID.PoisonDartBlowgun)
            {
                damage *= 2;
            }
            if (player.altFunctionUse == 2)
            {
                if (type == ProjectileID.Seed)
                {
                    damage *= 2;
                }
                type = ModContent.ProjectileType<AlienBlindingLaser>();
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(ModContent.BuffType<AlienBlindingLaserCooldown>())) //cooldown gets applied on projectile spawn
            {
                player.altFunctionUse = 2;
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
            }
        }
        public override void HoldItem(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>()))
            {
                if (!player.HasBuff(ModContent.BuffType<ScoutsBoost2>()))
                {
                    player.AddBuff(ModContent.BuffType<ScoutsBoost>(), 1);
                }
            }
            if (player.HasBuff(ModContent.BuffType<ScoutsBoost2>()))
            {
                //nothing
            } else 
            if (player.itemAnimation > 14 && (!player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>())))
            {
                player.velocity *= 0.92f;
            }
            else if (player.itemAnimation > 14 && player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>()))
            {
                player.velocity *= 0.01f;
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || !player.HasBuff(ModContent.BuffType<AlienBlindingLaserCooldown>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<ToxicShot>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 11);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}