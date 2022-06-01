using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Ammo
{
    public class PowerBolt : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.ammo = Mod.Find<ModItem>("Bolt").Type;
            Item.damage = 40;
            Item.height = 28;
            Item.knockBack = 3f;
            Item.maxStack = 2000;
            Item.shootSpeed = 3.5f;
            Item.value = 1000;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt>();
        }
    }
}
