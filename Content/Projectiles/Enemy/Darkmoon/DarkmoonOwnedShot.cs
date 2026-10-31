using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.GhostFighter;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Darkmoon
{
    public abstract class DarkmoonOwnedShot : ModProjectile
    {
        protected virtual int LiveTicks => 60;
        public bool IsLive => Projectile.ai[2] <= 0 && Projectile.localAI[0] < LiveTicks;
        public bool OwnedBy(int npc, int serial) => Projectile.ai[0] == npc && Projectile.ai[1] == serial;
        public void BeginFade()
        {
            if (Projectile.ai[2] > 0) return;
            Projectile.ai[2] = 12; Projectile.timeLeft = 12; Projectile.netUpdate = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 96; Projectile.hostile = true; Projectile.penetrate = 1;
            Projectile.tileCollide = false; Projectile.aiStyle = -1; Projectile.timeLeft = 72; Projectile.extraUpdates = 0;
            Projectile.DamageType = DamageClass.Magic;
        }
        public override bool? CanDamage() => IsLive;
        public override void AI()
        {
            Projectile.localAI[0]++;
            int owner = (int)Projectile.ai[0];
            if (Main.netMode != NetmodeID.MultiplayerClient && (owner < 0 || owner >= Main.maxNPCs || !Main.npc[owner].active
                || Main.npc[owner].ModNPC is not GhostOfTheDarkmoonKnight k || k.EncounterSerial != (int)Projectile.ai[1])) BeginFade();
            if (Projectile.localAI[0] >= LiveTicks && Projectile.ai[2] == 0) BeginFade();
            if (Projectile.ai[2] > 0) Projectile.velocity *= 0.85f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!Main.dedServ && Projectile.localAI[0] % 3 == 0) DarkmoonVisuals.Motes(Projectile.Center, Color.MediumPurple, 1, 4);
        }
        protected float Opacity => Projectile.ai[2] > 0 ? MathHelper.Clamp(Projectile.timeLeft / 12f, 0, 1) : 1;
        public override void OnKill(int timeLeft) => DarkmoonVisuals.Motes(Projectile.Center, Color.MediumPurple, 40, 10);
        protected bool SweptCircle(Rectangle box, float radius)
        {
            float point = 0;
            Vector2 from = Projectile.Center - Projectile.velocity;
            return Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), from, Projectile.Center, radius * 2, ref point);
        }
    }
    public class DarkmoonShadowShot : DarkmoonOwnedShot
    {
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/ShadowShot";
        public override void SetStaticDefaults() => Main.projFrames[Type] = 6;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => SweptCircle(targetHitbox, 6);
        public override bool PreDraw(ref Color lightColor) { DarkmoonVisuals.DrawHandOrb(Projectile.Center, Opacity, true); return false; }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        { target.AddBuff(BuffID.WitheredArmor, 300); target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 300); }
    }
    public class DarkmoonCrescent : DarkmoonOwnedShot
    {
        public override string Texture => "tsorcRevamp/Content/Projectiles/Melee/MLGSCrescent";
        public override void SetDefaults() { base.SetDefaults(); Projectile.tileCollide = true; }
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            float c = System.Math.Abs((float)System.Math.Cos(Projectile.rotation));
            float s = System.Math.Abs((float)System.Math.Sin(Projectile.rotation));
            width = (int)(20 * c + 40 * s); height = (int)(20 * s + 40 * c);
            return true;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle box)
        {
            for (int i = 0; i < 9; i++)
            {
                float angle = -MathHelper.PiOver2 + MathHelper.Pi * i / 8f;
                Vector2 offset = new Vector2(10f * (float)System.Math.Cos(angle), 20f * (float)System.Math.Sin(angle)).RotatedBy(Projectile.rotation);
                float point = 0;
                if (Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), Projectile.Center + offset - Projectile.velocity, Projectile.Center + offset, 6, ref point)) return true;
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor) { DarkmoonVisuals.DrawCrescent(Projectile.Center, Projectile.rotation, Opacity); return false; }
    }
}
