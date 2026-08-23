using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.NPCs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    public abstract class RuneterraDarts : ModItem
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int Rarity { get; }
        public abstract int Value { get; }
        public abstract float Knockback { get; }
        public abstract string SoundPath { get; }
        public abstract int ProjectileType { get; }
        public abstract int Tier { get; }
        public abstract string LocalizationPath { get; }
        public abstract float ShootSoundVolume { get; }
        public abstract int BlindingProjectileType { get; }
        public abstract int BlindingProjectileCooldownType { get; }

        public int ShootTimer = 0;
        public const int ShootCooldownBase = 50;
        public int ShootSoundStyle = 0;
        public const int DebuffDuration = 5;

        public const float ScoutsBoostMoveSpeedMult = 30f;
        public const float ScoutsBoostStaminaRegenMult = 15f;
        public const int ScoutsBoostOnHitCooldown = 3;
        public const int ScoutsBoost2Duration = 5;
        public const int ScoutsBoost2Cooldown = 25;
        public const float PoisonDartDmgMult = 1.5f;
        public const int PoisonDartPierceBonus = 3;

        public const float BlindingLaserDmgMult = 3;
        public static int BlindingLaserSeedDmgMult = 2;
        public const int BlindingLaserCooldown = 5;
        public const int BlindingLaserBonusCritChance = 100;
        public const float BlindingLaserPercentHpDmg = 0.8f;
        public const int BlindingLaserHpDmgCap = (int)(450000f * BlindingLaserPercentHpDmg / 100f);

        public const int ShroomCooldown = 5;
        public const int ShroomBonusCritChance = 100;
        public const int ShroomSetupTime = 3;
        public const int ShroomIrradiationDuration = 10;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }
        public override void SetDefaults()
        {
            Item.width = Width;
            Item.height = Height;
            Item.rare = Rarity;
            Item.value = Value;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = Knockback;
            Item.noMelee = true;
            Item.shoot = ProjectileID.Seed;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Dart;
            CustomSetDefaults();
        }
        public virtual void CustomSetDefaults()
        {
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                switch (ShootSoundStyle)
                {
                    case 0:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Shot1") with { Volume = ShootSoundVolume });
                            ShootSoundStyle += 1;
                            break;
                        }
                    case 1:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Shot2") with { Volume = ShootSoundVolume });
                            ShootSoundStyle += 1;
                            break;
                        }
                    case 2:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Shot3") with { Volume = ShootSoundVolume });
                            ShootSoundStyle = 0;
                            break;
                        }
                }
                ShootTimer = (int)((float)ShootCooldownBase / ((float)player.GetTotalAttackSpeed(DamageClass.Ranged)));
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "BlindingLaserShot") with { Volume = ShootSoundVolume * 2 });
            }
            return true;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.Seed)
            {
                type = ProjectileType;
            }
            if (type == ProjectileID.PoisonDartBlowgun)
            {
                damage = (int)(damage * PoisonDartDmgMult);
            }
            if (player.altFunctionUse == 2)
            {
                if (type == ProjectileID.Seed)
                {
                    damage *= BlindingLaserSeedDmgMult;
                }
                type = BlindingProjectileType;
            }
        }
        public override void HoldItem(Player player)
        {
            var modPlayer = player.GetModPlayer<RuneterraDartsPlayer>();
            switch (Tier)
            {
                case 1:
                {
                    modPlayer.ScoutsHeldItem = true;
                    break;
                }
                case 2:
                {
                    modPlayer.AlienScoutsHeldItem = true;
                    break;
                }
                case 3:
                {
                    modPlayer.OmegaScoutsHeldItem = true;
                    break;
                }
            }
            ShootTimer--;
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Tier == 1)
            {
                return;
            }
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(BlindingProjectileCooldownType)) //cooldown gets applied on projectile spawn
            {
                player.altFunctionUse = 2;
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
            }
        }
        public override bool CanUseItem(Player player)
        {
            if (Tier == 1 && ShootTimer <= 0)
            {
                return true;
            }
            else if (Tier == 1)
            {
                return false;
            }
            if ((ShootTimer <= 0 && !Main.mouseRight) || (Main.mouseRight && !Main.mouseLeft && !player.HasBuff(BlindingProjectileCooldownType)))
            {
                return true;
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var SpecialAbilityKey = tsorcRevamp.specialAbility.GetAssignedKeys();
            string SpecialAbilityString = SpecialAbilityKey.Count > 0 ? SpecialAbilityKey[0] : LangUtils.GetTextValue("Keybinds.Special Ability.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip5");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.ToxicShot.Keybind1") + SpecialAbilityString + LangUtils.GetTextValue("Items.ToxicShot.Keybind2")));
            }
            int ttindex2 = tooltips.FindIndex(t => (t.Name == "Tooltip7"));
            if (ttindex2 != -1 && Tier == 3)
            {
                tooltips.RemoveAt(ttindex2);
                tooltips.Insert(ttindex2, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.OmegaSquadRifle.Keybind1") + SpecialAbilityString + LangUtils.GetTextValue("Items.OmegaSquadRifle.Keybind2")));
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", LangUtils.GetTextValue(LocalizationPath + "Details").FormatWith(ScoutsBoostMoveSpeedMult, ScoutsBoostStaminaRegenMult, ScoutsBoostOnHitCooldown, ScoutsBoost2Duration,  //Toxic Shot
                        BlindingLaserDmgMult, BlindingLaserSeedDmgMult, BlindingLaserCooldown, BlindingLaserBonusCritChance, BlindingLaserPercentHpDmg, BlindingLaserHpDmgCap, //Alien Gun
                        ShroomSetupTime, ShroomCooldown))); //Omega Squad Rifle
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
        }
        public override bool AltFunctionUse(Player player)
        {
            if (Tier > 1)
            {
                return true;
            }
            return false;
        }
    }

    public class RuneterraDartsPlayer : ModPlayer
    {
        public bool ScoutsHeldItem;
        public bool AlienScoutsHeldItem;
        public bool OmegaScoutsHeldItem;
        public bool ScoutsBoostPassive;
        public bool ScoutsBoostActive;
        public override void ResetEffects()
        {
            ScoutsHeldItem  = false;
            AlienScoutsHeldItem  = false;
            OmegaScoutsHeldItem = false;
            ScoutsBoostPassive = false;
            ScoutsBoostActive = false;
        }
        public void CustomPostUpdateMiscEffects()
        {
            float velocityMult1 = 0.93f;
            float velocityMult2 = 0;
            if (Player.HeldItem.type == ModContent.ItemType<ToxicShot>() || Player.HeldItem.type == ModContent.ItemType<AlienGun>())
            {
                if (!Player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>()) && !Player.HasBuff(ModContent.BuffType<ScoutsBoost2>()))
                {
                    Player.AddBuff(ModContent.BuffType<ScoutsBoost>(), 2);
                }
                if (Player.itemAnimation > 1 && Player.HasBuff(ModContent.BuffType<ScoutsBoost>())) //Scouts Boost 2 blocks Scouts Boost 1 and its cooldown so this won't occur then
                {
                    Player.velocity.X *= velocityMult1;
                }
                else if (Player.itemAnimation > 1 && Player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>()))
                {
                    Player.velocity.X *= velocityMult2;
                }
            }
            if (Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>())
            {
                if (!Player.HasBuff(ModContent.BuffType<ScoutsBoostCooldownOmega>()) && !Player.HasBuff(ModContent.BuffType<ScoutsBoost2Omega>()))
                {
                    Player.AddBuff(ModContent.BuffType<ScoutsBoost>(), 2); //ScoutsBoost buff itself does not play any sounds in it's code so I didn't need to make an Omega version
                }
                if (Player.itemAnimation > 1 && Player.HasBuff(ModContent.BuffType<ScoutsBoost>())) //Scouts Boost 2 blocks Scouts Boost 1 and its cooldown so this won't occur then
                {
                    Player.velocity.X *= velocityMult1;
                }
                else if (Player.itemAnimation > 1 && Player.HasBuff(ModContent.BuffType<ScoutsBoostCooldownOmega>()))
                {
                    Player.velocity.X *= velocityMult2;
                }
            }
            if (ScoutsBoostPassive)
            {
                Player.moveSpeed *= 1f + ToxicShot.ScoutsBoostMoveSpeedMult / 100f;
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + ToxicShot.ScoutsBoostStaminaRegenMult / 100f; //this runs right before stamina regen is calculated, meaning it will multiply all of your additive bonuses properly
            }

            if (ScoutsBoostActive)
            {
                Player.moveSpeed *= 1f + ToxicShot.ScoutsBoostMoveSpeedMult * 2f / 100f;
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + ToxicShot.ScoutsBoostStaminaRegenMult * 2f / 100f;
            }
        }
    }

    public class PoisonDartsEdit : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool AppliedAlready;

        public override void AI(Projectile projectile)
        {
            Player player = Main.player[projectile.owner];
            var modPlayer = player.GetModPlayer<RuneterraDartsPlayer>();
            bool isPoisonDart = projectile.type == ProjectileID.PoisonDartBlowgun;
            bool holdingWeapon = modPlayer.ScoutsHeldItem || modPlayer.AlienScoutsHeldItem || modPlayer.OmegaScoutsHeldItem;
            if (!AppliedAlready && isPoisonDart && holdingWeapon)
            {
                projectile.penetrate += RuneterraDarts.PoisonDartPierceBonus;
                projectile.usesLocalNPCImmunity = true;
                projectile.localNPCHitCooldown = -1;
                AppliedAlready = true;
            }
        }
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[projectile.owner];
            var modPlayer = player.GetModPlayer<RuneterraDartsPlayer>();
            bool isPoisonDart = projectile.type == ProjectileID.PoisonDartBlowgun;
            if (isPoisonDart)
            {
                if (modPlayer.ScoutsHeldItem)
                {
                    if (hit.Crit)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/ShotCrit") with { Volume = 0.5f }, target.Center);
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/ShotHit") with { Volume = 0.5f }, target.Center);
                    }
                    target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[projectile.owner];
                    target.AddBuff(ModContent.BuffType<VenomDebuff>(), 2 * 60);
                } 
                else if (modPlayer.AlienScoutsHeldItem)
                {
                    if (hit.Crit)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/AlienGun/ShotCrit") with { Volume = 0.5f }, target.Center);
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/AlienGun/ShotHit") with { Volume = 0.5f }, target.Center);
                    }
                    target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[projectile.owner];
                    target.AddBuff(ModContent.BuffType<ElectrifiedDebuff>(), 2 * 60);
                }
                else if (modPlayer.OmegaScoutsHeldItem)
                {
                    if (hit.Crit)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShotCrit") with { Volume = 0.5f }, target.Center);
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShotHit") with { Volume = 0.5f }, target.Center);
                    }
                    target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[projectile.owner];
                    target.AddBuff(ModContent.BuffType<IrradiatedDebuff>(), 2 * 60);
                }
            }
        }
    }
}