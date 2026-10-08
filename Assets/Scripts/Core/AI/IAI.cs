


namespace YuJanggi.Core.AI
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    public interface IAI
    {
        bool TrySelectMove(
            IAIPosition position,
            PlayerTeam team,
            out AIMove move);
    }
}
