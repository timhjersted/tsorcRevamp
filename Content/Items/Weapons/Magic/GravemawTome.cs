using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Magic.Gravemaw;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Weapons.Magic
{
    ///<summary>
    ///The Vessel of Souls' spell tome: four of its moves in one book, all on LEFT CLICK (cursor
    ///high/low × tap/hold), routed through GravemawController — the same scheme as Heart of Winter.
    ///  • Cursor high tap:  Soulspit    — a fan of homing soul-skulls
    ///  • Cursor high hold: Reliquary Nova — charge, then release an expanding soul ring
    ///  • Cursor low tap:   Gravemaw Orb — lob an orb that opens a brief black hole, then bursts
    ///  • Cursor low hold:  Hungering Maw — channel a short INHALE cone that pulls foes in, damages
    ///                                       them rapidly, and trickles a little life back (soul eating)
    ///Crafted from 1 Soul of the Vessel + 4000 Dark Souls.
    ///</summary>
    class GravemawTome : ModItem
    {
        public const int BaseUseTime = 27;
        public const byte ShadowflamePerkLevel = 3;
        public const int ShadowflameDurationTicks = 180;

        public override void SetDefaults()
        {
            Item.damage = 18;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 8;
            Item.width = 28;
            Item.height = 32;
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.channel = true; // the controller watches player.channel to split tap vs hold
            Item.knockBack = 3f;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item20;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<GravemawController>();
            Item.shootSpeed = 1f;
        }

        public override bool CanUseItem(Player player) =>
            player.ownedProjectileCounts[ModContent.ProjectileType<GravemawController>()] == 0;

        public override bool AltFunctionUse(Player player) => FourAttackWeaponControls.UsesAltFunction;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int useTicks = CombinedHooks.TotalUseTime(Item.useTime, Main.LocalPlayer, Item);
            TooltipLine tapSpeed = new TooltipLine(Mod, "TapSpeed", Language.GetTextValue(
                "Mods.tsorcRevamp.Items.GravemawTome.TapSpeedTooltip", useTicks, useTicks / 60f));
            int speedIndex = tooltips.FindIndex(line => line.Mod == "Terraria" && line.Name == "Speed");
            tooltips.Insert(speedIndex >= 0 ? speedIndex + 1 : tooltips.Count, tapSpeed);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int mode = FourAttackWeaponControls.GetAttackMode(player);
            float rightClickFlag = player.altFunctionUse == 2 ? 1f : 0f;
            float effectiveUseTime = Item.useTime * CombinedHooks.TotalUseTimeMultiplier(player, Item);

            int projectileIndex = Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback,
                player.whoAmI, mode, rightClickFlag, effectiveUseTime);

            // Forward reinforcement level onto the controller so the +3 milestone perk applies and syncs cleanly in MP.
            if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
            {
                Projectile controllerProjectile = Main.projectile[projectileIndex];
                if (controllerProjectile.ModProjectile is GravemawController controller)
                {
                    controller.ReinforcementLevel = WeaponReinforcement.GetDisplayedLevel(player.HeldItem);
                    controllerProjectile.netUpdate = true;
                }
            }

            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfTheVessel>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
