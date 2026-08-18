using System.Collections.Generic;
using Humanizer;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Systems;
using tsorcRevamp.Systems.ArcaneSorcery;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class Murassame : ModItem
    {
        public const int MaxManaSubtractBase = 200;
        public const int MaxManaDivisorBase = 8;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxManaSubtractBase, MaxManaDivisorBase);

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.damage = 28;
            Item.knockBack = 5;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 20;
            Item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            Item.shootSpeed = 13f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilBar, 5);
            recipe.AddIngredient(ModContent.ItemType<Muramassa>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var arcaneSorceryPlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
            int maxManaSubtract = MaxManaSubtractBase;
            int maxManaDivisor = MaxManaDivisorBase;
            int maxMana = player.statManaMax2;
            if (arcaneSorceryPlayer.ArcaneSorcerer)
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
            if (arcaneSorceryPlayer.ArcaneSorcerer)
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
