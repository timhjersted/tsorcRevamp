using tsorcRevamp.Projectiles.Ranged.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    public class AlienRifle : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Alien Rifle");
            Tooltip.SetDefault("Converts seeds into Alien Lasers and allows you to gather Seeds from grass" +
                "\nAlien Lasers apply a short burst of Electrified and home into enemies" +
                "\nAlso uses all darts as ammo" +
                "\nStops players movement for a fraction of the weapon's usetime if recently hurt, slows otherwise" +
                "\nGrants movement speed and stamina regen boost whilst being held that gets removed upon taking damage temporarily" +
                "\nPress Special Ability to gain an even higher temporary boost and remove the movement penalties" +
                "\nRight click to shoot a homing blinding shot which inflicts confusion" +
                "\n'Armed and ready'");
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
            Item.damage = 60;
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
            if (player.altFunctionUse == 2)
            {
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