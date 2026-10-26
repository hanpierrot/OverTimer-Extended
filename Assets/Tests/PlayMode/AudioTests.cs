using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// AudioService trong scene thật. Kiểm tra clip nào được gán cho nguồn phát (AudioSource.clip), không kiểm tra
/// isPlaying: thiết bị âm thanh có thể không có (batchmode) hoặc bị trình duyệt chặn cho tới khi người chơi bấm.
/// </summary>
public class AudioTests : GameSceneTest
{
    private static AudioService Service => AudioService.Instance;
    private static AudioSource[] Voices => TestHooks.GetField<AudioSource[]>(Service, "_sources");
    private static AudioCatalogSO Catalog => TestHooks.GetField<AudioCatalogSO>(Service, "catalog");

    private static AudioCatalogSO.Entry Entry(SfxId id) => Catalog.entries.Single(e => e.id == id);

    private static bool AnyVoiceHasClip(AudioClip clip) => Voices.Any(v => v.clip == clip);

    [Test]
    public void AudioServiceCoTrongSceneVaCoCatalog()
    {
        Assert.IsNotNull(Service, "Thiếu AudioService trên MANAGERS/Services");
        Assert.IsNotNull(Catalog, "AudioService chưa gán AudioCatalog");
        Assert.GreaterOrEqual(Voices.Length, 1);
    }

    [Test]
    public void PhatMoiSfxId_KhongNemLoi()
    {
        foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            Assert.DoesNotThrow(() => AudioService.Play(id), id.ToString());
    }

    [Test]
    public void NhanTien_PhatTiengTheoMucSoTien()
    {
        var catalog = Catalog;
        var cases = new[]
        {
            (amount: catalog.moneySmallMax, id: SfxId.MoneyGainSmall),
            (amount: catalog.moneyMediumMax, id: SfxId.MoneyGainMedium),
            (amount: catalog.moneyMediumMax + 1, id: SfxId.MoneyGainLarge),
        };

        foreach (var (amount, id) in cases)
        {
            foreach (var v in Voices) v.clip = null;
            var expected = Entry(id).clips[0];

            MoneyService.Instance.Add(amount, "test");

            Assert.IsTrue(AnyVoiceHasClip(expected), $"${amount}: phải phát {id}");
        }
    }

    [Test]
    public void HoanTienCuoc_KhongPhatTiengNhanTien()
    {
        foreach (var v in Voices) v.clip = null;

        MoneyService.Instance.Add(20, MoneyService.StakeRefundReason);

        Assert.IsFalse(Voices.Any(v => v.clip != null), "Tiền cược trả lại không phải thu nhập");
    }

    [Test]
    public void KetThucThang_PhatWinStingerVoiIgnoreListenerPause()
    {
        foreach (var v in Voices) v.clip = null;
        var entry = Entry(SfxId.WinStinger);

        GameManager.Instance.GameOver(EndReason.ClockWin);

        var voice = Voices.FirstOrDefault(v => v.clip == entry.clips[0]);
        Assert.IsNotNull(voice, "Thắng phải phát WinStinger");
        Assert.IsTrue(voice.ignoreListenerPause, "Game over đóng băng AudioListener: stinger phải bỏ qua");
    }

    [Test]
    public void ThuTheNhieuLanTrongKhungHinh_BoQuaTheoMinInterval()
    {
        // Click có minInterval > 0: phát hai lần liên tiếp chỉ gán clip một lần.
        var entry = Entry(SfxId.Click);
        Assert.Greater(entry.minInterval, 0f, "Click cần minInterval để không dội liên tục");
        foreach (var v in Voices) v.clip = null;

        AudioService.Play(SfxId.Click);
        AudioService.Play(SfxId.Click);

        Assert.AreEqual(1, Voices.Count(v => v.clip == entry.clips[0]));
    }
}
