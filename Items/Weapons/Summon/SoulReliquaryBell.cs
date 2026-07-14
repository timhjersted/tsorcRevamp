using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon
{
    ///<summary>
    ///Early-game soul summon crafted from the Vessel of Souls' drop. Rings the reliquary to conjure a
    ///Lesser Vessel — a floating soul-eye that spits homing skulls. Modelled on the Archer Spirit Bell,
    ///but 1 slot. Costs 60 mana, lasts 2.5 minutes, and its damage scales with the Dark Souls the player
    ///is holding (see SoulDamageBonus). 
    ///</summary>
    public class SoulReliquaryBell : ModItem
    {
        public const float SlotsRequired = 1f;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = SlotsRequired;
        }

        public override void SetDefaults()
        {
            Item.damage = 12;
            Item.knockBack = 2f;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 60);
            Item.UseSound = SoundID.Item35;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.mana = 60;
            Item.buffType = ModContent.BuffType<LesserVesselBuff>();
            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.VesselOfSouls.LesserVessel>();
        }

        ///<summary>Flat bonus damage from the player's held Dark Souls. Tiered: the first 3 points cost
        ///5,000 each, the next 3 cost 10,000 each, the next 3 cost 15,000 each, the next 3 cost 20,000
        ///each, and every point beyond that costs 20,000. (150,000 held = +12.)</summary>
        public static int SoulDamageBonus(long darkSouls)
        {
            int bonus = 0;
            long remaining = darkSouls;
            Span<(int count, int cost)> tiers = stackalloc (int, int)[] { (3, 5000), (3, 10000), (3, 15000), (3, 20000) };
            for (int t = 0; t < tiers.Length; t++)
                for (int i = 0; i < tiers[t].count; i++)
                {
                    if (remaining < tiers[t].cost) return bonus;
                    remaining -= tiers[t].cost;
                    bonus++;
                }
            while (remaining >= 20000) { remaining -= 20000; bonus++; }
            return bonus;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Flat += SoulDamageBonus(player.GetModPlayer<tsorcRevampPlayer>().darkSoulQuantity);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int souls = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().darkSoulQuantity;
            int bonus = SoulDamageBonus(souls);
            tooltips.Add(new TooltipLine(Mod, "SoulScaling", $"+{bonus} damage from {souls:N0} Dark Souls held")
            {
                OverrideColor = Color.MediumPurple
            });
        }

        public override bool CanUseItem(Player player) => player.maxMinions - player.slotsMinions >= SlotsRequired;

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
            if (Main.myPlayer == player.whoAmI)
            {
                var proj = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
                proj.originalDamage = Item.damage;
            }
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfTheVessel>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
