using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    // Enemy-copy display weapon for the Hero of Lumelia invader puppet.  The real hitbox/damage
    // come from the PuppetNPC melee system; this item only supplies the held sprite + slash color.
    public class EnemyAncientFireSword : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Broadswords/AncientFireSword";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.damage = 1;
            Item.knockBack = 4f;
            Item.width = 34;
            Item.height = 38;
            Item.scale = 1.1f;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 26;
            Item.useTime = 26;
            Item.value = 0;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed;
        }
    }
}
