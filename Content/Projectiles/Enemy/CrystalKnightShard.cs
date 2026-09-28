using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>Conjured frost crystal: harmless gathering node -> locked flight -> shatter/fade.</summary>
    public class CrystalKnightShard : ModProjectile
    {
        // CrystalShard: five 18x42 frames, sharp edge at the TOP (-pi/2).
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/Crystal/CrystalShard";
        private bool _hail;
        private int _warningTotal;
        private int FlightTicks => _hail ? 60 : 120;

        public override void SetStaticDefaults() => Main.projFrames[Type] = 5;

        public static void Spawn(NPC owner, Vector2 position, Vector2 velocity, int damage, int warning, int sequence, bool hail)
            => SpawnOwned(owner.GetSource_FromAI(), owner.whoAmI, position, velocity, damage, warning, sequence, hail);

        public static void SpawnFromAnchor(Projectile anchor, Vector2 position, Vector2 velocity, int damage, int sequence)
            => SpawnOwned(anchor.GetSource_FromAI(), (int)anchor.ai[0], position, velocity, damage, 0, sequence, false);

        private static void SpawnOwned(IEntitySource source, int ownerIndex, Vector2 position, Vector2 velocity,
            int damage, int warning, int sequence, bool hail)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int index = Projectile.NewProjectile(source, position, velocity,
                ModContent.ProjectileType<CrystalKnightShard>(), damage, 2f, Main.myPlayer, ownerIndex, warning, sequence);
            if (index < Main.maxProjectiles)
            {
                var shard = (CrystalKnightShard)Main.projectile[index].ModProjectile;
                shard._hail = hail; shard._warningTotal = warning;
                Main.projectile[index].netUpdate = true;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.hostile = true; Projectile.friendly = false; Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1; Projectile.timeLeft = 240; Projectile.tileCollide = true; Projectile.ignoreWater = true;
        }
        public override bool ShouldUpdatePosition() => Projectile.ai[1] <= 0f;
        public override bool? CanDamage() => Projectile.ai[1] < 0f && -Projectile.ai[1] < FlightTicks - 15;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 8f;
            float collision = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - axis, Projectile.Center + axis, 10f, ref collision);
        }

        public static bool OwnerCasting(Projectile projectile)
        {
            int owner = (int)projectile.ai[0];
            return owner >= 0 && owner < Main.maxNPCs && Main.npc[owner].active
                && Main.npc[owner].ModNPC is CrystalKnight knight && knight.HasLiveCast((int)projectile.ai[2]);
        }
        public override void AI()
        {
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Projectile.tileCollide = Projectile.ai[1] <= 0f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (_warningTotal <= 0) _warningTotal = (int)Projectile.ai[1];
            if (Projectile.ai[1] > 0f)
            {
                // Only authority clears a cancelled warning; a client may receive owner/projectile packets out of order.
                if (Main.netMode != NetmodeID.MultiplayerClient && !OwnerCasting(Projectile)) { Projectile.Kill(); return; }
                if (!Main.dedServ)
                {
                    float progress = 1f - Projectile.ai[1] / System.Math.Max(1, _warningTotal);
                    GatherDust(Projectile.Center, progress, 20f);
                    if (_hail && Main.GameUpdateCount % 3 == 0)
                        GatherDust(Projectile.Center + new Vector2(0f, 240f), progress, 12f);
                    if (Main.GameUpdateCount % 4 == 0)
                    {
                        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                        for (int i = 1; i <= 4; i++) FrostDust(Projectile.Center + direction * (i * 12f), Vector2.Zero, 0.55f);
                    }
                }
                Projectile.ai[1]--;
                if (Projectile.ai[1] == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // A player who approaches a gathering node must still have >=20t of travel at release.
                    float minimum = Projectile.velocity.Length() * 20f;
                    for (int i = 0; i < Main.maxPlayers; i++)
                        if (Main.player[i].active && !Main.player[i].dead && Projectile.Distance(Main.player[i].Center) < minimum)
                        { Projectile.Kill(); return; }
                    Projectile.netUpdate = true;
                }
                return;
            }
            Projectile.ai[1]--;
            if (!Main.dedServ)
            {
                if (Projectile.ai[1] == -1f) SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.65f, Volume = 0.35f }, Projectile.Center);
                if (Main.GameUpdateCount % 2 == 0) FrostDust(Projectile.Center, -Projectile.velocity * 0.08f, 0.8f);
                Lighting.AddLight(Projectile.Center, 0.08f, 0.25f, 0.34f);
            }
            if (-Projectile.ai[1] >= FlightTicks) Projectile.Kill();
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 180);
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.TornWings>(), 540);
            Buffs.Debuffs.FrostBuildup.Apply(target);
        }
        public override void OnKill(int timeLeft) => Shatter(Projectile.Center, Projectile.ai[1] > 0 ? 16 : 70);
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = new(0, Projectile.frame * 42, 18, 42);
            float warning = Projectile.ai[1] > 0 ? 1f - Projectile.ai[1] / System.Math.Max(1, _warningTotal) : 1f;
            float fade = Projectile.ai[1] <= 0 ? MathHelper.Clamp((FlightTicks + Projectile.ai[1]) / 15f, 0f, 1f) : 1f;
            float scale = Projectile.ai[1] > 0 ? MathHelper.Lerp(0.25f, 1f, warning) : 1f;
            Color color = Color.Lerp(lightColor, Color.LightCyan, 0.6f) * fade * (Projectile.ai[1] > 0 ? 0.25f + warning * 0.45f : 1f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, color,
                Projectile.rotation, frame.Size() * 0.5f, scale, SpriteEffects.None, 0);
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer) { writer.Write(_hail); writer.Write((short)_warningTotal); }
        public override void ReceiveExtraAI(BinaryReader reader) { _hail = reader.ReadBoolean(); _warningTotal = reader.ReadInt16(); }

        public static void FrostDust(Vector2 point, Vector2 velocity, float scale)
        {
            if (Main.dedServ) return;
            Dust dust = Dust.NewDustPerfect(point, DustID.IceTorch, velocity, 80, new Color(130, 225, 255), scale);
            dust.noGravity = true;
        }
        public static void GatherDust(Vector2 point, float progress, float radius)
        {
            if (Main.dedServ) return;
            Vector2 offset = Main.rand.NextVector2Unit() * MathHelper.Lerp(radius, 4f, progress);
            FrostDust(point + offset, -offset * 0.1f, 0.65f + progress * 0.25f);
        }
        public static void Shatter(Vector2 point, int count)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < count; i++)
            {
                int type = i % 3 == 0 ? DustID.BlueCrystalShard : DustID.IceTorch;
                Dust dust = Dust.NewDustPerfect(point, type, Main.rand.NextVector2Circular(5f, 5f), 65,
                    new Color(135, 220, 255), Main.rand.NextFloat(0.7f, 1.3f));
                dust.noGravity = i % 3 != 0;
            }
        }
    }
}
