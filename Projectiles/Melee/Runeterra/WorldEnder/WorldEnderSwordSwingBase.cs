using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;
using tsorcRevamp.UI;

namespace tsorcRevamp.Projectiles.Melee.Runeterra.WorldEnder
{
    public abstract class WorldEnderSwordSwingBase : ModProjectile
    {
        public abstract int Frames { get; }
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int Tier {  get; }
        public int ProjectileLifetime = 60;
        public Vector2 Velocity;
        public Vector2 Hitbox1 = Vector2.Zero;
        public Vector2 Hitbox2 = Vector2.Zero;
        public Vector2 Hitbox3 = Vector2.Zero;
        public Vector2 Hitbox4 = Vector2.Zero;
        public Vector2 Hitbox5 = Vector2.Zero;
        public Vector2 Hitbox6 = Vector2.Zero;
        public Vector2 Hitbox7 = Vector2.Zero;
        public Vector2 Hitbox8 = Vector2.Zero;
        public Vector2 Hitbox9 = Vector2.Zero;
        public Vector2 Hitbox10 = Vector2.Zero;
        public Vector2 Hitbox11 = Vector2.Zero;
        public Vector2 Hitbox12 = Vector2.Zero;
        public Vector2 Hitbox13 = Vector2.Zero;
        public Vector2 Hitbox14 = Vector2.Zero;
        public Vector2 Hitbox15 = Vector2.Zero;
        public Vector2 Hitbox16 = Vector2.Zero;
        public Vector2 Hitbox17 = Vector2.Zero;
        public Vector2 Hitbox18 = Vector2.Zero;
        public Vector2 HitboxSize = Vector2.Zero;

        public Vector2 CritHitbox1 = Vector2.Zero;
        public Vector2 CritHitbox2 = Vector2.Zero;
        public Vector2 CritHitbox3 = Vector2.Zero;
        public Vector2 CritHitbox4 = Vector2.Zero;
        public Vector2 CritHitbox5 = Vector2.Zero;
        public Vector2 CritHitbox6 = Vector2.Zero;
        public Vector2 CritHitbox7 = Vector2.Zero;
        public Vector2 CritHitbox8 = Vector2.Zero;
        public Vector2 CritHitbox9 = Vector2.Zero;
        public Vector2 CritHitbox10 = Vector2.Zero;
        public Vector2 CritHitbox11 = Vector2.Zero;
        public Vector2 CritHitbox12 = Vector2.Zero;
        public Vector2 CritHitboxSize = Vector2.Zero;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Frames;
        }
        public override void SetDefaults()
        {
            Projectile.width = Width;
            Projectile.height = Height;
            Projectile.aiStyle = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = ProjectileLifetime;
            Projectile.ownerHitCheck = true; // Prevents hits through tiles. Most melee weapons that use projectiles have this
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 100;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Velocity = Projectile.velocity;
            Projectile.spriteDirection = 1;
            Projectile.velocity = Vector2.Zero;
            switch (Tier)
            {
                case 1:
                    {
                        player.AddBuff(ModContent.BuffType<WorldEnderTimer>(), (int)(WorldEnderItem.TimeWindow * 60));
                        break;
                    }
                case 2:
                    {
                        player.AddBuff(ModContent.BuffType<WorldEnderTimer>(), (int)(WorldEnderItem.TimeWindow * 60));
                        break;
                    }
                case 3:
                    {
                        player.ClearBuff(ModContent.BuffType<WorldEnderTimer>());
                        player.AddBuff(ModContent.BuffType<WorldEnderCooldown>(), (int)(WorldEnderItem.ThirdSwingCooldown / player.GetTotalAttackSpeed(DamageClass.Melee) * 60f));
                        break;
                    }
            }
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.rotation = Velocity.ToRotation();

            player.heldProj = Projectile.whoAmI;

            // Keep locked onto the player, but extend further based on the given velocity (Requires ShouldUpdatePosition returning false to work)
            Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, reverseRotation: false, addGfxOffY: false);
            Projectile.Center = playerCenter + Velocity;

            int frameSpeed = (int)Math.Round((double)ProjectileLifetime / (double)Frames);
            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            CustomAI(player);
        }
        public virtual void CustomAI(Player player)
        {
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage += (float)(Tier / 4.5f) + (Tier == 3 ? 1 : 0);
            if (Utils.CenteredRectangle(CritHitbox1, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitbox2, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitbox3, CritHitboxSize).Intersects(target.Hitbox) 
                || Utils.CenteredRectangle(CritHitbox4, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitbox5, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitbox6, CritHitboxSize).Intersects(target.Hitbox) 
                || Utils.CenteredRectangle(CritHitbox7, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitbox8, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitbox9, CritHitboxSize).Intersects(target.Hitbox)
                || Utils.CenteredRectangle(CritHitbox10, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitbox11, CritHitboxSize).Intersects(target.Hitbox) || Utils.CenteredRectangle(CritHitbox12, CritHitboxSize).Intersects(target.Hitbox))
            {
                modifiers.SourceDamage += (float)Projectile.CritChance / 100f * Tier;
                modifiers.SetCrit();
            }
            else
            {
                modifiers.DisableCrit();
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox1, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox2, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox3, HitboxSize)) 
                || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox4, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox5, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox6, HitboxSize)) 
                || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox7, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox8, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox9, HitboxSize))
                || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox10, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox11, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox12, HitboxSize))
                || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox13, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox14, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox15, HitboxSize))
                || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox16, HitboxSize)) || targetHitbox.Intersects(Utils.CenteredRectangle(Hitbox17, HitboxSize)))
            {
                return true;
            }
            return false;
        }
        public override bool? CanDamage()
        {
            if ((Projectile.frame == 4 || Projectile.frame == 5) && Tier < 3)
            {
                return null;
            }
            if ((Projectile.frame >= 5 && Projectile.frame <= 7) && Tier == 3)
            {
                return null;
            }
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int HitSoundStyle = Main.rand.Next(1, 4);
            if (hit.Crit)
            {
                switch (HitSoundStyle)
                {
                    case 1:
                        {
                            SoundEngine.PlaySound(new SoundStyle(WorldEnderItem.SoundPath + "QCrit1") with { Volume = WorldEnderItem.SoundVolume });
                            break;
                        }
                    case 2:
                        {
                            SoundEngine.PlaySound(new SoundStyle(WorldEnderItem.SoundPath + "QCrit2") with { Volume = WorldEnderItem.SoundVolume });
                            break;
                        }
                    default:
                        {
                            SoundEngine.PlaySound(new SoundStyle(WorldEnderItem.SoundPath + "QCrit3") with { Volume = WorldEnderItem.SoundVolume });
                            break;
                        }
                }
            }
            else
            {
                switch (HitSoundStyle)
                {
                    case 1:
                        {
                            SoundEngine.PlaySound(new SoundStyle(WorldEnderItem.SoundPath + "QHit1") with { Volume = WorldEnderItem.SoundVolume });
                            break;
                        }
                    case 2:
                        {
                            SoundEngine.PlaySound(new SoundStyle(WorldEnderItem.SoundPath + "QHit2") with { Volume = WorldEnderItem.SoundVolume });
                            break;
                        }
                    default:
                        {
                            SoundEngine.PlaySound(new SoundStyle(WorldEnderItem.SoundPath + "QHit3") with { Volume = WorldEnderItem.SoundVolume });
                            break;
                        }
                }
            }
        }
    }
}
