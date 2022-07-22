using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class UltimaWeapon : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Ultimate sword drawing power from the wielder" +
                                "\nDamage scales directly with health");
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightPurple;
            Item.damage = 500;
            Item.height = 64;
            Item.width = 64;
            Item.knockBack = 14f;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useAnimation = 16;
            Item.scale = 2.1f;
            Item.useTime = 16;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Yellow_8;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.TerraBlade, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("BlueTitanite").Type, 20);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfArtorias").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 144000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        static Texture2D glowTexture;
        static Texture2D baseTexture;
        public override bool? UseItem(Player player)
        {
            //Item.scale *= player.GetAttackSpeed(DamageClass.Melee);
            return base.UseItem(player);
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (glowTexture == null || glowTexture.IsDisposed)
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

            spriteBatch.Draw(baseTexture, Item.Center - Main.screenPosition, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), lightColor, rotation, new Vector2(Item.width / 2, Item.height / 2), Item.scale, SpriteEffects.None, 0.1f);
            spriteBatch.Draw(glowTexture, Item.Center - Main.screenPosition, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, rotation, new Vector2(Item.width / 2, Item.height / 2), Item.scale, SpriteEffects.None, 0.1f);

            return false;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            //I don't care if this many parentheses aren't necessary, integer division is hell
            damage *= (((float)player.statLife) / ((float)player.statLifeMax2));
        }
    }
}
