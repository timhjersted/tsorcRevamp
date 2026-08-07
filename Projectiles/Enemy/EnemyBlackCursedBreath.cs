using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyBlackCursedBreath : ModProjectile
    {
        // Telegraph: the drop appears frozen in place and spinning for this many ticks before it
        // actually falls. Embedding the windup in the projectile itself (rather than a separate
        // marker + a delayed second spawn) means the telegraph and the real hitbox are always in
        // perfect agreement about where the drop is.
        //
        // ONLY for ai[0]==1 (BlackKnight.FireGravefallWave's overhead rain). This class is ALSO
        // fired by GreatBlackKnight's Ultrakill channel ("Black Breath", ai[0] defaults to 0) as an
        // immediate close-range burst — that one must keep flying the instant it spawns, or the
        // channel's whole rhythm breaks.
        const int WindupTicks = 45;

        Vector2 fallVelocity;
        bool IsRainDrop => Projectile.ai[0] == 1f;
        bool Windup => IsRainDrop && Projectile.localAI[0] < WindupTicks;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cursed Breath");
        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.alpha = 5;
            //Projectile.aiStyle = 24; //8 with 96 AI Style works; with no AIType it rained down 5 streams like a firework, good if launched above player (23 is a orange flame)
            Projectile.timeLeft = 360;
            Projectile.friendly = false;
            Projectile.light = 0.8f;
            Projectile.penetrate = 4; //was 4, was causing curse buildup way too fast
            Projectile.tileCollide = false;
            //AIType = 96;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;

        }

        // fallVelocity isn't part of the vanilla ai[] sync, so it needs its own MP sync — otherwise
        // clients would see the drop pop straight from frozen to a velocity only the server knows.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(fallVelocity.X);
            writer.Write(fallVelocity.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            fallVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (!IsRainDrop)
            {
                return; // GBK's immediate burst — keep flying, no windup, no stored velocity needed.
            }

            // The spawner passes the intended fall velocity as Projectile.velocity as usual; stash it
            // and freeze in place for the windup instead.
            fallVelocity = Projectile.velocity;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Windup)
            {
                // Frozen and spinning in place — the telegraph the whole rain attack was missing.
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation += 0.35f;
                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 50, Color.DarkGray, 0.6f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.2f;
                }
                return;
            }

            if (IsRainDrop && Projectile.localAI[0] == WindupTicks)
            {
                // Guarded by IsRainDrop: for the non-windup (GBK burst) case fallVelocity is never
                // set, and without this check every cursed breath would get its velocity stomped to
                // Vector2.Zero the instant it reached tick 45.
                Projectile.velocity = fallVelocity;
                Projectile.netUpdate = true;
            }

            Projectile.rotation += 5f;

            if (Main.rand.NextBool(3))
            {
                int pink = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Wraith, Projectile.velocity.X, Projectile.velocity.Y, Scale: 1f, Alpha: 200);
                Main.dust[pink].noGravity = true;

                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0, 0, 50, Color.DarkGray, 0.7f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Windup)
            {
                // No trail while frozen — a trail behind a stationary object reads as a rendering bug.
                return true;
            }
            if (Projectile.ai[0] == 1f)
            {
                EnemyVFX.DrawBlackKnightGraveTrail(Projectile.Center, Projectile.velocity);
            }
            else if (Projectile.ai[0] == 2f)
            {
                EnemyVFX.DrawBlackKnightDeathTrail(Projectile.Center, Projectile.velocity, new Vector2(78f, 25f), 0.66f);
            }
            return true;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            //Vanilla Debuffs cut in half to counter expert mode doubling them
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 36000, false);
            //target.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel += 10;
            target.AddBuff(33, 300, false); //weak
        }
    }
}