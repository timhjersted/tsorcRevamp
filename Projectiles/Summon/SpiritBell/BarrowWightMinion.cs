using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Summon.SpiritBell;

class BarrowWightMinion : ModProjectile
{

    int chargeDamage = 0;
    bool chargeDamageFlag = false;
    float chargetimer0;
    float chargeTimer;
    float chargeTimer2;
    float chargeTimer3;
    NPC target;

    public override void SetStaticDefaults()
    {

        Main.projFrames[Projectile.type] = 4;
        // This is necessary for right-click targeting
        ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

        Main.projPet[Projectile.type] = true; // Denotes that this projectile is a pet or minion

        ProjectileID.Sets.MinionSacrificable[Projectile.type] = true; // This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned
        ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true; // Make the cultist resistant to this projectile, as it's resistant to all homing projectiles.
    }

    public sealed override void SetDefaults()
    {
        Projectile.width = 104;
        Projectile.height = 93;
        Projectile.tileCollide = false; // Makes the minion go through tiles freely

        // These below are needed for a minion weapon
        Projectile.friendly = true; // Only controls if it deals damage to enemies on contact (more on that later)
        Projectile.minion = true; // Declares this as a minion (has many effects)
        Projectile.DamageType = DamageClass.Summon; // Declares the damage type (needed for it to deal damage)
        Projectile.minionSlots = 0f; // Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
        Projectile.penetrate = -1; // Needed so the minion doesn't despawn on collision with enemies or tiles

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;

        Projectile.aiStyle = -1;
    }


    public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
    {
        chargeTimer2 = 0f;

        if (Main.rand.NextBool(2))
        {
            target.AddBuff(BuffID.BrokenArmor, 1200);
            target.AddBuff(BuffID.Chilled, 1200);
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 36000);
        }

        if (crit == true)
        {
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 45, 0.3f, 0.3f, 200, default, 1f);
            Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 2f);
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 45, 0.2f, 0.2f, 200, default, 2f);
            Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 3f);
            Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 2f);
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 45, 0.2f, 0.2f, 200, default, 4f);
            Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 4f);
            Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 2f);
            Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 4f);
        }
    }
    // Here you can decide if your minion breaks things like grass or pots
    public override bool? CanCutTiles()
    {
        return false;
    }

    // This is mandatory if your minion deals contact damage (further related stuff in AI() in the Movement region)
    public override bool MinionContactDamage()
    {
        return true;
    }
    public override void AI()
    {

        int? index = UsefulFunctions.GetClosestEnemyNPC(Projectile.Center);

        if (index == null)
        {
            Idle();
        }
        else
        {
            target = Main.npc[index.Value];
            Attack();
        }


    }

    public void Attack()
    {

        chargeTimer += 0.3f;
        if (chargeTimer >= 10f)
        {
            Projectile.netUpdate = true;



            // charge forward code 
            if (Main.rand.NextBool(2050))
            {
                chargeDamageFlag = true;
                Projectile.velocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, target.Center, 8);
                chargeTimer = 1f;
                Projectile.netUpdate = true;
            }
            if (chargeDamageFlag == true)
            {
                Projectile.damage = 55;
                chargeDamage++;
            }
            if (chargeDamage >= 115)
            {
                chargeDamageFlag = false;
                Projectile.damage = 55;
                chargeDamage = 0;
            }




        }
        if (chargeTimer2 >= 0f)
        {
            int num258 = 16;
            bool flag26 = false;
            bool flag27 = false;
            if (Projectile.position.X > chargetimer0 - (float)num258 && Projectile.position.X < chargetimer0 + (float)num258)
            {
                flag26 = true;
            }
            else
            {
                if ((Projectile.velocity.X < 0f && Projectile.direction > 0) || (Projectile.velocity.X > 0f && Projectile.direction < 0))
                {
                    flag26 = true;
                }
            }
            num258 += 24;
            if (Projectile.position.Y > chargeTimer - (float)num258 && Projectile.position.Y < chargeTimer + (float)num258)
            {
                flag27 = true;
            }
            if (flag26 && flag27)
            {
                chargeTimer2 += 1f;
                if (chargeTimer2 >= 60f)
                {
                    chargeTimer2 = -200f;
                    Projectile.direction *= -1;
                    Projectile.velocity.X = Projectile.velocity.X * -1f;
                    Projectile.tileCollide = false;
                }
            }
            else
            {
                chargetimer0 = Projectile.position.X;
                chargeTimer = Projectile.position.Y;
                chargeTimer2 = 0f;
            }
            //something missing here
        }
        else
        {
            chargeTimer2 += 1f;
            if (target.position.X + (float)(target.width / 2) > Projectile.position.X + (float)(Projectile.width / 2))
            {
                Projectile.direction = -1;
            }
            else
            {
                Projectile.direction = 1;
            }
        }
        if (Projectile.position.Y > target.position.Y)
        {
            Projectile.velocity.Y -= .05f;
        }
        if (Projectile.position.Y < target.position.Y)
        {
            Projectile.velocity.Y += .05f;
        }
        if (Projectile.tileCollide)
        {
            Projectile.velocity.X = Projectile.oldVelocity.X * -0.4f;
            if (Projectile.direction == -1 && Projectile.velocity.X > 0f && Projectile.velocity.X < 1f)
            {
                Projectile.velocity.X = 1f;
            }
            if (Projectile.direction == 1 && Projectile.velocity.X < 0f && Projectile.velocity.X > -1f)
            {
                Projectile.velocity.X = -1f;
            }
        }
        if (Projectile.tileCollide)
        {
            Projectile.velocity.Y = Projectile.oldVelocity.Y * -0.25f;
            if (Projectile.velocity.Y > 0f && Projectile.velocity.Y < 1f)
            {
                Projectile.velocity.Y = 1f;
            }
            if (Projectile.velocity.Y < 0f && Projectile.velocity.Y > -1f)
            {
                Projectile.velocity.Y = -1f;
            }
        }
        float topSpeed = .5f; //looks problematic
        if (Projectile.direction == -1 && Projectile.velocity.X > -topSpeed)
        {
            Projectile.velocity.X = Projectile.velocity.X - 0.1f;
            if (Projectile.velocity.X > topSpeed)
            {
                Projectile.velocity.X = Projectile.velocity.X - 0.1f;
            }
            else
            {
                if (Projectile.velocity.X > 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X + 0.05f;
                }
            }
            if (Projectile.velocity.X < -topSpeed)
            {
                Projectile.velocity.X = -topSpeed;
            }
        }
        else
        {
            if (Projectile.direction == 1 && Projectile.velocity.X < topSpeed)
            {
                Projectile.velocity.X = Projectile.velocity.X + 0.1f;
                if (Projectile.velocity.X < -topSpeed)
                {
                    Projectile.velocity.X = Projectile.velocity.X + 0.1f;
                }
                else
                {
                    if (Projectile.velocity.X < 0f)
                    {
                        Projectile.velocity.X = Projectile.velocity.X - 0.05f;
                    }
                }
                if (Projectile.velocity.X > topSpeed)
                {
                    Projectile.velocity.X = topSpeed;
                }
            }
        }
        if (target.direction == -1 && Projectile.velocity.Y > -2.5)
        {
            Projectile.velocity.Y = Projectile.velocity.Y - 0.04f;
            if (Projectile.velocity.Y > 2.5)
            {
                Projectile.velocity.Y = Projectile.velocity.Y - 0.05f;
            }
            else
            {
                if (Projectile.velocity.Y > 0f)
                {
                    Projectile.velocity.Y = Projectile.velocity.Y + 0.03f;
                }
            }
            if (Projectile.velocity.Y < -2.5)
            {
                Projectile.velocity.Y = -2.5f;
            }
        }
        else
        {
            if (target.direction == 1 && Projectile.velocity.Y < 2.5)
            {
                Projectile.velocity.Y = Projectile.velocity.Y + 0.04f;
                if (Projectile.velocity.Y < -2.5)
                {
                    Projectile.velocity.Y = Projectile.velocity.Y + 0.05f;
                }
                else
                {
                    if (Projectile.velocity.Y < 0f)
                    {
                        Projectile.velocity.Y = Projectile.velocity.Y - 0.03f;
                    }
                }
                if (Projectile.velocity.Y > 2.5)
                {
                    Projectile.velocity.Y = 2.5f;
                }
            }
        }
        Lighting.AddLight((int)Projectile.position.X / 16, (int)Projectile.position.Y / 16, 0.4f, 0f, 0.25f);
        return;
    }
    public void Idle()
    {
    }

    #region Frames
    public void Visuals()
    {
        int num = 1;
        if (!Main.dedServ)
        {
            num = TextureAssets.Projectile[Projectile.type].Value.Height / Projectile.frameCounter;
        }
        if (Projectile.velocity.X < 0)
        {
            Projectile.spriteDirection = -1;
        }
        else
        {
            Projectile.spriteDirection = 1;
        }
        Projectile.rotation = Projectile.velocity.X * 0.08f;
        Projectile.frameCounter += 1;
        if (Projectile.frameCounter >= 4.0)
        {
            Projectile.frameCounter = 0;
        }

        if (chargeTimer3 == 0)
        {
            Projectile.alpha = 0;
        }
        else
        {
            Projectile.alpha = 200;
        }
    }
    #endregion



    /* what the hell IS this? i cant find anything about it
     public int MagicDefenseValue() 
        { 
            return 5;
        } 
      
     */
}
