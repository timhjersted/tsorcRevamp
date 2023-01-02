using tsorcRevamp.Projectiles.Swords.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    public class Nightbringer: ModItem
    {
        public int AttackSpeedScalingDuration;
        public float DashingTimer = 0f;
        public float Invincibility = 0f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Nightbringer");
            Tooltip.SetDefault("Doubled crit chance scaling" +
                "\nThrusts on right click dealing 125% damage, cooldown scales down with attack speed" +
                "\nGain a stack of Steel Tempest upon thrusting any enemy" +
                "\nUpon reaching 2 stacks, the next right click will release a chaotic tornado dealing double damage" +
                "\nHover your mouse over an enemy and press Special Ability to dash through the enemy" +
                "\nPress Special Ability to create a stationary windwall which blocks most enemy projectiles for 4 seconds");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.damage = 120;
            Item.crit = 6;
            Item.width = 52;
            Item.height = 54;
            Item.knockBack = 1f;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.scale = 2f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.noUseGraphic = false;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shootSpeed = 4.2f;
            Item.useTurn = false;
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit *= 2;
        }
        public override void HoldItem(Player player)
        {
            AttackSpeedScalingDuration = (int)(3 / player.GetTotalAttackSpeed(DamageClass.Melee) * 60); //3 seconds divided by player's melee speed
            if (AttackSpeedScalingDuration <= 80)
            {
                AttackSpeedScalingDuration = 80; //1.33 seconds minimum
            }
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];

                if (other.active & !other.friendly & other.Distance(Main.MouseWorld) <= 25 & other.Distance(player.Center) <= 10000 & !player.HasBuff(ModContent.BuffType<NightbringerDashCooldown>()))
                {
                    if (DashingTimer > 0)
                    {
                        player.velocity = UsefulFunctions.GenerateTargetingVector(player.Center, other.Center, 15f);
                        Invincibility = 1f;
                        player.AddBuff(ModContent.BuffType<NightbringerDashCooldown>(), 30 * 60);
                    }
                    break;
                }
            }
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft & player.GetModPlayer<tsorcRevampPlayer>().steeltempest >= 2 & !player.HasBuff(ModContent.BuffType<NightbringerThrustCooldown>()))
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                Item.shoot = ModContent.ProjectileType<NightbringerTornado>();
                player.AddBuff(ModContent.BuffType<NightbringerThrustCooldown>(), AttackSpeedScalingDuration);
                player.GetModPlayer<tsorcRevampPlayer>().steeltempest = 0;
            } else
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(ModContent.BuffType<NightbringerThrustCooldown>()))
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                player.AddBuff(ModContent.BuffType<NightbringerThrustCooldown>(), AttackSpeedScalingDuration);
                Item.shoot = ModContent.ProjectileType<NightbringerThrust>();
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            }

        }
        public override void UpdateInventory(Player player)
        {
            if (Invincibility > 0f)
            {
                player.immune = true;
            }
            if (Main.GameUpdateCount % 1 == 0)
            {
                DashingTimer -= 0.0167f;
                Invincibility -= 0.0167f;
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || !player.HasBuff(ModContent.BuffType<NightbringerThrustCooldown>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /*public override bool CanShoot(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                return true;
            } return false;
        }*/

        public override bool AltFunctionUse(Player player)
        {
                return true;
        }
        /*
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<PlasmaWhirlwind>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }*/
    }
}