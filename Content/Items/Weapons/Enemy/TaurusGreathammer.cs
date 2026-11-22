using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Hammers;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Taurus Knight's two-handed great hammer. Presentation and archetype only: it is never
    /// dropped, and the puppet's own MeleeDamage drives every hit. Hammer > 0 is what makes
    /// WeaponArchetypeTables.DetectMelee read it as WeaponArchetype.Hammer.</summary>
    public class TaurusGreathammer : ModItem
    {
        // Intentional, permanent texture share: the Doomhammer's dark steel head and long haft is the art.
        // Taurus draws it at 1.6x (TaurusKnight.MeleeWeaponDrawScale) so it reads as a two-hander.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Doomhammer));

        public override void SetDefaults()
        {
            Item.width = 46;
            Item.height = 46;
            Item.damage = 40;
            Item.knockBack = 9f;
            Item.hammer = 100;
            Item.useAnimation = 44;
            Item.useTime = 44;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Red;
            Item.value = 0;
        }
    }
}
