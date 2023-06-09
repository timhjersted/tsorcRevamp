using tsorcRevamp.Projectiles.Swords.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using Terraria.DataStructures;
using tsorcRevamp.Buffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    [Autoload(true)]
    public class Nightbringer: ModItem
    {
        public int AttackSpeedScalingDuration;
        public float DashingTimer = 0f;
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Nightbringer");
            /* Tooltip.SetDefault("Thrusts on right click, cooldown scales down with attack speed" +
                "\nGain a stack of Steel Tempest upon thrusting any enemy" +
                "\nUpon reaching 2 stacks, the next right click will release a chaotic tempest" +
                "\nHover your mouse over an enemy and press Special Ability to dash through the enemy" +
                "\nThis grants you brief invulnerability and a huge melee damage boost" +
                "\nPress Shift + Special Ability to create a stationary firewall which blocks most enemy projectiles for 5 seconds" +
                "\n'Harmony is a lie told to force Obedience'"); */
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.damage = 220;
            Item.crit = 6;
            Item.width = 52;
            Item.height = 54;
            Item.knockBack = 5f;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.scale = 0.9f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useTime = 20;
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
                Vector2 MouseHitboxSize = new Vector2(1000000, 1000000);

                if (other.active & !other.friendly & other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) & other.Distance(player.Center) <= 400 & !player.HasBuff(ModContent.BuffType<NightbringerDashCooldown>()))
                {
                    if (DashingTimer > 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item104, player.Center);
                        player.velocity = UsefulFunctions.Aim(player.Center, other.Center, 15f);
                        player.AddBuff(ModContent.BuffType<Invincible>(), 2 * 60);
                        player.AddBuff(ModContent.BuffType<NightbringerDash>(), 5 * 60);
                        player.AddBuff(ModContent.BuffType<NightbringerDashCooldown>(), 20 * 60);
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
            Vector2 playerCenter = new Vector2(-13, 0);
            if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2)
            {
                Dust.NewDust(player.TopLeft + playerCenter, 50, 50, DustID.DesertTorch, Scale: 2);
            }
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.altFunctionUse == 2 && player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2)
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
            if (player.altFunctionUse == 2 && player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2)
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

            if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NightbringerThrust>(), damage, 10, player.whoAmI);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NightbringerTornado>(), damage, 10, player.whoAmI);
                player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks = 0;
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
            if (Main.GameUpdateCount % 1 == 0)
            {
                DashingTimer -= 0.0167f;
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