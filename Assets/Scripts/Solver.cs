using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EditorTools;
using UnityEngine;
using UnityEngine.Events;

public class Solver : MonoBehaviour
{
    [SerializeField] private Board _board;
    [SerializeField] private int _cycleLimit = 100;
    [SerializeField] private float _cyclePauseTime = 0.1f;
    [SerializeField] private float _generationTimeoutTime = 1f;

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;
    [SerializeField] private bool _stepThrough = false;
    [SerializeField] private bool _slowMode = false;

    [Space]
    [SerializeField] private bool _working = false;
    [SerializeField] private bool _abort = false;
    [SerializeField] private int _cycles;

    public bool SlowMode { get => _slowMode; set => _slowMode = value; }
    public bool Working { get => _working; }

    public event System.Action OnSolverStart;
    public event System.Action OnSolverFinished;
    public UnityEvent OnSolverStart_UE;
    public UnityEvent OnSolverFinished_UE;

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
        if (!_board) return;
        if (_working) return;

        _abort = true;
        StartCoroutine(SolveBoardRoutine(_board, _slowMode));
    }
    public void GenerateBoard()
    {
        if (!_board) return;
        if (_working) return;

        _abort = true;
        StartCoroutine(GenerateBoardRoutine(_board, _generationTimeoutTime, _slowMode));
    }
    public void Abort() { if (_working) _abort = true; }

    private IEnumerator SolveBoardRoutine(Board board, bool slow)
    {
        _working = true;
        _cycles = 0;
        _abort = false;

        this.Log("Starting Solver. Slow Mode: " + _slowMode);

        OnSolverStart?.Invoke();
        OnSolverStart_UE.Invoke();

        if (slow)
            yield return SolveRecursiveSlow(board, _cyclePauseTime);
        else
        {
#if UNITY_WEBGL
            yield return SolveRecursiveSlow(board, 0f);
#else
            DataOnlyBoard dBoard = board;

            while (_cycles < _cycleLimit)
            {
                Task task = Task.Run(() => SolveRecursive(dBoard));

                yield return new WaitUntil(() => task.IsCompleted);

                if (task.Exception == null)
                {
                    if (!dBoard.ValidateSolved())
                        yield return WaitForInstruction();
                }
                else
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
            }

            board.SetState(dBoard);
#endif
        }

        this.Log("Board Solved: " + board.ValidateSolved());

        _working = false;

        OnSolverFinished?.Invoke();
        OnSolverFinished_UE.Invoke();
    }
    private IEnumerator GenerateBoardRoutine(Board board, float timeOutTime, bool slow)
    {
        _working = true;
        _cycles = 0;
        _abort = false;

        this.Log("Starting Generator. Slow Mode: " + _slowMode);

        OnSolverStart?.Invoke();
        OnSolverStart_UE.Invoke();

        board.SetState(IBoard.State.GenerateEmpty(board.BoardSize));
        board.AllSquares[Random.Range(0, board.AllSquares.Length)].Number = Random.Range(1, board.BoardSize + 1);

        if (slow)
        {
            yield return SolveRecursiveSlow(board, _cyclePauseTime);
        }
        else
        {
#if UNITY_WEBGL
            Coroutine generation = StartCoroutine(SolveRecursiveSlow(board, 0f));

            float t = 0f;
            while (!board.ValidateSolved())
            {
                yield return null;
                t += Time.deltaTime;
                if (t > timeOutTime)
                {
                    _abort = true;

                    this.Log("Generation Timeout");
                    StartCoroutine(GenerateBoard(board, timeOutTime * 1.1f, slow));
                    yield break;
                }
            }
#else
            DataOnlyBoard dBoard = board;

            Task task = Task.Run(() =>
            {
                SolveRecursive(dBoard);
            });

            float t = 0f;
            while (!task.IsCompleted)
            {
                yield return null;
                t += Time.deltaTime;
                if (t > timeOutTime)
                {
                    _abort = true;

                    this.Log("Generation Timeout");
                    StartCoroutine(GenerateBoardRoutine(board, timeOutTime * 1.1f, slow));
                    yield break;
                }
            }

            if (task.Exception != null)
                throw task.Exception;

            board.SetState(dBoard);
#endif
        }

        this.Log("Generation Successful: " + board.ValidateSolved());

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

            yield return Step(waitTime);

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

                            int score = GetSquareScore(board.AllSquares[i]) + notes.Length;

                            bestSquares.Add((i, score, notes[n]));

                            board.AllSquares[i].Number = 0;
                        }
                    }
                }

                bestSquares.Sort((x, y) => x.Item2 < y.Item2 ? -1 : 1);

                foreach (var indexScore in bestSquares)
                {
                    board.AllSquares[indexScore.Item1].Number = indexScore.Item3;

                    Verbose("Setting square " + board.AllSquares[indexScore.Item1].Name + " to " + board.AllSquares[indexScore.Item1].Number, recursionDepth);

                    yield return SolveRecursiveSlow(board, recursionDepth + 1);

                    if (_cycles >= _cycleLimit)
                    {
                        yield return WaitForInstruction();
                        _abort = _cycles >= _cycleLimit;
                    }

                    if (_abort || board.ValidateSolved())
                        yield break;

                    board.SetState(state);
                }

                yield break;
            }

            _cycles++;
        }
        while (!board.ValidateSolved() && _cycles < _cycleLimit);
    }
    private void SolveRecursive(DataOnlyBoard board, int recursionDepth = 0)
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

                            int score = GetSquareScore(board.AllSquares[i]) + notes.Length;

                            bestSquares.Add((i, score, notes[n]));

                            board.AllSquares[i].Number = 0;
                        }
                    }
                }

                bestSquares.Sort((x, y) => x.Item2 < y.Item2 ? -1 : 1);

                foreach (var indexScore in bestSquares)
                {
                    board.AllSquares[indexScore.Item1].Number = indexScore.Item3;

                    Verbose("Setting square " + board.AllSquares[indexScore.Item1].Name + " to " + board.AllSquares[indexScore.Item1].Number, recursionDepth);

                    SolveRecursive(board, recursionDepth + 1);

                    if (board.ValidateSolved() || _cycles >= _cycleLimit)
                        _abort = true;

                    if (_abort)
                        return;

                    board.SetState(state);
                }

                return;
            }

            _cycles++;
        }
        while (!board.ValidateSolved() && _cycles < _cycleLimit);
    }
    private int GetSquareScore(ISquare changed)
    {
        int score = 0;

        for (int g = 0; g < changed.GroupCount; g++)
        {
            foreach (ISquare square in changed.GetGroup(g))
            {
                if (square.Number == 0)
                {
                    score += square.GetValidNumbersCount();
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
    private IEnumerator Step(float waitTime)
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
    private IEnumerator WaitForInstruction()
    {
        if (Modal.Instance)
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
                _cycles = 0;
        }
    }
}
