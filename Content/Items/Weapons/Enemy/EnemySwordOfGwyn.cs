using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    ///<summary>
    ///The invader-side copy of the Great Lord Greatsword, held by the LordGwyn puppet. Shares the
    ///player weapon's sprite (Broadswords/SwordOfGwyn.png); the player-obtainable version with its
    ///special abilities is developed separately as the boss-bag drop.
    ///</summary>
    public class EnemySwordOfGwyn : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(SwordOfLordGwyn));

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.damage = 1;
            Item.knockBack = 8f;
            Item.width = 128;
            Item.height = 128;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 32;
            Item.useTime = 32;
            Item.value = 0;
            Item.shoot = ModContent.ProjectileType<Projectiles.InvisibleNothingProj>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed; //the First Flame's edge
        }
    }
}
