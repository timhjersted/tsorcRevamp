using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Runeterra;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    public class ToxicShot : ModItem
    {
        public int ShootTimer = 0;
        public int ShootCooldown = 60;
        public const int DebuffDuration = 5;
        public int ShootSoundStyle = 0;
        public float ShootSoundVolume = 0.5f;
        public static float ScoutsBoostMoveSpeedMult = 30f;
        public static float ScoutsBoostStaminaRegenMult = 15f;
        public const int BaseDamage = 30;
        public const int ScoutsBoostOnHitCooldown = 3;
        public const int ScoutsBoost2Duration = 5;
        public const int ScoutsBoost2Cooldown = 25;
        public static float PoisonDartDmgMult = 1.5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(PoisonDartDmgMult);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 24;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item63;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = BaseDamage;
            Item.knockBack = 4f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.Seed;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Dart;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(+7f, -9f);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            switch (ShootSoundStyle)
            {
                case 0:
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/Shot1") with { Volume = ShootSoundVolume });
                        ShootSoundStyle += 1;
                        break;
                    }
                case 1:
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/Shot2") with { Volume = ShootSoundVolume });
                        ShootSoundStyle += 1;
                        break;
                    }
                case 2:
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/Shot3") with { Volume = ShootSoundVolume });
                        ShootSoundStyle = 0;
                        break;
                    }
            }
            ShootTimer = (int)((float)ShootCooldown / ((float)player.GetTotalAttackSpeed(DamageClass.Ranged)));
            return true;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.Seed)
            {
                type = ModContent.ProjectileType<ToxicShotDart>();
            }
            if (type == ProjectileID.PoisonDartBlowgun)
            {
                damage = (int)(damage * PoisonDartDmgMult);
            }
        }
        public override void HoldItem(Player player)
        {
            ShootTimer--;
            if (!player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>()) && !player.HasBuff(ModContent.BuffType<ScoutsBoost2>()))
            {
                player.AddBuff(ModContent.BuffType<ScoutsBoost>(), 1);
            }
            if (player.itemAnimation > 18 && player.HasBuff(ModContent.BuffType<ScoutsBoost>())) //Scouts Boost 2 blocks Scouts Boost 1 and its cooldown so this won't occur then
            {
                player.velocity *= 0.92f;
            }
            else if (player.itemAnimation > 18 && player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>()))
            {
                player.velocity *= 0.01f;
            }
        }
        public override bool CanUseItem(Player player)
        {
            if (ShootTimer <= 0)
            {
                return true;
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var SpecialAbilityKey = tsorcRevamp.specialAbility.GetAssignedKeys();
            string SpecialAbilityString = SpecialAbilityKey.Count > 0 ? SpecialAbilityKey[0] : Language.GetTextValue("Mods.tsorcRevamp.Keybinds.Special Ability.DisplayName") + Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip5");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.ToxicShot.Keybind1") + SpecialAbilityString + Language.GetTextValue("Mods.tsorcRevamp.Items.ToxicShot.Keybind2")));
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", Language.GetTextValue("Mods.tsorcRevamp.Items.ToxicShot.Details").FormatWith(ScoutsBoostMoveSpeedMult, ScoutsBoostStaminaRegenMult, ScoutsBoostOnHitCooldown, ScoutsBoost2Duration)));
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

            recipe.AddIngredient(ItemID.Blowpipe);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

    }
}