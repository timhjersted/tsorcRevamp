using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.GravelordNito;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Enemies.Dworc;
using tsorcRevamp.NPCs.Invaders;
using Move = tsorcRevamp.NPCs.Enemies.Dworc.DworcCasterPuppet.CasterMove;

namespace tsorcRevamp.Content.Projectiles.Enemy.Dworc
{
    internal static class DworcSpellDust
    {
        internal static void Burst(Vector2 center, Color color, int count = 64, int dustType = DustID.MagicMirror)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                Dust dust = Dust.NewDustPerfect(center, dustType, velocity, 100, color, Main.rand.NextFloat(0.7f, 1.2f));
                dust.noGravity = true;
            }
        }
    }

    public class DworcFlask : ModProjectile
    {
        public const int VenomTicks = 4 * 60;
        public Move Kind => (Move)(int)Projectile.ai[0];
        public int OwnerIndex => (int)Projectile.ai[1];
        public int OwnerToken => (int)Projectile.ai[2];
        private int age;
        private bool retiring;
        private int retireAge;
        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ItemID.FlaskofNanites);

        public static float GravityFor(Move move) => move == Move.SuppressionFlask ? 0.2f : 0.18f;

        // Reject impossible or obstructed arcs, rather than switching to an unadvertised straight
        // fallback. Eligibility, committed launch and the visible arc all use this same solver.
        public static bool TrySolve(Move move, Vector2 origin, Player target, int facing, out Vector2 velocity, out Vector2 landing)
        {
            float speed = 10f;
            if (move == Move.VenomFlask)
            {
                speed = 8f;
            }
            else if (move == Move.SuppressionFlask)
            {
                speed = 11f;
            }

            float gravity = GravityFor(move);
            float lead = MathHelper.Clamp(Vector2.Distance(origin, target.Center) / speed, 8f, 32f);
            landing = target.Center + target.velocity * lead;
            if (move == Move.SuppressionFlask)
            {
                lead = MathHelper.Clamp(Vector2.Distance(origin, target.Center) / speed, 10f, 40f);
                landing = target.Center + new Vector2(target.velocity.X * lead, 0f);
                float side = facing;
                if (Math.Abs(landing.X - origin.X) > 1f)
                {
                    side = Math.Sign(landing.X - origin.X);
                }

                landing.X += side * 80f;
            }
            else if (move == Move.StormFlask)
            {
                landing.Y = target.Bottom.Y - 6f;
            }

            velocity = UsefulFunctions.BallisticTrajectory(origin, landing, speed, gravity, highAngle: false, fallback: false);
            if (velocity.LengthSquared() < 0.01f || !float.IsFinite(velocity.X) || !float.IsFinite(velocity.Y))
            {
                return false;
            }

            Vector2 sample = origin;
            Vector2 sampleVelocity = velocity;
            int flightLimit = 120;
            if (move == Move.VenomFlask)
            {
                flightLimit = 100;
            }

            float distance = Vector2.Distance(origin, landing);
            for (int i = 0; i < flightLimit; i++)
            {
                sampleVelocity.Y += gravity;
                sample += sampleVelocity;
                if (Vector2.Distance(sample, landing) < 24f)
                {
                    return true;
                }

                if (Collision.SolidCollision(sample - Vector2.One * 7f, 14, 14))
                {
                    // A valid floor/wall landing near the predicted point is an authored impact.
                    return Vector2.Distance(sample, landing) < 48f && Vector2.Distance(sample, origin) > distance * 0.65f;
                }
            }

            return false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void AI()
        {
            age++;
            bool ownerValid = OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs && Main.npc[OwnerIndex].active
                && Main.npc[OwnerIndex].ModNPC is DworcCasterPuppet caster && caster.SpawnToken == OwnerToken;
            if (Main.netMode != NetmodeID.MultiplayerClient && !ownerValid && !retiring)
            {
                retiring = true;
                retireAge = 0;
                Projectile.netUpdate = true;
            }

            if (Kind == Move.VenomFlask && age >= 120 && !retiring)
            {
                retiring = true;
                retireAge = 0;
                Projectile.netUpdate = true;
            }

            Projectile.hostile = !retiring && (Kind == Move.VenomFlask || Kind == Move.FireFlask);
            if (Kind == Move.FireFlask)
            {
                Vector2 center = Projectile.Center;
                Projectile.width = 18;
                Projectile.height = 22;
                Projectile.Center = center;
            }

            if (retiring)
            {
                retireAge++;
                Projectile.velocity *= 0.9f;
                Projectile.alpha = (int)MathHelper.Lerp(0f, 255f, retireAge / 12f);
                if (retireAge >= 12)
                {
                    Projectile.Kill();
                }

                return;
            }

            Projectile.velocity.Y += GravityFor(Kind);
            if (Kind == Move.FireFlask)
            {
                Projectile.velocity.Y = Math.Min(16f, Projectile.velocity.Y);
                Projectile.rotation += Projectile.velocity.X * 0.08f;
            }
            else
            {
                Projectile.rotation += 0.25f * Projectile.direction;
            }

            if (Kind == Move.StormFlask && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox))
                    {
                        Projectile.Kill();
                        return;
                    }
                }
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                int dustType = DustID.BlueTorch;
                if (Kind == Move.FireFlask)
                {
                    dustType = DustID.Torch;
                }
                else if (Kind == Move.VenomFlask)
                {
                    dustType = DustID.Venom;
                }

                Dust mote = Dust.NewDustPerfect(Projectile.Center, dustType, -Projectile.velocity * 0.12f, 110, default, 0.8f);
                mote.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Vector2 normal = -Vector2.UnitY;
            if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > Math.Abs(oldVelocity.Y))
            {
                normal = oldVelocity.X > 0f ? -Vector2.UnitX : Vector2.UnitX;
            }
            else if (Projectile.velocity.Y != oldVelocity.Y)
            {
                normal = oldVelocity.Y > 0f ? -Vector2.UnitY : Vector2.UnitY;
            }

            impactNormal = normal;
            return true;
        }

        private Vector2 impactNormal = -Vector2.UnitY;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Kind == Move.VenomFlask)
            {
                int previousIndex = target.FindBuffIndex(BuffID.Venom);
                int previousTime = 0;
                if (previousIndex >= 0)
                {
                    previousTime = target.buffTime[previousIndex];
                }

                target.AddBuff(BuffID.Venom, VenomTicks);
                int index = target.FindBuffIndex(BuffID.Venom);
                if (index >= 0)
                {
                    // Vanilla doubles Venom in Expert. This flask promises four actual seconds;
                    // retain buff immunity and any longer Venom already present before this hit.
                    target.buffTime[index] = Math.Max(previousTime, VenomTicks);
                }
            }
            else if (Kind == Move.FireFlask)
            {
                target.AddBuff(BuffID.OnFire, 240);
            }
        }

        public override void OnKill(int timeLeft)
        {
            Color color = Color.CornflowerBlue;
            if (Kind == Move.FireFlask)
            {
                color = Color.OrangeRed;
            }
            else if (Kind == Move.VenomFlask)
            {
                color = Color.YellowGreen;
            }

            if (Kind == Move.FireFlask)
            {
                DworcSpellDust.Burst(Projectile.Center, Color.OrangeRed, 35, DustID.Torch);
                DworcSpellDust.Burst(Projectile.Center, Color.Gray, 24, DustID.Smoke);
            }
            else
            {
                DworcSpellDust.Burst(Projectile.Center, color);
            }
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.8f }, Projectile.Center);
                for (int i = 0; i < 24; i++)
                {
                    Dust.NewDustPerfect(Projectile.Center, DustID.Glass, Main.rand.NextVector2Circular(3f, 3f));
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || retiring)
            {
                return;
            }

            if (Kind == Move.FireFlask)
            {
                SurfaceFlameHelper.SpawnSurfaceFlames(Projectile, Projectile.Center, impactNormal, 5,
                    ModContent.ProjectileType<DworcFireFlame>(), Projectile.damage);
            }
            else if (Kind == Move.StormFlask)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<EnemySpellAbyssStormExplosion>(), Projectile.damage, 8f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<DworcStormRing>(), Projectile.damage, 8f, Projectile.owner, OwnerIndex, OwnerToken);
            }
            else if (Kind == Move.SuppressionFlask)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<DworcSuppressionBurst>(), 0, 0f, Projectile.owner, OwnerIndex, OwnerToken);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int itemType = ItemID.FlaskofNanites;
            if (Kind == Move.VenomFlask)
            {
                itemType = ItemID.FlaskofVenom;
            }

            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;
            if (Kind == Move.FireFlask)
            {
                texture = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(Content.Projectiles.Ranged.FireFlaskProj))).Value;
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor),
                Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(age);
            writer.Write(retiring);
            writer.Write(retireAge);
            writer.WriteVector2(impactNormal);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            age = reader.ReadInt32();
            retiring = reader.ReadBoolean();
            retireAge = reader.ReadInt32();
            impactNormal = reader.ReadVector2();
            Projectile.timeLeft = Math.Max(1, 240 - age);
        }
    }

    public class DworcFireFlame : EnemyFireFlaskLingeringFlame
    {
        private int ownerIndex = -1;
        private int token;
        private int fadeAge;
        public bool BelongsTo(int index, int ownerToken) => ownerIndex == index && token == ownerToken;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (source is EntitySource_Parent parent && parent.Entity is Projectile root && root.ModProjectile is DworcFlask flask)
            {
                ownerIndex = flask.OwnerIndex;
                token = flask.OwnerToken;
                Projectile.netUpdate = true;
            }
        }

        public override void AI()
        {
            bool ownerValid = ownerIndex >= 0 && ownerIndex < Main.maxNPCs && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].ModNPC is DworcCasterPuppet caster && caster.SpawnToken == token;
            if (Main.netMode != NetmodeID.MultiplayerClient && !ownerValid && fadeAge == 0)
            {
                fadeAge = 1;
                Projectile.netUpdate = true;
            }

            if (fadeAge > 0)
            {
                fadeAge++;
                Projectile.hostile = false;
                if (fadeAge >= 12)
                {
                    Projectile.Kill();
                }

                return;
            }

            if (!Main.dedServ)
            {
                base.AI();
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ownerIndex);
            writer.Write(token);
            writer.Write(fadeAge);
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ownerIndex = reader.ReadInt32();
            token = reader.ReadInt32();
            fadeAge = reader.ReadInt32();
            Projectile.timeLeft = reader.ReadInt32();
        }
    }

    internal class DworcStormRing : EnemySpellAbyssStorm
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemySpellAbyssStorm));
        public bool BelongsTo(int index, int token) => Projectile.ai[0] == index && Projectile.ai[1] == token;
    }

    public class DworcPoisonBolt : EnemySpellGreatPoisonStrikeBall
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemySpellGreatPoisonStrikeBall));

        public override void OnKill(int timeLeft)
        {
            DworcSpellDust.Burst(Projectile.Center, Color.YellowGreen);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 2; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.BottomRight, Vector2.Zero,
                        ModContent.ProjectileType<DworcPoisonImpact>(), Projectile.damage, 1f, Projectile.owner);
                }
            }
        }
    }

    public class DworcPoisonImpact : EnemySpellGreatPoisonStrike
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemySpellGreatPoisonStrike));

        public override void OnKill(int timeLeft)
        {
            DworcSpellDust.Burst(Projectile.Center, Color.YellowGreen, 24);
        }
    }

    internal class DworcAbyssBolt : EnemySpellAbyssPoisonStrikeBall
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemySpellAbyssPoisonStrikeBall));

        public override bool PreKill(int timeLeft) => true;

        public override void OnKill(int timeLeft)
        {
            DworcSpellDust.Burst(Projectile.Center, Color.YellowGreen);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.BottomRight, Vector2.Zero,
                    ModContent.ProjectileType<DworcAbyssImpact>(), Projectile.damage, 1f, Projectile.owner);
            }
        }
    }

    internal class DworcAbyssImpact : EnemySpellAbyssPoisonStrike
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemySpellAbyssPoisonStrike));

        public override void OnKill(int timeLeft)
        {
            DworcSpellDust.Burst(Projectile.Center, Color.YellowGreen, 24);
        }
    }

    public class DworcSpiritExplosion : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 184;
            Projectile.height = 184;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 36;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage()
        {
            if (Projectile.timeLeft <= 28)
            {
                return false;
            }

            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, Projectile.Center) <= ShamanSpirit.BlastRadius * ShamanSpirit.BlastRadius;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                DworcSpellDust.Burst(Projectile.Center, Color.MediumPurple, 30, DustID.ShadowbeamStaff);
                DworcSpellDust.Burst(Projectile.Center, Color.CornflowerBlue, 24, DustID.BlueCrystalShard);
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 point = Projectile.Center + Main.rand.NextVector2Circular(80f, 80f);
                        Dust wisp = Dust.NewDustPerfect(point, DustID.Wraith, Main.rand.NextVector2Circular(2f, 2f), 150, Color.MediumPurple, 0.85f);
                        wisp.noGravity = true;
                    }

                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.15f }, Projectile.Center);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            NitoVFX.DrawSpiritNova(Projectile.Center, 37 - Projectile.timeLeft, ShamanSpirit.BlastRadius);
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
        }
    }

    public class DworcSuppressionBurst : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));
        private bool applied;
        private int age;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 120;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            age++;
            float radius = Math.Min(1600f, age * 16f);
            if (Main.dedServ)
            {
                return;
            }

            if (age == 1)
            {
                DworcSpellDust.Burst(Projectile.Center, Color.CornflowerBlue, 120);
                SoundEngine.PlaySound(SoundID.Item107, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.5f }, Projectile.Center);
            }

            Player player = Main.LocalPlayer;
            int ownerIndex = (int)Projectile.ai[0];
            int ownerToken = (int)Projectile.ai[1];
            bool ownerValid = ownerIndex >= 0 && ownerIndex < Main.maxNPCs && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].ModNPC is DworcCasterPuppet caster && caster.SpawnToken == ownerToken;
            if (!applied && ownerValid && player.active && !player.dead && player.Distance(Projectile.Center) <= radius)
            {
                applied = true;
                player.GetModPlayer<DworcSuppressionPlayer>().Apply((int)Projectile.ai[0], (int)Projectile.ai[1], 12600);
                DworcSpellDust.Burst(player.Center, Color.CornflowerBlue, 24);
            }

            if (age <= 90)
            {
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(160f, 112f);
                Dust cloud = Dust.NewDustPerfect(position, DustID.BlueTorch, new Vector2(0f, -0.5f), 130, default, 1.3f);
                cloud.noGravity = true;
            }

            int points = Math.Max(12, (int)(MathHelper.TwoPi * radius / 20f));
            int count = 0;
            float fade = MathHelper.Clamp((120 - age) / 20f, 0f, 1f);
            for (int i = 0; i < points && count < 60; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / points).ToRotationVector2();
                Vector2 point = Projectile.Center + direction * radius;
                if (player.Distance(point) < 1100f && Main.rand.NextBool(2))
                {
                    Dust front = Dust.NewDustPerfect(point, DustID.IceTorch, direction * 1.5f, 70, default, 1.6f * fade + 0.3f);
                    front.noGravity = true;
                    count++;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(age);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            age = reader.ReadInt32();
            Projectile.timeLeft = Math.Max(1, 120 - age);
        }
    }

    // Buffs are owned by the affected player. Track each contributing caster so defeating one
    // Alchemist never erases a surviving Alchemist's or Blaidd's suppression.
    public class DworcSuppressionPlayer : ModPlayer
    {
        private readonly struct Contribution
        {
            public readonly int Index;
            public readonly int Token;
            public readonly ulong Until;
            public bool Alive => Index >= 0 && Index < Main.maxNPCs && Main.npc[Index].active
                && Main.npc[Index].ModNPC is DworcCasterPuppet caster && caster.SpawnToken == Token
                && Main.GameUpdateCount < Until;

            public Contribution(int index, int token, ulong until)
            {
                Index = index;
                Token = token;
                Until = until;
            }
        }

        private readonly List<Contribution> sources = new List<Contribution>();
        private ulong outsideSuppressed;
        private ulong outsideGrapple;
        public bool HasActiveSuppression
        {
            get
            {
                foreach (Contribution source in sources)
                {
                    if (source.Alive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Apply(int ownerIndex, int ownerToken, int duration)
        {
            if (sources.Count == 0 && !Player.GetModPlayer<BlaiddSuppressionPlayer>().SuppressedByBlaidd)
            {
                int suppressedIndex = Player.FindBuffIndex(ModContent.BuffType<Suppressed>());
                int grappleIndex = Player.FindBuffIndex(ModContent.BuffType<GrappleMalfunction>());
                if (suppressedIndex >= 0)
                {
                    outsideSuppressed = Main.GameUpdateCount + (ulong)Player.buffTime[suppressedIndex];
                }

                if (grappleIndex >= 0)
                {
                    outsideGrapple = Main.GameUpdateCount + (ulong)Player.buffTime[grappleIndex];
                }
            }

            sources.Add(new Contribution(ownerIndex, ownerToken, Main.GameUpdateCount + (ulong)duration));
            Player.AddBuff(ModContent.BuffType<Suppressed>(), duration);
            Player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), duration);
        }

        public override void PostUpdateBuffs()
        {
            if (Player.whoAmI != Main.myPlayer || sources.Count == 0)
            {
                return;
            }

            ulong until = Main.GameUpdateCount;
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                Contribution source = sources[i];
                if (!source.Alive)
                {
                    sources.RemoveAt(i);
                }
                else
                {
                    until = Math.Max(until, source.Until);
                }
            }

            BlaiddSuppressionPlayer blaidd = Player.GetModPlayer<BlaiddSuppressionPlayer>();
            if (blaidd.SuppressedByBlaidd && NPC.AnyNPCs(ModContent.NPCType<Blaidd>()))
            {
                until = Math.Max(until, blaidd.SuppressionExpiresAt);
            }

            int[] types = { ModContent.BuffType<Suppressed>(), ModContent.BuffType<GrappleMalfunction>() };
            ulong[] outside = { outsideSuppressed, outsideGrapple };
            for (int i = 0; i < types.Length; i++)
            {
                ulong expiry = Math.Max(until, outside[i]);
                if (expiry > Main.GameUpdateCount)
                {
                    int remaining = (int)(expiry - Main.GameUpdateCount);
                    Player.AddBuff(types[i], remaining);
                    int index = Player.FindBuffIndex(types[i]);
                    if (index >= 0)
                    {
                        Player.buffTime[index] = remaining;
                    }
                }
                else
                {
                    Player.ClearBuff(types[i]);
                }
            }
        }

        public override void UpdateDead()
        {
            sources.Clear();
            outsideSuppressed = 0;
            outsideGrapple = 0;
        }
    }
}
