using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Projectiles.Summon.Archer;

namespace tsorcRevamp.Items.Weapons.Summon
{
    public class ArcherSpiritBell : ModItem
    {
        public const float SlotsRequired = 2f;

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = SlotsRequired;
        }

        public override void SetDefaults()
        {
            Item.damage = 10; // Custom scaling applied in projectile
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
            Item.mana = 100; // Costs 100 magic/mana
            Item.buffType = ModContent.BuffType<ArcherSpiritBuff>();
            Item.shoot = ModContent.ProjectileType<ArcherSpirit>();
        }

        public override bool CanUseItem(Player player)
        {
            // Don't let the bell fire (and spend its 100 mana) unless there are enough free minion slots
            // for the Archer Spirit. StaffMinionSlotsRequired only affects slot accounting, not usability.
            return player.maxMinions - player.slotsMinions >= SlotsRequired;
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
                var projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
                projectile.originalDamage = Item.damage;
            }

            return false;
        }
    }
}
