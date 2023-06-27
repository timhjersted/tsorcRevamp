using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class HealingElixir : ModItem
    {
        public static int Duration = 10;
        public static int Cooldown = 20;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 24;
            Item.consumable = true;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 45;
            Item.maxStack = 9999;
            Item.scale = 1;
            Item.rare = ItemRarityID.LightRed;
            Item.value = 1000;
            Item.buffType = BuffID.RapidHealing; // If deemed too strong, we can make our own slightly weaker regen buff, but these are pretty rare.
            Item.buffTime = Duration * 60; // 10 seconds = 30HP healed every 10 seconds, but can be used in parallel with heals that cause potion sickness.
        }

        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.HealingElixirCooldown>()))
            {
                return false;
            }

            return base.CanUseItem(player);
        }
        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.HealingElixirCooldown>(), Cooldown * 60);
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
                    || (buffType == BuffID.Ichor)
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
            Lighting.AddLight(Item.Right, 0.25f, 0.15f, 0.15f);

            if (Main.rand.NextBool(15))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X, Item.position.Y), 14, 24, 114, Item.velocity.X, Item.velocity.Y, 100, default(Color), .5f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = .8f;
            }

            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.HealingElixirGlowmask];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
    }
}
