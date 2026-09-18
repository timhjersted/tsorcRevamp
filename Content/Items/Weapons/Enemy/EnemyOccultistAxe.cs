using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>
    /// Presentation-only great axe-maul the Grand Occultist of Oolacile draws at 50% HP: axe blade on the
    /// right of the head, hammer face on the left. 112x104 sprite following the broadsword convention —
    /// haft butt at the bottom-left (4,101), head at the upper-right (107,6), a ~140px diagonal. The boss
    /// grips it 25% along that haft (MeleeHandleNorm), leaving ~105px of reach past the hand.
    /// </summary>
    public class EnemyOccultistAxe : ModItem
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/GrandOolacileAxe";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.damage = 1;
            Item.knockBack = 11f;
            Item.width = 112;
            Item.height = 104;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            // Longest authored swing in the kit (Dark Judgment, 46t) — the Use frames run off this.
            Item.useAnimation = 46;
            Item.useTime = 46;
            Item.value = 0;
            Item.shoot = ModContent.ProjectileType<Projectiles.InvisibleNothingProj>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = new Color(150, 20, 45);
        }
    }
}
