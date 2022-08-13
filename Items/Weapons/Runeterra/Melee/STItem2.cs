/*
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Runeterra.Melee
{
    public class STItem2: ModItem
    {
        public float cooldown = 0;
        public float attackspeedscaling;
        public float doublecritchancetimer = 0;
        public static bool doublecritchance = false;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Iron Tempest");
            Tooltip.SetDefault("Doubled crit chance" +
                "\nStabs on right click dealing 125% damage, with a 4 second cooldown, scaling down with attack speed" +
                "\nGain a stack of Steel Tempest upon stabbing an enemy" +
                "\nUpon reaching 2 stacks, the next right click will release a tornado dealing 150% damage" +
                "\nHover your mouse over an enemy and press Q hotkey on a cd to dash through the enemy");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.buyPrice(0, 30, 0, 0);
            Item.damage = 40;
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
            if (Main.mouseRight & !Main.mouseLeft & STStab2.steeltempest == 2 & cooldown <= 0)
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.damage = 30;
                Item.shoot = ModContent.ProjectileType<STNado2>();
                cooldown = ((3 / attackspeedscaling) + 1);
                STStab2.steeltempest = 0;
            } else
            if (Main.mouseRight & !Main.mouseLeft)
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.damage = 25;
                cooldown = ((3 / attackspeedscaling) + 1);
                Item.shoot = ModContent.ProjectileType<STStab2>();
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                Item.damage = 20;
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

            recipe.AddIngredient(ModContent.ItemType<STItem1>());
            recipe.AddIngredient(ItemID.HallowedBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}*/