using UnityEngine;

using System.Collections;

public class MatchDirector : MonoBehaviour
{
    [SerializeField] private BoardManager board;
    [SerializeField] private MonoBehaviour player1Behaviour; // must implement IPlayer
    [SerializeField] private MonoBehaviour player2Behaviour;
    [SerializeField] private float moveDelaySeconds = 0.2f;
    [SerializeField] private float moveTimeout = 2f; // Max time to wait for a move to be applied
    [SerializeField] private bool autoStart = true;     // Start a match automatically on scene start
    [SerializeField] private bool autoRestart = false;  // Automatically start next match when one ends
    [SerializeField] private int logEveryNGames = 10;   // Print running stats every N matches

    private IPlayer player1;
    private IPlayer player2;
    private int currentPlayer; // 1 or 2
    private bool matchRunning;
    private int gamesPlayed;
    private int p1Wins;
    private int p2Wins;
    private int draws;

    private void Start()
    {
        if (autoStart)
        {
            StartMatch();
        }
    }

    public void StartMatch()
    {
        StopAllCoroutines();

        player1 = player1Behaviour as IPlayer;
        player2 = player2Behaviour as IPlayer;

        if (player1 == null || player2 == null)
        {
            Debug.LogError("Players must implement IPlayer.");
            return;
        }

        board.PublicStartNewGame();
        StartCoroutine(WaitForBoardReadyAndRun());
    }

    private IEnumerator WaitForBoardReadyAndRun()
    {
        while (!board.IsBoardReady) yield return null;

        player1.Configure(board, 1);
        player2.Configure(board, 2);

        currentPlayer = 1;
        matchRunning = true;

        yield return StartCoroutine(MatchLoop());
    }

    private IEnumerator MatchLoop()
    {
        while (matchRunning)
        {
            board.UpdateLegalMoveList(1);
            board.UpdateLegalMoveList(2);

            bool p1HasMove = board.HasAnyLegalMove(1);
            bool p2HasMove = board.HasAnyLegalMove(2);

            if (!p1HasMove && !p2HasMove)
            {
                EndMatch();
                yield break;
            }

            if (!board.HasAnyLegalMove(currentPlayer))
            {
                yield return new WaitForSeconds(moveDelaySeconds);
                currentPlayer = 3 - currentPlayer;
                continue;
            }

            bool moveDone = false;
            board.lastMoveApplied = false;
            int waitedPlayer = currentPlayer; // capture player for the wait check

            IPlayer current = (waitedPlayer == 1) ? player1 : player2;
            current.RequestMove(idx =>
            {
                bool alreadyApplied = board.lastMoveApplied && board.LastMoveIndex == idx;

                if (idx == -1)
                {
                    if (!board.HasAnyLegalMove(waitedPlayer))
                    {
                        // Valid pass; no board change expected
                    }
                    else
                    {
                        Debug.LogWarning($"Player {waitedPlayer} attempted to pass despite having legal moves.");
                    }
                }
                else if (alreadyApplied)
                {
                    // Move was already applied by the player (e.g., MLAgentsPlayer via Agent->BoardManager)
                }
                else if (idx >= 0 && board.IsLegalCached(waitedPlayer, idx))
                {
                    board.MakeMove(waitedPlayer, idx);
                }
                else
                {
                    bool hadLegalNow = board.HasAnyLegalMove(waitedPlayer);
                    Debug.LogWarning($"[MatchDirector] Illegal move from {current.GetType().Name} (P{waitedPlayer}) idx={idx} | hadLegal={hadLegalNow}");
                }
                moveDone = true;
            });

            // Wait until the move callback fired AND (if a legal move existed) the board applied it
            float t = 0f;
            bool hadLegal = board.HasAnyLegalMove(waitedPlayer);
            while (((!moveDone) || (hadLegal && !board.lastMoveApplied)) && t < moveTimeout)
            {
                yield return null;
                t += Time.deltaTime;
            }

            if (hadLegal && !board.lastMoveApplied)
            {
                Debug.LogWarning($"No move applied for player {waitedPlayer} within timeout {moveTimeout}s. Proceeding to next turn.");
            }

            yield return new WaitForSeconds(moveDelaySeconds);

            currentPlayer = 3 - currentPlayer;
        }
    }

    private void EndMatch()
    {
        matchRunning = false;
        var (p1Score, p2Score) = board.CountPieces();
        int resultP1 = p1Score > p2Score ? 1 : p1Score == p2Score ? 0 : -1;
        int resultP2 = -resultP1;

        player1.OnMatchEnd(p1Score, p2Score, resultP1);
        player2.OnMatchEnd(p2Score, p1Score, resultP2);

        // Update running stats
        gamesPlayed++;
        if (resultP1 > 0) p1Wins++;
        else if (resultP1 < 0) p2Wins++;
        else draws++;

        string winner = resultP1 > 0 ? "Player 1" : resultP1 < 0 ? "Player 2" : "Draw";
        Debug.Log($"Match Ended. P1: {p1Score}, P2: {p2Score} | Winner: {winner} | Totals => Games: {gamesPlayed}, P1: {p1Wins}, P2: {p2Wins}, Draws: {draws}");

        if (logEveryNGames > 0 && gamesPlayed % logEveryNGames == 0)
        {
            float winRateP1 = gamesPlayed > 0 ? (float)p1Wins / gamesPlayed : 0f;
            float winRateP2 = gamesPlayed > 0 ? (float)p2Wins / gamesPlayed : 0f;
            Debug.Log($"[Stats] After {gamesPlayed} games | P1 W%: {winRateP1:P1} | P2 W%: {winRateP2:P1} | Draws: {draws}");
        }

        if (autoRestart)
        {
            StartCoroutine(RestartNextMatch());
        }
    }

    private IEnumerator RestartNextMatch()
    {
        // Small delay so logs/UI can settle
        yield return new WaitForSeconds(0.1f);
        StartMatch();
    }

    public (int games, int p1, int p2, int d) GetStats() => (gamesPlayed, p1Wins, p2Wins, draws);
    public bool IsMatchRunning => matchRunning;
}
