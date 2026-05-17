using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using tsorcRevamp.Projectiles.Summon.YoungHunter;

namespace tsorcRevamp.Items.Accessories.Damage
{
    public class EyeOfTheHunt : ModItem
    {
        public const float CritDamage = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritDamage);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 34;
            Item.accessory = true;
            Item.value = PriceByRarity.Pink_5;
            Item.expert = true;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().HasYoungHunterAccessory = true;

            player.aggro -= 400;
            
            player.AddBuff(ModContent.BuffType<Buffs.Accessories.YoungHunterBuff>(), 2);

            if (player.ownedProjectileCounts[ModContent.ProjectileType<YoungHunter>()] <= 0)
            {
                Projectile.NewProjectile(
                    player.GetSource_Accessory(Item),
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<YoungHunter>(),
                    40, 
                    2f, 
                    player.whoAmI
                );
            }
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual && player.direction == 1)
            {
                int dust = Dust.NewDust(player.position + new Vector2(9, 9), 2, 2, 107, 0f, 0f, 100, default(Color), .7f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.11f;
                Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 1f;
                Main.dust[dust].velocity.X = Main.dust[dust].velocity.X + 2f;
            }
        }

        public override void UpdateVanity(Player player)
        {
            if (player.direction == 1)
            {
                int dust = Dust.NewDust(player.position + new Vector2(9, 9), 2, 2, 107, 0f, 0f, 100, default(Color), .7f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.11f;
                Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 1f;
                Main.dust[dust].velocity.X = Main.dust[dust].velocity.X - 2f;
            }
        }
    }
}