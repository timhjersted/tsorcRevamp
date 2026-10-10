using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Death
{
    /// <summary>
    /// Standalone Death Sickle entity. It can follow an owner or be driven independently through
    /// the movement-mode interface. ai[0] is the optional owner index; ai[1] is MovementMode.
    /// </summary>
    class DeathSickleWeapon : ModNPC
    {
        public enum MovementMode : byte
        {
            OwnerAttached = 0,
            EndPivot = 1,
            MiddlePivot = 2
        }

        public static readonly Rectangle VerticalCollisionBox = new(42, 14, 8, 81);
        public static readonly Rectangle TopCollisionBox = new(41, 18, 34, 12);
        public static readonly Vector2 EndPoint = new(46f, 95f);
        public static readonly Vector2 MiddlePoint = new(46f, 57f);
        public const float VisualScale = 7.2f;
        const float TextureWidth = 97f;
        int currentContactDamage;

        public override string Texture => "tsorcRevamp/NPCs/Bosses/Death/DeathSickleWeapon";

        internal int OwnerIndex => (int)NPC.ai[0];
        internal MovementMode Mode => (MovementMode)(int)NPC.ai[1];

        Vector2 BaseDrawOrigin => Mode == MovementMode.MiddlePivot ? MiddlePoint : EndPoint;

        public Vector2 EndPointWorld => LocalToWorld(EndPoint);
        public Vector2 MiddlePointWorld => LocalToWorld(MiddlePoint);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.scale = VisualScale;
            NPC.width = 97;
            NPC.height = 97;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = false;
            NPC.friendly = false;
            NPC.dontTakeDamage = true;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.chaseable = false;
            NPC.ShowNameOnHover = false;
            NPC.npcSlots = 0f;
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void AI()
        {
            NPC.velocity = Vector2.Zero;
            Lighting.AddLight(NPC.Center, GetGlowColor().ToVector3() * (2f * NPC.Opacity));

            if (OwnerIndex >= 0
                && (OwnerIndex >= Main.maxNPCs || !Main.npc[OwnerIndex].active))
            {
                NPC.active = false;
                return;
            }

            if (OwnerIndex >= 0 && Main.npc[OwnerIndex].ModNPC is DeathBossBase ownerBoss)
            {
                NPC.scale = VisualScale * ownerBoss.SickleScaleMultiplier;
            }

            TickCustomContactDamage();

            if (Mode != MovementMode.OwnerAttached)
            {
                return;
            }

            NPC owner = Main.npc[OwnerIndex];
            NPC.Center = owner.Center;
            NPC.rotation = 0f;
            NPC.direction = owner.direction;
            NPC.spriteDirection = owner.direction;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Vector2 origin = EffectiveDrawOrigin;
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float opacity = NPC.Opacity;

            Color glowColor = GetGlowColor();
            for (int i = 3; i >= 1; i--)
            {
                Main.EntitySpriteDraw(
                    texture,
                    NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY),
                    null,
                    glowColor * (0.10f * i * opacity),
                    NPC.rotation,
                    origin,
                    NPC.scale * (1f + i * 0.045f),
                    effects,
                    0f);
            }

            Main.EntitySpriteDraw(
                texture,
                NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY),
                null,
                NPC.GetAlpha(Color.White),
                NPC.rotation,
                origin,
                NPC.scale,
                effects,
                0f);

            return false;
        }

        public bool IntersectsCustomHitboxes(Rectangle targetHitbox)
        {
            return IntersectsRotatedRectangle(VerticalCollisionBox, targetHitbox)
                || IntersectsRotatedRectangle(TopCollisionBox, targetHitbox);
        }

        void TickCustomContactDamage()
        {
            if (currentContactDamage <= 0 || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int difficultyMultiplier = Main.masterMode ? 3 : Main.expertMode ? 2 : 1;
            int damage = currentContactDamage * difficultyMultiplier;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || !IntersectsCustomHitboxes(player.Hitbox))
                {
                    continue;
                }

                int hitDirection = Math.Sign(player.Center.X - NPC.Center.X);
                if (hitDirection == 0)
                {
                    hitDirection = 1;
                }

                player.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), damage, hitDirection);
            }
        }

        bool IntersectsRotatedRectangle(Rectangle localRectangle, Rectangle targetHitbox)
        {
            Vector2[] rectangleCorners =
            {
                LocalToWorld(new Vector2(localRectangle.Left, localRectangle.Top)),
                LocalToWorld(new Vector2(localRectangle.Right, localRectangle.Top)),
                LocalToWorld(new Vector2(localRectangle.Right, localRectangle.Bottom)),
                LocalToWorld(new Vector2(localRectangle.Left, localRectangle.Bottom))
            };

            Vector2[] targetCorners =
            {
                targetHitbox.TopLeft(),
                targetHitbox.TopRight(),
                targetHitbox.BottomRight(),
                targetHitbox.BottomLeft()
            };

            Vector2[] axes =
            {
                (rectangleCorners[1] - rectangleCorners[0]).SafeNormalize(Vector2.UnitX),
                (rectangleCorners[3] - rectangleCorners[0]).SafeNormalize(Vector2.UnitY),
                Vector2.UnitX,
                Vector2.UnitY
            };

            for (int i = 0; i < axes.Length; i++)
            {
                if (!OverlapsOnAxis(rectangleCorners, targetCorners, axes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool OverlapsOnAxis(Vector2[] first, Vector2[] second, Vector2 axis)
        {
            ProjectOntoAxis(first, axis, out float firstMin, out float firstMax);
            ProjectOntoAxis(second, axis, out float secondMin, out float secondMax);
            return firstMax >= secondMin && secondMax >= firstMin;
        }

        static void ProjectOntoAxis(Vector2[] points, Vector2 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                float projection = Vector2.Dot(points[i], axis);
                min = MathHelper.Min(min, projection);
                max = MathHelper.Max(max, projection);
            }
        }

        Vector2 LocalToWorld(Vector2 localPoint)
        {
            Vector2 effectivePoint = NPC.spriteDirection < 0
                ? new Vector2(TextureWidth - localPoint.X, localPoint.Y)
                : localPoint;

            return NPC.Center + ((effectivePoint - EffectiveDrawOrigin) * NPC.scale).RotatedBy(NPC.rotation);
        }

        Vector2 EffectiveDrawOrigin => NPC.spriteDirection < 0
            ? new Vector2(TextureWidth - BaseDrawOrigin.X, BaseDrawOrigin.Y)
            : BaseDrawOrigin;

        Color GetGlowColor()
        {
            if (OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs && Main.npc[OwnerIndex].ModNPC is DeathBossBase owner)
            {
                return owner.SickleGlowColor;
            }

            return new Color(125, 38, 185);
        }

        internal void SetPose(Vector2 center, float rotation, int direction, float opacity, int damage)
        {
            NPC.Center = center;
            NPC.rotation = rotation;
            NPC.direction = direction;
            NPC.spriteDirection = direction;
            NPC.alpha = (int)(255f * (1f - MathHelper.Clamp(opacity, 0f, 1f)));
            currentContactDamage = damage;
        }

        internal void SetMovementMode(MovementMode mode)
        {
            NPC.ai[1] = (float)mode;
            NPC.netUpdate = true;
        }

        public static int Spawn(IEntitySource source, Vector2 position, float rotation,
            int ownerIndex = -1, MovementMode mode = MovementMode.OwnerAttached, int damage = 0)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return -1;
            }

            int index = NPC.NewNPC(
                source,
                (int)position.X,
                (int)position.Y,
                ModContent.NPCType<DeathSickleWeapon>(),
                ai0: ownerIndex,
                ai1: (float)mode,
                ai2: rotation);

            if (index >= 0 && index < Main.maxNPCs)
            {
                NPC sickle = Main.npc[index];
                sickle.Center = position;
                sickle.rotation = rotation;
                sickle.damage = damage;
                sickle.alpha = 255;
                if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs
                    && Main.npc[ownerIndex].ModNPC is DeathBossBase owner)
                {
                    sickle.scale = VisualScale * owner.SickleScaleMultiplier;
                }
                sickle.netUpdate = true;
            }

            return index;
        }
    }
}
