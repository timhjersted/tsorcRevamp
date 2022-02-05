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
            DisplayName.SetDefault("Charcoal Pine Resin");
            Tooltip.SetDefault("Black charcoal-like pine resin\n" +
                               "Consume to imbue melee weapons with fire for 4 minutes\n" +
                               "Particularly effective against woody enemies\n" +
                               "+10% melee damage");
        }

        public override void SetDefaults()
        {
            item.width = 40;
            item.height = 40;
            item.scale = 0.9f;
            item.maxStack = 99;
            item.rare = ItemRarityID.Blue;
            item.UseSound = SoundID.Item20;
            item.useStyle = ItemUseStyleID.HoldingUp; //no idea why it still swings sometimes
            item.useTime = 40;
            item.useAnimation = 40;
            item.value = 500;
            item.consumable = true;
            item.buffType = BuffID.WeaponImbueFire;
            item.buffTime = 14400;
        }

        public override bool UseItem(Player player)
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
            if (Main.rand.Next(10) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 10, item.position.Y + 10), 16, 16, DustID.Fire, item.velocity.X, item.velocity.Y - 2f, 100, default(Color), .8f)];
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = 1f;
            }
            return true;
        }
    }
}