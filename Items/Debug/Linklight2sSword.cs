using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items;

namespace tsorcRevamp.Items.Debug
{
    class Linklight2sSword : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Broadswords/UltimaWeapon";
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Ultimate sword drawing power from the wielder" +
                                "\nDamage scales directly with life"); */
        }

        public override void SetDefaults()
        {
            Item.damage = 1500;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.width = 68;
            Item.height = 68;
            Item.knockBack = 14f;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.scale = 3f;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Cyan;
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

        public override bool CanUseItem(Player player)
        {
            return Terraria.ModLoader.ModContent.GetInstance<tsorcRevampConfig>().DebugMode;
        }
    }
}