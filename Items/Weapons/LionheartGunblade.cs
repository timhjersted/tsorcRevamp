using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Ranged;

namespace tsorcRevamp.Items.Weapons
{
    class LionheartGunblade : ModItem
    {
        public const int LionheartMarkDuration = 6;
        public const int MaxMarks = 5;
        public const float MarkExtraDmgMultBase = 1.5f;
        public const float MarkDistanceBasedDmgDivisor = 7f;
        public const int BaseDmg = 70;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMarks);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
            Item.staff[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = BaseDmg;
            Item.knockBack = 5f;
            Item.width = 100;
            Item.height = 100;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<Nothing>();
            Item.scale = 1.1f;
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
            damage.Flat += (player.GetDamage(DamageClass.Melee).ApplyTo(BaseDmg) + player.GetDamage(DamageClass.Ranged).ApplyTo(BaseDmg) + player.GetDamage(DamageClass.Magic).ApplyTo(BaseDmg) + player.GetDamage(DamageClass.Summon).ApplyTo(BaseDmg) + player.GetDamage(DamageClass.Throwing).ApplyTo(BaseDmg)) - (5f * BaseDmg);
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
                SoundEngine.PlaySound(SoundID.Item11);
            }
            else
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noMelee = false;
                SoundEngine.PlaySound(SoundID.Item1);
            }
            return false;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<LionheartMark>(), LionheartMarkDuration * 60);
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks < MaxMarks)
            {
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks++;
                if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks == MaxMarks)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/LionheartGunbladeMarked") with { Volume = 1f }, player.Center);
                }
            }
        }
    }
}
