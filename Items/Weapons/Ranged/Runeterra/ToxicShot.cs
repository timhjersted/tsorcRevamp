using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Runeterra;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    public class ToxicShot : RuneterraDarts
    {
        public override int Width => 56;
        public override int Height => 24;
        public override int Rarity => ItemRarityID.Green;
        public override int Value => Item.buyPrice(0, 10, 0, 0);
        public override float Knockback => 4f;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/";
        public override int ProjectileType => ModContent.ProjectileType<ToxicShotDart>();
        public override int Tier => 1;
        public override string LocalizationPath => "Items.ToxicShot.";
        public override float ShootSoundVolume => 0.5f;
        public override int BlindingProjectileType => throw new System.NotImplementedException();
        public override int BlindingProjectileCooldownType => throw new System.NotImplementedException();
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(PoisonDartDmgMult);
        public const int BaseDamage = 30;
        public override void CustomSetDefaults()
        {
            Item.damage = BaseDamage;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(+7f, -9f);
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

    }
}