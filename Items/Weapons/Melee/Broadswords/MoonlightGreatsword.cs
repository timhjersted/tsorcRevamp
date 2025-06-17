using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class MoonlightGreatsword : ModItem
    {
        public static int NightDamageIncrease => 20;

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ModContent.RarityType<OrangeRed>();
            Item.damage = 600;
            Item.height = 88;
            Item.width = 88;
            Item.knockBack = 10f;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useAnimation = 27;
            Item.useTime = 27;
            Item.scale = 1f;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 1000000;
            Item.shoot = ModContent.ProjectileType<MLGSCrescent>();
            Item.shootSpeed = 12f; 
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Teal;
        }

        public static DamageClass GetDamageType(Player player)
        {
            DamageClass dmgClass;

            if (player.GetTotalDamage(DamageClass.Magic).ApplyTo(100) < player.GetTotalDamage(DamageClass.Melee).ApplyTo(100))
            {
                dmgClass = DamageClass.Melee;
            }
            else
            {
                dmgClass =  DamageClass.Magic;
            }

            return dmgClass;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            Item.DamageType = GetDamageType(player);
        }

        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!Main.dayTime)
                modifiers.SourceDamage.Base *= 1f + (NightDamageIncrease / 100f);
        }

        Texture2D glowTexture;
        Texture2D baseTexture;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (glowTexture == null || glowTexture.IsDisposed)
            {
                glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatswordGlowmask];
            }
            if (baseTexture == null || baseTexture.IsDisposed)
            {
                baseTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatsword];
            }
            spriteBatch.Draw(baseTexture, position, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), drawColor, 0f, origin, scale, SpriteEffects.None, 0.1f);
            if (!Main.dayTime)
            {
                spriteBatch.Draw(glowTexture, position, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, 0f, origin, scale, SpriteEffects.None, 0.1f);
            }

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (glowTexture == null || glowTexture.IsDisposed)
            {
                glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatswordGlowmask];
            }
            if (baseTexture == null || baseTexture.IsDisposed)
            {
                baseTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatsword];
            }
            spriteBatch.Draw(baseTexture, Item.Center - Main.screenPosition, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), lightColor, rotation, new Vector2(Item.width / 2, Item.height / 2), Item.scale, SpriteEffects.None, 0.1f);
            if (!Main.dayTime)
            {
                spriteBatch.Draw(glowTexture, Item.Center - Main.screenPosition, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, rotation, new Vector2(Item.width / 2, Item.height / 2), Item.scale, SpriteEffects.None, 0.1f);
            }

            return false;
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 89, player.velocity.X, player.velocity.Y, 100, default, .8f);
            Main.dust[dust].noGravity = true;
        }

        // Show the proper damage in the tooltip
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Item.DamageType = GetDamageType(Main.LocalPlayer);

            if (!Main.dayTime)
            {
                foreach (TooltipLine line in tooltips)
                {
                    if (line.Name == "Damage")
                    {
                        Player player = Main.LocalPlayer;

                        string nightDamage = ((int)(player.GetWeaponDamage(Item) * (1f + NightDamageIncrease / 100f))).ToString();
                        string typeOfDamage = Item.DamageType == DamageClass.Magic ? "magic" : "melee";

                        line.Text = string.Join(" ", nightDamage, typeOfDamage, "damage");
                    }
                }
            }
        }
    }
}
