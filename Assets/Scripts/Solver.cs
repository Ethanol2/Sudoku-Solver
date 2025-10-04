using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EditorTools;
using UnityEngine;
using UnityEngine.Events;

public class Solver : MonoBehaviour
{
    // Static
    public static event System.Action OnBoardSolved;

    // Inspector
    [SerializeField] private Board _board;
    [SerializeField] private int _stepLimit = 200000;
    [SerializeField] private float _stepPauseTime = 0.1f;
    [SerializeField] private float _generationTimeoutTime = 1f;

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;
    [SerializeField] private bool _stepThrough = false;
    [SerializeField] private bool _slowMode = false;

    [Space]
    [SerializeField] private bool _working = false;
    [SerializeField] private bool _abort = false;
    [SerializeField] private int _steps;

    public bool SlowMode { get => _slowMode; set => _slowMode = value; }
    public bool Working { get => _working; }

    public event System.Action OnSolverStart;
    public event System.Action OnSolverFinished;
    public UnityEvent OnSolverStart_UE;
    public UnityEvent OnSolverFinished_UE;

    private TaskCompletionSource<bool> _continueSolvingFlag;
    private ConcurrentQueue<System.Action> _asyncActions = new ConcurrentQueue<System.Action>();

#if UNITY_EDITOR
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) && _board != null)
        {
            if (_working)
            {
                StopAllCoroutines();
                _working = false;
            }
            else
                StartCoroutine(SolveBoardRoutine(_board, Input.GetKey(KeyCode.LeftShift)));
        }
        if (Input.GetKeyDown(KeyCode.G) && _board != null)
        {
            if (_working)
            {
                StopAllCoroutines();
                _working = false;
            }
            else
                StartCoroutine(GenerateBoardRoutine(_board, _generationTimeoutTime, Input.GetKey(KeyCode.LeftShift)));
        }
    }
#endif
    void OnDisable()
    {
        _abort = true;
    }
    void OnDestroy()
    {
        _abort = true;
    }
    void OnApplicationQuit()
    {
        _abort = true;
    }

    public void OnBoardCreated(Board board)
    {
        _board = board;
    }
    public void OnBoardDestroyed()
    {
        StopAllCoroutines();
        _working = false;
    }

    public void SolveBoard()
    {
        _abort = true;
        StartCoroutine(SolveBoardRoutine(_board, _slowMode));
    }
    public void GenerateBoard()
    {
        _abort = true;
        StartCoroutine(GenerateBoardRoutine(_board, _generationTimeoutTime, _slowMode));
    }
    public void Abort() { if (_working) _abort = true; }

    private IEnumerator SolveBoardRoutine(Board board, bool slow)
    {
        if (_working) yield break;
        if (!board) yield break;
        if (!Modal.Instance)
        {
            this.LogError("The solver uses the modal system to display messages. Please ensure there is a Modal component in the scene.");
            yield break;
        }

        int areYouSureResponse = 0;
        Modal.ShowModal(new Modal.ModalData()
        {
            Title = "Are you sure?",
            Body = "Solving a board will try to fill in all empty squares. This cannot be undone. Are you sure you want to continue?",

            ShowConfirmButton = true,
            ConfirmButtonText = "Yes",
            ConfirmButtonEvent = () => areYouSureResponse = 1,

            ShowCancelButton = true,
            CancelButtonText = "No",
            CancelButtonEvent = () => areYouSureResponse = 2
        });

        yield return new WaitWhile(() => areYouSureResponse == 0);

        if (areYouSureResponse == 2)
            yield break;

        _working = true;
        _steps = 0;
        _abort = false;

        this.Log("Starting Solver. Slow Mode: " + _slowMode);

        OnSolverStart?.Invoke();
        OnSolverStart_UE.Invoke();

        if (slow)
            yield return SafeRun(SolveRecursiveSlow(board, _stepPauseTime));
        else
        {
#if false
//#if UNITY_WEBGL
            yield return SafeRun(SolveRecursiveSlow(board, 0f));
#else
            DataOnlyBoard dBoard = board;

            _asyncActions.Clear();

            Task task = Task.Run(() => SolveRecursive(dBoard));

            while (!task.IsCompleted)
            {
                while (_asyncActions.TryDequeue(out var action))
                {
                    action();
                }
                yield return null;
            }

            if (task.Exception != null)
            {
                Modal.ShowModal(new Modal.ModalData()
                {
                    Title = "Something went Wrong",
                    Body = "The solver encountered an error",
                    ShowConfirmButton = true,
                    TimeoutTime = 30f
                });
                throw task.Exception;
            }

            board.SetState(dBoard);
#endif
        }

        if (board.ValidateSolved())
        {
            OnBoardSolved?.Invoke();
            this.Log("Board Solved: True");
        }
        else
        {
            this.Log("Board Solved: False");
        }

        _working = false;

        OnSolverFinished?.Invoke();
        OnSolverFinished_UE.Invoke();
    }
    private IEnumerator GenerateBoardRoutine(Board board, float timeOutTime, bool slow)
    {
        if (_working) yield break;
        if (!board) yield break;
        if (!Modal.Instance)
        {
            this.LogError("The solver uses the modal system to display messages. Please ensure there is a Modal component in the scene.");
            yield break;
        }

        _working = true;
        _steps = 0;
        _abort = false;

        this.Log("Starting Generator. Slow Mode: " + _slowMode);

        OnSolverStart?.Invoke();
        OnSolverStart_UE.Invoke();

        board.SetState(IBoard.State.GenerateEmpty(board.BoardSize));
        board.AllSquares[Random.Range(0, board.AllSquares.Length)].Number = Random.Range(1, board.BoardSize + 1);

        if (slow)
        {
            yield return SafeRun(SolveRecursiveSlow(board, _stepPauseTime));
        }
        else
        {

            DataOnlyBoard dBoard = board;

            _asyncActions.Clear();

            Task task = Task.Run(() => SolveRecursive(dBoard));

            while (!task.IsCompleted)
            {
                while (_asyncActions.TryDequeue(out var action))
                {
                    action();
                }
                yield return null;
            }

            if (task.Exception != null)
            {
                Modal.ShowModal(new Modal.ModalData()
                {
                    Title = "Something went Wrong",
                    Body = "The solver encountered an error",
                    ShowConfirmButton = true,
                    TimeoutTime = 30f
                });
                throw task.Exception;
            }

            board.SetState(dBoard);
        }

        if (board.ValidateSolved())
        {
            OnBoardSolved?.Invoke();
            this.Log("Board Generated: True");
        }
        else
        {
            this.Log("Board Generated: False");
        }

        _working = false;

        OnSolverFinished?.Invoke();
        OnSolverFinished_UE.Invoke();
    }

    private IEnumerator SolveRecursiveSlow(IBoard board, float waitTime, int recursionDepth = 0)
    {
        if (recursionDepth >= 1020)
        {
            this.Log("Something went wrong: Max recursion reached");
            Modal.ShowModal(new Modal.ModalData()
            {
                Title = "Something went Wrong",
                Body = "The solver hit the maximum recursion depth. That shouldn't happen.",
                ShowConfirmButton = true,
                TimeoutTime = 30f
            });
            _abort = true;
            yield break;
        }

        do
        {
            int goodSquareCount = 0;

            foreach (ISquare square in board.AllSquares)
                square.SetNotes();

            yield return IncrementSteps();
            if (_abort)
                yield break;

            foreach (ISquare square in board.AllSquares)
            {
                if (square.Number == 0)
                {
                    square.CheckForUniqueNotes(true);

                    if (square.Notes.Count == 0)
                    {
                        yield break;
                    }
                    else if (square.Notes.Count == 1)
                    {
                        square.Number = square.Notes.GetSmallestNote();
                        goodSquareCount++;
                    }
                }
            }

            yield return StepThrough(waitTime);
            yield return IncrementSteps();
            if (_abort)
                yield break;

            if (goodSquareCount == 0)
            {
                Verbose("No good squares", recursionDepth);

                IBoard.State state = board.GetState();

                List<(int, int, int)> bestSquares = new List<(int, int, int)>();

                for (int i = 0; i < board.AllSquares.Length; i++)
                {
                    if (board.AllSquares[i].Number == 0)
                    {
                        int[] notes = board.AllSquares[i].Notes.GetActiveNotes();
                        for (int n = 0; n < board.AllSquares[i].Notes.Count; n++)
                        {
                            board.AllSquares[i].Number = notes[n];

                            int score = GetSquareScore(board.AllSquares[i], board.BoardSize) + notes.Length;

                            bestSquares.Add((i, score, notes[n]));

                            board.AllSquares[i].Number = 0;
                        }
                    }
                }

                bestSquares.Sort((x, y) => x.Item2 < y.Item2 ? -1 : 1);

                yield return IncrementSteps();
                if (_abort)
                    yield break;

                foreach (var indexScore in bestSquares)
                {
                    board.AllSquares[indexScore.Item1].Number = indexScore.Item3;

                    Verbose("Setting square " + board.AllSquares[indexScore.Item1].Name + " to " + board.AllSquares[indexScore.Item1].Number, recursionDepth);

                    yield return SolveRecursiveSlow(board, recursionDepth + 1);

                    if (_abort || board.ValidateSolved())
                        yield break;

                    board.SetState(state);
                }

                yield break;
            }

            yield return IncrementSteps();
        }
        while (!board.ValidateSolved() && !_abort);
    }
    private async Task SolveRecursive(DataOnlyBoard board, int recursionDepth = 0)
    {
        if (recursionDepth >= 1020)
        {
            this.Log("Something went wrong: Max recursion reached");
            _abort = true;
            return;
        }

        do
        {
            int goodSquareCount = 0;

            foreach (ISquare square in board.AllSquares)
                square.SetNotes();

            await IncrementStepsAsync();
            if (_abort)
                return;

            foreach (ISquare square in board.AllSquares)
            {
                if (square.Number == 0)
                {
                    square.CheckForUniqueNotes(true);

                    if (square.Notes.Count == 0)
                    {
                        return;
                    }
                    else if (square.Notes.Count == 1)
                    {
                        square.Number = square.Notes.GetSmallestNote();
                        goodSquareCount++;
                    }
                }
            }

            await IncrementStepsAsync();
            if (_abort)
                return;

            if (goodSquareCount == 0)
            {
                Verbose("No good squares", recursionDepth);

                IBoard.State state = board.GetState();

                List<(int, int, int)> bestSquares = new List<(int, int, int)>();

                for (int i = 0; i < board.AllSquares.Length; i++)
                {
                    if (board.AllSquares[i].Number == 0)
                    {
                        int[] notes = board.AllSquares[i].Notes.GetActiveNotes();
                        for (int n = 0; n < board.AllSquares[i].Notes.Count; n++)
                        {
                            board.AllSquares[i].Number = notes[n];

                            int score = GetSquareScore(board.AllSquares[i], board.BoardSize) + notes.Length;

                            bestSquares.Add((i, score, notes[n]));

                            board.AllSquares[i].Number = 0;
                        }
                    }
                }

                // Correct Comparison: x.Item2 < y.Item2 ? -1 : 1
                // If the comparer is flipped it's because I was doing tests on the solver and forgot to swith it back
                bestSquares.Sort((x, y) => x.Item2 < y.Item2 ? -1 : 1);

                await IncrementStepsAsync();
                if (_abort)
                    return;

                foreach (var indexScore in bestSquares)
                {
                    board.AllSquares[indexScore.Item1].Number = indexScore.Item3;

                    Verbose("Setting square " + board.AllSquares[indexScore.Item1].Name + " to " + board.AllSquares[indexScore.Item1].Number, recursionDepth);

                    await SolveRecursive(board, recursionDepth + 1);

                    if (_abort || _board.ValidateSolved())
                        return;

                    board.SetState(state);
                }

                return;
            }

            await IncrementStepsAsync();
        }
        while (!board.ValidateSolved() && !_abort);
    }
    private int GetSquareScore(ISquare changed, int boardSize)
    {
        int score = 0;

        for (int g = 0; g < changed.GroupCount; g++)
        {
            foreach (ISquare square in changed.GetGroup(g))
            {
                if (square.Number == 0)
                {
                    score += boardSize - square.GetValidNumbersCount();
                }
            }
        }

        return score;
    }


    private void Verbose(object message, int depth)
    {
        if (_verboseLogging)
        {
            this.Log((depth == -1 ? "" : $"({depth}) ") + message);
        }
    }
    private IEnumerator StepThrough(float waitTime)
    {
        if (_stepThrough)
        {
            this.Log("Waiting for step...");
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.M));
        }
        else
        {
            yield return new WaitForSeconds(waitTime);
        }
    }
    private IEnumerator IncrementSteps()
    {
        _steps++;
        if (_steps >= _stepLimit)
        {
            int answer = 0;
            Modal.ShowModal(new Modal.ModalData()
            {
                Title = "No Solution Found",
                Body = "The solver couldn't find a solution fast enough. Keep trying or stop it now?",

                ShowConfirmButton = true,
                ConfirmButtonText = "Continue",
                ConfirmButtonEvent = () => answer = 1,

                ShowCancelButton = true,
                CancelButtonText = "Stop",
                CancelButtonEvent = () => answer = 2
            });

            yield return new WaitWhile(() => answer == 0);

            if (answer == 1)
            {
                _steps = 0;
                _abort = false;
            }
            else
            {
                _steps = _stepLimit;
                _abort = true;
            }
        }
        else
        {
            yield return null;
        }
    }
    private async Task IncrementStepsAsync()
    {
        _steps++;
        if (_steps >= _stepLimit)
        {
            _continueSolvingFlag = new TaskCompletionSource<bool>();
            _asyncActions.Enqueue(() =>
            {
                Modal.ShowModal(new Modal.ModalData()
                {
                    Title = "No Solution Found",
                    Body = "The solver couldn't find a solution fast enough. Keep trying or stop it now?",

                    ShowConfirmButton = true,
                    ConfirmButtonText = "Continue",
                    ConfirmButtonEvent = () => _continueSolvingFlag.SetResult(false),

                    ShowCancelButton = true,
                    CancelButtonText = "Stop",
                    CancelButtonEvent = () => _continueSolvingFlag.SetResult(true)
                });
            });

            this.Log("Waiting for continue response");
            _abort = await _continueSolvingFlag.Task;
            this.Log("Response (True is Abort): " + _abort);
            _steps = _abort ? _stepLimit : 0;
        }
    }
    private IEnumerator SafeRun(IEnumerator routine, System.Action OnError = null)
    {
        while (true)
        {
            object current;
            try
            {
                if (!routine.MoveNext())
                    yield break;
                current = routine.Current;
            }
            catch (System.Exception e)
            {
                this.LogError("An error occurred in the solver: " + e);
                OnError?.Invoke();

                Modal.ShowModal(new Modal.ModalData()
                {
                    Title = "Something went Wrong",
                    Body = "The solver encountered an error",
                    ShowConfirmButton = true,
                    TimeoutTime = 30f
                });

                yield break;
            }
            yield return current;
        }
    }
}
