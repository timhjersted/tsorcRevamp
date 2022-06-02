using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    class GlaiveBeam : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A legendary weapon of war from beyond the stars\n" +
                               "Fabled to cut entire ships in half with a single blast");
        }
        public override void SetDefaults()
        {
            //item.CloneDefaults(ItemID.LastPrism);
            Item.mana = 0;
            Item.damage = 750;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.height = 28;
            Item.width = 12;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.Purple;
            Item.shoot = ModContent.ProjectileType<Projectiles.GlaiveBeamHoldout>();
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = null;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shootSpeed = 30;
            Item.useAnimation = 150;
            Item.useTime = 200;
            Item.value = 8000000;

        }
        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.GlaiveBeamHoldout>()] <= 0;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GlaiveBeamItemGlowmask];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        public override Vector2? HoldoutOffset()
        {

            return new Vector2(-18, -10);
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Items.Weapons.Ranged.FocusedEnergyBeam>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Items.GhostWyvernSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Items.BequeathedSoul>(), 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 250000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
