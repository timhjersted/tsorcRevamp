using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using Terraria.DataStructures;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Runeterra;
using Terraria.Audio;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Terraria.Localization;
using Humanizer;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    public class SteelTempest: ModItem
    {
        public int SwingSoundStyle = 0;
        public float SwingSoundVolume = 0.15f;
        public const int CritChance = 6;
        public int AttackSpeedScalingDuration = 240;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.damage = 20;
            Item.crit = CritChance;
            Item.width = 86;
            Item.height = 82;
            Item.scale = 0.7f;
            Item.knockBack = 3.5f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shootSpeed = 5f;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit *= 2;
            if (player.GetModPlayer<tsorcRevampPlayer>().SmoughAttackSpeedReduction)
            {
                crit -= 100;
            }
        }
        public override void HoldItem(Player player)
        {
            AttackSpeedScalingDuration = (int)(4 / player.GetTotalAttackSpeed(DamageClass.Melee) * 60); //3 seconds divided by player's melee speed
            if (AttackSpeedScalingDuration <= 80)
            {
                AttackSpeedScalingDuration = 80; //1.33 seconds minimum
            }
            Vector2 playerCenter = new Vector2(-13, 0);
            if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && player.ownedProjectileCounts[ModContent.ProjectileType<SteelTempestTornado>()] < 1)
            {
                Dust.NewDust(player.TopLeft + playerCenter, 50, 50, DustID.Smoke, Scale: 1);
            }
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (SwingSoundStyle == 1) //Shoot will always run before this can occur, so they have to be incremented by 1
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/SwingHit1") with { Volume = SwingSoundVolume });
            }
            else
            if (SwingSoundStyle == 2)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/SwingHit2") with { Volume = SwingSoundVolume });
            }
            else
            if (SwingSoundStyle == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/SwingHit3") with { Volume = SwingSoundVolume });
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2) //shoot Nothing
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                if (SwingSoundStyle == 0)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/Swing1") with { Volume = SwingSoundVolume });
                    SwingSoundStyle += 1;
                }
                else
                if (SwingSoundStyle == 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/Swing2") with { Volume = SwingSoundVolume });
                    SwingSoundStyle += 1;
                }
                else
                if (SwingSoundStyle == 2)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/Swing3") with { Volume = SwingSoundVolume });
                    SwingSoundStyle = 0;
                }
                return true;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2)
            {
                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                player.AddBuff(ModContent.BuffType<SteelTempestThrustCooldown>(), AttackSpeedScalingDuration);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/Thrust") with { Volume = SwingSoundVolume });
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<SteelTempestThrust>(), damage, 7, player.whoAmI);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2)
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                player.AddBuff(ModContent.BuffType<SteelTempestThrustCooldown>(), AttackSpeedScalingDuration);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/TornadoCast") with { Volume = SwingSoundVolume });
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<SteelTempestTornado>(), damage, 7, player.whoAmI);
            }
            return true;
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<SteelTempestThrustCooldown>()))
                return true;
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", Language.GetTextValue("Mods.tsorcRevamp.Items.SteelTempest.Details").FormatWith((float)AttackSpeedScalingDuration / 60f)));
                }
            } else
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Shift", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.Details")));
                }
            }
            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SmoughAttackSpeedReduction)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", Language.GetTextValue("Mods.tsorcRevamp.Items.SteelTempest.SmoughBalance")));
                }
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Katana);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}