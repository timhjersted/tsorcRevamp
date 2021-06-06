using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class CrescentMoonSword : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crescent Moon Sword");
            Tooltip.SetDefault("Ringfinger Leonhard's weapon of choice," +
                               "\na type of shotel imbued with the power of the moon" + 
                               "\nShoots beams of crescent moonlight when above 80% health");
        }

        public override void SetDefaults()
        {
            item.autoReuse = true;
            item.rare = ItemRarityID.Cyan;
            item.damage = 24;
            item.width = 40;
            item.height = 40;
            item.knockBack = 4f;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 25;
            item.useTime = 25;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 100000;
            item.shoot = ModContent.ProjectileType<Projectiles.Crescent>();
            item.shootSpeed = 12f;
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if ((player.name == "Zeodexic") || (player.name == "Chroma TSORC test")) //Add whatever names you use -C
            {
                item.shoot = ModContent.ProjectileType<Projectiles.CrescentTrue>();
                item.shootSpeed = 22f;
                return true;
            }
            else if (player.statLife > (player.statLifeMax2 * 0.8f))
            {
                return true;
            }

            return false;
        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat)
        {
            if ((player.name == "Zeodexic") || (player.name == "Chroma TSORC test")) //Add whatever names you use -C
            {
                item.damage = 120; //change this to whatever suits your testing needs -C
            }
        }
    }
}
