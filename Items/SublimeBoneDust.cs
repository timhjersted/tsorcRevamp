using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class SublimeBoneDust : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Yellow;
            Item.value = 0;
            Item.maxStack = Item.CommonMaxStack;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 6f;
            Item.noUseGraphic = true;
            Item.consumable = true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.whoAmI == Main.LocalPlayer.whoAmI && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && player.GetModPlayer<tsorcRevampEstusPlayer>().estusHealthGain < 300)
            {
                return true;
            }

            else
            {
                return false;
            }
        }
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.LocalPlayer.whoAmI && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && player.GetModPlayer<tsorcRevampEstusPlayer>().estusHealthGain < 300)
            {
                player.GetModPlayer<tsorcRevampEstusPlayer>().estusHealthGain += 30;
                player.GetModPlayer<tsorcRevampCeruleanPlayer>().ceruleanManaGain += 40;
                Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.SublimeBoneDust.EstusFlaskPotency") + Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusHealthGain, Color.OrangeRed);
                Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.SublimeBoneDust.CeruleanFlaskPotency") + Main.LocalPlayer.GetModPlayer<tsorcRevampCeruleanPlayer>().ceruleanManaGain, Color.RoyalBlue);
                return true;
            }
            return false;
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
