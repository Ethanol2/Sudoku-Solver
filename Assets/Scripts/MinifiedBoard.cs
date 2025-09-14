using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MinifiedBoard : MonoBehaviour, IBoard
{
    [Header("Game Settings")]
    [SerializeField] private int _boardSize = 9;
    [SerializeField] private Vector2Int _squareCount = new Vector2Int(3, 3);
    [SerializeField] private Vector2Int _quadrantCount;

    [Header("Visual Settings")]
    [SerializeField] private float _quadrantAnchorPadding = 0.01f;
    [SerializeField] private float _squareAnchorPadding = 0.005f;

    [Header("References")]
    [SerializeField] private Transform _minifiedSquare;
    [SerializeField] private Transform _quadrantsParent;

    public int BoardSize => _boardSize;

    public Vector2Int SquareCount => _squareCount;

    public SquareGroup[,] Quadrants => null;

    public SquareGroup[] Rows => null;

    public SquareGroup[] Columns => null;

    public ISquare[] AllSquares => null;
    public List<ISquare> EmptySquares => null;

    public void Init(IBoard.State state)
    {
        _boardSize = state.Numbers.GetLength(0);

        float sqrtSize = Mathf.Sqrt(_boardSize);
        _squareCount = new Vector2Int(
            Mathf.CeilToInt(sqrtSize),
            Mathf.FloorToInt(sqrtSize)
        );

        _quadrantCount = new Vector2Int(_squareCount.y, _squareCount.x);

        RectTransform[,] canvasQuadrants = new RectTransform[_quadrantCount.x, _quadrantCount.y];

        int qX = 0;
        for (int x = 0; x < _boardSize; x++)
        {
            if ((x - (_squareCount.x * qX)) == _squareCount.x)
                qX++;

            int qY = 0;
            for (int y = 0; y < _boardSize; y++)
            {
                if ((y - (_squareCount.y * qY)) == _squareCount.y)
                    qY++;

                if (!canvasQuadrants[qX, qY])
                {
                    GameObject newQuadrantGO = new GameObject($"Quadrant ({qX}, {qY})");
                    RectTransform newQuadrant = newQuadrantGO.AddComponent<RectTransform>();
                    newQuadrant.SetParent(_quadrantsParent);
                    newQuadrant.localScale = Vector3.one;
                    Board.SetAnchors(newQuadrant, qX, qY, _quadrantCount.x, _quadrantCount.y, _quadrantAnchorPadding);

                    canvasQuadrants[qX, qY] = newQuadrant;
                }

                Transform newSquare = GameObject.Instantiate(_minifiedSquare, canvasQuadrants[qX, qY]);
                newSquare.name = $"({x}, {y})";

                TMP_Text squareText = newSquare.GetComponentInChildren<TMP_Text>();
                int number = state.Numbers[_boardSize - 1 - y, x];
                if (number > 0)
                    squareText.text = number.ToString();
                else
                    squareText.gameObject.SetActive(false);

                RectTransform squareRect = newSquare.transform as RectTransform;
                Board.SetAnchors(squareRect, x - (_squareCount.x * qX), y - (_squareCount.y * qY), _squareCount.x, _squareCount.y, _squareAnchorPadding);
            }
        }
    }
    public IBoard.State GetState()
    {
        throw new System.NotImplementedException();
    }

    public void SetState(IBoard.State state)
    {
        throw new System.NotImplementedException();
    }

    public bool ValidateSolved()
    {
        throw new System.NotImplementedException();
    }

    public bool HasViolation() => throw new System.NotImplementedException();
}
