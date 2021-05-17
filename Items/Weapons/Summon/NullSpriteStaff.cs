using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon {
    public class NullSpriteStaff : ModItem {

        public override string Texture => "tsorcRevamp/Projectiles/Summon/NullSprite"; //placeholder

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons a null sprite to fight for you.");
        }

        public override void SetDefaults() {
            item.damage = 39;
            item.knockBack = 1f;
            item.width = 44; //placeholder, item doesnt have a sprite yet
            item.height = 50;
            item.useTime = item.useAnimation = 36;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = Item.buyPrice(0, 15, 0, 0);
            item.rare = ItemRarityID.Yellow;
            item.UseSound = SoundID.Item44;
            item.noMelee = true;
            item.summon = true;
            item.buffType = ModContent.BuffType<Buffs.Summon.NullSpriteBuff>();
            item.shoot = ModContent.ProjectileType<Projectiles.Summon.NullSprite>();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
            player.AddBuff(item.buffType, 2);
            position = Main.MouseWorld;
            return true;
        }
    }
}
