
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Runeterra.Melee
{
    public class STItem3: ModItem
    {
        public float cooldown = 0;
        public static float dashCD = 0f;
        public static float dashTimer = 0f;
        public static float wallCD = 0f;
        public float attackspeedscaling;
        public float doublecritchancetimer = 0;
        public static bool doublecritchance = false;
        public float invincibility = 0f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Nightbringer");
            Tooltip.SetDefault("Doubled crit chance" +
                "\nStabs on right click dealing 125% damage, with a 4 second cooldown, scaling down with attack speed" +
                "\nGain a stack of Steel Tempest upon stabbing any enemy" +
                "\nUpon reaching 2 stacks, the next right click will release a devastating tornado dealing 175% damage" +
                "\nHover your mouse over an enemy and press Q hotkey on a cd to dash through the enemy" +
                "\nPress Q hotkey to create a stationary windwall which blocks all enemy projectiles for 5 seconds on a long cooldown" +
                "\nStabbing an enemy refunds some of these cooldown, the tornado refunds more");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.damage = 120;
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
            if (Main.mouseRight & !Main.mouseLeft & STStab3.steeltempest == 2 & cooldown <= 0)
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.shoot = ModContent.ProjectileType<STNado3>();
                cooldown = ((3 / attackspeedscaling) + 1);
                STStab3.steeltempest = 0;
            } else
            if (Main.mouseRight & !Main.mouseLeft)
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                cooldown = ((3 / attackspeedscaling) + 1);
                Item.shoot = ModContent.ProjectileType<STStab3>();
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
            }

        }
        public override void HoldItem(Player player)
        {
            doublecritchancetimer = 0.1f;
            doublecritchance = true;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];

                if (other.active & !other.friendly & other.Distance(Main.MouseWorld) <= 15 & other.Distance(player.Center) <= 10000 & (doublecritchance)/*& dashCD <= 0*/)
                {
                    if (dashTimer > 0)
                    {
                        player.velocity = UsefulFunctions.GenerateTargetingVector(player.Center, other.Center, 15f);
                        invincibility = 1f;
                    }
                    break;
                }
            }
            if (dashTimer > 0)
            {
                player.immune = true;
            }
        }
        public override void UpdateInventory(Player player)
        {
            if (invincibility > 0f)
            {
                player.immune = true;
            }
            if (Main.GameUpdateCount % 1 == 0)
            {
                cooldown -= 0.0167f;
                doublecritchancetimer -= 0.0167f;
                dashCD -= 0.0167f;
                dashTimer -= 0.0167f;
                wallCD -= 0.0167f;
                invincibility -= 0.0167f;
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

            recipe.AddIngredient(ModContent.ItemType<STItem2>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}