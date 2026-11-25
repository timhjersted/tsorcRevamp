using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Content.Items.Weapons.Generic.LionheartGunblade
{
    class LionheartGunshot : DynamicTrail
    {
        public float Timer;
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 3;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.knockBack = 0f;          
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 100;

            trailWidth = 20;
            trailPointLimit = 120;
            trailCollision = true;
            collisionFrequency = 2;
            trailYOffset = 40;
            trailMaxLength = 100;
            collisionEndPadding = 1;
            collisionPadding = 1;

            //customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/RadiantStrand", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            //customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/HomingStarShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        private static readonly Color GunshotBlue = new Color(50, 140, 255);

        public override void AI()
        {
            base.AI();
            Lighting.AddLight(Projectile.Center, GunshotBlue.ToVector3());
            Timer++;
        }

        public override void SetEffectParameters(Effect effect)
        {
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(0.5f);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 100f);
            effect.Parameters["shaderColor"].SetValue(GunshotBlue.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);


            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            var globalNpc = target.GetGlobalNPC<LionheartGunbladeNPC>();
            if (globalNpc.LionheartMarks >= LionheartGunbladeItem.MaxMarks)
            {
                modifiers.SetCrit();
                modifiers.CritDamage += (float)Projectile.CritChance / 100f;
                modifiers.FinalDamage *= LionheartGunbladeItem.MarkExtraDmgMultBase + Timer / LionheartGunbladeItem.MarkDistanceBasedDmgDivisor;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            var globalNpc = target.GetGlobalNPC<LionheartGunbladeNPC>();
            if (globalNpc.LionheartMarks >= LionheartGunbladeItem.MaxMarks)
            {
                globalNpc.LionheartMarks = 0;
                SoundEngine.PlaySound(SoundID.Item14);

                for (int i = 0; i < 25; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                    int dustIndex = Dust.NewDust(target.Center, 0, 0, DustID.GemSapphire, velocity.X, velocity.Y, 150, default, 2.5f);
                    Main.dust[dustIndex].noGravity = true;
                }
            }
        }
    }
}
