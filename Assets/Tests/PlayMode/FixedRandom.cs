/// <summary>
/// Random trả về giá trị cố định cho Next(min, max), dùng để chọn đúng trường hợp cần test
/// (ví dụ “vein 6 khối”) mà không phải dò seed. Giá trị được kẹp vào [min, max).
/// </summary>
public sealed class FixedRandom : System.Random
{
    private readonly int _value;

    public FixedRandom(int value) => _value = value;

    public override int Next(int minValue, int maxValue)
    {
        if (_value < minValue) return minValue;
        return _value >= maxValue ? maxValue - 1 : _value;
    }
}
