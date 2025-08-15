using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class ReversiAgent : Agent
{
    [Header("Refs")]
    public BoardManager board;
    [Tooltip("1 = Black, 2 = White")]
    public int myPlayer = 1;

    [SerializeField] private bool logDebug = false;

#if UNITY_EDITOR
    private int _maskDebugCount = 0;
    private int _actionDebugCount = 0;
#endif


    public override void Initialize()
    {
        if (board == null)
        {
            Debug.LogError($"{nameof(ReversiAgent)} on {name}: Board reference not assigned. Disabling agent.");
            enabled = false;
            return;
        }
        if (myPlayer != 1 && myPlayer != 2)
        {
            Debug.LogError($"{nameof(ReversiAgent)} on {name}: myPlayer must be 1 or 2, got {myPlayer}. Disabling agent.");
            enabled = false;
            return;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // If misconfigured, output zeros to keep obs size consistent
        if (board == null)
        {
            for (int i = 0; i < 192; i++) sensor.AddObservation(0f);
            return;
        }

        int opp = 3 - myPlayer;

        // Your stones
        for (int i = 0; i < 64; i++)
            sensor.AddObservation(board.GetFieldValue(i) == myPlayer ? 1f : 0f);

        // Opponent stones
        
        for (int i = 0; i < 64; i++)
            sensor.AddObservation(board.GetFieldValue(i) == opp ? 1f : 0f);

        // Legal-moves plane (cached by BoardManager; TrainingDirector updates each turn)
        for (int i = 0; i < 64; i++)
            sensor.AddObservation(board.IsLegalCached(myPlayer, i) ? 1f : 0f);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // Expect 65 actions: 0..63 = board indices, 64 = PASS
        // Fail-safe if agent not fully configured: allow only PASS
        if (board == null || (myPlayer != 1 && myPlayer != 2))
        {
            for (int i = 0; i < 64; i++) actionMask.SetActionEnabled(0, i, false);
            actionMask.SetActionEnabled(0, 64, true);
            return;
        }

        // Check if the current player has any legal moves
        bool hasAny = board.HasAnyLegalMove(myPlayer);

        if (!hasAny)
        {
            // No legal moves -> only PASS is allowed
            for (int i = 0; i < 64; i++) actionMask.SetActionEnabled(0, i, false);
            actionMask.SetActionEnabled(0, 64, true);
            return;
        }

        // Legal moves exist -> enable only legal indices and disable PASS
        for (int i = 0; i < 64; i++)
        {
            bool legal = board.IsLegalCached(myPlayer, i);
            actionMask.SetActionEnabled(0, i, legal);
        }
        actionMask.SetActionEnabled(0, 64, false);

#if UNITY_EDITOR
        if (logDebug && _maskDebugCount < 5)
        {
            _maskDebugCount++;
            bool hasAnyDbg = board != null && (myPlayer == 1 || myPlayer == 2) && board.HasAnyLegalMove(myPlayer);
            Debug.Log($"[Mask] Agent={name} Player={myPlayer} HasAny={hasAnyDbg} Scene={gameObject.scene.name}");
        }
#endif
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (board == null) return;

        int a = actions.DiscreteActions[0];
#if UNITY_EDITOR
        if (logDebug && _actionDebugCount < 5)
        {
            _actionDebugCount++;
            bool legalDbg = (a == 64) ? !board.HasAnyLegalMove(myPlayer) : (a >= 0 && a < 64 && board.IsLegalCached(myPlayer, a));
            Debug.Log($"[Action] Agent={name} Player={myPlayer} Chosen={a} LegalNow={legalDbg}");
        }
#endif
        if (a < 0 || a > 64) return; // out of bounds safety

        // Pass
        if (a == 64)
        {
            // Only valid if truly no legal moves; ignore otherwise
            if (board.HasAnyLegalMove(myPlayer)) return;
            return; // Director will handle turn swap
        }

        // Place piece if legal (cached)
        if (board.IsLegalCached(myPlayer, a))
        {
            board.MakeMove(myPlayer, a);
        }
        // else: masked should have prevented this; ignore silently
    }
    

    // Called by your director at end of game with final reward
    public void EndWithResult(float reward)
    {
        AddReward(reward);
        EndEpisode();
    }
    
    public void SetPlayerNumber(int p) { myPlayer = p; }
}