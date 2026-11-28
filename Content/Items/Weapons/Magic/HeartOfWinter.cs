using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Magic.HeartOfWinter;

namespace tsorcRevamp.Content.Items.Weapons.Magic
{
    ///<summary>
    ///The Ice Gigas's spell tome: four of its moves in one book.
    ///  • Cursor high tap:   Icicle Crown — three icicles form overhead and lance at the cursor
    ///  • Cursor high hold:  Frost Breath — a channeled cone of frost
    ///  • Cursor low tap:    Rimefang — ice spikes erupt from the ground at the cursor
    ///  • Cursor low hold:   Absolute Zero — charge, then release an expanding freeze ring
    ///All four are routed through HeartOfWinterController, which watches whether the button is
    ///tapped (released within 18 ticks) or held. Drops on first Ice Gigas kill in Super Hardmode.
    ///</summary>
    class HeartOfWinter : ModItem
    {
        public const int BaseUseTime = 20;

        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 12;
            Item.width = 28;
            Item.height = 32;
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.channel = true; // the controller watches player.channel to split tap vs hold
            Item.knockBack = 4f;
            Item.value = Item.sellPrice(gold: 20);
            Item.rare = ItemRarityID.Cyan;
            Item.UseSound = SoundID.Item28;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<HeartOfWinterController>();
            Item.shootSpeed = 1f;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<HeartOfWinterController>()] == 0;
        }

        public override bool AltFunctionUse(Player player)
        {
            return FourAttackWeaponControls.UsesAltFunction;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int useTicks = CombinedHooks.TotalUseTime(Item.useTime, Main.LocalPlayer, Item);
            string tapSpeedText = Language.GetTextValue(
                "Mods.tsorcRevamp.Items.HeartOfWinter.TapSpeedTooltip", useTicks, useTicks / 60f);
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
