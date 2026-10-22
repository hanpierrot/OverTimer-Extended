using System;
using System.Collections.Generic;

public enum LetterKind { Tax, Junk }

/// One letter on the pile at the door.
public sealed class Letter
{
    public int id;
    public LetterKind kind;
    public string text;      // junk: the joke on the flyer
}

/// The letters lying at the door (tax letters and junk mail). At most `capacity`; when it is full a new letter pushes the
/// oldest JUNK letter off, and if there is no junk to push off the new letter is refused (a tax letter is never lost,
/// because only one bill is open at a time). Pure logic, one instance per run (owned by DoorController).
public sealed class LetterPile
{
    readonly int _capacity;
    readonly List<Letter> _letters = new List<Letter>();
    int _nextId = 1;

    public LetterPile(int capacity) { _capacity = Math.Max(1, capacity); }

    public IReadOnlyList<Letter> Letters => _letters;
    public int Capacity => _capacity;
    public event Action<Letter> Added;
    public event Action<Letter> Removed;

    /// Returns the letter, or null when the pile is full of letters that can't be pushed off.
    public Letter Add(LetterKind kind, string text)
    {
        if (_letters.Count >= _capacity)
        {
            var old = _letters.Find(l => l.kind == LetterKind.Junk);
            if (old == null) return null;
            Remove(old);
        }
        var letter = new Letter { id = _nextId++, kind = kind, text = text };
        _letters.Add(letter);
        Added?.Invoke(letter);
        return letter;
    }

    public bool Remove(Letter letter)
    {
        if (letter == null || !_letters.Remove(letter)) return false;
        Removed?.Invoke(letter);
        return true;
    }

    public Letter FirstOf(LetterKind kind) => _letters.Find(l => l.kind == kind);
}

/// When the next piece of junk mail is due: every min..max seconds of running clock, and never before `grace` seconds
/// (Q6: none in the first 60 s of a run). All randomness comes from the passed System.Random (RngService).
public sealed class JunkMailTimer
{
    readonly Random _rng;
    readonly float _min, _max;
    float _left;

    public JunkMailTimer(Random rng, float min, float max, float grace)
    {
        _rng = rng;
        _min = Math.Max(0f, Math.Min(min, max));
        _max = Math.Max(min, max);
        _left = Math.Max(grace, Roll());
    }

    float Roll() => _min + (float)_rng.NextDouble() * (_max - _min);

    /// Advance by game seconds. True when a letter is due (the next one is then rolled).
    public bool Tick(float gameDelta)
    {
        if (gameDelta <= 0f) return false;
        _left -= gameDelta;
        if (_left > 0f) return false;
        _left = Roll();
        return true;
    }
}
