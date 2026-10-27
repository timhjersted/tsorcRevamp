using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Buffs;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Bases;

public class RuneterraGauntletNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public bool Scorched;
    public int ScorchMarks = 0;
    public float SuperScorchDuration = 0f;
    public bool Shocked;
    public int ShockMarks = 0;
    public float SuperShockDuration = 0f;
    public bool Sunburnt;
    public bool Awestruck;
    public int SunburnMarks = 0;
    public float SuperSunburnDuration = 0f;

    public Player LastHitter;

    public override void ResetEffects(NPC npc)
    {
        Scorched  = false;
        Shocked = false;
        Sunburnt = false;
        Awestruck = false;
    }

    public float DoDOT(NPC npc, bool flag, int baseDmg, bool lessPotency = false, bool isFire = true)
    {
        if (flag && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
        {
            float dotBase = LastHitter.GetTotalDamage(DamageClass.Summon).ApplyTo((float)baseDmg / 3f);
            return tsorcRevampGlobalNPC.DoDamageOverTime(npc, dotBase, lessPotency, isFire);
        }
        return 0;
    }
    
    
    Texture2D ScorchMarksSprite;
    Texture2D ShockMarksSprite;
    Texture2D SunburnMarksSprite;
    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
            if (npc.HasBuff(ModContent.BuffType<ScorchingDebuff>()) && ScorchMarks > 0)
            {
                ScorchMarksSprite = (Texture2D)ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(ScorchingDebuff)) + "_MarkVisual");
                Rectangle ScorchingMarkSourceRectangle = new Rectangle(0, (6 - ScorchMarks) * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                Main.EntitySpriteDraw(ScorchMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, ScorchMarksSprite.Height / 6 * ScorchMarks - 100), 
                    ScorchingMarkSourceRectangle, Color.White, 0, ScorchingMarkSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }
            if (npc.HasBuff(ModContent.BuffType<ShockedDebuff>()) && ShockMarks > 0)
            {
                ShockMarksSprite = (Texture2D)ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(ShockedDebuff)) + "_MarkVisual");
                Rectangle ShockedMarkSourceRectangle = new Rectangle(0, (6 - ShockMarks) * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                Main.EntitySpriteDraw(ShockMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, ShockMarksSprite.Height / 6 * ShockMarks - 100), 
                    ShockedMarkSourceRectangle, Color.White, 0, ShockedMarkSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }
            if (npc.HasBuff(ModContent.BuffType<SunburnDebuff>()) && SunburnMarks > 0)
            {
                SunburnMarksSprite = (Texture2D)ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(SunburnDebuff)) + "_MarkVisual");
                Rectangle SunburnMarkSourceRectangle = new Rectangle(0, (6 - SunburnMarks) * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                Main.EntitySpriteDraw(SunburnMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, SunburnMarksSprite.Height / 6 * SunburnMarks - 100), 
                    SunburnMarkSourceRectangle, Color.White, 0, SunburnMarkSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }
            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
    }
}