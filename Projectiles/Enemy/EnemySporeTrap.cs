using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySporeTrap : ModProjectile
    {
        int spriteType;

        public override void SetDefaults()
        {
            //projectile.aiStyle = ProjectileID.SporeTrap;

            Projectile.height = 8;

            Projectile.light = 1;

            //projectile.penetrate = 1; //was 8

            Projectile.CloneDefaults(ProjectileID.SporeTrap);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.width = 8;
            Projectile.timeLeft = 80;
            spriteType = Main.rand.Next(2);
            //AIType = ProjectileID.SporeTrap;
        }


        //Turn into the spore cloud by expanding and changing sprites
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            Projectile.width += 20;
            Projectile.height += 20;
            spriteType = Main.rand.Next(3, 5);

            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPCID.EaterofWorldsHead)))
            {
                target.AddBuff(20, 600, false); //poisoned
            }

            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPCID.SkeletronHead)))
            {
                //target.AddBuff(30, 150, false); //bleeding
                target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 HP after several hits
                target.GetModPlayer<tsorcRevampPlayer>().CurseLevel += 1;
            }
        }




        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = null;
            if (spriteType == 0)
            {
                texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ProjectileID.SporeTrap];
            }
            else if (spriteType == 1)
            {
                texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ProjectileID.SporeTrap2];
            }
            else if (spriteType == 2)
            {
                texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ProjectileID.SporeGas];
            }
            else if (spriteType == 3)
            {
                texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ProjectileID.SporeGas2];
            }
            else if (spriteType == 4)
            {
                texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ProjectileID.SporeGas3];
            }

            if (texture != null)
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, texture.Height / 2), 1f, Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            }

            return false;
        }

        /*public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[projectile.type];
			//Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ProjectileID.SporeTrap];
			Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, null, Color.White, projectile.rotation, new Vector2(texture.Width / 2, texture.Height / 2), 1f, projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
			return false;
		}
		*/



    }
}

