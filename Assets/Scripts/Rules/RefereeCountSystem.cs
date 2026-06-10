using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Referee five-count for rope-assisted illegal holds (Morgana's Tarantula).
    /// The referee is a system + UI, not a physical character, in this prototype.
    public class RefereeCountSystem : MonoBehaviour
    {
        public static RefereeCountSystem Instance { get; private set; }

        public bool Counting { get; private set; }
        public int CurrentCount { get; private set; }

        Coroutine _routine;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void StartFiveCount(float totalSeconds, System.Action onComplete)
        {
            Cancel();
            _routine = StartCoroutine(CountRoutine(totalSeconds, onComplete));
        }

        IEnumerator CountRoutine(float totalSeconds, System.Action onComplete)
        {
            Counting = true;
            CurrentCount = 0;
            Debug.Log("[Referee] Five-count started");
            float interval = totalSeconds / 5f;
            for (int i = 1; i <= 5; i++)
            {
                yield return new WaitForSeconds(interval);
                CurrentCount = i;
                MatchHUD.TryShowCount(i.ToString());
            }
            Counting = false;
            MatchHUD.TryShowCount("");
            MatchHUD.TryShowMessage("Break at five!");
            Debug.Log("[Referee] Five-count release");
            onComplete?.Invoke();
            _routine = null;
        }

        public void Cancel()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            Counting = false;
            CurrentCount = 0;
        }
    }
}
