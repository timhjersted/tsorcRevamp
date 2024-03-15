using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee.Runeterra;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    public abstract class RuneterraKatanaItem : ModItem
    {
        public int SwingSoundStyle = 0;
        public const int BaseCritChanceBonus = 6;
        public int AttackSpeedScalingDuration = 240;

        //Plasma Wirlwind specific
        public const float DashDuration = 0.2f;
        public const int DashCooldown = 6;
        public static float PercentHealthDamage = 0.1f;
        public static int HealthDamageCap = 450;
        public abstract int DashCooldownBuffID { get; }
        public abstract int DashDustID { get; }
        public abstract int DashBuffID { get; }
        public abstract int SpinProjectileID { get; }

        //Nightbringer specific
        public const int WindwallDuration = 5;
        public const int WindwallCooldown = 30;

        public abstract int Tier { get; }
        public abstract float SwingSoundVolume { get; }
        public abstract int RarityID { get; }
        public abstract int Value { get; }
        public abstract int BaseDamage { get; }
        public abstract int ItemWidth { get; }
        public abstract int ItemHeight { get; }
        public abstract float ItemScale { get; }
        public abstract float ItemKnockback { get; }
        public abstract Color SlashColor { get; }
        public abstract int TornadoReadyDustID { get; }
        public abstract string SoundPath { get; }
        public abstract int ThrustCooldownBuffID { get; }
        public abstract int ThrustProjectileID { get; }
        public abstract string LocalizationPath { get; }
        public override void SetDefaults()
        {
            Item.rare = RarityID;
            Item.value = Value;
            Item.damage = BaseDamage;
            Item.crit = BaseCritChanceBonus;
            Item.width = ItemWidth;
            Item.height = ItemHeight;
            Item.scale = ItemScale;
            Item.knockBack = ItemKnockback;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shootSpeed = 5f;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = SlashColor;
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
            if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && player.ownedProjectileCounts[ModContent.ProjectileType<RuneterraKatanaTornado>()] < 1)
            {
                Dust TornadoReady = Dust.NewDustDirect(player.VisualPosition, player.width, player.height, TornadoReadyDustID);
                if (Tier == 3)
                {
                    TornadoReady.scale = 2;
                }
            }
            if (Tier > 1)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC other = Main.npc[i];

                    if (other.active && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, player.GetModPlayer<tsorcRevampPlayer>().MouseHitboxSize)) & other.Distance(player.Center) <= 400 && !other.HasBuff(DashCooldownBuffID))
                    {
                        UsefulFunctions.DustRing(other.Center, other.width / 2, DashDustID, 5, 2);
                    }
                }
            }
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            switch (SwingSoundStyle)//Shoot will always run before this can occur, so they have to be incremented by 1
            {
                case 1:
                    {
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "SwingHit1") with { Volume = SwingSoundVolume });
                        break;
                    }
                case 2:
                    {
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "SwingHit2") with { Volume = SwingSoundVolume });
                        break;
                    }
                case 3:
                    {
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "SwingHit3") with { Volume = SwingSoundVolume });
                        break;
                    }
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2) //shoot Nothing
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                switch (SwingSoundStyle)
                {
                    case 0:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing1") with { Volume = SwingSoundVolume });
                            SwingSoundStyle += 1;
                            break;
                        }
                    case 1:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing2") with { Volume = SwingSoundVolume });
                            SwingSoundStyle += 1;
                            break;
                        }
                    case 2:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing3") with { Volume = SwingSoundVolume });
                            SwingSoundStyle = 0;
                            break;
                        }
                }
                return true;
            }

            if (Tier == 1)
            {
                if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2)
                {
                    Item.useStyle = ItemUseStyleID.Rapier;
                    Item.noUseGraphic = true;
                    Item.noMelee = true;
                    player.AddBuff(ThrustCooldownBuffID, AttackSpeedScalingDuration);
                    SoundEngine.PlaySound(new SoundStyle(SoundPath + "Thrust") with { Volume = SwingSoundVolume });
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile.NewProjectile(source, position, velocity, ThrustProjectileID, damage, ItemKnockback * 2, player.whoAmI);
                    }
                }
                else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2)
                {
                    Item.useStyle = ItemUseStyleID.Swing;
                    Item.noUseGraphic = false;
                    Item.noMelee = false;
                    player.AddBuff(ThrustCooldownBuffID, AttackSpeedScalingDuration);
                    SoundEngine.PlaySound(new SoundStyle(SoundPath + "TornadoCast") with { Volume = SwingSoundVolume });
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile Tornado = Projectile.NewProjectileDirect(source, position, velocity, ModContent.ProjectileType<RuneterraKatanaTornado>(), damage, ItemKnockback * 2, player.whoAmI, Tier);
                        Tornado.width = 80;
                        Tornado.height = 150;
                    }
                }
            }
            else
            {
                if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2 && !player.HasBuff(DashBuffID))
                {
                    Item.useStyle = ItemUseStyleID.Rapier;
                    Item.noUseGraphic = true;
                    Item.noMelee = true;
                    player.AddBuff(ThrustCooldownBuffID, AttackSpeedScalingDuration);
                    if (Tier == 2)
                    {
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "Thrust") with { Volume = 0.8f });
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "Thrust") with { Volume = 1f });
                    }
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile.NewProjectile(source, position, velocity, ThrustProjectileID, damage, knockback * 2, player.whoAmI);
                    }
                }
                else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2 && player.HasBuff(DashBuffID))
                {
                    Item.useStyle = ItemUseStyleID.Shoot;
                    Item.noUseGraphic = true;
                    Item.noMelee = true;
                    SoundEngine.PlaySound(new SoundStyle(SoundPath + "Spin") with { Volume = 1f });
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile.NewProjectile(source, position, velocity, SpinProjectileID, damage, knockback * 2, player.whoAmI);
                    }
                }
                else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && !player.HasBuff(DashBuffID))
                {
                    Item.useStyle = ItemUseStyleID.Swing;
                    Item.noUseGraphic = false;
                    Item.noMelee = false;
                    player.AddBuff(ThrustCooldownBuffID, AttackSpeedScalingDuration);
                    SoundEngine.PlaySound(new SoundStyle(SoundPath + "TornadoCast") with { Volume = 1f });
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile Tornado = Projectile.NewProjectileDirect(source, position, velocity, ModContent.ProjectileType<RuneterraKatanaTornado>(), damage, knockback * 2, player.whoAmI, Tier);
                        switch (Tier)
                        {
                            case 2:
                                {
                                    Tornado.width = 80;
                                    Tornado.height = 180;
                                    break;
                                }
                            case 3:
                                {
                                    Tornado.width = 90;
                                    Tornado.height = 180;
                                    break;
                                }
                        }
                    }
                }
                else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && player.HasBuff(DashBuffID))
                {
                    Item.useStyle = ItemUseStyleID.Shoot;
                    Item.noUseGraphic = true;
                    Item.noMelee = true;
                    SoundEngine.PlaySound(new SoundStyle(SoundPath + "Spin") with { Volume = 1f });
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile.NewProjectile(source, position, velocity, SpinProjectileID, damage, knockback * 2, player.whoAmI);
                    }
                    SoundEngine.PlaySound(new SoundStyle(SoundPath + "TornadoCast") with { Volume = 1f });
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile Tornado = Projectile.NewProjectileDirect(source, position, Vector2.Zero, ModContent.ProjectileType<RuneterraKatanaTornado>(), damage, knockback * 2, player.whoAmI, Tier);
                    }
                }
            }
            return true;
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!player.HasBuff(ThrustCooldownBuffID))
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var SpecialAbilityKey = tsorcRevamp.specialAbility.GetAssignedKeys();
            string SpecialAbilityString = SpecialAbilityKey.Count > 0 ? SpecialAbilityKey[0] : LangUtils.GetTextValue("Keybinds.Special Ability.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex1 != -1 && Tier > 1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.PlasmaWhirlwind.Keybind1") + SpecialAbilityString + LangUtils.GetTextValue("Items.PlasmaWhirlwind.Keybind2")));
            }
            int ttindex2 = tooltips.FindIndex(t => t.Name == "Tooltip5");
            if (ttindex2 != -1 && Tier == 3)
            {
                tooltips.RemoveAt(ttindex2);
                tooltips.Insert(ttindex2, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.Nightbringer.Keybind1") + SpecialAbilityString + LangUtils.GetTextValue("Items.Nightbringer.Keybind2")));
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", LangUtils.GetTextValue(LocalizationPath + "Details").FormatWith((float)AttackSpeedScalingDuration / 60f, PercentHealthDamage, HealthDamageCap, DashCooldown, WindwallDuration, WindwallCooldown)));
                }
            }
            else
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Shift", LangUtils.GetTextValue("CommonItemTooltip.Details")));
                }
            }
            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SmoughAttackSpeedReduction)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", LangUtils.GetTextValue("Items.SteelTempest.SmoughBalance")));
                }
            }
        }
    }
}