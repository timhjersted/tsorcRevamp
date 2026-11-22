using System.Collections.Generic;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases
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

        public const int MaxShrooms = 5;
        public const int ShroomCooldown = 10;
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
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "_Shot1") with { Volume = ShootSoundVolume });
                            ShootSoundStyle += 1;
                            break;
                        }
                    case 1:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "_Shot2") with { Volume = ShootSoundVolume });
                            ShootSoundStyle += 1;
                            break;
                        }
                    case 2:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "_Shot3") with { Volume = ShootSoundVolume });
                            ShootSoundStyle = 0;
                            break;
                        }
                }
                ShootTimer = (int)((float)ShootCooldownBase / ((float)player.GetTotalAttackSpeed(DamageClass.Ranged)));
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "_BlindingLaserShot") with { Volume = ShootSoundVolume * 2 });
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
                        ShroomSetupTime, ShroomCooldown, MaxShrooms))); //Omega Squad Rifle
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
}