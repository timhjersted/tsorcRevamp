using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.NPCs.Bosses.Ornsmough;

namespace tsorcRevamp.Content.Items.Armor.Enemy
{
    [AutoloadEquip(EquipType.Head)]
    public class OrnsteinHead : ModItem
    {
        public const float CritChance = 10f;
        public const int MaxMana = 40;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance, MaxMana);

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 24;
            Item.defense = 14;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += CritChance;
            player.GetCritChance(DamageClass.Magic) += CritChance;
            player.statManaMax2 += MaxMana;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfOrnstein>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class OrnsteinBody : ModItem
    {
        public const float Damage = 10f;
        public const int ManaPerMeleeHit = 3;
        public const int ManaRestoreCooldownTicks = 6;

        // Spellblade Current
        public const int MaxStatic = 5;
        public const int StaticDrainSeconds = 3;
        public const float MagicDamagePerStatic = 8f;
        public const int ArcRangeTiles = 25;
        public const float ArcDamagePercent = 30f;
        public const int ChargedEdgeSeconds = 3;
        public const float EdgeBoltDamagePercent = 50f;
        public const int EdgeBoltRangeTiles = 20;

        // Lightning Dash. The motion constants equal Ghost Step's today, and are separate so either can be tuned alone.
        public const int LightningDashManaCost = 15;
        public const int LightningDashBaseDamage = 90;
        public const float LightningDashKnockback = 4f;
        public const int LightningDashElectrifiedSeconds = 3;
        public const int LightningDashExtraImmunityFrames = 2;
        public const int LightningDashExtraDistanceTiles = 3;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            Damage,
            ManaPerMeleeHit,
            MaxStatic,
            StaticDrainSeconds,
            MagicDamagePerStatic,
            ArcRangeTiles,
            ArcDamagePercent,
            ChargedEdgeSeconds,
            EdgeBoltDamagePercent,
            LightningDashManaCost,
            LightningDashBaseDamage);

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 20;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += Damage / 100f;
            player.GetDamage(DamageClass.Magic) += Damage / 100f;
            player.GetModPlayer<OrnsteinSpellbladePlayer>().ManaOnMeleeHit = true;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<OrnsteinHead>() && legs.type == ModContent.ItemType<OrnsteinLegs>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<OrnsteinSpellbladePlayer>().SetActive = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfOrnstein>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class OrnsteinLegs : ModItem
    {
        public const float MoveSpeed = 12f;
        public const float MeleeSpeed = 8f;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, MeleeSpeed);

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.defense = 14;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfOrnstein>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }

    /// <summary>Spellblade Current: melee hits charge the next spell, and magic hits charge the next melee hit.
    /// All of it is decided on the owning client. Only the Static count is sent out, so others can see the sparks.</summary>
    public class OrnsteinSpellbladePlayer : ModPlayer
    {
        private const float SparkOrbitRadius = 28f;
        private const float SparkOrbitSpeed = 0.09f;

        public bool ManaOnMeleeHit;
        public bool SetActive;
        public int StaticStacks;

        private int staticDrainTimer;
        private int chargedEdgeTimer;
        private int manaRestoreCooldown;

        private bool IsOwner => Player.whoAmI == Main.myPlayer;

        public override void ResetEffects()
        {
            ManaOnMeleeHit = false;
            SetActive = false;
        }

        public override void PostUpdateEquips()
        {
            if (!IsOwner)
            {
                return;
            }

            if (manaRestoreCooldown > 0)
            {
                manaRestoreCooldown--;
            }

            if (!SetActive)
            {
                if (StaticStacks > 0 || chargedEdgeTimer > 0)
                {
                    ClearCharges();
                }

                return;
            }

            if (staticDrainTimer > 0)
            {
                staticDrainTimer--;

                if (staticDrainTimer == 0)
                {
                    StaticStacks = 0;
                }
            }

            if (chargedEdgeTimer > 0)
            {
                chargedEdgeTimer--;
            }

            // The timers above are the real state. The buffs only display them, so cancelling an icon changes nothing.
            int staticBuff = ModContent.BuffType<OrnsteinStatic>();
            int edgeBuff = ModContent.BuffType<OrnsteinChargedEdge>();

            if (StaticStacks > 0)
            {
                Player.AddBuff(staticBuff, staticDrainTimer);
            }
            else
            {
                Player.ClearBuff(staticBuff);
            }

            if (chargedEdgeTimer > 0)
            {
                Player.AddBuff(edgeBuff, chargedEdgeTimer);
            }
            else
            {
                Player.ClearBuff(edgeBuff);
            }
        }

        public override void PostUpdate()
        {
            if (Main.dedServ || Player.dead)
            {
                return;
            }

            // One spark per stack, spaced evenly around the player. Dust is redrawn every other tick to read as a solid orbit.
            if (StaticStacks > 0 && Main.GameUpdateCount % 2 == 0)
            {
                float baseAngle = Main.GameUpdateCount * SparkOrbitSpeed;

                for (int i = 0; i < StaticStacks; i++)
                {
                    float sparkAngle = baseAngle + MathHelper.TwoPi * i / StaticStacks;
                    Vector2 sparkPosition = Player.Center + sparkAngle.ToRotationVector2() * SparkOrbitRadius;
                    Dust spark = Dust.NewDustPerfect(sparkPosition, DustID.Electric, Vector2.Zero, 80, default, 0.55f);
                    spark.noGravity = true;
                }
            }

            // Charged Edge isn't synced, so only its owner sees this crackle.
            if (IsOwner && chargedEdgeTimer > 0 && Main.GameUpdateCount % 4 == 0)
            {
                Vector2 cracklePosition = Player.Center + Main.rand.NextVector2Circular(20f, 24f);
                Dust crackle = Dust.NewDustPerfect(cracklePosition, DustID.GoldFlame, Main.rand.NextVector2Circular(1f, 1f), 60, default, 0.8f);
                crackle.noGravity = true;
            }
        }

        public void AddStatic()
        {
            StaticStacks = Math.Min(StaticStacks + 1, OrnsteinBody.MaxStatic);
            staticDrainTimer = OrnsteinBody.StaticDrainSeconds * 60;
        }

        private void ClearCharges()
        {
            StaticStacks = 0;
            staticDrainTimer = 0;
            chargedEdgeTimer = 0;
            Player.ClearBuff(ModContent.BuffType<OrnsteinStatic>());
            Player.ClearBuff(ModContent.BuffType<OrnsteinChargedEdge>());
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            ClearCharges();
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Item swings have no projectile to fire from, so estimate the blade tip from the item's size along the line to the target.
            float itemReach = (item.width + item.height) * 0.5f * Player.GetAdjustedItemScale(item);
            float targetDistance = Player.Distance(target.Center);
            Vector2 toTarget = Player.DirectionTo(target.Center);
            Vector2 weaponTip = Player.Center + toTarget * Math.Min(itemReach, targetDistance);

            ProcessHit(target, hit, weaponTip);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // The set's own lightning must not feed itself. The dash grants its Static directly.
            if (proj.type == ModContent.ProjectileType<OrnsteinLightning>())
            {
                return;
            }

            ProcessHit(target, hit, proj.Center);
        }

        private void ProcessHit(NPC target, NPC.HitInfo hit, Vector2 weaponTip)
        {
            if (hit.DamageType.CountsAsClass(DamageClass.Magic))
            {
                if (SetActive)
                {
                    chargedEdgeTimer = OrnsteinBody.ChargedEdgeSeconds * 60;
                }

                return;
            }

            if (!hit.DamageType.CountsAsClass(DamageClass.Melee))
            {
                return;
            }

            if (ManaOnMeleeHit && manaRestoreCooldown <= 0)
            {
                Player.statMana = Math.Min(Player.statMana + OrnsteinBody.ManaPerMeleeHit, Player.statManaMax2);
                manaRestoreCooldown = OrnsteinBody.ManaRestoreCooldownTicks;
            }

            if (!SetActive)
            {
                return;
            }

            AddStatic();

            if (chargedEdgeTimer > 0)
            {
                chargedEdgeTimer = 0;
                FireEdgeBolt(target, hit, weaponTip);
            }
        }

        private void FireEdgeBolt(NPC target, NPC.HitInfo hit, Vector2 weaponTip)
        {
            Vector2 direction = Player.DirectionTo(target.Center);
            float maxRange = OrnsteinBody.EdgeBoltRangeTiles * 16f;
            float boltLength = 0f;

            // Walk the line one tile at a time, so the bolt stops at the first wall instead of passing through it.
            while (boltLength < maxRange)
            {
                float nextLength = Math.Min(boltLength + 16f, maxRange);
                Vector2 nextPoint = weaponTip + direction * nextLength;

                if (!Collision.CanHitLine(weaponTip, 1, 1, nextPoint, 1, 1))
                {
                    break;
                }

                boltLength = nextLength;
            }

            int boltDamage = Math.Max(1, (int)(hit.SourceDamage * OrnsteinBody.EdgeBoltDamagePercent / 100f));
            Vector2 boltSpan = direction * Math.Max(boltLength, 16f);
            float mode = (float)OrnsteinLightningMode.EdgeBolt;

            Projectile.NewProjectile(Player.GetSource_FromThis(), weaponTip, boltSpan, ModContent.ProjectileType<OrnsteinLightning>(), boltDamage, 0f, Player.whoAmI, mode);
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.6f, PitchVariance = 0.2f }, weaponTip);
        }

        private bool WillSpendStatic(Item item)
        {
            return IsOwner && SetActive && StaticStacks > 0 && item.DamageType.CountsAsClass(DamageClass.Magic);
        }

        // ModifyShootStats runs just before Shoot for the same cast, so the bonus covers every projectile of that cast.
        public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (!WillSpendStatic(item))
            {
                return;
            }

            float damageBonus = StaticStacks * OrnsteinBody.MagicDamagePerStatic / 100f;
            damage = (int)(damage * (1f + damageBonus));
        }

        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!WillSpendStatic(item))
            {
                return true;
            }

            float arcRange = OrnsteinBody.ArcRangeTiles * 16f;
            List<NPC> targets = new List<NPC>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC candidate = Main.npc[i];
                bool inRange = candidate.CanBeChasedBy() && Player.Distance(candidate.Center) <= arcRange;

                if (inRange && Collision.CanHitLine(Player.Center, 1, 1, candidate.Center, 1, 1))
                {
                    targets.Add(candidate);
                }
            }

            targets.Sort((first, second) => Player.DistanceSQ(first.Center).CompareTo(Player.DistanceSQ(second.Center)));

            // Nearest distinct enemies first. Spare arcs wrap around, so a lone boss still takes one arc per stack.
            if (targets.Count > 0)
            {
                int arcDamage = Math.Max(1, (int)(damage * OrnsteinBody.ArcDamagePercent / 100f));
                float mode = (float)OrnsteinLightningMode.Arc;

                for (int i = 0; i < StaticStacks; i++)
                {
                    NPC target = targets[i % targets.Count];
                    Vector2 arcSpan = target.Center - Player.Center;
                    Projectile.NewProjectile(source, Player.Center, arcSpan, ModContent.ProjectileType<OrnsteinLightning>(), arcDamage, 0f, Player.whoAmI, mode, target.whoAmI);
                }

                SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.7f, PitchVariance = 0.2f }, Player.Center);
            }

            StaticStacks = 0;
            staticDrainTimer = 0;
            return true;
        }

        /// <summary>Called by the dodge roll on the owning client when a paid Lightning Dash starts.</summary>
        public void SpawnDashHitbox()
        {
            float baseDamage = OrnsteinBody.LightningDashBaseDamage;
            float meleeDamage = Player.GetTotalDamage(DamageClass.Melee).ApplyTo(baseDamage);
            float magicDamage = Player.GetTotalDamage(DamageClass.Magic).ApplyTo(baseDamage);
            int dashDamage = (int)Math.Max(meleeDamage, magicDamage);
            float mode = (float)OrnsteinLightningMode.Dash;

            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<OrnsteinLightning>(), dashDamage, OrnsteinBody.LightningDashKnockback, Player.whoAmI, mode);
        }

        // ── Multiplayer: only the Static count leaves the owning client ──
        public override void CopyClientState(ModPlayer targetCopy)
        {
            OrnsteinSpellbladePlayer clone = (OrnsteinSpellbladePlayer)targetCopy;
            clone.StaticStacks = StaticStacks;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            OrnsteinSpellbladePlayer clone = (OrnsteinSpellbladePlayer)clientPlayer;

            if (clone.StaticStacks != StaticStacks)
            {
                SendStaticPacket(-1, -1);
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SendStaticPacket(toWho, fromWho);
        }

        private void SendStaticPacket(int toWho, int ignoreWho)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write(tsorcPacketID.SyncOrnsteinStatic);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)StaticStacks);
            packet.Send(toWho, ignoreWho);
        }

        public static void ReceiveStaticPacket(BinaryReader reader, int sender)
        {
            int playerIndex = reader.ReadByte();
            int stacks = reader.ReadByte();
            bool isServer = Main.netMode == NetmodeID.Server;

            // On the server, a client may only report its own player.
            if (playerIndex >= Main.maxPlayers || (isServer && playerIndex != sender))
            {
                return;
            }

            OrnsteinSpellbladePlayer spellblade = Main.player[playerIndex].GetModPlayer<OrnsteinSpellbladePlayer>();
            spellblade.StaticStacks = Math.Min(stacks, OrnsteinBody.MaxStatic);

            if (isServer)
            {
                spellblade.SendStaticPacket(-1, sender);
            }
        }
    }

    public enum OrnsteinLightningMode
    {
        Dash,
        Arc,
        EdgeBolt
    }

    /// <summary>The set's friendly lightning. Dash rides the player as an invisible hitbox. Arc and EdgeBolt are instant
    /// strikes: position is the start of the line, velocity is the whole line, and neither ever moves.</summary>
    public class OrnsteinLightning : ModProjectile
    {
        private const int LineLifeTicks = 14;
        private const int LineLiveTicks = 3;
        private const float LineHitWidth = 20f;

        private static readonly Color GoldEdge = new Color(255, 190, 70, 0);
        private static readonly Color GoldCore = new Color(255, 240, 190, 0);

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private OrnsteinLightningMode Mode => (OrnsteinLightningMode)(int)Projectile.ai[0];

        private ref float LineAge => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LineLifeTicks;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // each enemy can be hit once per dash or strike
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            if (Mode == OrnsteinLightningMode.Dash)
            {
                Player owner = Main.player[Projectile.owner];
                tsorcRevampPlayer rollPlayer = owner.GetModPlayer<tsorcRevampPlayer>();

                if (!owner.active || owner.dead || !rollPlayer.isDodging || !rollPlayer.dodgeIsLightningDash)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.Center = owner.Center;
                Projectile.timeLeft = 2;
                return;
            }

            if (Mode == OrnsteinLightningMode.EdgeBolt)
            {
                Projectile.DamageType = DamageClass.Melee;
            }

            if (LineAge == 0f && !Main.dedServ)
            {
                Vector2 lineStart = Projectile.Center;
                Vector2 lineEnd = lineStart + Projectile.velocity;
                int dustCount = (int)(Projectile.velocity.Length() / 20f) + 1;

                for (int i = 0; i <= dustCount; i++)
                {
                    Vector2 dustPosition = Vector2.Lerp(lineStart, lineEnd, i / (float)dustCount);
                    Dust lineDust = Dust.NewDustPerfect(dustPosition, DustID.Electric, Main.rand.NextVector2Circular(1.5f, 1.5f), 60, default, 0.8f);
                    lineDust.noGravity = true;
                }
            }

            LineAge++;
        }

        public override bool? CanDamage()
        {
            if (Mode == OrnsteinLightningMode.Dash || LineAge <= LineLiveTicks)
            {
                return null;
            }

            return false;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (Mode == OrnsteinLightningMode.Arc && target.whoAmI != (int)Projectile.ai[1])
            {
                return false;
            }

            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Mode == OrnsteinLightningMode.Dash)
            {
                return null;
            }

            Vector2 lineStart = Projectile.Center;
            Vector2 lineEnd = lineStart + Projectile.velocity;
            float collisionPoint = 0f;

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), lineStart, lineEnd, LineHitWidth, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Mode != OrnsteinLightningMode.Dash)
            {
                return;
            }

            target.AddBuff(ModContent.BuffType<ElectrifiedDebuff>(), OrnsteinBody.LightningDashElectrifiedSeconds * 60);
            Main.player[Projectile.owner].GetModPlayer<OrnsteinSpellbladePlayer>().AddStatic();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Mode == OrnsteinLightningMode.Dash)
            {
                return false; // the dash is drawn by the player's trail layer and dust
            }

            Vector2 lineStart = Projectile.Center;
            Vector2 lineEnd = lineStart + Projectile.velocity;
            float opacity = 1f - MathHelper.Clamp(LineAge / LineLifeTicks, 0f, 1f);
            int segments = Math.Max(3, (int)(Projectile.velocity.Length() / 28f));
            int seed = Projectile.identity * 31 + (int)(Main.GameUpdateCount / 3) * 7;

            WardenLightning.DrawJaggedBolt(seed, lineStart, lineEnd, GoldEdge * opacity, GoldCore * opacity, 2f, segments, 10f);
            return false;
        }
    }
}
