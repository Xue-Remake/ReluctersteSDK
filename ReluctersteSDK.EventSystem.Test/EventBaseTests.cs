namespace ReluctersteSDK.EventSystem.Test;

public sealed class EventBaseTests
{
    [Fact]
    public void EventId_IsStableAcrossReads()
    {
        var evt = new OtherTestEvent();

        Assert.Equal(evt.EventId, evt.EventId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
    }

    [Fact]
    public void DifferentInstances_HaveDifferentEventIds()
    {
        var first = new OtherTestEvent();
        var second = new OtherTestEvent();

        Assert.NotEqual(first.EventId, second.EventId);
    }

    [Fact]
    public void Timestamp_IsStableUtc()
    {
        var evt = new OtherTestEvent();

        Assert.Equal(evt.Timestamp, evt.Timestamp);
        Assert.Equal(DateTimeKind.Utc, evt.Timestamp.Kind);
    }

    [Fact]
    public void InitAccessors_AllowFixedMetadata()
    {
        Guid id = Guid.NewGuid();
        DateTime timestamp = new(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

        var evt = new OtherTestEvent { EventId = id, Timestamp = timestamp };

        Assert.Equal(id, evt.EventId);
        Assert.Equal(timestamp, evt.Timestamp);
    }
}
