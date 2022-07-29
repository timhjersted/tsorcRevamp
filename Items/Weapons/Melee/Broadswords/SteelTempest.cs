using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class SteelTempest : ModItem
    {
        public float cooldown = 0;
        public float attackspeedscaling;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Doubled crit chance" +
                "\nStabs on right click, with a 4 second cooldown, scaling down with attack speed" +
                "\nGain a stack of Steel Tempest upon stabbing an enemy" +
                "\nUpon reaching 2 stacks, the next right click will release a tornado");
        }
        public override void SetDefaults()
        {
            Item.stack = 1;
            Item.rare = ItemRarityID.Green;
            Item.damage = 20;
            Item.crit = 4;
            Item.width = 52;
            Item.height = 54;
            Item.knockBack = 1f;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 22;
            Item.useTime = 22;
            Item.noUseGraphic = false;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Green_2;
            Item.shootSpeed = 4.2f;
            Item.useTurn = false;
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            attackspeedscaling = player.GetTotalAttackSpeed(DamageClass.Melee);
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit = player.GetTotalCritChance(DamageClass.Melee) * 2;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {

            if (Main.mouseRight & !Main.mouseLeft & Projectiles.Shortswords.SteelTempestProjectile.steeltempest == 2 & cooldown <= 0)
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.shoot = ProjectileID.WeatherPainShot;
                cooldown = ((3 / attackspeedscaling) + 1);
                Projectiles.Shortswords.SteelTempestProjectile.steeltempest = 0;
            } else
            if (Main.mouseRight & !Main.mouseLeft)
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                cooldown = ((3 / attackspeedscaling) + 1);
                Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.SteelTempestProjectile>();
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                Item.useTurn = false;
            }

        }
        public override void UpdateInventory(Player player)
        {
            if (Main.GameUpdateCount % 1 == 0)
            {
                cooldown -= 0.0167f;
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || cooldown <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool CanShoot(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                return true;
            } return false;
        }

        public override bool AltFunctionUse(Player player)
        {
                return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Katana, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

    }
}
