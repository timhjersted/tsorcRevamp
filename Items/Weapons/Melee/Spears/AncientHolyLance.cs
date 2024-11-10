using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class AncientHolyLance : ModItem
    {
        private int swingCounter = 0; 

        public override void SetStaticDefaults()
        {
            ItemID.Sets.SkipsInitialUseSound[Item.type] = true; 
            ItemID.Sets.Spears[Item.type] = true; 
        }

        public override void SetDefaults() 
        {
            Item.rare = ItemRarityID.LightRed;
            Item.value = ItemRarityID.LightRed;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 24;
            Item.useTime = 24;
            Item.UseSound = SoundID.Item71;
            Item.autoReuse = true;
            Item.damage = 48;
            Item.knockBack = 6.5f;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.shootSpeed = 3.7f;
            Item.shoot = ModContent.ProjectileType<AncientHolyLanceProj>();
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override bool? UseItem(Player player)
        {
            if (!Main.dedServ && Item.UseSound.HasValue)
            {
                SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
            }

            swingCounter++; 

            if (swingCounter >= 2)
            {
                swingCounter = 0; 

                Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, player.DirectionTo(Main.MouseWorld) * 15, ProjectileID.FairyQueenRangedItemShot, Item.damage, Item.knockBack, player.whoAmI
                );
            }

            return null;
        }

        public override void AddRecipes() 
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilHalberd);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
