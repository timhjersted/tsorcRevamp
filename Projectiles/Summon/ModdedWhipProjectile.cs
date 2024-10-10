using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Projectiles.Summon.Whips.PolarisLeash;

namespace tsorcRevamp.Projectiles.Summon
{
    public abstract class ModdedWhipProjectile : ModProjectile
    {
        public abstract int WhipWidth { get; } //the width of the whip's grip
        public abstract int WhipHeight { get; } //the height of the whip's grip
        public abstract int WhipSegments { get; } //how many segments the whip is supposed to generate on it's line, purely visual
        public abstract float WhipRangeMult { get; } //the length the whip will have, the item's usetime and animation will impact this too
        public abstract int DustId { get; } //id of the whips dust, keep in mind that some dusts do not have a name assigned to them
        public abstract int DustWidth { get; } //width of the whips dust
        public abstract int DustHeight { get; } //height of the whips dust
        public abstract Color DustColor { get; } //color of the whips dust
        public abstract float DustScale { get; } //multiplier determining the size of the whips dust
        public abstract float MaxChargeTime { get; } //Updates twice a tick so /2 this for the actual amount of ticks this needs
        public abstract Vector2 WhipTipBase { get; } //the size of the whip tips sprite
        public abstract float MaxChargeDmgDivisor { get; } //how much bonus dmg it will deal by charging up, higher is better
        public abstract float ChargeRangeBonus { get; } //how much range it will gain from charging up, higher is better
        public abstract int WhipDebuffId { get; } //the ID of the whip's debuff
        public abstract int WhipDebuffDuration { get; } //how long it will inflict it's debuff for
        public const int DefaultWhipBuffDuration = 4; //duration for tag effects that are applied to the player like attack speed
        public const int DefaultWhipDebuffDuration = 4; //duration for tag effects that are applied to the enemy like tag damage
        public abstract float WhipMultihitPenalty { get; } //how much dmg it loses on-hit, to recude it's crowd control: 1 is no dmg loss, 0.5 would mean it loses half it's dmg on each enemy hit
        public abstract Color WhipLineColor { get; } //color of the line that gets generated between each whip segment, depending on the whip it might not even be visible most of the time but you should set this to something fitting your whip anyways
        public virtual void CustomDustAndTipEffects(List<Vector2> points) { } //for whips that release more dust
        public virtual void CustomModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) { } //for special whips, simply set the values used in the modifyhit function to 0 to nullify them
        public virtual void CustomOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) { } //for special whips, simply set the values used in the onhit function to 0 to nullify them
        public int TimesHitThisSwing;
        public override void SetStaticDefaults()
        {
            // This makes the projectile use whip collision detection and allows flasks to be applied to it.
            ProjectileID.Sets.IsAWhip[Type] = true;
            tsorcRevamp.WhipTipBases.Add(Type, WhipTipBase);
        }
        public override void SetDefaults()
        {
            Projectile.width = WhipWidth;
            Projectile.height = WhipHeight;
            Projectile.WhipSettings.Segments = WhipSegments;
            Projectile.WhipSettings.RangeMultiplier = WhipRangeMult; //only thing affecting the actual whip range

            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true; // This prevents the projectile from hitting through solid tiles.
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;

            Projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ModdedWhip = true;
            if (MaxChargeTime > 0)
            {
                Projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ChargedWhip = true;
            }
            TimesHitThisSwing = 0;
        }

        public float Timer
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public float ChargeTime
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }
        private float ChargeTimer2 = 0;
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2; // Without PiOver2, the rotation would be off by 90 degrees counterclockwise.

            Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * Timer;
            // Vanilla uses Vector2.Dot(Projectile.velocity, Vector2.UnitX) here. Dot Product returns the difference between two vectors, 0 meaning they are perpendicular.
            // However, the use of UnitX basically turns it into a more complicated way of checking if the projectile's velocity is above or equal to zero on the X axis.
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;

            if (!Charge(owner))
            {
                return; // timer doesn't update while charging, freezing the animation at the start.
            }


            Timer++;

            float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;
            if (Timer >= swingTime || owner.itemAnimation <= 0)
            {
                Projectile.Kill();
                return;
            }

            owner.heldProj = Projectile.whoAmI;

            // Plays a whipcrack sound at the tip of the whip.
            List<Vector2> points = Projectile.WhipPointsForCollision;
            Projectile.FillWhipControlPoints(Projectile, points);
            Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], DustWidth, DustHeight, DustId, 0f, 0f, 150, DustColor, DustScale);
            CustomDustAndTipEffects(points);

            if (owner.GetModPlayer<tsorcRevampPlayer>().Goredrinker && !owner.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && owner.GetModPlayer<tsorcRevampPlayer>().GoredrinkerReady && Timer == 1) //this check is needed at the start to allow all goredrinker hits to calculate dmg correctly
            {
                owner.GetModPlayer<tsorcRevampPlayer>().GoredrinkerSwung = true;
            }

            if (Timer == swingTime / 2)
            {
                if (owner.GetModPlayer<tsorcRevampPlayer>().Goredrinker && !owner.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && owner.GetModPlayer<tsorcRevampPlayer>().GoredrinkerSwung)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/GoredrinkerSwing") with { Volume = .3f }, owner.Center);
                }
                else
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Item/SummonerWhipcrack") with { Volume = tsorcGlobalProjectile.WhipVolume, PitchVariance = tsorcGlobalProjectile.WhipPitch }, points[points.Count - 1]);
                }
            }
        }

        // This method handles a charging mechanic.
        // Returns true if fully charged or charging is disabled
        private bool Charge(Player owner)
        {
            // Like other whips, this whip updates twice per frame (Projectile.extraUpdates = 1), so 120 is equal to 1 second.
            if (!owner.channel || ChargeTime >= MaxChargeTime)
            {
                return true; // finished charging
            }

            ChargeTime += 1f * (owner.GetTotalAttackSpeed(DamageClass.SummonMeleeSpeed));
            ChargeTimer2 += 1f * (owner.GetTotalAttackSpeed(DamageClass.SummonMeleeSpeed));

            if (ChargeTimer2 >= (MaxChargeTime / 15))
            {
                Projectile.WhipSettings.Segments++;
                Projectile.WhipSettings.RangeMultiplier += ChargeRangeBonus;
                ChargeTimer2 = 0;
            }

            owner = Main.player[Projectile.owner];
            Vector2 mountedCenter = owner.MountedCenter;
            Vector2 unitVectorTowardsMouse = mountedCenter.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * owner.direction);
            owner.ChangeDir((unitVectorTowardsMouse.X > 0f) ? 1 : (-1));
            Projectile.velocity = unitVectorTowardsMouse * 4;

            // Reset the animation and item timer while charging.
            owner.itemAnimation = owner.itemAnimationMax;
            owner.itemTime = owner.itemTimeMax;

            return false; // still charging
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[Main.myPlayer];
            Vector2 WhipTip = WhipTipBase * Main.player[Main.myPlayer].whipRangeMultiplier * Projectile.WhipSettings.RangeMultiplier * player.GetModPlayer<tsorcRevampPlayer>().WhipCritHitboxSize;
            List<Vector2> points = Projectile.WhipPointsForCollision;
            if (MaxChargeTime > 0)
            {
                modifiers.SourceDamage *= MathF.Max(ChargeTime / (MaxChargeTime / MaxChargeDmgDivisor), 1f);
                modifiers.Knockback *= MathF.Max(ChargeTime / (MaxChargeTime / MaxChargeDmgDivisor), 1f);
            }
            if (Utils.CenteredRectangle(Projectile.WhipPointsForCollision[points.Count - 2], WhipTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(Projectile.WhipPointsForCollision[points.Count - 1], WhipTip).Intersects(target.Hitbox))
            {
                modifiers.SetCrit();
            }
            CustomModifyHitNPC(target, ref modifiers);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (WhipDebuffDuration != 0)
            {
                target.AddBuff(WhipDebuffId, (int)(WhipDebuffDuration * 60 * Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().SummonTagDuration));
            }
            if (Projectile.type != ModContent.ProjectileType<PolarisLeashProjectile>())
            {
                Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
            }
            Projectile.damage = (int)(Projectile.damage * WhipMultihitPenalty); // Multihit penalty. Decrease the damage the more enemies the whip hits.
            TimesHitThisSwing++;
            CustomOnHitNPC(target, hit, damageDone);
        }

        // This method draws a line between all points of the whip, in case there's empty space between the sprites.
        public void DrawLine(List<Vector2> list)
        {
            Texture2D texture = TextureAssets.FishingLine.Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = new Vector2(frame.Width / 2, 2);

            Vector2 pos = list[0];
            for (int i = 0; i < list.Count - 1; i++)
            {
                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;

                float rotation = diff.ToRotation() - MathHelper.PiOver2;
                Color color = Lighting.GetColor(element.ToTileCoordinates(), WhipLineColor);
                Vector2 scale = new Vector2(1, (diff.Length() + 2) / frame.Height);

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

                pos += diff;
            }
        }
    }
}
