using tsorcRevamp.Projectiles.Ranged.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    [Autoload(false)]
    public class ToxicShot : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Toxic Shot");
            Tooltip.SetDefault("Converts seeds into Toxic Shots and allows you to gather Seeds from grass" +
                "\nToxic Shots apply a short burst of venom and home into enemies" +
                "\nAlso uses all darts as ammo, but Poison Darts deal double damage" +
                "\nStops players movement for a fraction of the weapon's usetime if recently hurt, slows otherwise" +
                "\nGrants movement speed and stamina regen boost whilst being held that gets removed upon taking damage temporarily" +
                "\nPress Special Ability to gain an even higher temporary boost and remove the movement penalties" +
                "\n'That's gotta sting'");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 24;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item63;
            Item.DamageType = DamageClass.Ranged; 
            Item.damage = 20;
            Item.knockBack = 4f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.Seed;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Dart;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0f, -10f);
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.Seed)
            {
                type = ModContent.ProjectileType<ToxicShotDart>();
            }
            if (type == ProjectileID.PoisonDartBlowgun)
            {
                damage *= 2;
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
            }
            else
            if (player.itemAnimation > 14 && (!player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>())))
            {
                player.velocity *= 0.92f;
            }
            else if (player.itemAnimation > 14 && player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>()))
            {
                player.velocity *= 0.01f;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Blowpipe);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

    }
}