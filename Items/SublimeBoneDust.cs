using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class SublimeBoneDust : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Charred, ashen bones" +
                             "\nCast into the flames of a Bonfire to increase Estus" +
                             "\nFlask potency, increasing the amount of HP healed"); */
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Yellow;
            Item.value = 0;
            Item.maxStack = 10;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 6f;
            Item.noUseGraphic = true;
            Item.consumable = true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.whoAmI == Main.LocalPlayer.whoAmI && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && player.GetModPlayer<tsorcRevampEstusPlayer>().estusHealthGain < 120)
            {
                player.GetModPlayer<tsorcRevampEstusPlayer>().estusHealthGain += 20;
                Main.NewText("Estus Flask potency increased! Heal per charge: " + Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusHealthGain, Color.OrangeRed);
                return true;
            }

            else
            {
                return false;
            }
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 8f),
                new Rectangle(0, 0, texture.Width, texture.Height), lightColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);

            return false;
        }


    }
}
