/// <summary>
/// Random trả về giá trị cố định, dùng để chọn đúng trường hợp cần test (ví dụ “vein 6 khối”,
/// “mèo chắc chắn chặn”) mà không phải dò seed.
///  - Next(min, max) trả về <c>value</c>, kẹp vào [min, max).
///  - NextDouble() trả về <c>sample</c> (mặc định 0, tức mọi phép “tung xác suất &lt; p” đều trúng).
/// </summary>
public sealed class FixedRandom : System.Random
{
    private readonly int _value;
    private readonly double _sample;

    public FixedRandom(int value, double sample = 0.0)
    {
        _value = value;
        _sample = sample;
    }

    public override int Next(int minValue, int maxValue)
    {
        if (_value < minValue) return minValue;
        return _value >= maxValue ? maxValue - 1 : _value;
    }

    public override int Next(int maxValue) => Next(0, maxValue);

    protected override double Sample() => _sample;

    public override double NextDouble() => _sample;
}
