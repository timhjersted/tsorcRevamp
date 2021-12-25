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
            Tooltip.SetDefault("Charred, ashen bones" +
                             "\nCast into the flames of a Bonfire to increase Estus" +
                             "\nFlask potency, increasing the amount of HP healed");
        }

        public override void SetDefaults()
        {
            item.width = 24;
            item.height = 20;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.rare = ItemRarityID.Yellow;
            item.value = 0;
            item.maxStack = 10;
            item.useAnimation = 20;
            item.useTime = 20;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 6f;
            item.noUseGraphic = true;
            item.consumable = true;
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
            Texture2D texture = Main.itemTexture[item.type];
            spriteBatch.Draw(texture, new Vector2(item.position.X - Main.screenPosition.X + item.width * 0.5f, item.position.Y - Main.screenPosition.Y + item.height - texture.Height * 0.5f + 8f),
                new Rectangle(0, 0, texture.Width, texture.Height), lightColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            return false;
        }


    }
}
