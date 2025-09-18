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
    [SerializeField] private MiniSquare _minifiedSquarePrefab;
    [SerializeField] private Transform _quadrantsParent;

    [Space]
    [SerializeField] private List<MiniSquare> _squares = new List<MiniSquare>();

    private List<RectTransform> _quadrants = new List<RectTransform>();

    public int BoardSize => _boardSize;

    public Vector2Int SquareCount => _squareCount;

    public SquareGroup[,] Quadrants => null;

    public SquareGroup[] Rows => null;

    public SquareGroup[] Columns => null;

    public ISquare[] AllSquares => null;
    public List<ISquare> EmptySquares => null;

    void OnValidate()
    {
        if (Application.isPlaying) return;

        if (_minifiedSquarePrefab)
        {
            _boardSize = Mathf.Clamp(_boardSize, 0, 100);
            int squareCount = _boardSize * _boardSize;
            if (_squares.Count < squareCount)
            {
                while (_squares.Count < squareCount)
                    _squares.Add(GameObject.Instantiate(_minifiedSquarePrefab, this.transform));
            }
            else if (_squares.Count > squareCount)
            {
                while (_squares.Count > squareCount)
                {
                    Destroy(_squares[_squares.Count - 1].gameObject);
                    _squares.RemoveAt(_squares.Count - 1);
                }

            }
        }
    }

    public void Init(IBoard.State state)
    {
        _boardSize = state.Numbers.GetLength(0);

        while (_squares.Count < _boardSize * _boardSize)
            _squares.Add(GameObject.Instantiate(_minifiedSquarePrefab, this.transform));
        foreach (MiniSquare square in _squares)
            square.gameObject.SetActive(false);

        float sqrtSize = Mathf.Sqrt(_boardSize);
        _squareCount = new Vector2Int(
            Mathf.CeilToInt(sqrtSize),
            Mathf.FloorToInt(sqrtSize)
        );

        _quadrantCount = new Vector2Int(_squareCount.y, _squareCount.x);
        while (_quadrants.Count < _quadrantCount.x * _quadrantCount.y)
        {
            RectTransform newQuad = new GameObject($"Quadrant").AddComponent<RectTransform>();
            newQuad.SetParent(_quadrantsParent);
            _quadrants.Add(newQuad);
        }
        foreach (RectTransform rect in _quadrants)
            rect.gameObject.SetActive(false);

        RectTransform[,] canvasQuadrants = new RectTransform[_quadrantCount.x, _quadrantCount.y];

        int qX = 0;
        int s = 0, q = 0;
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
                    RectTransform newQuadrant = _quadrants[q];
                    newQuadrant.name = $"Quadrant ({qX}, {qY})";
                    newQuadrant.gameObject.SetActive(true);
                    q++;

                    newQuadrant.localScale = Vector3.one;
                    Board.SetAnchors(newQuadrant, qX, qY, _quadrantCount.x, _quadrantCount.y, _quadrantAnchorPadding);

                    canvasQuadrants[qX, qY] = newQuadrant;
                }

                MiniSquare newSquare = _squares[s];
                newSquare.transform.SetParent(canvasQuadrants[qX, qY]);
                newSquare.gameObject.SetActive(true);
                newSquare.name = $"({x}, {y})";

                int number = state.Numbers[_boardSize - 1 - y, x];
                if (number > 0)
                    newSquare.Text.text = number.ToString();
                else
                    newSquare.Text.text = string.Empty;

                RectTransform squareRect = newSquare.transform as RectTransform;
                Board.SetAnchors(squareRect, x - (_squareCount.x * qX), y - (_squareCount.y * qY), _squareCount.x, _squareCount.y, _squareAnchorPadding);

                s++;
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
