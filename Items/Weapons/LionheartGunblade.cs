using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Ranged;

namespace tsorcRevamp.Items.Weapons
{
    class LionheartGunblade : ModItem
    {
        public const int LionheartMarkDuration = 6;
        public const int MaxMarks = 5;
        public const float MarkExtraDmgMultBase = 2f;
        public const float MarkDistanceBasedDmgDivisor = 8f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMarks);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.knockBack = 5f;
            Item.width = 73;
            Item.height = 29;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<Nothing>();
            Item.scale = 1.2f;
            Item.shootSpeed = 5f;
            Item.useAmmo = AmmoID.Bullet;
            Item.DamageType = DamageClass.Generic;
            Item.useAnimation = 24;
            Item.useTime = 24;
            Item.expert = true;
            Item.value = PriceByRarity.LightRed_4;
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Blue;
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage *= player.GetDamage(DamageClass.Melee).ApplyTo(1f) + player.GetDamage(DamageClass.Ranged).ApplyTo(1f) + player.GetDamage(DamageClass.Magic).ApplyTo(1f) + player.GetDamage(DamageClass.Summon).ApplyTo(1f) + player.GetDamage(DamageClass.Throwing).ApplyTo(1f) - 4f;
        }
        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit += player.GetCritChance(DamageClass.Melee) + player.GetCritChance(DamageClass.Ranged) + player.GetCritChance(DamageClass.Magic) + player.GetCritChance(DamageClass.Throwing) + player.GetCritChance(DamageClass.Throwing);
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!Main.mouseLeft && player.ItemTimeIsZero)
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.noMelee = true;
            }
            else
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noMelee = false;
            }
        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            if (player.altFunctionUse != 2)
            {
                return false;
            }
            return base.CanConsumeAmmo(ammo, player);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.noMelee = true;
                Projectile.NewProjectileDirect(source, position, velocity, ModContent.ProjectileType<LionheartGunshot>(), damage, knockback);
            }
            else
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noMelee = false;
            }
            return false;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<LionheartMark>(), LionheartMarkDuration * 60);
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks < MaxMarks)
            {
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks++;
            }
        }
    }
}
