using UnityEngine;
using System.Collections;

public class TrainingDirector : MonoBehaviour
{
    [Header("Refs")]
    public BoardManager board;
    public ReversiAgent yellowAgent; // myPlayer=1
    public ReversiAgent blackAgent; // myPlayer=2

    [Header("Flow")]
    public bool autoStartOnPlay = true;
    public float decisionDelay = 0f; // 0 for max speed
    public float moveTimeout = 1f; // Max time to wait for an agent to apply a move

    int currentPlayer = 1;
    bool running;
    
    private int turnCount;

    void Awake()
    {
        if (board == null || yellowAgent == null || blackAgent == null)
        {
            Debug.LogError("TrainingDirector: Missing references. Please assign BoardManager and both agents in the Inspector. Disabling.");
            enabled = false;
            return;
        }

        // Ensure agents are configured correctly
        if (yellowAgent.myPlayer != 1) yellowAgent.myPlayer = 1;
        if (blackAgent.myPlayer != 2) blackAgent.myPlayer = 2;

        if (yellowAgent.board != board || blackAgent.board != board)
        {
            Debug.LogError("TrainingDirector: Agents must reference the SAME BoardManager as the director. Disabling.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (autoStartOnPlay) StartEpisode();
    }

    public void StartEpisode()
    {
        board.PublicStartNewGame();
        // Wait until board has placed starters and updated legal lists
        StartCoroutine(WaitForBoardReadyAndRun());
        return; // prevent starting SelfPlayLoop immediately
    }

    private IEnumerator WaitForBoardReadyAndRun()
    {
        while (!board.IsBoardReady)
            yield return null;
        currentPlayer = 1;
        turnCount = 0;
        running = true;
        StopAllCoroutines();
        StartCoroutine(SelfPlayLoop());
    }

    IEnumerator SelfPlayLoop()
    {
        while (running)
        {
            int safetySteps = 0; 
            const int MAX_STEPS = 200;

            while (true)
            {
                // Terminal check (force fresh legal lists first)
                board.UpdateLegalMoveList(1);
                board.UpdateLegalMoveList(2);
                bool p1Has = board.HasAnyLegalMove(1);
                bool p2Has = board.HasAnyLegalMove(2);

                if (!p1Has && !p2Has)
                {
                    var (p1, p2) = board.CountPieces();
                    float rP1 = p1 > p2 ? +1f : (p1 < p2 ? -1f : 0f);
                    float rP2 = -rP1;
                    yellowAgent?.EndWithResult(rP1);  // player 1 (yellow)
                    blackAgent?.EndWithResult(rP2);   // player 2 (black)
                    running = false;
                    break;
                }

                // Pass if current player cannot move
                if (!board.HasAnyLegalMove(currentPlayer))
                {
                    currentPlayer = 3 - currentPlayer;
                    continue;
                }
                

                // Handshake: reset move-applied flag before giving control to the agent
                board.lastMoveApplied = false;

                // Request decision from the correct agent
                if (currentPlayer == 1) yellowAgent?.RequestDecision();
                else blackAgent?.RequestDecision();

                // Wait until the agent actually makes a legal move (BoardManager.MakeMove sets LastMoveApplied = true)
                float waitT = 0f;
                while (!board.lastMoveApplied && waitT < moveTimeout)
                {
                    yield return null;
                    waitT += Time.deltaTime;
                }

                // Swap turn only after the move has been applied (or timeout hit)
                currentPlayer = 3 - currentPlayer;

                // Debug / safety: count turns and bail out if something goes wrong
                turnCount++;
                safetySteps++;
                if (safetySteps > MAX_STEPS)
                {
                    Debug.LogError("TrainingDirector: Episode exceeded max steps; forcing end to avoid a stall.");
                    var (p1s, p2s) = board.CountPieces();
                    float rP1s = p1s > p2s ? +1f : (p1s < p2s ? -1f : 0f);
                    float rP2s = -rP1s;
                    yellowAgent?.EndWithResult(rP1s);
                    blackAgent?.EndWithResult(rP2s);
                    running = false;
                    break;
                }

                if (decisionDelay > 0f) yield return new WaitForSeconds(decisionDelay);
                else yield return null;
            }

            if (decisionDelay > 0f)
                yield return new WaitForSeconds(0.5f);
            else
                yield return null;

            board.PublicStartNewGame();
            // Wait until board is ready before starting next episode
            yield return StartCoroutine(WaitForBoardReadyAndRun());
        }
    }
}