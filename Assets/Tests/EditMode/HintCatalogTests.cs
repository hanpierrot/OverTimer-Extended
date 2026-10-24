using System;
using NUnit.Framework;

/// <summary>Mục 4.7.6 của kế hoạch kiểm thử, TC-HNT-1: HintCatalog là lớp static thuần nên chạy Edit Mode.</summary>
public class HintCatalogTests
{
    [Test]
    public void TC_HNT_1_MoiGoiYDeuCoNoiDung()
    {
        foreach (HintId id in Enum.GetValues(typeof(HintId)))
        {
            string text = HintCatalog.Get(id);

            Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"{id}: không có nội dung gợi ý");
            Assert.IsFalse(text.Contains("{1}"), $"{id}: chỉ được dùng tối đa 1 tham số");
            Assert.DoesNotThrow(() => string.Format(text, 1), $"{id}: chuỗi định dạng sai");
        }
    }
}
