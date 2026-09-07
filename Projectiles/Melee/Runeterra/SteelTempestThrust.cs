using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;
using tsorcRevamp.Projectiles.Melee.Shortswords;

namespace tsorcRevamp.Projectiles.Melee.Runeterra
{
    public class SteelTempestThrust : ModdedShortswordProj
    {
        public override float HitboxWidth => 20f;
        public override float HitboxLength => 12f;

        public override int SpriteWidth => 110;
        public override int SpriteHeight => 104;
        public override int TotalDuration => 32;
        public override float SetDefaultScale()
        {
            return SteelTempest.BaseScale;
        }

        public override void Initialization(Player player)
        {
            Projectile.scale = player.GetAdjustedItemScale(player.HeldItem);
            Projectile.Resize((int)(Projectile.width / SetDefaultScale() * Projectile.scale), (int)(Projectile.height / SetDefaultScale() * Projectile.scale));
            CollisionWidth *= Projectile.scale;
            Projectile.velocity /= player.GetTotalAttackSpeed(DamageClass.Melee);
        }
        
        public bool Hit = false;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 2;
        }

        public override void SetVisualOffsets()
        {
            int HalfSpriteWidth = 158 / 2;
            int HalfSpriteHeight = 148 / 2;

            int HalfProjWidth = Projectile.width / 2;
            int HalfProjHeight = Projectile.height / 2;

            DrawOriginOffsetX = 0;
            DrawOffsetX = -(HalfSpriteWidth - HalfProjWidth);
            DrawOriginOffsetY = -(HalfSpriteHeight - HalfProjHeight);
            
            Projectile.frame = (int)((Timer / 28f) * 6f);
            if (Timer > 28f)
            {
                Projectile.frame = 0;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (!Hit)
            {
                Hit = true;
                ; SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/ThrustHit") with { Volume = 0.4f });
            }
        }
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            player.itemAnimation = 0;
            player.itemTime = 0;
            if (Hit)
            {
                player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks += 1;
                if (Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/TornadoReady") with { Volume = 0.4f });
                }
            }
        }
    }
}
