using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class UltimaWeapon : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Ultimate sword drawing power from the wielder" +
                                "\nThe true form of your father's legendary sword revealed" +
                                "\nDoes 150 damage when at full health, and 100 damage at 200 health, scaling with current HP");
        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.Pink;
            item.damage = 50;
            item.height = 64;
            item.width = 64;
            item.knockBack = 14f;
            item.melee = true;
            item.autoReuse = true;
            item.useAnimation = 21;
            item.useTime = 21;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 1000000;
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Excalibur, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 5);
            recipe.AddIngredient(ItemID.SoulofSight, 5);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 85000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        Texture2D glowTexture;
        Texture2D baseTexture;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if(glowTexture == null || glowTexture.IsDisposed)
            {
                glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.UltimaWeaponGlowmask];
            }
            if (baseTexture == null || baseTexture.IsDisposed)
            {
                baseTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.UltimaWeapon];
            }

            spriteBatch.Draw(baseTexture, position, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), drawColor, 0, origin, scale, SpriteEffects.None, 0.1f);
            spriteBatch.Draw(glowTexture, position, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, 0, origin, scale, SpriteEffects.None, 0.1f);            

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (glowTexture == null || glowTexture.IsDisposed)
            {
                glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.UltimaWeaponGlowmask];
            }
            if (baseTexture == null || baseTexture.IsDisposed)
            {
                baseTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.UltimaWeapon];
            }

            spriteBatch.Draw(baseTexture, item.Center - Main.screenPosition, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), lightColor, rotation, new Vector2(item.width / 2, item.height / 2), item.scale, SpriteEffects.None, 0.1f);            
            spriteBatch.Draw(glowTexture, item.Center - Main.screenPosition, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, rotation, new Vector2(item.width / 2, item.height / 2), item.scale, SpriteEffects.None, 0.1f);            

            return false;
        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat) {

            //I don't care if this many parentheses aren't necessary, integer division is hell
            add += ((((float)player.statLife) / ((float)player.statLifeMax2)) * ((float)2)) * player.meleeDamage;
        }
    }
}
