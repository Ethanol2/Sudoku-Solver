using System.Collections;
using EditorTools;
using UnityEngine;

public class Solver : MonoBehaviour
{
    [SerializeField] private BoardSelection _boardSelector;

    [Space]
    [SerializeField] private Board _board;
    [SerializeField] private int _cycleLimit = 100;
    [SerializeField] private float _cyclePauseTime = 0.1f;

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
            StartCoroutine(SolveBoard(_board, _cycleLimit, _cyclePauseTime));
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

    private IEnumerator SolveBoard(Board board, int cycleLimit, float pauseTime)
    {
        int cycles = 0;
        bool winner;

        do
        {
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

                    if (square.Notes.Count == 1)
                    {
                        square.Number = square.Notes.GetActiveNotes()[0];
                    }
                }
            }

            yield return new WaitForSeconds(pauseTime);

            winner = board.ValidateSolved();
            cycles++;
        }
        while (!winner && cycles < cycleLimit);

        this.Log("Board Solved: " + winner);
    }
}
