using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class HealingElixir : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Heals the player of a multitude of debuffs" + 
                                "\nGrants the rapid healing buff");
        }
        public override void SetDefaults() {
            item.width = 14;
            item.height = 24;
            item.consumable = true;
            item.useAnimation = 30;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 45;
            item.maxStack = 200;
            item.scale = 1;
            item.rare = ItemRarityID.LightRed;
            item.value = 1000;
            item.buffType = BuffID.RapidHealing; // If deemed too strong, we can make our own slightly weaker regen buff, but these are pretty rare.
            item.buffTime = 600; // 10 seconds = 30HP healed every 10 seconds, but can be used in parallel with heals that cause potion sickness.
        }

        public override bool UseItem(Player player)
        {
            int buffIndex = 0;

            foreach (int buffType in player.buffType)
            {

                if ((buffType == BuffID.Bleeding)
                    || (buffType == BuffID.Poisoned)
                    || (buffType == BuffID.Confused)
                    || (buffType == BuffID.BrokenArmor)
                    || (buffType == BuffID.Darkness)
                    || (buffType == BuffID.OnFire)
                    || (buffType == BuffID.Slow)
                    || (buffType == BuffID.Weak)
                    || (buffType == BuffID.CursedInferno)
                    )
                {
                    player.buffTime[buffIndex] = 0;
                }
                buffIndex++;
            }
            return true;
        }
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(item.Right, 0.25f, 0.15f, 0.15f);

            if (Main.rand.Next(15) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X, item.position.Y), 14, 24, 114, item.velocity.X, item.velocity.Y, 100, default(Color), .5f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = .8f;
            }

            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.HealingElixirGlowmask];
            spriteBatch.Draw(texture, new Vector2(item.position.X - Main.screenPosition.X + item.width * 0.5f, item.position.Y - Main.screenPosition.Y + item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
