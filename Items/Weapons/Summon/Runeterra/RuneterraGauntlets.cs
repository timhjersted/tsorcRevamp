using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Summon.Runeterra
{
    public abstract class RuneterraGauntlets : ModItem
    {
        public abstract float SoundVolumeAbstract { get; }
        public abstract string SoundPath { get; }
        public abstract int Damage { get; }
        public abstract float Knockback { get; }
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int Value { get; }
        public abstract int Rarity { get; }
        public abstract int BuffType { get; }
        public abstract int ProjectileType { get; }
        public abstract int DragonType { get; }
        public abstract Vector3 HoldItemLight { get; }
        public abstract string LocalizationPath { get; }
        public abstract int Tier { get; }

        public static float BallSummonTagDmgMult = 80f;
        public const float DragonSummonTagDmgMult = 40f;
        public const float MarkChance = 20f;
        public const int DragonDebuffDuration = 5;
        public const int SuperBurnDuration = 5;
        public const float SuperBurnDmgAmp = 50f;
        public const float SummonTagCrit = 10f;
        public const float MarkDetonationCritDmgAmp = 2f;

        public const float BoostDmgAmp = 25f;

        public const int AwestruckDebuffDuration = 10;
        public const float AwestruckStarDamageAmp = 10f;

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 0.5f;
        }
        public override void SetDefaults()
        {
            Item.damage = Damage;
            Item.knockBack = Knockback;
            Item.mana = 10;
            Item.width = Width;
            Item.height = Height;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.holdStyle = ItemHoldStyleID.HoldFront;
            Item.noUseGraphic = true;
            Item.useTurn = false;
            Item.value = Value;
            Item.rare = Rarity;

            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = BuffType;
            Item.shoot = ProjectileType;

            CustomSetDefaults();
        }
        public virtual void CustomSetDefaults()
        {
        }
        public Projectile Dragon;
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
            position = player.Bottom;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // This is needed so the buff that keeps your minion alive and allows you to despawn it properly applies
            player.AddBuff(Item.buffType, 2);

            // Minions have to be spawned manually, then have originalDamage assigned to the damage of the summon item
            Projectile CirclingBall = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer, SoundVolumeAbstract);

            CustomShoot(CirclingBall);

            CirclingBall.originalDamage = Item.damage;

            if (player.ownedProjectileCounts[DragonType] == 0 && Main.myPlayer == player.whoAmI)
            {
                Dragon = Projectile.NewProjectileDirect(source, position, Vector2.Zero, DragonType, damage, 0, Main.myPlayer);
                Dragon.originalDamage = Item.damage;
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "DragonCast") with { Volume = SoundVolumeAbstract });
            }
            else if (Main.rand.NextBool(2))
            {
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "CirclingProjectileCast1") with { Volume = SoundVolumeAbstract });
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "CirclingProjectileCast2") with { Volume = SoundVolumeAbstract });
            }

            // Since we spawned the projectile manually already, we do not need the game to spawn it for ourselves anymore, so return false
            return false;
        }
        public virtual void CustomShoot(Projectile proj)
        {

        }

        public override void HoldItem(Player player)
        {
            Lighting.AddLight(player.Center, HoldItemLight);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var SpecialAbilityKey = tsorcRevamp.specialAbility.GetAssignedKeys();
            string SpecialAbilityString = SpecialAbilityKey.Count > 0 ? SpecialAbilityKey[0] : LangUtils.GetTextValue("Keybinds.Special Ability.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip2");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.ScorchingPoint.Keybind1") + SpecialAbilityString + LangUtils.GetTextValue("Items.ScorchingPoint.Keybind2")));
            }
            int ttindex2 = tooltips.FindIndex(t => t.Name == "Tooltip4");
            if (ttindex2 != -1 && Tier == 2)
            {
                tooltips.RemoveAt(ttindex2);
                tooltips.Insert(ttindex2, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue(LocalizationPath + "Keybind1") + SpecialAbilityString + LangUtils.GetTextValue(LocalizationPath + "Keybind2")));
            }
            if (ttindex2 != -1 && Tier == 3)
            {
                tooltips.RemoveAt(ttindex2);
                tooltips.Insert(ttindex2, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue(LocalizationPath + "Keybind1") + SpecialAbilityString + LangUtils.GetTextValue(LocalizationPath + "Keybind2")));
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", LangUtils.GetTextValue(LocalizationPath + "Details", ScorchingPoint.MarkChance, ScorchingPoint.SuperBurnDuration, ScorchingPoint.SummonTagCrit, AwestruckDebuffDuration, AwestruckStarDamageAmp)));
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