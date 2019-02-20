using System;
using System.Reactive.Subjects;
using FluentAssertions;
using DynamicData.Tests.Domain;
using Microsoft.Reactive.Testing;
using Xunit;

namespace DynamicData.Tests.List
{
    
    public class BatchIfWithTimeOutFixture: IDisposable
    {
        private readonly ISourceList<Person> _source;
        private readonly ChangeSetAggregator<Person> _results;
        private readonly TestScheduler _scheduler;

        private readonly ISubject<bool> _pausingSubject = new Subject<bool>();

        public  BatchIfWithTimeOutFixture()
        {
            _pausingSubject = new Subject<bool>();
            _scheduler = new TestScheduler();
            _source = new SourceList<Person>();
            _results = _source.Connect().BufferIf(_pausingSubject, TimeSpan.FromMinutes(1), _scheduler).AsAggregator();
        }

        public void Dispose()
        {
            _results.Dispose();
            _source.Dispose();
            _pausingSubject.OnCompleted();
        }

        [Fact]
        public void WillApplyTimeout()
        {
            _pausingSubject.OnNext(true);

            //should timeout 
            _scheduler.AdvanceBy(TimeSpan.FromSeconds(61).Ticks);

            _source.Add(new Person("A", 1));

            //go forward an arbitary amount of time
            // _scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
            _results.Messages.Count.Should().Be(2);
        }

        [Fact]
        public void NoResultsWillBeReceivedIfPaused()
        {
            _pausingSubject.OnNext(true);
            //advance otherwise nothing happens
            _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(10).Ticks);

            _source.Add(new Person("A", 1));

            _results.Messages.Count.Should().Be(1);
        }

        [Fact]
        public void ResultsWillBeReceivedIfNotPaused()
        {
            _source.Add(new Person("A", 1));

            //go forward an arbitary amount of time
            _scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
            _results.Messages.Count.Should().Be(2);
        }

        [Fact]
        public void CanToggleSuspendResume()
        {
            _pausingSubject.OnNext(true);
            ////advance otherwise nothing happens
            _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(10).Ticks);

            _source.Add(new Person("A", 1));

            //go forward an arbitary amount of time
            _results.Messages.Count.Should().Be(1);

            _pausingSubject.OnNext(false);
            _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(10).Ticks);

            _source.Add(new Person("B", 1));

            _results.Messages.Count.Should().Be(3);
        }
    }
}
