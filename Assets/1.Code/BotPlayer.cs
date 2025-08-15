using UnityEngine;

using UnityEngine;

public class BotPlayer : MonoBehaviour, IPlayer
{
    [SerializeField] private HeuristicsBot bot;

    private BoardManager board;
    private int myPlayer;

    public void Configure(BoardManager board, int myPlayer)
    {
        this.board = board;
        this.myPlayer = myPlayer;
    }

    public void RequestMove(System.Action<int> onChosen)
    {
        if (!board.HasAnyLegalMove(myPlayer))
        {
            onChosen(-1);
            return;
        }

        int idx = bot.ChooseMove(board, myPlayer);
        onChosen(idx);
    }

    public void OnMatchEnd(int myScore, int oppScore, int result) { }
}

