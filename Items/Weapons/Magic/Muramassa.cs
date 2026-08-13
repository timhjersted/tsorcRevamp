using System.Collections.Generic;
using Humanizer;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class Muramassa : ModItem
    {
        public const int MaxManaSubtractBase = 200;
        public const int MaxManaDivisorBase = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxManaSubtractBase, MaxManaDivisorBase);

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.useAnimation = 18;
            Item.useTime = 18;
            Item.damage = 20;
            Item.knockBack = 3;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            Item.shootSpeed = 11f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Muramasa, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var arcaneSorceryPlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
            int maxManaSubtract = MaxManaSubtractBase;
            int maxManaDivisor = MaxManaDivisorBase;
            int maxMana = player.statManaMax2;
            if (arcaneSorceryPlayer.Enabled)
            {
                maxManaSubtract *= (int)(arcaneSorceryPlayer.MaxManaAmplifier / 100f);
                maxManaDivisor *= (int)(arcaneSorceryPlayer.MaxManaAmplifier / 100f);
            }
            if (maxMana >= maxManaSubtract)
            {
                damage.Flat += (maxMana - maxManaSubtract) / maxManaDivisor;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var arcaneSorceryPlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
            int maxManaSubtract = MaxManaSubtractBase;
            int maxManaDivisor = MaxManaDivisorBase;
            if (arcaneSorceryPlayer.Enabled)
            {
                maxManaSubtract *= (int)(arcaneSorceryPlayer.MaxManaAmplifier / 100f);
                maxManaDivisor *= (int)(arcaneSorceryPlayer.MaxManaAmplifier / 100f);
            }
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Proper Scaling", Language.GetTextValue(Tooltip.Key + "0").FormatWith(maxManaSubtract, maxManaDivisor)));
            }
        }
    }
}
