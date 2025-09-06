using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SquareGroup
{
    [Header("References")]
    [SerializeField] private List<Square> _squares;
    [SerializeField] private Board _board;

    [Header("State")]
    [SerializeField] private bool _valid = false;

    [Header("Debug")]
    [SerializeField] private int _groupIndex = -1;

    public bool IsValid => _valid;
    public Square this[int i] { get => _squares[i]; set => _squares[i] = value; }

    public SquareGroup(Board board, int groupIndex)
    {
        _board = board;
        _squares = new List<Square>();
        _groupIndex = groupIndex;
    }
    public void PushSquare(Square square, bool refreshState = true)
    {
        square.OnChanged += OnSquareChanged;
        _squares.Add(square);

        if (refreshState)
            UpdateGroupState();
    }
    public Square PopSquare(int index = 0, bool refreshState = true)
    {
        Square pop = _squares[_squares.Count - 1];
        _squares.RemoveAt(_squares.Count - 1);

        if (refreshState)
            UpdateGroupState();

        return pop;
    }

    public bool UpdateGroupState(bool updateSquaresColour = true)
    {
        List<int> nums = new List<int>();
        bool valid = true;

        foreach (Square square in _squares)
        {
            if (square == 0)
                continue;
            if (nums.Contains(square))
                {
                    valid = false;
                    break;
                }
            nums.Add(square);
        }

        if (updateSquaresColour)
        {
            foreach (Square square in _squares)
            {
                square.SetValidity(valid, _groupIndex);
            }
        }

        _valid = valid;
        return valid;
    }

    private void OnSquareChanged(Square square)
    {
        UpdateGroupState();
    }
}
