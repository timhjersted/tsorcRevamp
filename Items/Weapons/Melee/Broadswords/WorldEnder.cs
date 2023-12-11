using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Projectiles.Melee.Swords;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    [Autoload(false)]
    public class WorldEnder : ModItem //supposed to be a normal slow sword on left click
                                      //on right click, supposed to create a projectile in front of the player(positioned to the direction of the cursor) that creates 3 slightly different zones/hitboxes with sweet spots that deal extra damage if hit
                                      //right click alternates between each style and gets a cooldown depending on how far through the combo you went
    { //check how League of Legends Aatrox champion works if ur curious, it's Q ability is the idea
        public int AltFunctionMode = 1;
        public override void SetDefaults()
        {
            Item.width = 132;
            Item.height = 132;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.damage = 500;
            Item.knockBack = 20f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Red;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            Item.shootSpeed = 135f;
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkRed;
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<WorldEnderCooldown>()))
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2) //shoot Nothing
            {
                Item.useStyle = ItemUseStyleID.Swing;
                return true;
            }
            int SwordProjectile = ModContent.ProjectileType<WorldEnderSword>();
            switch (AltFunctionMode)
            {
                case 1:
                    {
                        Projectile.NewProjectile(source, position, velocity, SwordProjectile, damage, knockback, Main.myPlayer, 1);
                        AltFunctionMode++;
                        break;
                    }
                case 2:
                    {
                        Projectile.NewProjectile(source, position, velocity, SwordProjectile, damage, knockback, Main.myPlayer, 2);
                        AltFunctionMode++;
                        break;
                    }
                case 3:
                    {
                        Projectile.NewProjectile(source, position, velocity, SwordProjectile, damage, knockback, Main.myPlayer, 3);
                        AltFunctionMode = 1;
                        break;
                    }
            }
            return true;
        }
    }
}
