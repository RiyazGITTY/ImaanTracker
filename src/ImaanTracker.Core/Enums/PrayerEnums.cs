namespace ImaanTracker.Core.Enums;

public enum PrayerName
{
    Fajr,
    Dhuhr,
    Asr,
    Maghrib,
    Isha,
    Tahajjud,
    Witr,
    Duha
}

public enum PrayerType
{
    Fard,
    Sunnah,
    Nafl,
    Wajib,
    Tahajjud
}

public enum PrayerStatus
{
    Pending,
    Completed,
    Missed,
    Prayed
}
