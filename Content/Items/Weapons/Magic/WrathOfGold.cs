using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Magic.WrathOfGold;

namespace tsorcRevamp.Content.Items.Weapons.Magic
{
    ///<summary>
    ///The Holy Gigas's spell tome: four of its moves in one book.
    ///  • Cursor high tap:   Heavenly Spears — three golden spears form over the cursor and lance down
    ///  • Cursor high hold:  Halo Volley — suns gather in orbit around you and hunt nearby foes
    ///  • Cursor low tap:    Sun Pillar — a column of judgment light slams down at the cursor
    ///  • Cursor low hold:   Lightning of the First Sun — charge, then release a long golden sweep
    ///Routed through WrathOfGoldController (tap = released within 18 ticks, otherwise hold).
    ///Drops on first Holy Gigas kill in Super Hardmode.
    ///</summary>
    class WrathOfGold : ModItem
    {
        public const int BaseUseTime = 20;

        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 14;
            Item.width = 28;
            Item.height = 32;
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.channel = true; // the controller watches player.channel to split tap vs hold
            Item.knockBack = 4.5f;
            Item.value = Item.sellPrice(gold: 20);
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = SoundID.Item29;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<WrathOfGoldController>();
            Item.shootSpeed = 1f;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<WrathOfGoldController>()] == 0;
        }

        public override bool AltFunctionUse(Player player)
        {
            return FourAttackWeaponControls.UsesAltFunction;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int useTicks = CombinedHooks.TotalUseTime(Item.useTime, Main.LocalPlayer, Item);
            string tapSpeedText = Language.GetTextValue(
                "Mods.tsorcRevamp.Items.WrathOfGold.TapSpeedTooltip", useTicks, useTicks / 60f);
            TooltipLine tapSpeed = new TooltipLine(Mod, "TapSpeed", tapSpeedText);

            int speedIndex = tooltips.FindIndex(line => line.Mod == "Terraria" && line.Name == "Speed");
            int insertIndex = speedIndex >= 0 ? speedIndex + 1 : tooltips.Count;
            tooltips.Insert(insertIndex, tapSpeed);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int mode = FourAttackWeaponControls.GetAttackMode(player);
            float rightClickFlag = player.altFunctionUse == 2 ? 1f : 0f;
            float effectiveUseTime = Item.useTime * CombinedHooks.TotalUseTimeMultiplier(player, Item);

            Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback,
                player.whoAmI, mode, rightClickFlag, effectiveUseTime);
            return false;
        }
    }
}
