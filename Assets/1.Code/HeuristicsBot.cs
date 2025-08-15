using UnityEngine;

public enum HeuristicStrategy
{
    RandomLegal,
    GreedyFlips,
    CornerEdge
}

public class HeuristicsBot : MonoBehaviour
{
    public HeuristicStrategy strategy = HeuristicStrategy.CornerEdge;
    [SerializeField] private bool randomizeIfAllEqual = true; // random choice only if ALL legal moves tie

    public int ChooseMove(BoardManager _boardManager, int player)
    {
        var legal = _boardManager.GetLegalIndices(player);
        if (legal.Count == 0) return -1; // pass

        switch (strategy)
        {
            case HeuristicStrategy.RandomLegal:
            {
                // Pick any legal move uniformly at random.
                int pick = UnityEngine.Random.Range(0, legal.Count);
                return legal[pick];
            }

            case HeuristicStrategy.GreedyFlips:
            {
                // Always choose move(s) with the highest flip count. Randomize among ties.
                int bestFlips = int.MinValue;
                var bestMoves = new System.Collections.Generic.List<int>(8);

                for (int i = 0; i < legal.Count; i++)
                {
                    int idx = legal[i];
                    _boardManager.CheckMoveLegality(player, idx, out var flippables);
                    int flips = flippables.Count;

                    if (flips > bestFlips)
                    {
                        bestFlips = flips;
                        bestMoves.Clear();
                        bestMoves.Add(idx);
                    }
                    else if (flips == bestFlips)
                    {
                        bestMoves.Add(idx);
                    }
                }

                int pick = UnityEngine.Random.Range(0, bestMoves.Count);
                return bestMoves[pick];
            }

            case HeuristicStrategy.CornerEdge:
            {
                // Corner > Edge (+3 over flips) > Interior ;
                // If any corners exist, pick randomly among them. Otherwise score = flips + (isEdge ? 3 : 0)
                var cornerMoves = new System.Collections.Generic.List<int>(4);

                // First pass: collect corners
                for (int i = 0; i < legal.Count; i++)
                {
                    int idx = legal[i];
                    _boardManager.IndexToRowCol(idx, out int row, out int col);
                    if (_boardManager.IsCorner(row, col))
                    {
                        cornerMoves.Add(idx);
                    }
                }

                if (cornerMoves.Count > 0)
                {
                    int pick = UnityEngine.Random.Range(0, cornerMoves.Count);
                    return cornerMoves[pick];
                }

                // No corners → compute weighted score (flips + edgeBonus), randomize ties
                int bestScore = int.MinValue;
                var bestMoves = new System.Collections.Generic.List<int>(8);

                for (int i = 0; i < legal.Count; i++)
                {
                    int idx = legal[i];
                    _boardManager.CheckMoveLegality(player, idx, out var flippables);
                    int flips = flippables.Count;
                    _boardManager.IndexToRowCol(idx, out int row, out int col);
                    int edgeBonus = _boardManager.IsEdge(row, col) ? 3 : 0; // edge counts as +3 flips
                    int score = flips + edgeBonus;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMoves.Clear();
                        bestMoves.Add(idx);
                    }
                    else if (score == bestScore)
                    {
                        bestMoves.Add(idx);
                    }
                }

                int pick2 = UnityEngine.Random.Range(0, bestMoves.Count);
                return bestMoves[pick2];
            }
        }

        // Fallback (should never hit)
        return legal[0];
    }
    
}
