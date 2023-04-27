using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class LightOfDawn : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Light of Dawn");
            Tooltip.SetDefault("Day finally breaks" +
                "Fires illuminant streaks of hallowed light");
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 42;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTurn = true;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.maxStack = 1;
            Item.damage = 50;
            Item.autoReuse = true;
            Item.knockBack = 4;
            Item.UseSound = SoundID.Item34;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 10;
            Item.crit = 2;
            Item.mana = 8;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.LightOfDawn>();
        }

        //Make it start offset from the player
        float rotation = MathHelper.TwoPi / 12f;
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            rotation += MathHelper.TwoPi / 6f;
            position += new Vector2(80, 0).RotatedBy(rotation);
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            
            modPlayer.collapseDelay = 30;
            //tsorcRevampPlayerAuraDrawLayers.StartAura(player, 150, fadeOutSpeed: 30);
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SetAuraState(tsorcAuraState.Cataluminance);
        }

        public static Texture2D crystalTexture;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if(crystalTexture == null || crystalTexture.IsDisposed)
            {
                crystalTexture = (Texture2D)ModContent.Request<Texture2D>(Texture + "Crystal", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            spriteBatch.Draw((Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type], position, null, Color.White, 0, origin, scale, SpriteEffects.None, 0);

            Color shaderColor = Color.Lerp(new Color(0.1f, 0.5f, 1f), new Color(1f, 0.3f, 0.85f), (float)Math.Pow(Math.Sin((float)Main.timeForVisualEffects / 60f), 2));
            Color rgbColor = UsefulFunctions.ShiftColor(shaderColor, (float)Main.timeForVisualEffects, 0.03f);

            spriteBatch.Draw(crystalTexture, position, null, rgbColor, 0, origin, scale, SpriteEffects.None, 0);
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DamagedCrystal>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 5);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 5);
            recipe.AddIngredient(ItemID.SoulofFright, 5);
            recipe.AddIngredient(ItemID.SoulofSight, 5);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
    }
}
