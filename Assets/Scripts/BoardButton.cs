using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardButton : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _playButton;

    [Header("Labels")]
    [SerializeField] private TMP_Text _diffText;
    [SerializeField] private TMP_Text _sizeText;
    [SerializeField] private TMP_Text _solvedText;

    [Header("References")]
    [SerializeField] private MinifiedBoard _miniBoard;
    [SerializeField] private IBoard.State _board;

    public float Difficulty
    {
        get => _board == null ? 0f : _board.Difficulty;
        private set
        {
            if (!_diffText) return;

            string labelText = "Error";
            if (value > 5f)
                labelText = "Diabolical";
            else if (value > 3.5)
                labelText = "Hard";
            else if (value > 2f)
                labelText = "Medium";
            else if (value > 0f)
                labelText = "Easy";

            _diffText.text = labelText;
        }
    }
    public bool Solved { set { if (_solvedText) _solvedText.text = value ? "Solved" : ""; } }
    public IBoard.State Board
    {
        get => _board;
        set
        {
            _board = value;
            if (value != null)
            {
                if (_sizeText) _sizeText.text = $"{value.Numbers.GetLength(0)}x{value.Numbers.GetLength(1)}";
                Solved = value.Solved;
                Difficulty = value.Difficulty;
                _miniBoard.Init(_board);
            }
        }
    }

    public event System.Action<IBoard.State> OnClicked;

    void OnEnable()
    {
        _playButton.onClick.AddListener(OnClick);
    }
    void OnDisable()
    {
        _playButton.onClick.RemoveListener(OnClick);
    }
    private void OnClick() => OnClicked?.Invoke(_board);
}
