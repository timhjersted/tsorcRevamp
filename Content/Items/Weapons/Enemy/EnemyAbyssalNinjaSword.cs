using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    // Display-only enemy weapon. Deliberate reuse of Steel Tempest's 86x82 diagonal sword art.
    public class EnemyAbyssalNinjaSword : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Weapons/Melee/Runeterra/Yasuo/SteelTempest";
        public override bool IsLoadingEnabled(Mod mod) => true;
        public override void SetDefaults()
        {
            Item.width = 86; Item.height = 82; Item.damage = 24; Item.knockBack = 3.5f;
            Item.DamageType = DamageClass.Melee; Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 20; Item.useTime = 20; Item.value = 0;
        }
    }
}
