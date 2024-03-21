using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public abstract class RuneterraOrb : ModItem
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int Damage { get; }
        public abstract int ManaCost { get; }
        public abstract int Rarity { get; }
        public abstract int Value { get; }
        public abstract int HeldOrbProjectile { get; }
        public abstract int OrbProjectile { get; }
        public abstract int FlameProjectile { get; }
        public abstract int CharmProjectile { get; }
        public abstract int CharmCooldownType { get; }
        public abstract int Tier { get; }
        public abstract string LocalizationPath { get; }

        public const float OrbDmgMod = 50f;
        public const float OrbReturnDmgMod = 50f;
        public const float EssenceThiefOnKillChance = 20f;
        public const float FilledOrbDmgMod = 75f;
        public const float ShootSpeed = 20f;
        public const float OrbSoundVolume = 0.5f;
        public const int HealManaDivisor = 100;
        public const int HealBaseValue = 4;

        public const float FireballDmgMod = 250f;
        public const float FireballHPPercentDmg = 0.6f;
        public const int FireballHPDmgCap = (int)(450000f * FireballHPPercentDmg / 100f);
        public const float MagicSunder = 20f;
        public const int FireballCD = 4;
        public const int FireballDuration = 6;

        public static int DashBuffDuration = 15;
        public static int DashCD = 60;
        public static int DashCostMultiplier = 4;
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 8));
        }

        public override void SetDefaults()
        {
            Item.width = Width;
            Item.height = Height;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = false;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.damage = Damage;
            Item.crit = 6;
            Item.mana = ManaCost;
            Item.knockBack = 8;
            Item.UseSound = null;
            Item.rare = Rarity;
            Item.shootSpeed = ShootSpeed;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = Value;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = OrbProjectile;
            Item.holdStyle = ItemHoldStyleID.HoldLamp;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.ownedProjectileCounts[OrbProjectile] != 0)
            {
                type = FlameProjectile;
            }
            else
            {
                type = OrbProjectile;
            }
            if (player.altFunctionUse == 2 && Tier > 1)
            {
                type = CharmProjectile;
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Tier > 1)
            {
                if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(CharmCooldownType)) //cooldown gets applied on projectile spawn
                {
                    player.altFunctionUse = 2;
                }
                if (Main.mouseLeft)
                {
                    player.altFunctionUse = 1;
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
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || !player.HasBuff(CharmCooldownType))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[OrbProjectile] == 0 && player.ownedProjectileCounts[HeldOrbProjectile] == 0 && !player.dead && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.InheritSource(player), player.Center, Vector2.Zero, HeldOrbProjectile, 0, 0, player.whoAmI);
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var SpecialAbilityKey = tsorcRevamp.specialAbility.GetAssignedKeys();
            string SpecialAbilityString = SpecialAbilityKey.Count > 0 ? SpecialAbilityKey[0] : LangUtils.GetTextValue("Keybinds.Special Ability.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip5");
            if (ttindex1 != -1 && Tier == 3)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue(LocalizationPath + "Keybind1") + SpecialAbilityString + LangUtils.GetTextValue(LocalizationPath + "Keybind2")));
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", LangUtils.GetTextValue(LocalizationPath + "Details").FormatWith(OrbDmgMod, (OrbReturnDmgMod + OrbDmgMod), EssenceThiefOnKillChance, FilledOrbDmgMod, FireballDmgMod / 100f, FireballCD, FireballHPPercentDmg, FireballHPDmgCap, MagicSunder, DashBuffDuration, DashCD, (int)(Item.mana * player.manaCost * DashCostMultiplier), FireballDuration)));
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
    }
}
