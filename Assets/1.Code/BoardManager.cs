using UnityEngine;
using System;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Color colorPlayer1;
    [SerializeField] private Color colorPlayer2;
    [SerializeField] private Color colorNeutral;
    
    private SpriteRenderer[] fieldRenderers;
    private int[] fieldEntries;
    [HideInInspector] public bool[] legalMovesP1;
    [HideInInspector] public bool[] legalMovesP2;
    public bool IsBoardReady { get; private set; }
    [HideInInspector] public bool lastMoveApplied;
    
    public event Action<int,int> MoveApplied; // (index, player)
    public int LastMoveIndex { get; private set; } = -1;
    
    void Awake()
    {
        // Allocate arrays before using them
        fieldRenderers = new SpriteRenderer[64];
        fieldEntries = new int[64];
        
        legalMovesP1 = new bool[64];
        legalMovesP2 = new bool[64];
        
        for (int i = 0; i < 64; i++)
        {
            fieldRenderers[i] = transform.GetChild(i).GetComponent<SpriteRenderer>();
        }
        IsBoardReady = false;
    }
    
    // External hook to start/reset a game
    public void PublicStartNewGame()
    {
        IsBoardReady = false;
        StopAllCoroutines();
        StartCoroutine(StartNewGameRoutine());
    }

    // Clears board data and visuals, then waits a frame so Destroy() completes,
    // then places the four starting stones and updates legal lists.
    private System.Collections.IEnumerator StartNewGameRoutine()
    {
        IsBoardReady = false;
        ClearAllPieces();
        // Wait one frame so all Destroy() calls are processed before we place starters
        yield return null;

        ClearLastMoveIndex();
        // Place Starter Pieces (D4/E5 are white(2), E4/D5 are black(1) in 1-based; indices here are 0-based)
        PlacePiece(1, 27);
        PlacePiece(2, 28);
        PlacePiece(2, 35);
        PlacePiece(1, 36);

        UpdateLegalMoveList(1);
        UpdateLegalMoveList(2);
        IsBoardReady = true;
    }

    // Remove all piece GameObjects from every field and reset state to empty
    private void ClearAllPieces()
    {
        for (int i = 0; i < 64; i++)
        {
            fieldEntries[i] = 0;
            
            // Color all fields neutral
            fieldRenderers[i].color = colorNeutral;
            
        }
    }

    public void MakeMove(int player, int fieldIndex)
    {
        ApplyMoveAndFlip(player, fieldIndex);
        UpdateLegalMoveList(1);
        UpdateLegalMoveList(2);
        lastMoveApplied = true;
        
        LastMoveIndex = fieldIndex;
        lastMoveApplied = true;               // you already set this – keep it
        MoveApplied?.Invoke(fieldIndex, player);   // notify listeners
    }
    
    void PlacePiece(int player, int fieldIndex)
    {
        
        if (player == 1)
        {
            fieldEntries[fieldIndex] = 1;
            fieldRenderers[fieldIndex].color = colorPlayer1;
        }
        else
        {
            fieldEntries[fieldIndex] = 2;
            fieldRenderers[fieldIndex].color = colorPlayer2;
        }
    }
    
        // ===== Reversi legality helpers =====
    private static readonly (int rowStep, int colStep)[] EightDirections = new (int, int)[]
    {
        (-1, 0),  // Up
        (1, 0),   // Down
        (0, -1),  // Left
        (0, 1),   // Right
        (-1, -1), // Up-Left
        (-1, 1),  // Up-Right
        (1, -1),  // Down-Left
        (1, 1)    // Down-Right
    };

    private static int GetRow(int index) => index / 8;
    private static int GetColumn(int index) => index % 8;
    private static bool IsOnBoard(int rowIndex, int columnIndex) => rowIndex >= 0 && rowIndex < 8 && columnIndex >= 0 && columnIndex < 8;
    private static int ToIndex(int rowIndex, int columnIndex) => rowIndex * 8 + columnIndex;
    
    public bool CheckMoveLegality(int currentPlayer, int targetIndex, out System.Collections.Generic.List<int> flippableIndices)
    {
        flippableIndices = new System.Collections.Generic.List<int>(16);

        // Rule 0: The target cell must be empty.
        if (targetIndex < 0 || targetIndex >= 64) return false;
        if (fieldEntries[targetIndex] != 0) return false;

        int opponentPlayer = (currentPlayer == 1) ? 2 : 1;
        int targetRow = GetRow(targetIndex);
        int targetColumn = GetColumn(targetIndex);

        // Quick pre-check (Condition 1): at least one adjacent opponent piece in the 8-neighborhood.
        bool hasAdjacentOpponent = false;
        foreach (var (rowStep, colStep) in EightDirections)
        {
            int neighborRow = targetRow + rowStep;
            int neighborColumn = targetColumn + colStep;
            if (!IsOnBoard(neighborRow, neighborColumn)) continue;
            int neighborIndex = ToIndex(neighborRow, neighborColumn);
            if (fieldEntries[neighborIndex] == opponentPlayer)
            {
                hasAdjacentOpponent = true;
                break;
            }
        }
        if (!hasAdjacentOpponent) return false; // fails Condition 1

        // Condition 2: in at least one direction, we must have a contiguous run of opponent stones
        // followed by a friendly stone. Collect all stones that would flip.
        foreach (var (rowStep, colStep) in EightDirections)
        {
            int probeRow = targetRow + rowStep;
            int probeColumn = targetColumn + colStep;

            // First step must be an opponent piece, otherwise this direction cannot flip anything.
            if (!IsOnBoard(probeRow, probeColumn)) continue;
            int probeIndex = ToIndex(probeRow, probeColumn);
            if (fieldEntries[probeIndex] != opponentPlayer) continue;

            // Move along this direction and gather opponent pieces until we hit our own piece or an empty/edge.
            var lineToFlip = new System.Collections.Generic.List<int>(8);
            while (IsOnBoard(probeRow, probeColumn))
            {
                probeIndex = ToIndex(probeRow, probeColumn);
                int cellValue = fieldEntries[probeIndex];
                if (cellValue == opponentPlayer)
                {
                    lineToFlip.Add(probeIndex);
                    probeRow += rowStep;
                    probeColumn += colStep;
                    continue;
                }
                else if (cellValue == currentPlayer)
                {
                    // Found a friendly piece after at least one opponent piece → valid flip in this direction
                    if (lineToFlip.Count > 0)
                    {
                        flippableIndices.AddRange(lineToFlip);
                    }
                    break;
                }
                else // Empty cell → cannot flip in this direction
                {
                    break;
                }
            }
        }

        // Legal if we can flip at least one piece in any direction
        return flippableIndices.Count > 0;
    }
    
    public int ApplyMoveAndFlip(int currentPlayer, int targetIndex)
    {
        if (!CheckMoveLegality(currentPlayer, targetIndex, out var toFlip))
            return 0;

        // Place the new piece
        PlacePiece(currentPlayer, targetIndex);

        // Flip captured lines
        for (int i = 0; i < toFlip.Count; i++)
        {
            PlacePiece(currentPlayer, toFlip[i]);
        }

        return toFlip.Count;
    }

    // Recomputes and overwrites the per-player legal array from scratch.
    public void UpdateLegalMoveList(int player)
    {
        bool[] target = (player == 1) ? legalMovesP1 : legalMovesP2;

        // Safety: clear first
        for (int i = 0; i < 64; i++) target[i] = false;

        // Fill legal flags
        for (int i = 0; i < 64; i++)
        {
            // Only check empties (optional optimization)
            if (fieldEntries[i] != 0) continue;

            if (CheckMoveLegality(player, i, out _))
                target[i] = true;
        }
    }
    
    public bool IsLegalCached(int player, int index)
    {
        if (index < 0 || index >= 64) return false;
        return (player == 1 ? legalMovesP1 : legalMovesP2)[index];
    }
    
    
    public void IndexToRowCol(int index, out int row, out int col)
    {
        row = GetRow(index);
        col = GetColumn(index);
    }

    public bool IsCorner(int row, int col) => (row == 0 || row == 7) && (col == 0 || col == 7);
    public bool IsEdge(int row, int col) => (row == 0 || row == 7 || col == 0 || col == 7) && !IsCorner(row, col);

    public System.Collections.Generic.List<int> GetLegalIndices(int player)
    {
        var list = new System.Collections.Generic.List<int>(16);
        var src = (player == 1) ? legalMovesP1 : legalMovesP2;
        for (int i = 0; i < 64; i++) if (src[i]) list.Add(i);
        return list;
    }
    
    
    // Checks if the given player has at least one legal move available
    public bool HasAnyLegalMove(int player)
    {
        // Get the correct legal moves array
        bool[] legalMoves = (player == 1) ? legalMovesP1 : legalMovesP2;

        for (int i = 0; i < legalMoves.Length; i++)
        {
            if (legalMoves[i]) return true;
        }
        return false;
    }

// Counts pieces for both players and returns as a tuple
    public (int p1, int p2) CountPieces()
    {
        int p1Count = 0;
        int p2Count = 0;
        for (int i = 0; i < fieldEntries.Length; i++)
        {
            if (fieldEntries[i] == 1) p1Count++;
            else if (fieldEntries[i] == 2) p2Count++;
        }
        return (p1Count, p2Count);
    }
    // Added public getter for fieldEntries values
    public int GetFieldValue(int index) => (index >= 0 && index < fieldEntries.Length) ? fieldEntries[index] : -1;
    
    public void ClearLastMoveIndex() { LastMoveIndex = -1; }

}
