using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class CrescentMoonSword : ModItem //Same DPS as the Ancient Blood Lance when at close range, less than half when only the projectile hits.
    {                                        //Projectile has same range as the Ancient Blood Lance
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crescent Moon Sword");
            Tooltip.SetDefault("Ringfinger Leonhard's weapon of choice," +
                               "\na type of shotel imbued with the power of the moon" + 
                               "\nShoots beams of crescent moonlight that pierce walls");
        }

        public override void SetDefaults()
        {
            item.autoReuse = true;
            item.rare = ItemRarityID.Cyan;
            item.damage = 36;
            item.width = 40;
            item.height = 40;
            item.knockBack = 4.5f;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 25;
            item.useTime = 25;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 100000;
            item.shoot = ModContent.ProjectileType<Projectiles.CMSCrescent>();
            item.shootSpeed = 4.5f;
        }
        public override bool OnlyShootOnSwing => true;

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if ((player.name == "Zeodexic")/* || (player.name == "Chroma TSORC test")*/) //Add whatever names you use -C
            {
                item.shoot = ModContent.ProjectileType<Projectiles.CrescentTrue>();
                item.shootSpeed = 22f;
            }

            return true;
        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat)
        {
            if ((player.name == "Zeodexic")/* || (player.name == "Chroma TSORC test")*/) //Add whatever names you use -C
            {
                item.damage = 120; //change this to whatever suits your testing needs -C
            }
        }
    }
}
