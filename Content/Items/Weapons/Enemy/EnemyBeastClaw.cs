using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only bladed glove (Fetid Baghnakhs style) the Oolacile Cultist fights with
    /// below half health. Vanilla bladed gloves are a plain fast swing (useStyle 1, useAnimation 8).</summary>
    public class EnemyBeastClaw : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 30;
            Item.damage = 1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.DamageType = DamageClass.Melee;
        }
    }
}
