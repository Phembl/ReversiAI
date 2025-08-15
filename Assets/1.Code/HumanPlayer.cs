using UnityEngine;

public class HumanPlayer : MonoBehaviour, IPlayer
{
    private BoardManager board;
    private int myPlayer;
    private System.Action<int> pendingMove;

    public void Configure(BoardManager board, int myPlayer)
    {
        this.board = board;
        this.myPlayer = myPlayer;
    }

    public void RequestMove(System.Action<int> onChosen)
    {
        pendingMove = onChosen;
        /*
        // Wire clicks to this HumanPlayer
        foreach (var t in FindObjectsOfType<TileClickHandler>())
        t.Register(this);
        */
    }

    // Called by your tile click handlers:
    public void OnTileClicked(int index)
    {
        if (pendingMove != null && board.IsLegalCached(myPlayer, index))
        {
            pendingMove(index);
            pendingMove = null;
        }
    }
}