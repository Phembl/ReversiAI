using UnityEngine;
using System.Collections;

public class GameDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager board;
    [SerializeField] private HeuristicsBot player1Bot; 
    [SerializeField] private HeuristicsBot player2Bot; 

    [Header("Flow")]
    [SerializeField] private float moveDelaySeconds = 0.05f; // small delay to watch play
    [SerializeField] private bool autoStartOnPlay = true;

    private int currentPlayer = 1; // 1 = black, 2 = white
    private bool isRunning;

   
    private void Start()
    {
        if (autoStartOnPlay) StartMatch();
    }

    public void StartMatch()
    {
        if (board == null)
        {
            Debug.LogError("GameDirector: No BoardManager assigned.");
            return;
        }

        board.PublicStartNewGame();
        currentPlayer = 1;
        isRunning = true;
        StopAllCoroutines();
        StartCoroutine(BotVsBotLoop());
    }

    public void StopMatch()
    {
        isRunning = false;
        StopAllCoroutines();
    }

    private IEnumerator BotVsBotLoop()
    {
        while (isRunning)
        {
            // Terminal: both players have no legal moves
            bool p1Has = board.HasAnyLegalMove(1);
            bool p2Has = board.HasAnyLegalMove(2);
            if (!p1Has && !p2Has)
            {
                var (p1, p2) = board.CountPieces();
                string result = p1 > p2 ? "Yellow (P1) wins" : (p1 < p2 ? "Black (P2) wins" : "Draw");
                Debug.Log($"Game over. P1={p1} P2={p2}. {result}.");
                PrintBoardState();
                isRunning = false;
                yield break;
            }

            // Pass if current player cannot move
            if (!board.HasAnyLegalMove(currentPlayer))
            {
                currentPlayer = 3 - currentPlayer;
                continue;
            }

            // Choose bot for current player
            HeuristicsBot bot = (currentPlayer == 1) ? player1Bot : player2Bot;
            if (bot == null)
            {
                Debug.LogWarning($"GameDirector: No bot assigned for player {currentPlayer}. Passing turn.");
                currentPlayer = 3 - currentPlayer;
                continue;
            }

            int chosen = bot.ChooseMove(board, currentPlayer);
            if (chosen >= 0)
            {
                board.MakeMove(currentPlayer, chosen);
            }

            currentPlayer = 3 - currentPlayer;

            if (moveDelaySeconds > 0f) yield return new WaitForSeconds(moveDelaySeconds);
            else yield return null;
        }
    }
    
    
    private void PrintBoardState()
    {
        string boardStr = "";
        for (int i = 0; i < 64; i++)
        {
            int val = board.GetFieldValue(i);
            boardStr += val.ToString() + " ";
            
        }
        
        Debug.Log(boardStr);
    }
}