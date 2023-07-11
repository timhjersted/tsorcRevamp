using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles.Magic.Runeterra;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Items.Materials;
using Microsoft.Xna.Framework.Input;
using Terraria.Localization;
using Humanizer;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbOfFlame : ModItem
    {
        public static Color FilledColor = Color.PaleVioletRed;
        public static float FireballDmgMod = 250f;
        public const float FireballHPPercentDmg = 0.1f;
        public static int FireballHPDmgCap = 450;
        public static float MagicSunder = 20f;
        public static int FireballCD = 4;
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 8));
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = false;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.damage = 60;
            Item.mana = 40;
            Item.knockBack = 8;
            Item.UseSound = null;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = OrbOfDeception.ShootSpeed;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<OrbOfFlameOrb>();
            Item.holdStyle = ItemHoldStyleID.HoldLamp;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfFlameOrb>()] != 0)
            {
                type = ModContent.ProjectileType<OrbOfFlameFlame>();
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfFlameOrb>()] == 0 && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief < 9)
            {
                type = ModContent.ProjectileType<OrbOfFlameOrb>();
            }
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<OrbOfFlameFireball>();
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(ModContent.BuffType<OrbOfFlameFireballCooldown>())) //cooldown gets applied on projectile spawn
            {
                player.altFunctionUse = 2;
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
            }
        }
        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfFlameOrb>()] == 0 && player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfFlameOrbIdle>()] == 0 && !player.dead)
            {
                Projectile.NewProjectile(Projectile.InheritSource(player), player.Center, Vector2.Zero, ModContent.ProjectileType<OrbOfFlameOrbIdle>(), 0, 0);
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || !player.HasBuff(ModContent.BuffType<OrbOfFlameFireballCooldown>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", Language.GetTextValue("Mods.tsorcRevamp.Items.OrbOfFlame.Details").FormatWith(OrbOfDeception.OrbDmgMod - 100f, (OrbOfDeception.OrbReturnDmgMod * OrbOfDeception.OrbDmgMod) / 100f - 100f, OrbOfDeception.EssenceThiefOnKillChance, OrbOfDeception.FilledOrbDmgMod - 100f, FireballDmgMod / 100f, FireballCD, FireballHPPercentDmg, FireballHPDmgCap, MagicSunder)));
                }
            }
            else
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Shift", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.Details")));
                }
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<OrbOfDeception>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 11);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
