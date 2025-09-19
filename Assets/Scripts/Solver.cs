using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EditorTools;
using UnityEngine;

public class Solver : MonoBehaviour
{
    [SerializeField] private Board _board;
    [SerializeField] private int _cycleLimit = 100;
    [SerializeField] private float _cyclePauseTime = 0.1f;
    [SerializeField] private float _generationTimeoutTime = 1f;

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;
    [SerializeField] private bool _stepThrough = false;

    [Space]
    [SerializeField] private bool _solving = false;
    [SerializeField] private bool _abort = false;
    [SerializeField] private int _cycles;

#if UNITY_EDITOR
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) && _board != null)
        {
            if (_solving)
            {
                StopAllCoroutines();
                _solving = false;
            }
            else
                StartCoroutine(SolveBoard(_board, Input.GetKey(KeyCode.LeftShift)));
        }
        if (Input.GetKeyDown(KeyCode.G) && _board != null)
        {
            if (_solving)
            {
                StopAllCoroutines();
                _solving = false;
            }
            else
                StartCoroutine(GenerateBoard(_board, _generationTimeoutTime, Input.GetKey(KeyCode.LeftShift)));
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
        _solving = false;
    }

    private IEnumerator SolveBoard(Board board, bool slow)
    {
        _solving = true;
        _cycles = 0;
        _abort = false;

        if (slow)
            yield return SolveRecursiveSlow(board, _cyclePauseTime);
        else
        {
#if UNITY_WEBGL
            yield return SolveRecursiveSlow(board, 0f);
#else
            DataOnlyBoard dBoard = board;
            Task task = Task.Run(() => SolveRecursive(dBoard));

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
                throw task.Exception;
            board.SetState(dBoard);
#endif
        }

        this.Log("Board Solved: " + board.ValidateSolved());

        _solving = false;
    }
    private IEnumerator GenerateBoard(Board board, float timeOutTime, bool slow)
    {
        _solving = true;
        _cycles = 0;
        _abort = false;

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
                    StartCoroutine(GenerateBoard(board, timeOutTime * 1.1f, slow));
                    yield break;
                }
            }

            if (task.Exception != null)
                throw task.Exception;

            board.SetState(dBoard);
#endif
        }

        this.Log("Generation Successful: " + board.ValidateSolved());

        _solving = false;
    }

    private IEnumerator SolveRecursiveSlow(IBoard board, float waitTime, int recursionDepth = 0)
    {
        if (recursionDepth >= 1020)
        {
            this.Log("Something went wrong: Max recursion reached");
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

                    if (board.ValidateSolved() || _cycles >= _cycleLimit)
                        _abort = true;

                    if (_abort)
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
}
