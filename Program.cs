using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using Calendar = Ical.Net.Calendar;

TimeZoneInfo currentTimeZone = TimeZoneInfo.Local;

void ConfigTimeZone(string timeZone)
{
    currentTimeZone=TimeZoneInfo.FindSystemTimeZoneById(timeZone);
}

DateOnly Today()
{
    return DateOnly.FromDateTime( TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,currentTimeZone).Date);
}
DateTime TodayDateTime()
{
    return Today().ToDateTime(TimeOnly.MinValue);
}
// i: number of cycles
IEnumerable<(int, DateOnly)> GetEventDates(Event @event)
{
    switch (@event.Repeat)
    {
        case RepeatType.None:
            yield return (0, @event.Since);
            yield break;
        case RepeatType.EveryDay:
            {
                var i = 0;
                for (var start = @event.Since; ; start = start.AddDays(1))
                {
                    yield return (i, start);
                    i++;
                }
            }
        case RepeatType.EveryWeek:
            {
                var i = 0;
                for (var start = @event.Since; ; start = start.AddDays(7))
                {
                    yield return (i, start);
                    i++;
                }
            }
        case RepeatType.EveryMonth:
            {
                var i = 0;
                for (var start = @event.Since; ; start = start.AddMonths(1))
                {
                    yield return (i, start);
                    i++;
                }
            }
        case RepeatType.EveryYear:
            {
                var i = 0;
                for (var start = @event.Since; ; start = start.AddYears(1))
                {
                    yield return (i, start);
                    i++;
                }
            }
        default:
            throw new ArgumentOutOfRangeException();
    }
}
// Range: 90 days before and after
IEnumerable<(int, DateOnly)> GetRecentEventDates(Event @event, int from, int to)
{
    var today = TodayDateTime();
    foreach (var (noc, date) in GetEventDates(@event))
    {
        var theDay = date.ToDateTime(TimeOnly.MinValue);
        var diffDays = (theDay - today).TotalDays;
        if (diffDays < from)
        {
            continue;
        }

        if (diffDays > to)
        {
            break;
        }

        yield return (noc, date);
    }
}

// Range: 90 days before and after
IEnumerable<CalendarEvent> GetCalendarEvents(Event @event)
{
    foreach (var (noc, date) in GetRecentEventDates(@event, -90, 90))
    {
        var theDay = date.ToDateTime(TimeOnly.MinValue);
        var cEvent = new CalendarEvent()
        {
            Summary = @event.Expression.Replace("%NOC%", noc.ToString()),
            IsAllDay = true,
            DtStart = new CalDateTime(theDay),

        };
        cEvent.Alarms.Clear();
        foreach (var notification in @event.Notification)
        {
            cEvent.Alarms.Add(new Alarm()
            {
                Summary = notification.Expression
                    .Replace("%NOC%", noc.ToString())
                    .Replace("%Offset%", notification.DayOffset.ToString()),

                Action = AlarmAction.Display,
                Trigger = new(theDay.AddDays(-notification.DayOffset).AddTicks(notification.Time.Ticks) - theDay)
            });
        }
        yield return cEvent;
    }
}

Calendar GetCalendar(Subscription subscription)
{
    var events = subscription.Events.SelectMany(GetCalendarEvents);
    var calendar = new Calendar();
    calendar.TimeZones.Clear();
    calendar.AddTimeZone(new VTimeZone(subscription.TimeZone));
    calendar.Events.AddRange(events);
    return calendar;

}

IEnumerable<string> GetEventsOfDay(Subscription subscription, DateOnly date)
{
    foreach (var @event in subscription.Events)
    {
        foreach (var (noc, eventDate) in GetRecentEventDates(@event, 0, 30))
        {
            if (date == eventDate)
            {
                yield return @event.Expression.Replace("%NOC%", noc.ToString());
            }

            foreach (var notification in @event.Notification)
            {
                if (date == eventDate.AddDays(-notification.DayOffset))
                {
                    yield return notification.Expression
                        .Replace("%NOC%", noc.ToString())
                        .Replace("%Offset%", notification.DayOffset.ToString());
                }
            }
        }
    }
}
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Configuration.Bind<Config>("ScheduleServer");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapGet("/cal/{subscriptionName}",
        async ([FromRoute] string subscriptionName, [FromQuery] string secret) =>
        {
            
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
            var sub = config.Subscriptions.FirstOrDefault(s => s.Name == subscriptionName);
            if (sub is null) return Results.NotFound();
            if (sub.Secret != secret) return Results.Forbid();
            ConfigTimeZone(sub.TimeZone);
            var cal = GetCalendar(sub);
            await using var calStream = new MemoryStream();
            new CalendarSerializer().Serialize(cal, calStream, Encoding.UTF8);
            var calData = calStream.ToArray();
            return Results.File(calData, "text/calendar", $"{subscriptionName}.ics");
        })
.WithName("GetCalendar");

app.MapGet("/today/{subscriptionName}",
        ([FromRoute] string subscriptionName, [FromQuery] string secret) =>
        {

            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
            
            var sub = config.Subscriptions.FirstOrDefault(s => s.Name == subscriptionName);
            if (sub is null) return Results.NotFound();
            ConfigTimeZone(sub.TimeZone);
            if (sub.Secret != secret) return Results.Forbid();
            var events = GetEventsOfDay(sub, DateOnly.FromDateTime(DateTime.Today));
            return Results.Ok(events.ToArray());
        })
    .WithName("GetEventsOfDate");

app.Run();

public enum RepeatType
{
    None = 0,
    EveryDay = 1,
    EveryWeek = 2,
    EveryMonth = 3,
    EveryYear = 4,
}

public record Config(Subscription[] Subscriptions);
public record Subscription(string Name, Event[] Events, string TimeZone, string Secret);
public record Event(string Expression, DateOnly Since, RepeatType Repeat, EventNotification[] Notification);
/// <summary>
/// 
/// </summary>
/// <param name="Expression">Expression of Notification</param>
/// <param name="DayOffset">The number of days the reminder needs to advance.</param>
/// <param name="Time">Time to show Notification</param>
public record EventNotification(string Expression, int DayOffset, TimeOnly Time);