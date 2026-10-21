using System;
using UnityEngine;

public static class RunRating
{
    public static bool TryParseReason(string value, out EndReason reason) => Enum.TryParse(value, out reason);

    public static bool IsWin(EndReason reason)
    {
        switch (reason)
        {
            case EndReason.ClockWin:
            case EndReason.SetCompletionWin:
            case EndReason.FinalCountdownWin:
            case EndReason.TicketJackpotWin:
            case EndReason.MotherLodeWin:
            case EndReason.BlackjackCharlieWin:
                return true;
            default:
                return false;
        }
    }

    public static string Title(EndReason reason)
    {
        switch (reason)
        {
            case EndReason.ClockWin: return $"Survived {FormatTime(GameManager.Instance.GameConfig.winClock)}";
            case EndReason.SetCompletionWin: return "Set Complete";
            case EndReason.FinalCountdownWin: return "Final Countdown";
            case EndReason.ClockLoss: return "Time Out";
            case EndReason.FinalCountdownLoss: return "Countdown Lost";
            case EndReason.CreepyJesterLoss: return "The Han";
            case EndReason.TicketJackpotWin: return "Jackpot";
            case EndReason.TicketCurseLoss: return "Cursed Ticket";
            case EndReason.MotherLodeWin: return "Mother Lode";
            case EndReason.BlackjackCharlieWin: return "Seven-Card Charlie";
            default: return "";
        }
    }

    public static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }

    public static float Score(RunRecord record)
    {
        var config = GameManager.Instance.GameConfig;

        bool parsed = TryParseReason(record.winCondition, out EndReason reason);
        bool win = parsed && IsWin(reason);

        float outcome = parsed ? OutcomeValue(reason, config) : 0f;
        float survival = win ? 1f : Mathf.Clamp01(record.bestTime / config.winClock);
        float money = record.totalMoneyEarned / (record.totalMoneyEarned + Mathf.Max(1f, config.ratingMoneyHalfPoint));

        float weightSum = config.ratingOutcomeWeight + config.ratingSurvivalWeight + config.ratingMoneyWeight;
        if (weightSum <= 0f) return 0f;

        float weighted = config.ratingOutcomeWeight * outcome
                         + config.ratingSurvivalWeight * survival
                         + config.ratingMoneyWeight * money;

        return 100f * weighted / weightSum;
    }
    
    public static bool IsMechanicWin(EndReason reason)
    {
        return reason == EndReason.TicketJackpotWin
               || reason == EndReason.MotherLodeWin
               || reason == EndReason.BlackjackCharlieWin;
    }

    private static float OutcomeValue(EndReason reason, GameConfig config)
    {
        switch (reason)
        {
            case EndReason.ClockWin: return config.ratingClockWin;
            case EndReason.SetCompletionWin: return config.ratingSetCompletionWin;
            case EndReason.FinalCountdownWin: return config.ratingFinalCountdownWin;
            case EndReason.ClockLoss: return config.ratingClockLoss;
            case EndReason.FinalCountdownLoss: return config.ratingFinalCountdownLoss;
            case EndReason.CreepyJesterLoss: return config.ratingCreepyJesterLoss;
            case EndReason.TicketJackpotWin:
            case EndReason.MotherLodeWin:
            case EndReason.BlackjackCharlieWin:
                return config.ratingMechanicWin;
            case EndReason.TicketCurseLoss:
                return config.ratingMechanicLoss;
            default: return 0f;
        }
    }
}
