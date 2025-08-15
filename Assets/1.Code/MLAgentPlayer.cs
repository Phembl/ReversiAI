using UnityEngine;

public class MLAgentsPlayer : MonoBehaviour, IPlayer
{
    [SerializeField] private ReversiAgent agent;

    private BoardManager board;
    private int myPlayer;
    private System.Action<int> pendingMove;

    public void Configure(BoardManager board, int myPlayer)
    {
        this.board = board;
        this.myPlayer = myPlayer;
        agent.SetPlayerNumber(myPlayer); // Add helper in ReversiAgent if needed
        board.MoveApplied += OnBoardMoveApplied; // subscribe
#if UNITY_EDITOR
        //Debug.Log($"[MLAgentsPlayer] Subscribed. Player={myPlayer}, AgentGO={agent?.name}");
#endif
    }
    
    private void OnDestroy()
    {
        if (board != null) board.MoveApplied -= OnBoardMoveApplied;
    }
    
    private void OnBoardMoveApplied(int index, int player)
    {
        if (pendingMove != null && player == myPlayer)
        {
#if UNITY_EDITOR
//            Debug.Log($"[MLAgentsPlayer] MoveApplied callback. Player={player} Index={index}");
#endif
            pendingMove(index);
            pendingMove = null;
        }
    }

    public void RequestMove(System.Action<int> onChosen)
    {
        pendingMove = onChosen;
        agent.RequestDecision(); // Inference mode
    }

    // Hook this from BoardManager when a move is applied:
    public void OnMoveApplied(int index, int player)
    {
        if (pendingMove != null && player == myPlayer)
        {
            pendingMove(index);
            pendingMove = null;
        }
    }
}