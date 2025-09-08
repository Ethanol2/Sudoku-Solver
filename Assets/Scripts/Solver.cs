using System.Collections;
using System.Collections.Generic;
using EditorTools;
using UnityEngine;

public class Solver : MonoBehaviour
{
    [SerializeField] private BoardSelection _boardSelector;

    [Space]
    [SerializeField] private Board _board;
    [SerializeField] private int _cycleLimit = 100;
    [SerializeField] private float _cyclePauseTime = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;
    [SerializeField] private bool _stepThrough = false;

    [Space]
    [SerializeField] private bool _solving = false;
    [SerializeField] private bool _abort = false;
    [SerializeField] private int _cycles;

    private HashSet<int[,]> _states = new HashSet<int[,]>();

    void OnEnable()
    {
        _boardSelector.OnBoardCreated += OnBoardCreated;
        _boardSelector.OnBoardDestroyed += OnBoardDestroyed;
    }
    void OnDisable()
    {
        _boardSelector.OnBoardCreated -= OnBoardCreated;
        _boardSelector.OnBoardDestroyed -= OnBoardDestroyed;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) && _board)
        {
            if (_solving)
            {
                StopAllCoroutines();
                _solving = false;
            }
            else
                StartCoroutine(SolveBoard(_board));
        }
    }

    private void OnBoardCreated(Board board)
    {
        _board = board;
    }
    private void OnBoardDestroyed()
    {
        StopAllCoroutines();
    }

    private IEnumerator SolveBoard(Board board)
    {
        _solving = true;
        _cycles = 0;
        _abort = false;

        _states.Clear();

        yield return SolveRecursive(board);

        this.Log("Board Solved: " + board.ValidateSolved());

        _solving = false;
    }
    private IEnumerator SolveRecursive(Board board, int recursionDepth = 0)
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
            foreach (Square square in board.AllSquares)
            {
                if (square == 0)
                {
                    for (int n = 1; n < board.BoardSize + 1; n++)
                    {
                        square.Notes[n] =
                            square.Groups[0].Contains(n) ||
                            square.Groups[1].Contains(n) ||
                            square.Groups[2].Contains(n)

                            ?

                            false : true;
                    }

                    int noteCount = square.Notes.Count;

                    if (noteCount == 0)
                    {
                        goodSquareCount = -1;
                        break;
                    }
                    else if (noteCount == 1)
                    {
                        int[] notes = square.Notes.GetActiveNotes();
                        square.Number = square.Notes.GetActiveNotes()[0];
                        goodSquareCount++;
                    }
                }
            }

            if (_stepThrough)
                yield return Step();
            else
                yield return new WaitForSeconds(_cyclePauseTime);

            if (goodSquareCount == 0)
            {
                Verbose("No good squares", recursionDepth);

                Board.State state = board.GetState();
                int noteDepth = 2;

                while (noteDepth <= board.BoardSize && !board.ValidateSolved() && _cycles < _cycleLimit)
                {
                    List<(int, int, int)> bestSquares = new List<(int, int, int)>();

                    for (int i = 0; i < board.AllSquares.Length; i++)
                    {
                        if (board.AllSquares[i] == 0 && board.AllSquares[i].Notes.Count == noteDepth)
                        {
                            for (int n = 0; n < noteDepth; n++)
                            {
                                board.AllSquares[i].Number = board.AllSquares[i].Notes.GetActiveNotes()[n];

                                int score = GetSquareScore(board, board.AllSquares[i], noteDepth);

                                bestSquares.Add((i, score, board.AllSquares[i].Number));

                                board.AllSquares[i].Number = 0;
                            }
                        }
                    }

                    bestSquares.Sort((x, y) => x.Item2 > y.Item2 ? -1 : 1);

                    foreach (var indexScore in bestSquares)
                    {
                        board.AllSquares[indexScore.Item1].Number = indexScore.Item3;

                        Verbose("(" + noteDepth + ") Setting square " + board.AllSquares[indexScore.Item1].name + " to " + board.AllSquares[indexScore.Item1].Number, recursionDepth);

                        yield return Step();

                        yield return SolveRecursive(board, recursionDepth + 1);

                        if (board.ValidateSolved() || _cycles >= _cycleLimit)
                            _abort = true;

                        if (_abort)
                            yield break;

                        board.SetState(state);

                        GenerateNotes(board);
                    }

                    noteDepth++;
                    yield return null;
                }

                yield break;
            }
            else if (goodSquareCount == -1)
            {
                //Verbose("Dead end", recursionDepth);
                yield break;
            }

            _cycles++;
        }
        while (!board.ValidateSolved() && _cycles < _cycleLimit);
    }
    private void GenerateNotes(Board board)
    {
        foreach (Square square in board.AllSquares)
        {
            if (square == 0)
            {
                for (int n = 1; n < board.BoardSize + 1; n++)
                {
                    if (square.Groups[0].Contains(n) || square.Groups[1].Contains(n) || square.Groups[2].Contains(n))
                    {
                        square.Notes[n] = false;
                    }
                    else
                    {
                        square.Notes[n] = true;
                    }
                }
            }
        }
    }
    private int GetSquareScore(Board board, Square changed, int threshhold)
    {
        int score = 0;

        for (int g = 0; g < changed.Groups.Length; g++)
        {
            foreach (Square square in changed.Groups[g])
            {
                if (square == 0)
                {
                    for (int n = 1; n < board.BoardSize + 1; n++)
                    {
                        if (square.Groups[0].Contains(n) || square.Groups[1].Contains(n) || square.Groups[2].Contains(n))
                        {
                            square.Notes[n] = false;
                        }
                        else
                        {
                            square.Notes[n] = true;
                        }

                        if (square.Notes.Count <= threshhold)
                            score++;
                    }
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
    private IEnumerator Step()
    {
        if (_stepThrough)
        {
            this.Log("Waiting for step...");
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.M));
        }
    }
}
