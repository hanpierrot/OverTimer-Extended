using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// Kiểm tra dữ liệu của AudioCatalog (asset do Han quản lý): mỗi SfxId có đúng một dòng, thông số hợp lệ.
/// Một dòng chưa có clip là hợp lệ (sẽ im lặng); test chỉ chặn dữ liệu sai chứ không bắt buộc đủ clip.
/// </summary>
public class AudioCatalogTests
{
    private AudioCatalogSO _catalog;

    [OneTimeSetUp]
    public void LoadCatalog()
    {
        var guids = AssetDatabase.FindAssets("t:AudioCatalogSO");
        Assert.AreEqual(1, guids.Length, "Phải có đúng một AudioCatalog trong project");
        _catalog = AssetDatabase.LoadAssetAtPath<AudioCatalogSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        Assert.IsNotNull(_catalog);
    }

    [Test]
    public void MoiSfxIdCoDungMotDong()
    {
        foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            Assert.AreEqual(1, _catalog.entries.Count(e => e.id == id), $"{id}: phải có đúng một dòng trong catalog");
    }

    [Test]
    public void ThongSoCuaTungDongHopLe()
    {
        foreach (var e in _catalog.entries)
        {
            Assert.That(e.volume, Is.InRange(0f, 1f), $"{e.id}: volume");
            Assert.That(e.pitchJitter, Is.InRange(0f, 0.5f), $"{e.id}: pitchJitter");
            Assert.GreaterOrEqual(e.minInterval, 0f, $"{e.id}: minInterval");
            Assert.IsNotNull(e.clips, $"{e.id}: clips null");
            foreach (var clip in e.clips) Assert.IsNotNull(clip, $"{e.id}: có ô clip trống trong danh sách");
        }
    }

    [Test]
    public void NguongTienXacDinhBaMucTangDan()
    {
        Assert.Greater(_catalog.moneySmallMax, 0);
        Assert.Greater(_catalog.moneyMediumMax, _catalog.moneySmallMax, "Mức vừa phải lớn hơn mức nhỏ");
    }

    [Test]
    public void TheoDoiClipConThieu_LietKe()
    {
        // Không phải lỗi: chỉ in ra các id còn im lặng để người làm audio biết còn thiếu clip nào.
        var silent = _catalog.entries.Where(e => e.clips.Length == 0).Select(e => e.id.ToString()).ToArray();
        TestContext.WriteLine("SfxId chưa có clip: " + (silent.Length == 0 ? "(không)" : string.Join(", ", silent)));
        Assert.Pass();
    }

    [Test]
    public void StingerVaAmThanhCuoiVanBoQuaListenerPause()
    {
        // Game over đóng băng game (AudioListener.pause): các âm cuối ván phải vẫn nghe được.
        foreach (var id in new[] { SfxId.WinStinger, SfxId.LoseStinger, SfxId.CoinRain })
            Assert.IsTrue(_catalog.entries.Single(e => e.id == id).ignoreListenerPause, $"{id} cần ignoreListenerPause");
    }
}
