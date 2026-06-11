using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.NPCs.EnemySpriteRendering;

namespace tsorcRevamp.NPCs.Invaders
{
    public class FaraamSpriteTestInvader : ModNPC, IEnemySpriteRendererHost
    {
        public override string Texture => "tsorcRevamp/NPCs/Invaders/InvaderPlaceholder";

        private EnemySpriteRenderer spriteRenderer;

        public EnemySpriteRenderer SpriteRenderer => spriteRenderer;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 0f;
            NPC.width = 20;
            NPC.height = 40;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 500;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.5f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 0;

            spriteRenderer = CreateSpriteRenderer();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return 0f;
        }

        public override void AI()
        {
            spriteRenderer ??= CreateSpriteRenderer();

            NPC.TargetClosest(false);
            Player target = Main.player[NPC.target];
            if (target.active && !target.dead)
            {
                NPC.direction = target.Center.X < NPC.Center.X ? -1 : 1;
            }

            NPC.spriteDirection = NPC.direction;
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * 1.2f, 0.06f);

            if (NPC.collideX && NPC.velocity.Y == 0f)
            {
                NPC.velocity.Y = -5f;
            }

            spriteRenderer.Tick(NPC);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            spriteRenderer ??= CreateSpriteRenderer();
            return spriteRenderer.Draw(NPC, spriteBatch, screenPos, drawColor);
        }

        private static EnemySpriteRenderer CreateSpriteRenderer()
        {
            return new EnemySpriteRenderer(
                ModContent.ItemType<FaraamDraftHelmet>(),
                ModContent.ItemType<FaraamDraftBreastplate>(),
                ModContent.ItemType<FaraamDraftGreaves>());
        }
    }
}
