using Terraria.GameContent.Creative;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Projectiles.Melee.Spears;
using tsorcRevamp.Projectiles.Summon.NullSprite;
using Microsoft.Xna.Framework;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Summon.ForgottenImp;
using tsorcRevamp.Projectiles.Summon.EtherianWyvern;
using Terraria.Localization;

namespace tsorcRevamp.Items.Weapons.Summon
{
    class EtherianWyvernStaff : ModItem
    {
        public const float SlotsRequired = 1f;
        public const int BaseDmg = 25;
        public const int DebuffDuration = 7;
        public const float SummonTagDmgMult = 75f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonTagDmgMult);
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = SlotsRequired;
        }
        public override void SetDefaults()
        {
            Item.damage = BaseDmg;
            Item.knockBack = 1f;
            Item.width = 54;
            Item.height = 54;
            Item.useTime = Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
            Item.mana = 10;
            Item.UseSound = SoundID.Item44;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<EtherianWyvernBuff>();
            Item.shoot = ModContent.ProjectileType<EtherianWyvernProjectile>();
            Item.noMelee = true;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
            position = Main.MouseWorld;
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            player.AddBuff(Item.buffType, 2);
            if (Main.myPlayer == player.whoAmI)
            {
                int p = Projectile.NewProjectile(source, position, speed, type, damage, knockBack);
                Main.projectile[p].originalDamage = Item.damage;
            }
            return false;
        }
    }
}
