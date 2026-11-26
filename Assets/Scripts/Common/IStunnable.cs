namespace Platformer.Common
{
    /// <summary>
    /// Interface for enemies that can be stunned by parries or heavy counterattacks.
    /// </summary>
    public interface IStunnable
    {
        bool IsStunned { get; }
        void Stun(float duration);
    }
}
