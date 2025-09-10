using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardSelectionButton : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _left, _right;

    [Header("Labels")]
    [SerializeField] private TMP_Text _diffText;
    [SerializeField] private TMP_Text _sizeText;
    [SerializeField] private TMP_Text _solvedText;

    [Header("References")]
    [SerializeField] private MinifiedBoard _miniBoard;
    [SerializeField] private IBoard.State _board;

    public string Difficulty { get => _diffText?.text; set { if (_diffText) _diffText.text = value; } }
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
                _miniBoard.Init(_board);
            }
        }
    }

    public event System.Action<IBoard.State> OnClicked;
    public event System.Action OnLeft, OnRight;

    void OnEnable()
    {
        _playButton.onClick.AddListener(OnClick);
        _left.onClick.AddListener(OnLeftClick);
        _right.onClick.AddListener(OnRightClick);

        RectTransform rectTransform = this.transform as RectTransform;
    }
    void OnDisable()
    {
        _playButton.onClick.RemoveListener(OnClick);
        _left.onClick.RemoveListener(OnLeftClick);
        _right.onClick.RemoveListener(OnRightClick);

        _playButton.onClick.RemoveListener(OnClick);
    }
    private void OnClick() => OnClicked?.Invoke(_board);
    private void OnLeftClick() => OnLeft?.Invoke();
    private void OnRightClick() => OnRight?.Invoke();
}
