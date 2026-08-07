using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class ArtoriasGreatsword : ModItem
    {
        public override void SetDefaults()
        {
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.autoReuse = true;
            Item.damage = 260;
            Item.width = 105;
            Item.height = 105;
            Item.knockBack = 7f;
            Item.scale = 1.2f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Purple_11;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.DarkViolet;
            // This weapon's 105x105 gameplay dimensions are much larger than its 70x70 art.
            // Keep collision intact while fitting both visual slash layers to the visible blade.
            instancedGlobal.slashVisualScale = 0.5f;
        }
    }
}
