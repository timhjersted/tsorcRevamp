using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Projectiles.Ranged;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.LegacyCode;

public class AccuracyPlayer : ModPlayer //move this player file if Accuracy is ever tied to any item to that item file
{

    public bool Enabled = false;
    public float AccuracyRangedBaseCritMult = 0.5f;
    public float CurrentAccuracyPercent = 0f;
    public float AccuracyPercentMax = 1f;
    // 10, not 8.5, so each of the ten 10% accuracy bands is worth exactly 1% crit — the buff icon shows
    // which band you're in, and "one frame = one percent" is a rule the player can hold in their head.
    public float AccuracyMaxFlatCrit = 10f;
    /// <summary>Which 10% band the accuracy meter was in last frame, for the buff icon frame and to fire
    /// combat text only on band changes instead of on every projectile. -1 = uninitialised.</summary>
    public int LastAccuracyBand = -1;
    public float AccuracyMaxCritMult = 0.75f;
    public float AccuracyGain = 0.04f;
    public float AccuracyLoss = 0.08f;
    public float AccuracyCurrentTotalRangedCritChance;

    public override void ResetEffects()
    {
        Enabled = false;
    }

    public override void PostUpdateBuffs()
    {
        // Accuracy meter readout. Unlike its stack-based siblings this isn't granted by landing a hit — it's
        // a persistent meter, so the buff is refreshed every frame (2 ticks to avoid flicker) rather than
        // given a duration. Shown while you're holding a ranged weapon OR the meter is above zero, so a
        // ranged build always has the readout while a melee BotC never sees it on their buff bar.
        if (Enabled && (CurrentAccuracyPercent > 0f 
                        || (Player.HeldItem != null && !Player.HeldItem.IsAir &&
                            Player.HeldItem.DamageType == DamageClass.Ranged && Player.HeldItem.damage > 0)))
        {
            Player.AddBuff(ModContent.BuffType<AccuracyBuff>(), 2);
        }
    }

    public override void PostUpdateEquips()
    {
        if (Enabled)
        {
            AccuracyCurrentTotalRangedCritChance = 
                Player.GetTotalCritChance(DamageClass.Ranged) + Player.HeldItem.crit; //catch total ranged crit chance
            Player.GetCritChance(DamageClass.Ranged) -= 
                Player.GetTotalCritChance(DamageClass.Ranged) + Player.HeldItem.crit; //subtract total ranged crit chance from ranged crit chance because you can't alter total ranged crit chance
            Player.GetCritChance(DamageClass.Ranged) += 
                (AccuracyCurrentTotalRangedCritChance + (CurrentAccuracyPercent * AccuracyMaxFlatCrit)) * 
                (AccuracyRangedBaseCritMult + (CurrentAccuracyPercent * AccuracyMaxCritMult)); //return total ranged crit chance in a way that lets you multiply it with accuracy
        }
    }
}

public class AccuracyProjectile : GlobalProjectile
{
    public bool IgnoresAccuracyOrSpecialCase = false;
    public bool HitSomething = false;

    public override bool InstancePerEntity => true;

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        Player player = Main.player[projectile.owner];
                var accuracyPlayer = Main.player[projectile.owner].GetModPlayer<AccuracyPlayer>();
                if (!IsAccuracySpecialCase(projectile) && projectile.DamageType == DamageClass.Ranged && accuracyPlayer.Enabled && projectile.damage != 0)
                {
                    if (HitSomething)
                    {
                        accuracyPlayer.CurrentAccuracyPercent += accuracyPlayer.AccuracyGain;
                    }
                    else
                    {
                        accuracyPlayer.CurrentAccuracyPercent -= accuracyPlayer.AccuracyLoss;
                    }
                    if (accuracyPlayer.CurrentAccuracyPercent > accuracyPlayer.AccuracyPercentMax)
                    {
                        accuracyPlayer.CurrentAccuracyPercent = accuracyPlayer.AccuracyPercentMax;
                    }
                    if (accuracyPlayer.CurrentAccuracyPercent < 0)
                    {
                        accuracyPlayer.CurrentAccuracyPercent = 0;
                    }

                    // Combat text fires only when the meter crosses into a new 10% band — the same bands the
                    // Accuracy buff icon shows — instead of once per projectile. That's roughly a tenth of the
                    // text, and what remains actually means something: your crit tier changed. The exact
                    // percentage now lives permanently in the buff tooltip rather than scrolling past.
                    // The two extremes always announce themselves, since bottoming out and capping are the
                    // states worth noticing.
                    int band = AccuracyBuff.BandOf(accuracyPlayer.CurrentAccuracyPercent);
                    bool atExtreme = accuracyPlayer.CurrentAccuracyPercent <= 0f
                                     || accuracyPlayer.CurrentAccuracyPercent >= accuracyPlayer.AccuracyPercentMax;
                    if ((band != accuracyPlayer.LastAccuracyBand || atExtreme)
                        && player.whoAmI == Main.myPlayer
                        && ModContent.GetInstance<tsorcRevampConfig>().AccuracyCombatText)
                    {
                        CombatText.NewText(player.Hitbox, Color.BurlyWood, LangUtils.GetTextValue(
                            HitSomething ? "UI.AccuracyHit" : "UI.AccuracyMiss",
                            (int)(accuracyPlayer.CurrentAccuracyPercent * 100f)));
                    }
                    accuracyPlayer.LastAccuracyBand = band;
                }
    }

    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {           
        if (!IsAccuracySpecialCase(projectile))
        {
            HitSomething = true;
        }
    }


    /// <summary>
        /// Simply returns true if the projectile is a special case that isn't supposed to count for or against accuracy (like explosions, projectiles spawned by projectiles, homing ones, purely visual ones, etc
        /// Projectile *types* that are always special cases should just go in PopulateAccuracySpecialCases(), this is mainly for more complex stuff (like checking the projectile's ai[] fields)
        /// </summary>
        /// <param name="projectile"></param>
        /// <returns></returns>
        public static bool IsAccuracySpecialCase(Projectile projectile)
        {
            return AccuracySpecialCaseList.Contains(projectile.type) || (projectile.type == ProjectileID.Bone && projectile.ai[2] == 1);
        }


        //This loads the list automatically the first time someone tries to access it
        //It works by checking if the list is null, loading it if it is, then returning it        
        public static List<int> AccuracySpecialCaseList
        {
            get
            {
                if (AccuracySpecialCases == null)
                {
                    PopulateAccuracySpecialCases();
                }

                return AccuracySpecialCases;
            }
        }

        /// <summary>
        /// This is where the list of all accuracy special case projectiles go
        /// Works like all the other PopulateX() functions we have
        /// </summary>
        private static void PopulateAccuracySpecialCases()
        {
            AccuracySpecialCases = new List<int>()
            {
               ModContent.ProjectileType<ElfinArrow>(), ModContent.ProjectileType<ToxicCatExplosion>(), ModContent.ProjectileType<VirulentCatExplosion>(), ModContent.ProjectileType<BiohazardExplosion>(),
               ModContent.ProjectileType<KrakenTsunamiShark>(), ProjectileID.CrystalShard, ModContent.ProjectileType<ShulletBellDark>(),  ModContent.ProjectileType<ShulletBellLight>(),
               ProjectileID.ChlorophyteBullet, ProjectileID.ChlorophyteArrow, ProjectileID.HallowStar, ProjectileID.DD2BetsyArrow, ProjectileID.Xenopopper,
               ProjectileID.DD2PhoenixBow
            };
        }

        public static List<int> AccuracySpecialCases;

        public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
        {
            if (projectile.type == ProjectileID.EyeLaser && projectile.ai[0] == 1)
            {
                target.AddBuff(BuffID.Slow, 3 * 60);
            }

            if (projectile.type == ProjectileID.DemonSickle)
            {
                target.AddBuff(ModContent.BuffType<Crippled>(), 15);
                target.AddBuff(BuffID.Slow, 3 * 60);
                target.AddBuff(BuffID.Darkness, 3 * 60);
            }
            if (projectile.type == ProjectileID.Bubble)
            {
                SoundEngine.PlaySound(SoundID.Drown, target.Center);
                target.AddBuff(BuffID.Chilled, 8 * 60);
                target.AddBuff(ModContent.BuffType<Gilled>(), 16 * 60);
            }
        }
}


    // PLACEHOLDER sprite (copy of Buffs/Runeterra/Magic/Attunement) — replace with bespoke art.
    // Both Accuracy.png and AccuracySheet.png are copies; the sheet is the 10-frame band strip.

    /// <summary>
    /// Readout for Bearer of the Curse's ranged mechanic. Unlike its three siblings (Conqueror, Lethal Tempo,
    /// Attunement) the underlying mechanic is NOT stack-based — accuracy is a continuously varying 0-100%
    /// tracked in tsorcRevampPlayer.BotCCurrentAccuracyPercent, gained on every projectile that connects and
    /// lost at double rate on every one that doesn't.
    ///
    /// That continuous value is why it had no buff for so long: a 0-100% meter doesn't map onto a stack icon.
    /// It's displayed here in ten 10% BANDS, which fit the same 10-frame strip the other mechanics use, and
    /// which each correspond to exactly 1% flat crit. The exact percentage lives in the tooltip below, so it's
    /// permanently readable rather than only visible in combat text as it scrolls past.
    ///
    /// This buff is display-only. The payout lives in tsorcRevampPlayer (crit chance and crit damage both
    /// scale off the meter), and the meter itself is driven from tsorcGlobalProjectile.
    /// </summary>
    public class AccuracyBuff : ModBuff
    {
        public const int FrameCount = 10;
        private Asset<Texture2D> animatedTexture;

        /// <summary>Which 10% band a raw 0-1 accuracy value falls into. 100% clamps into the top band rather
        /// than overflowing to a non-existent 11th frame.</summary>
        public static int BandOf(float accuracyPercent)
        {
            int band = (int)(accuracyPercent * FrameCount);
            return band < 0 ? 0 : (band > FrameCount - 1 ? FrameCount - 1 : band);
        }

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true; // a permanent meter has no meaningful countdown to show
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            if (Main.netMode != NetmodeID.Server)
            {
                // Do NOT load textures on the server!
                animatedTexture = ModContent.Request<Texture2D>(Texture + "Sheet");
            }
        }

        /// <summary>
        /// Built at display time rather than baked into the localization string, so the numbers can never drift
        /// from the constants and the last line can report the player's live accuracy and what it's currently
        /// granting — the persistent readout this mechanic never had.
        /// </summary>
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<AccuracyPlayer>();
            float accuracy = modPlayer.CurrentAccuracyPercent;

            tip = LangUtils.GetTextValue(
                "Buffs.Accuracy.Description",
                (int)(modPlayer.AccuracyGain * 100f),
                (int)(modPlayer.AccuracyLoss * 100f),
                (int)modPlayer.AccuracyMaxFlatCrit,
                (int)(modPlayer.AccuracyMaxCritMult * 100f),
                (int)(accuracy * 100f),
                (accuracy * modPlayer.AccuracyMaxFlatCrit).ToString("0.0"),
                (int)(accuracy * modPlayer.AccuracyMaxCritMult * 100f));
        }

        public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
        {
            // Frame of the band strip matching the current 10% band, same idea as Conqueror's stack icon.
            Texture2D ourTexture = animatedTexture.Value;
            Rectangle ourSourceRectangle = animatedTexture.Frame(
                verticalFrames: FrameCount,
                frameY: BandOf(Main.LocalPlayer.GetModPlayer<AccuracyPlayer>().CurrentAccuracyPercent));

            drawParams.Texture = ourTexture;
            drawParams.SourceRectangle = ourSourceRectangle;
            return true;
        }
    }

    public class AccuracyItems : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            if (player.GetModPlayer<AccuracyPlayer>().Enabled && item.DamageType == DamageClass.Ranged)
            {
                TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips, LangUtils.GetTextValue("CommonItemTooltip.Ranged.CurrentAccuracy", (int)(player.GetModPlayer<AccuracyPlayer>().CurrentAccuracyPercent * 100f)));
            }
        }
    }
    