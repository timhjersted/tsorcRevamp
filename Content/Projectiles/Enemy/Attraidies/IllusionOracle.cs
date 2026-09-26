using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Content.Projectiles.Enemy.Attraidies
{
    // Existing 16x16, one-frame Oracle art; spinning orb has no leading edge.
    // Birth: 20t harmless tip preview on the caster. Travel: fixed speed, green motes.
    // Deliberate spectral wall passage. Player hit kills; timeout fades harmlessly for 15t.
    public class IllusionOracle : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/TheOracle";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.tileCollide = false; Projectile.penetrate = 1;
            Projectile.timeLeft = 180; Projectile.DamageType = DamageClass.Magic;
        }
        public override bool? CanDamage() => Projectile.timeLeft <= 15 ? false : null;
        public override void OnSpawn(IEntitySource source)
        {
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item17, Projectile.Center);
        }
        public override void SendExtraAI(BinaryWriter writer) => writer.Write((short)Projectile.timeLeft);
        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt16();
        public override void AI()
        {
            int source = (int)Projectile.ai[0];
            if (Main.netMode != NetmodeID.MultiplayerClient
                && (source < 0 || source >= Main.maxNPCs || !Main.npc[source].active || Main.npc[source].ModNPC is not AttraidiesIllusion))
            { Projectile.Kill(); return; }
            Projectile.rotation += 0.5f;
            if (Projectile.timeLeft <= 15) Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / 15f));
            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.2f, 0.35f, 0.06f);
                if (Projectile.timeLeft % 3 == 0)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                        -Projectile.velocity * 0.15f, 80, new Color(135, 230, 65), 0.75f);
                    dust.noGravity = true;
                }
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
            => target.AddBuff(BuffID.Poisoned, Main.expertMode ? 200 : 400);
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 36; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(3.5f, 3.5f), 60, new Color(135, 230, 65), Main.rand.NextFloat(0.7f, 1.2f));
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.35f }, Projectile.Center);
        }
    }
}
