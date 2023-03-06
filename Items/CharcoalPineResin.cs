using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    public class CharcoalPineResin : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Charcoal Pine Resin");
            /* Tooltip.SetDefault("Black charcoal-like pine resin\n" +
                               "Consume to imbue melee weapons with fire for 4 minutes\n" +
                               "Particularly effective against woody enemies\n" +
                               "+10% melee damage"); */
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.scale = 0.9f;
            Item.maxStack = 99;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item20;
            Item.useStyle = ItemUseStyleID.HoldUp; //no idea why it still swings sometimes
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.value = 500;
            Item.consumable = true;
            Item.buffType = BuffID.WeaponImbueFire;
            Item.buffTime = 14400;
        }

        public override bool? UseItem(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.MagicWeapon>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicWeapon>()) || player.HasBuff(ModContent.BuffType<Buffs.CrystalMagicWeapon>()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (Main.rand.NextBool(10))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 10, Item.position.Y + 10), 16, 16, DustID.Torch, Item.velocity.X, Item.velocity.Y - 2f, 100, default(Color), .8f)];
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = 1f;
            }
            return true;
        }
    }
}