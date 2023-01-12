﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon
{
    public class NullSpriteStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Null Sprite Staff");
            Tooltip.SetDefault("Summons a null sprite to fight for you" +
                "\nTakes 3/4th of a minion slot");

            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
        public override void SetDefaults()
        {
            Item.damage = 115;
            Item.knockBack = 1f;
            Item.width = 44;
            Item.height = 50;
            Item.useTime = Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.buyPrice(2, 0, 0, 0);
            Item.mana = 10;
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = SoundID.Item44;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<Buffs.Summon.NullSpriteBuff>();
            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.NullSprite>();
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if ((player.name == "Zeodexic") || (player.name == "ChromaEquinox")) //*/) //Add whatever names you use -C
            {
                Item.damage = 100; //change this to whatever suits your testing needs -C
            }
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            player.AddBuff(Item.buffType, 2);
            int p = Projectile.NewProjectile(source, position, speed, type, damage, knockBack);
            Main.projectile[p].originalDamage = Item.damage;
            return true;
        }
    }
}
