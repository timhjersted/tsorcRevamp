using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Content.Projectiles.Summon.Archer;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Weapons.Summon
{
    public class ArcherSpiritBell : ModItem
    {
        public const float SlotsRequired = 2f;
        public const int BaseDamage = 24;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            ArcherSpirit.BaseManaPerArrow,
            ArcherSpirit.BotcManaMultiplier,
            ArcherSpirit.SummonerFreeMinionThreshold,
            (int)SlotsRequired);

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = SlotsRequired;
        }

        public override void SetDefaults()
        {
            Item.damage = BaseDamage;
            Item.knockBack = 2f;
            Item.width = 30;
            Item.height = 30;
            Item.scale = 0.6f;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
            Item.UseSound = SoundID.Item35; // Bell sound

            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<ArcherSpiritBuff>();
            Item.shoot = ModContent.ProjectileType<ArcherSpirit>();
        }

        public override bool CanUseItem(Player player)
        {
            // Don't let the bell fire unless there are enough free minion slots for the Archer Spirit.
            // StaffMinionSlotsRequired only affects slot accounting, not usability.
            return player.maxMinions - player.slotsMinions >= SlotsRequired;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine damageLine = tooltips.Find(line => line.Mod == "Terraria" && line.Name == "Damage");
            if (damageLine != null)
            {
                // Live arrow damage reflects both boss progression tier and the player's summon damage bonuses.
                int effectiveBaseDamage = WeaponReinforcement.GetEffectiveBaseDamage(Item);
                damageLine.Text = Language.GetTextValue("Mods.tsorcRevamp.Items.ArcherSpiritBell.ArrowDamage",
                    ArcherSpirit.CalculateArrowDamage(Main.LocalPlayer, effectiveBaseDamage));
            }
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            BellSwingAnimation.Apply(Item, player, heldItemFrame);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position = Main.MouseWorld;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 2);

            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.Unkindled)
            {
                modPlayer.unkindledManaDelayTimer = 3600;
            }

            if (Main.myPlayer == player.whoAmI)
            {
                byte reinforcementLevel = WeaponReinforcement.GetDisplayedLevel(Item);
                int effectiveBaseDamage = WeaponReinforcement.GetEffectiveBaseDamage(Item);
                var projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer, reinforcementLevel);
                projectile.originalDamage = effectiveBaseDamage;
            }

            return false;
        }
    }
}
