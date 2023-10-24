using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using NUnit.Framework;
using OpenGauss.NET;

namespace OpenGauss.Tests
{
    [NonParallelizable]
    public class OpenGaussEventSourceTests : TestBase
    {
        [Test]
        public void Command_start_stop()
        {
            using (var conn = OpenConnection())
            {
                // There is a new pool created, which sends a few queries to load pg types
                ClearEvents();
                conn.ExecuteScalar("SELECT 1");
            }

            var commandStart = _events.Single(e => e.EventId == OpenGaussEventSource.CommandStartId);
            Assert.That(commandStart.EventName, Is.EqualTo("CommandStart"));

            var commandStop = _events.Single(e => e.EventId == OpenGaussEventSource.CommandStopId);
            Assert.That(commandStop.EventName, Is.EqualTo("CommandStop"));
        }

        [OneTimeSetUp]
        public void EnableEventSource()
        {
            _listener = new TestEventListener(_events);
            _listener.EnableEvents(OpenGaussSqlEventSource.Log, EventLevel.Informational);
        }

        [OneTimeTearDown]
        public void DisableEventSource()
        {
            _listener.DisableEvents(OpenGaussSqlEventSource.Log);
            _listener.Dispose();
        }

        [SetUp]
        public void ClearEvents() => _events.Clear();

        TestEventListener _listener = null!;

        readonly List<EventWrittenEventArgs> _events = new();

        class TestEventListener : EventListener
        {
            readonly List<EventWrittenEventArgs> _events;
            public TestEventListener(List<EventWrittenEventArgs> events) => _events = events;
            protected override void OnEventWritten(EventWrittenEventArgs eventData) => _events.Add(eventData);
        }
    }
}
