using UnityEngine;

public interface IPlayer
{
    /// <summary>
    /// Called once at match start to set up the player.
    /// </summary>
    void Configure(BoardManager board, int myPlayer);

    /// <summary>
    /// Called when it's this player's turn. Pass -1 if no legal moves.
    /// </summary>
    void RequestMove(System.Action<int> onChosen);

    /// <summary>
    /// Optional: called at the end of the match.
    /// </summary>
    void OnMatchEnd(int myScore, int oppScore, int result) { }
}
