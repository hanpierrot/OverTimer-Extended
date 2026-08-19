using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The mining app: drag the pickaxe onto the ore block to hit it. Each vein is
/// a random-length (GameConfig.veinLengthMin..Max) walk through BlockConfig's
/// ordered block list, starting at index 0 (dirt) - fixed order, not random,
/// so a vein always progresses dirt -> wood -> ... -> ruby. Payout is
/// position-indexed from GameConfig.blockPayout, so quitting late in a vein
/// costs more (DESIGN.md §5.2). Finishing the vein banks the accumulated
/// money; closing the app mid-vein forfeits whatever's still unconfirmed,
/// same house rule as Blackjack forfeiting a live hand (BLACKJACK.md §2.3).
/// </summary>
public class MiningApp : LaptopApp
{
    [Header("Services")]
    [SerializeField] private GameConfig config;
    [SerializeField] private BlockConfig blocks;
    [SerializeField] private MoneyService money;
    [SerializeField] private RngService rng;

    [Header("UI")]
    [SerializeField] private PickaxeView pickaxe;
    [SerializeField] private Image oreImage;
    [SerializeField] private Image[] statusNodes;
    [SerializeField] private TMP_Text unconfirmedLabel;
    [SerializeField] private TMP_Text receivedLabel;
    [SerializeField] private Color filledColor = new Color(0f, 0.29644084f, 0.82641506f, 1f);
    [SerializeField] private Color unfilledColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    private List<BlockTypeDefinition> _vein;
    private int _blockIndex;
    private int _blockHp;
    private int _totalVeinHp;
    private int _damageDealt;
    private int _unconfirmed;
    private int _totalReceived;

    private void Start()
    {
        pickaxe.Hit += OnHit;
        StartVein();
    }

    private void StartVein()
    {
        int length = rng.Random.Next(config.veinLengthMin, config.veinLengthMax + 1);
        length = Mathf.Min(length, blocks.blocks.Length, statusNodes.Length);

        _vein = new List<BlockTypeDefinition>(length);
        _totalVeinHp = 0;
        for (int i = 0; i < length; i++)
        {
            _vein.Add(blocks.blocks[i]);
            _totalVeinHp += blocks.blocks[i].hp;
        }

        _blockIndex = 0;
        _blockHp = _vein[0].hp;
        _damageDealt = 0;
        _unconfirmed = 0;

        // Only show as many nodes as this vein actually has, so a length-6
        // vein reads as "mining to 6," not as a 10-node bar stuck at 60%.
        for (int i = 0; i < statusNodes.Length; i++)
            statusNodes[i].gameObject.SetActive(i < length);

        RefreshOre();
        RefreshProgress();
        RefreshLabels();
    }

    private void OnHit()
    {
        // A hit landing during the veinRespawn delay (vein finished, next one
        // not generated yet) would otherwise index past the old vein's end.
        if (_vein == null || _blockIndex >= _vein.Count) return;

        var block = _vein[_blockIndex];
        int damage = Mathf.Min(block.pickaxeDamage, _blockHp);

        _blockHp -= damage;
        _damageDealt += damage;

        if (_blockHp <= 0)
        {
            int payoutIndex = Mathf.Min(_blockIndex, config.blockPayout.Length - 1);
            _unconfirmed += config.blockPayout[payoutIndex];
            _blockIndex++;

            if (_blockIndex >= _vein.Count)
            {
                BankVein();
                StartCoroutine(RespawnAfterDelay());
                return;
            }

            _blockHp = _vein[_blockIndex].hp;
            RefreshOre();
        }

        RefreshProgress();
        RefreshLabels();
    }

    private void BankVein()
    {
        money.Add(_unconfirmed, "mining vein");
        _totalReceived += _unconfirmed;
        _unconfirmed = 0;
        RefreshLabels();
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(config.veinRespawn);
        StartVein();
    }

    private void RefreshOre() => oreImage.sprite = _vein[_blockIndex].sprite;

    private void RefreshProgress()
    {
        float fraction = _totalVeinHp == 0 ? 0f : (float)_damageDealt / _totalVeinHp;
        int filled = Mathf.RoundToInt(fraction * _vein.Count);

        for (int i = 0; i < _vein.Count; i++)
            statusNodes[i].color = i < filled ? filledColor : unfilledColor;
    }

    private void RefreshLabels()
    {
        unconfirmedLabel.text = $"Unconfirmed: ${_unconfirmed}";
        receivedLabel.text = $"Received: ${_totalReceived}";
    }

    public override void OnAppClosed()
    {
        _unconfirmed = 0;
        RefreshLabels();
    }
}
