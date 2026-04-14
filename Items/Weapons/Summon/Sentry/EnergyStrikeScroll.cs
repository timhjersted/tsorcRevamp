using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Magic;
using tsorcRevamp.Projectiles.Magic.Scrolls;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Items.Weapons.Summon.Sentry
{
    class EnergyStrikeScroll : ModItem
    {
        public override void SetStaticDefaults()
        {
           
        }
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(EnergyStrikeScrollTeslaCoil.MaxEnemiesToHit, Dissolving.MaxDissolvingStacks);
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 10;
            Item.damage = 35; // Since it's a sentry any player could have minimum 2 summons
            Item.DamageType = DamageClass.Summon;
            Item.mana = 10;
            Item.knockBack = 2f;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.LightPurple_6;
            Item.shoot = ModContent.ProjectileType<EnergyStrikeScrollTeslaCoil>();
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.sentry = true;
            Item.noMelee = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            // Player should have access to this after defeating destroyer, magnet sphere from corruption temple and might from the boss
            recipe.AddIngredient(ItemID.MagnetSphere);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddIngredient(ItemID.SoulofMight, 5);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return true;
        }
        
    }
}
