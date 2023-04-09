using Microsoft.Xna.Framework;
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
            Tooltip.SetDefault("Fires illuminant streaks of hallowed light");

        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTurn = true;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.maxStack = 1;
            Item.damage = 170;
            Item.autoReuse = true;
            Item.knockBack = 4;
            Item.UseSound = SoundID.Item34;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 10;
            Item.crit = 2;
            Item.mana = 14;
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
        }
    }
}
