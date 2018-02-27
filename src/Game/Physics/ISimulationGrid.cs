using linerider.Utils;

namespace linerider.Game
{
    public interface ISimulationGrid
    {
        ResourceSync Sync { get; }
        SimulationCell GetCell(int x, int y);
    }
}