using tsorcRevamp.Projectiles.Swords.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra;
using Terraria.DataStructures;

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
                "\nThrusts on right click dealing 150% damage, cooldown scales down with attack speed" +
                "\nGain a stack of Steel Tempest upon thrusting any enemy" +
                "\nUpon reaching 2 stacks, the next right click will release a chaotic tornado dealing 250% damage" +
                "\nHover your mouse over an enemy and press Special Ability to dash through the enemy" +
                "\nPress Special Ability to create a stationary windwall which blocks most enemy projectiles for 4 seconds");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.damage = 180;
            Item.crit = 6;
            Item.width = 52;
            Item.height = 54;
            Item.knockBack = 5f;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.scale = 0.9f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.noUseGraphic = false;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shootSpeed = 5f;
            Item.useTurn = false;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
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
            if (Main.mouseLeft)
            {
                //player.altFunctionUse = 1;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                //Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            }
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.altFunctionUse == 2 && player.GetModPlayer<tsorcRevampPlayer>().steeltempest >= 2)
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                //Item.shoot = ModContent.ProjectileType<SteelTempestTornado>();
                player.AddBuff(ModContent.BuffType<NightbringerThrustCooldown>(), AttackSpeedScalingDuration);
                //player.GetModPlayer<tsorcRevampPlayer>().steeltempest = 0;
                //player.altFunctionUse = 1;
            }
            else
            if (player.altFunctionUse == 2 && player.GetModPlayer<tsorcRevampPlayer>().steeltempest < 2)
            {

                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                player.AddBuff(ModContent.BuffType<NightbringerThrustCooldown>(), AttackSpeedScalingDuration);
                //Item.shoot = ModContent.ProjectileType<SteelTempestThrust>();
                //player.altFunctionUse = 1;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2) //shoot Nothing
            {
                return true;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().steeltempest < 2)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NightbringerThrust>(), damage, 10, player.whoAmI);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().steeltempest >= 2)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NightbringerTornado>(), damage, 10, player.whoAmI);
                player.GetModPlayer<tsorcRevampPlayer>().steeltempest = 0;
            }
            return false;

            /*if (Main.mouseRight & !Main.mouseLeft & player.GetModPlayer<tsorcRevampPlayer>().steeltempest >= 2 & !player.HasBuff(ModContent.BuffType<SteelTempestThrustCooldown>()))
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                Item.shoot = ModContent.ProjectileType<SteelTempestTornado>();
                player.AddBuff(ModContent.BuffType<SteelTempestThrustCooldown>(), AttackSpeedScalingDuration);
                player.GetModPlayer<tsorcRevampPlayer>().steeltempest = 0;
            }
            else
            if (Main.mouseRight & !Main.mouseLeft & player.altFunctionUse == 2 & !player.HasBuff(ModContent.BuffType<SteelTempestThrustCooldown>()))
            {
                player.altFunctionUse = 2;
                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                player.AddBuff(ModContent.BuffType<SteelTempestThrustCooldown>(), AttackSpeedScalingDuration);
                Item.shoot = ModContent.ProjectileType<SteelTempestThrust>();
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                Item.useTurn = false;
                Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            }*/
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

        /*public override bool CanUseItem(Player player)
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

        public override bool CanShoot(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                return true;
            } return false;
        }*/

        public override bool AltFunctionUse(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<NightbringerThrustCooldown>()))
                return true;
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<PlasmaWhirlwind>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}