using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs
{
    [Flags]
    public enum KnightPulseDebuffs : byte
    {
        GrappleMalfunction = 1,
        TornWings = 2,
        Crippled = 4,
        BrokenSpirit = 8
    }

    // Shared opening/75-second ability for the knights that previously refreshed proximity debuffs every tick.
    // The separate wave updates outside NPC AI, so the Soul Mimic's temporary player relocation cannot hit real players.
    public class KnightDebuffPulse : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public const int RefreshTicks = 75 * 60;
        private int cooldown;

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return entity.type == ModContent.NPCType<LothricKnight>()
                || entity.type == ModContent.NPCType<LothricSpearKnight>()
                || entity.type == ModContent.NPCType<LothricBlackKnight>()
                || entity.type == ModContent.NPCType<LothricKnightOriginal>()
                || entity.type == ModContent.NPCType<LothricSpearKnightOriginal>()
                || entity.type == ModContent.NPCType<LothricBlackKnightOriginal>()
                || entity.type == ModContent.NPCType<KnightOfGwyn>()
                || entity.type == ModContent.NPCType<GreatRedKnight>();
        }

        public override void PostAI(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (cooldown > 0)
            {
                cooldown--;

                if (cooldown > 0)
                {
                    return;
                }
            }

            if (!npc.active || npc.dontTakeDamage || !npc.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[npc.target];
            float distanceSquared = Vector2.DistanceSquared(npc.Center, target.Center);
            float maximumRadius = KnightDebuffPulseWave.MaximumRadius;
            bool inRange = distanceSquared <= maximumRadius * maximumRadius;
            bool visible = Collision.CanHitLine(npc.position, npc.width, npc.height, target.position, target.width, target.height);

            if (!inRange || !visible)
            {
                return;
            }

            KnightPulseDebuffs debuffs = KnightPulseDebuffs.GrappleMalfunction;
            bool blackKnight = npc.ModNPC is LothricBlackKnight || npc.ModNPC is LothricBlackKnightOriginal;
            bool alwaysDisablesWings = blackKnight || npc.ModNPC is KnightOfGwyn || npc.ModNPC is GreatRedKnight;

            if (Main.hardMode || alwaysDisablesWings)
            {
                debuffs |= KnightPulseDebuffs.TornWings;
            }

            if (Main.hardMode && blackKnight)
            {
                debuffs |= KnightPulseDebuffs.Crippled | KnightPulseDebuffs.BrokenSpirit;
            }

            cooldown = RefreshTicks;
            npc.netUpdate = true;
            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero,
                ModContent.ProjectileType<KnightDebuffPulseWave>(), 0, 0f, Main.myPlayer, (int)debuffs);

            if (npc.ModNPC is KnightOfGwyn)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.KnightOfGwyn.Protection"), 175, 75, 255);
            }
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write((short)cooldown);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            cooldown = binaryReader.ReadInt16();
        }
    }

    // Opening pulse: 0 damage; 500px radius over 60 ticks; each crossed player receives the selected
    // debuffs once for 3600 ticks. The red dust front fades locally within 64px of each player and over its final 12 ticks.
    public class KnightDebuffPulseWave : ModProjectile
    {
        public const int DurationTicks = 60;
        public const int DebuffDurationTicks = 60 * 60;
        public const float MaximumRadius = 500f;
        private const int FadeTicks = 12;
        private const float PlayerFadeDistance = 64f;
        private const int MoteCount = 192;

        private readonly bool[] affectedPlayers = new bool[Main.maxPlayers];
        private readonly bool[] hasPreviousHitbox = new bool[Main.maxPlayers];
        private readonly Rectangle[] previousHitboxes = new Rectangle[Main.maxPlayers];
        private readonly Dust[] motes = new Dust[MoteCount];
        private readonly List<Player> visualPlayers = new List<Player>();

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.timeLeft = DurationTicks + 1;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        // Sweep the expanding annulus against the player's occupied rectangle, including their previous
        // tick's rectangle. Fast movement cannot step through the front between collision checks.
        public static bool CrossesWave(Vector2 origin, Rectangle currentHitbox, Rectangle previousHitbox, float innerRadius, float outerRadius)
        {
            Vector2 currentNear = Vector2.Clamp(origin, currentHitbox.TopLeft(), currentHitbox.BottomRight());
            Vector2 previousNear = Vector2.Clamp(origin, previousHitbox.TopLeft(), previousHitbox.BottomRight());
            float nearestSquared = Math.Min(Vector2.DistanceSquared(origin, currentNear), Vector2.DistanceSquared(origin, previousNear));

            float currentFarX = Math.Max(Math.Abs(origin.X - currentHitbox.Left), Math.Abs(origin.X - currentHitbox.Right));
            float currentFarY = Math.Max(Math.Abs(origin.Y - currentHitbox.Top), Math.Abs(origin.Y - currentHitbox.Bottom));
            float previousFarX = Math.Max(Math.Abs(origin.X - previousHitbox.Left), Math.Abs(origin.X - previousHitbox.Right));
            float previousFarY = Math.Max(Math.Abs(origin.Y - previousHitbox.Top), Math.Abs(origin.Y - previousHitbox.Bottom));
            float currentFarSquared = currentFarX * currentFarX + currentFarY * currentFarY;
            float previousFarSquared = previousFarX * previousFarX + previousFarY * previousFarY;
            float farthestSquared = Math.Max(currentFarSquared, previousFarSquared);

            return nearestSquared <= outerRadius * outerRadius && farthestSquared >= innerRadius * innerRadius;
        }

        public override void AI()
        {
            int previousAge = (int)Projectile.ai[1];
            int age = Math.Min(DurationTicks, previousAge + 1);
            Projectile.ai[1] = age;
            float previousRadius = MaximumRadius * previousAge / DurationTicks;
            float radius = MaximumRadius * age / DurationTicks;
            KnightPulseDebuffs debuffs = (KnightPulseDebuffs)(int)Projectile.ai[0];
            visualPlayers.Clear();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (!player.active || player.dead || player.ghost)
                {
                    hasPreviousHitbox[i] = false;
                    continue;
                }

                visualPlayers.Add(player);
                Rectangle currentHitbox = player.Hitbox;
                Rectangle previousHitbox = currentHitbox;

                if (hasPreviousHitbox[i])
                {
                    previousHitbox = previousHitboxes[i];
                }

                previousHitboxes[i] = currentHitbox;
                hasPreviousHitbox[i] = true;

                if (Main.netMode == NetmodeID.MultiplayerClient || affectedPlayers[i]
                    || !CrossesWave(Projectile.Center, currentHitbox, previousHitbox, previousRadius, radius))
                {
                    continue;
                }

                affectedPlayers[i] = true;

                for (int bit = 0; bit < 4; bit++)
                {
                    KnightPulseDebuffs debuff = (KnightPulseDebuffs)(1 << bit);

                    if ((debuffs & debuff) == 0)
                    {
                        continue;
                    }

                    int buffType;

                    switch (debuff)
                    {
                        case KnightPulseDebuffs.GrappleMalfunction:
                            buffType = ModContent.BuffType<GrappleMalfunction>();
                            break;
                        case KnightPulseDebuffs.TornWings:
                            buffType = ModContent.BuffType<TornWings>();
                            break;
                        case KnightPulseDebuffs.Crippled:
                            buffType = ModContent.BuffType<Crippled>();
                            break;
                        default:
                            buffType = ModContent.BuffType<BrokenSpirit>();
                            break;
                    }

                    player.AddBuff(buffType, DebuffDurationTicks);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        // Server collision is authoritative. Packet 55 delivers the buff to its owning client;
                        // a server-only AddBuff is not enough to change that client's local player state.
                        NetMessage.SendData(MessageID.AddPlayerBuff, i, -1, null, i, buffType, DebuffDurationTicks);
                    }
                }
            }

            if (!Main.dedServ)
            {
                float endFade = MathHelper.Clamp((DurationTicks - age) / (float)FadeTicks, 0f, 1f);
                float birthFade = MathHelper.Clamp(age / 6f, 0f, 1f);

                for (int i = 0; i < MoteCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / MoteCount;
                    Vector2 outward = angle.ToRotationVector2();
                    Vector2 position = Projectile.Center + outward * radius;
                    float playerFade = 1f;

                    foreach (Player player in visualPlayers)
                    {
                        Vector2 closestPoint = Vector2.Clamp(position, player.Hitbox.TopLeft(), player.Hitbox.BottomRight());
                        float distance = Vector2.Distance(position, closestPoint);
                        float fade = MathHelper.Clamp(distance / PlayerFadeDistance, 0f, 1f);
                        playerFade = Math.Min(playerFade, fade);
                    }

                    float opacity = birthFade * endFade * playerFade;
                    Dust mote = motes[i];

                    if (opacity <= 0.02f)
                    {
                        if (mote != null && mote.customData == this)
                        {
                            mote.active = false;
                        }

                        continue;
                    }

                    bool fineMote = i % 3 == 0;
                    int dustType = DustID.RedTorch;
                    float scale = 1.1f;

                    if (fineMote)
                    {
                        dustType = DustID.GemRuby;
                        scale = 0.65f;
                    }

                    if (mote == null || !mote.active || mote.customData != this)
                    {
                        mote = Dust.NewDustPerfect(position, dustType, Vector2.Zero, 0, Color.Red, scale);

                        if (!mote.active || mote.dustIndex >= Main.maxDust)
                        {
                            continue;
                        }

                        mote.customData = this;
                        motes[i] = mote;
                    }

                    // Own these dusts for exactly the wave's lifetime. Reset vanilla drift/scale decay each tick;
                    // scale stays at or below 1.1, then tapers to zero near players and over the final 12 ticks.
                    mote.position = position;
                    mote.velocity = Vector2.Zero;
                    mote.noGravity = true;
                    mote.noLight = true;
                    mote.fadeIn = 0f;
                    mote.scale = scale * opacity;
                    mote.alpha = (int)(200f * (1f - opacity));
                }
            }

            if (age >= DurationTicks)
            {
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            foreach (Dust mote in motes)
            {
                if (mote != null && mote.customData == this)
                {
                    mote.active = false;
                }
            }
        }
    }
}
