using Terraria;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Implemented by a ModNPC whose attack state lives in bespoke fields (C# flags, custom ai-slot
    /// conventions) that the generic poise reset (<see cref="tsorcRevampGlobalNPC.PoiseStaggerResetsAI"/> →
    /// ai[1]=60/ai[2]=-100) can't clear on its own.
    /// <para/>
    /// When a poise break staggers the enemy, the poise system calls <see cref="OnStagger"/> so the enemy can
    /// cancel exactly its own in-progress attack — clear its attack-state flags and reset its attack timers to a
    /// neutral value — and return to neutral approach. The interface takes precedence over PoiseStaggerResetsAI
    /// (it's strictly more capable); simple timer-only enemies can keep using the flag instead of implementing this.
    /// </summary>
    public interface IStaggerable
    {
        /// <summary>Cancel any in-progress attack and return to neutral. Called once when the stagger begins.</summary>
        void OnStagger(NPC npc);
    }
}
