
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Runeterra.Melee
{
    public class STItem1: ModItem
    {
        public float cooldown = 0;
        public float attackspeedscaling;
        public float doublecritchancetimer = 0;
        public static bool doublecritchance = false;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Steel Tempest");
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
            doublecritchancetimer = 0.5f;
            if (Main.mouseRight & !Main.mouseLeft & STStab1.steeltempest == 2 & cooldown <= 0)
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.shoot = ModContent.ProjectileType<STNado1>();
                cooldown = ((3 / attackspeedscaling) + 1);
                STStab1.steeltempest = 0;
            } else
            if (Main.mouseRight & !Main.mouseLeft)
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                cooldown = ((3 / attackspeedscaling) + 1);
                Item.shoot = ModContent.ProjectileType<STStab1>();
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
        public override void HoldItem(Player player)
        {
            doublecritchancetimer = 0.1f;
            doublecritchance = true;

        }
        public override void UpdateInventory(Player player)
        {   
            if (Main.GameUpdateCount % 1 == 0)
            {
                cooldown -= 0.0167f;
            }
            if (Main.GameUpdateCount % 1 == 0)
            {
                doublecritchancetimer -= 0.0167f;
            }
            if (doublecritchancetimer <= 0)
            {
                doublecritchance = false;
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
