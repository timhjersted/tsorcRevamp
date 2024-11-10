using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Claws
{
    public class ForgottenBurningFist : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Randomly casts a great fireball explosion.");
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.damage = 40;
            Item.width = 24;
            Item.height = 20;
            Item.knockBack = 3;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 9;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 9;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
{
    // Calcule la direction du projectile en fonction de la position du joueur et de la cible (ennemi)
    Vector2 direction = target.Center - player.Center;  // Direction du joueur vers l'ennemi
    direction.Normalize();  // Normalise le vecteur pour obtenir une direction unitaire

    // Crée le projectile "Flames" avec 33% des dégâts de l'arme et une vélocité réduite de 50%
    float speed = 3f;  // Exemple de vitesse, vous pouvez ajuster selon vos besoins
    Projectile.NewProjectileDirect(
        Item.GetSource_FromThis(), 
        player.Center, 
        direction * speed,  // Vitesse dans la direction calculée
        ProjectileID.Flames, 
        (int)(player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage) * 0.5f), // Réduit les dégâts à 33% de ceux de l'arme
        player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), 
        Main.myPlayer
    );
}
    }
}
