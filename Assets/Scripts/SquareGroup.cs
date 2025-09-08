using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SquareGroup
{
    [Header("References")]
    [SerializeField] private List<ISquare> _squares;
    [SerializeField] private IBoard _board;

    [Header("State")]
    [SerializeField] private bool _valid = false;

    [Header("Debug")]
    [SerializeField] private int _groupIndex = -1;

    public bool IsValid => _valid;
    public ISquare this[int i] { get => _squares[i]; set => _squares[i] = value; }

    public event System.Action OnValidityChanged;

    public SquareGroup(IBoard board, int groupIndex)
    {
        _board = board;
        _squares = new List<ISquare>();
        _groupIndex = groupIndex;
    }
    public void PushSquare(ISquare square, bool refreshState = true)
    {
        square.AddGroup(this, _groupIndex);
        square.OnChanged += OnSquareChanged;
        _squares.Add(square);

        if (refreshState)
            UpdateGroupState();
    }
    public ISquare PopSquare(bool refreshState = true) => PopSquare(_squares.Count - 1, refreshState);
    public ISquare PopSquare(int index, bool refreshState = true)
    {
        ISquare pop = _squares[index];
        _squares.RemoveAt(index);

        if (refreshState)
            UpdateGroupState();

        return pop;
    }

    public bool UpdateGroupState()
    {
        List<int> nums = new List<int>();
        bool valid = true;

        foreach (ISquare square in _squares)
        {
            if (square.Number == 0)
                continue;
            if (nums.Contains(square.Number))
            {
                valid = false;
                break;
            }
            nums.Add(square.Number);
        }

        if (_valid != valid)
        {
            _valid = valid;
            OnValidityChanged?.Invoke();
        }

        return valid;
    }
    public bool AllSquaresFilled()
    {
        bool filled = true;
        foreach (ISquare square in _squares)
        {
            filled = filled && square.Number > 0;
        }

        return filled;
    }
    public bool Contains(int num)
    {
        foreach (ISquare square in _squares)
            if (square.Number == num)
                return true;

        return false;
    }
    public IEnumerator GetEnumerator() => _squares.GetEnumerator();

    private void OnSquareChanged(ISquare square)
    {
        UpdateGroupState();
    }
}
