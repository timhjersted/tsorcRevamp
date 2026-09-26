using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Content.Projectiles.Enemy.Attraidies
{
    // ScrewAttack's 32x166 sheet is irregular: visible frames begin at rows 0,45,90,135.
    // Read 32x31 source rectangles explicitly, rather than dividing 166 by four and clipping frames.
    // Portal forms harmlessly for 30t; travel accelerates 2->6 over 30t, homes for 60t then coasts.
    // Luminous core ~34px matches its 34x34 hitbox. Tiles and player hits kill; timeout fades 20t.
    public class IllusionCrystal : ModProjectile
    {
        private static readonly int[] FrameRows = { 0, 45, 90, 135 };
        private static readonly Color CrystalColor = new Color(180, 80, 240);
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/ScrewAttack";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.tileCollide = false; Projectile.penetrate = 1;
            Projectile.timeLeft = 210; Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1.05f;
        }
        public override bool ShouldUpdatePosition() => Projectile.ai[2] >= 30f;
        public override bool? CanDamage() => Projectile.ai[2] < 30f || Projectile.timeLeft <= 20 ? false : null;
        public override void AI()
        {
            int source = (int)Projectile.ai[0];
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (authority && (source < 0 || source >= Main.maxNPCs || !Main.npc[source].active || Main.npc[source].ModNPC is not AttraidiesIllusion))
            { Projectile.Kill(); return; }
            int age = (int)Projectile.ai[2]++;
            Projectile.rotation += 0.18f;
            Projectile.frame = age / 5 % 4;
            if (age < 30)
            {
                Projectile.scale = MathHelper.Lerp(0.1f, 1.05f, age / 30f);
                Projectile.alpha = (int)MathHelper.Lerp(180f, 0f, age / 30f);
            }
            else
            {
                Projectile.tileCollide = true;
                float speed = MathHelper.Lerp(2f, 6f, MathHelper.Clamp((age - 30f) / 30f, 0f, 1f));
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                int targetIndex = (int)Projectile.ai[1];
                if (authority && age < 90 && targetIndex >= 0 && targetIndex < Main.maxPlayers)
                {
                    Player target = Main.player[targetIndex];
                    if (target.active && !target.dead)
                    {
                        Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(direction);
                        // Bounded turn, never a snap or an unannounced target swap.
                        float angle = MathHelper.WrapAngle(desired.ToRotation() - direction.ToRotation());
                        direction = direction.RotatedBy(MathHelper.Clamp(angle, -0.035f, 0.035f));
                    }
                }
                Projectile.velocity = direction * speed;
                if (authority && (age % 6 == 0 && age < 90 || age == 90)) Projectile.netUpdate = true;
                if (age == 30 && !Main.dedServ) SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.55f }, Projectile.Center);
            }
            if (Projectile.timeLeft <= 20) Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / 20f));
            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, CrystalColor.ToVector3() * 0.4f);
                if (age % 2 == 0)
                {
                    Vector2 offset = age < 30 ? Main.rand.NextVector2CircularEdge(20f, 20f) : Main.rand.NextVector2Circular(12f, 12f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.TintableDustLighted,
                        age < 30 ? -offset * 0.08f : -Projectile.velocity * 0.15f,
                        80, CrystalColor, 0.8f);
                    dust.noGravity = true;
                }
            }
        }
        public override void SendExtraAI(BinaryWriter writer) => writer.Write((short)Projectile.timeLeft);
        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt16();
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = new Rectangle(0, FrameRows[Projectile.frame], 32, 31);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
                Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f,
                Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.WitheredArmor, Main.expertMode ? 300 : 600);
            target.AddBuff(BuffID.WitheredWeapon, Main.expertMode ? 150 : 300);
        }
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 60; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(5f, 5f), 50, CrystalColor, Main.rand.NextFloat(0.8f, 1.4f));
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.45f }, Projectile.Center);
        }
    }
}
