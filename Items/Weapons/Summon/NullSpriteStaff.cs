using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon
{
    public class NullSpriteStaff : ModItem
    {

        public override string Texture => "tsorcRevamp/Projectiles/Summon/NullSprite"; //placeholder

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons a null sprite to fight for you.");
        }

        public override void SetDefaults()
        {
            Item.damage = 39;
            Item.knockBack = 1f;
            Item.width = 44; //placeholder, item doesnt have a sprite yet
            Item.height = 50;
            Item.useTime = Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.buyPrice(0, 15, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = SoundID.Item44;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<Buffs.Summon.NullSpriteBuff>();
            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.NullSprite>();
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            player.AddBuff(Item.buffType, 2);
            position = Main.MouseWorld;
            return true;
        }
    }
}
