using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class GlaiveBeam : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A legendary weapon of war from beyond the stars\n" +
                               "Fabled to cut entire ships in half with a single blast");
        }
        public override void SetDefaults() {
            //item.CloneDefaults(ItemID.LastPrism);
            item.mana = 0;
            item.magic = false;
            item.damage = 850;
            item.noMelee = true;
            item.ranged = true;
            item.height = 28;
            item.width = 12;
            item.knockBack = 4;
            item.rare = ItemRarityID.Purple;
            item.shoot = ModContent.ProjectileType<Projectiles.GlaiveBeamHoldout>();
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.UseSound = null;
            item.channel = true;
            item.autoReuse = true;
            item.shootSpeed = 30;
            item.useAnimation = 230;
            item.useTime = 200;
            item.value = 8000000;
            
        }
        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.GlaiveBeamHoldout>()] <= 0;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GlaiveBeamItemGlowmask];
            spriteBatch.Draw(texture, new Vector2(item.position.X - Main.screenPosition.X + item.width * 0.5f, item.position.Y - Main.screenPosition.Y + item.height - texture.Height * 0.5f + 2f), 
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        public override Vector2? HoldoutOffset()
        {

            return new Vector2(-18, -10);
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<Items.Weapons.Ranged.FocusedEnergyBeam>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Items.GhostWyvernSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Items.BequeathedSoul>(), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 250000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
