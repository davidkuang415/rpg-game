using RPG.Core.Pooling;

namespace RPG.Vfx
{
    /// <summary>Pool of XP motes. A kill can spawn a dozen at once, so they are never instantiated.</summary>
    public class XpParticlePool : ComponentPool<XpParticle>
    {
        protected override void OnInstanceCreated(XpParticle instance) => instance.BindPool(this);
    }
}
