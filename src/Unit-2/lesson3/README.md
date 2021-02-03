# Akka 중급 2-3: `Scheduler`를 사용한 지연 메시지 보내기
3번째 레슨에 오신 것을 환영합니다!

얼마나 진행했나요? 시스템 메트릭을 그래프로 표시하는 `ChartingActor`와 함께 기본 차트를 설정했습니다. 이 시점에 `ChartingActor`는 실제로 아무것도 그래프로 표시하지 않습니다! 그것을 바꿀 때입니다.

이 레슨에서는 시스템의 다양한 구성 요소를 연결하여 리소스 모니터 애플리케이션이 실제로 시스템 리소스 소비를 차트로 표시하도록 할 것입니다! **이번 레슨이 Unit 2의 핵심입니다. 커피를 마시며 편안하게 즐기십시오!** 

리소스 모니터링앱이 의도한대로 작동하도록 하려면 그래프 데이터에대한 실제 시스템 [Performance Counters](https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.aspx "PerformanceCounter Class - C#")에 `ChartingActor`를 연결해야합니다. 이는 차트가 정기적으로 업데이트되도록 지속적으로 이루어져야합니다. 

Akka.NET이 제공하는 가장 강력한 기능중 하나는 정기적으로 발생하는 메시지를 포함하여 향후 전송될 메시지를 예약하는 기능입니다. 그리고 이것이 바로 `ChartingActor`가 그래프를 정기적으로 업데이트하는데 필요한 기능입니다. 

이 레슨에서는 두 가지 강력한 Akka.NET 개념을 배우게됩니다:

1. `스케줄러(Scheduler)`사용 방법과
2. 액터를 이용한 [게시-구독 패턴](http://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) 구현 방법. 이것은 반응 시스템을 만드는 강력한 기술입니다. 

## Key Concepts / Background
액터가 앞으로 뭔가를 하게하려면 어떻게 해야합니까? 그리고 그 액터가 미래에 반복적으로 무언가 하기를 원한다면 어떨까요? 

아마도 당신은 액터가 주기적으로 정보를 가져 오거나, 시스템 내의 다른 액터에 대한 상태를 가끔 핑(ping)하기를 원할 수 있습니다. 

Akka.NET은 이러한 작업을 수행하기 위한 메커니즘을 제공합니다. 새로운 친한 친구를 만나보세요: `스케줄러(Scheduler)` 입니다. 

### `스케줄러(Scheduler)`란?
`ActorSystem.Scheduler`([docs](http://api.getakka.net/docs/stable/html/FB15E2E6.htm "Akka.NET Stable API Docs - IScheduler interface"))는 모든 ActorSystem 내의 싱글톤으로, 앞으로 액터에게 메시지를 보낼 수 있도록 스케줄을 설정할 수 있습니다. `스케줄러(Scheduler)`는 일회성 메시지와 반복성 메시지를 모두 보낼 수 있습니다.

### `Scheduler`는 어떻게 사용하나요?
앞서 언급했듯이, 액터에게 일회성 또는 반복 메시지를 예약 할 수 있습니다. 

액터에게 메시지를 보내는 대신 앞으로 발생할 `동작(Action)`을 예약 할 수도 있습니다. 

#### `ActorSystem`을 통해`Scheduler`에 액세스 
`Scheduler`는 다음과 같이`ActorSystem`을 통해 액세스해야합니다:

```csharp
// inside Main.cs we have direct handle to the ActorSystem
var system = ActorSystem.Create("MySystem");
system.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(30),
				              someActor,
				              someMessage, ActorRefs.Nobody);

// but inside an actor, we access the ActorSystem via the ActorContext
Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(30),
								             someActor,
								             someMessage, ActorRefs.Nobody);
```

#### `ScheduleTellOnce()`를 사용하여 일회성 메시지 예약 
액터 중 하나에서 30 분 후에 RSS 피드에서 최신 콘텐츠를 가져 오도록하고 싶다고 가정해 보겠습니다. 이를 위해 [`IScheduler.ScheduleTellOnce()`](http://api.getakka.net/docs/stable/html/190E4EB.htm "Akka.NET Stable API Docs - IScheduler.ScheduleTellOnce method")를 사용할 수 있습니다:

```csharp
var system = ActorSystem.Create("MySystem");
var someActor = system.ActorOf<SomeActor>("someActor");
var someMessage = new FetchFeed() {Url = ...};
// schedule the message
system
   .Scheduler
   .ScheduleTellOnce(TimeSpan.FromMinutes(30), // initial delay of 30 min
             someActor, someMessage, ActorRefs.Nobody);
```

짜잔! `someActor`는 30분 후에 `someMessage`를 수신합니다. 

#### `ScheduleTellRepeatedly()`를 사용하여 반복 메시지 예약 
이제, **이 메시지가 *30분마다* 한 번 배달되도록 예약하려면 어떻게해야합니까?** 

이를 위해 다음 [`IScheduler.ScheduleTellRepeatedly()`](http://api.getakka.net/docs/stable/html/A909C289.htm "Akka.NET Stable API Docs - IScheduler.ScheduleTellRepeatedly")를 오버로드해 사용할 수 있습니다. 

```csharp
var system = ActorSystem.Create("MySystem");
var someActor = system.ActorOf<SomeActor>("someActor");
var someMessage = new FetchFeed() {Url = ...};
// schedule recurring message
system
   .Scheduler
   .ScheduleTellRepeatedly(TimeSpan.FromMinutes(30), // initial delay of 30 min
             TimeSpan.FromMinutes(30), // recur every 30 minutes
             someActor, someMessage, ActorRefs.Nobody);
```

이게 답니다!

### 예약된 메시지를 취소하려면 어떻게 하나요? 
예약되거나 반복되는 메시지를 취소해야하는 경우 어떻게됩니까? [`ICancelable`](http://api.getakka.net/docs/stable/html/3FA8058E.htm "Akka.NET Stable API Docs - ICancelable interface")을 사용하여 생성할 수있는 [`Cancelable`](http://api.getakka.net/docs/stable/html/8869EC52.htm) 인스턴스를 사용합니다. 

먼저 메시지를 취소 할 수 있도록 예약해야합니다. 메시지가 취소 가능하면, `ICancelable`에 대한 핸들에서 `Cancel()`을 반드시 호출해야 합니다. 그렇지 않으면 메세지 취소가 전달되지 않습니다. 예를 들어: 

```csharp
var system = ActorSystem.Create("MySystem");
var cancellation = new Cancelable(system.Scheduler);
var someActor = system.ActorOf<SomeActor>("someActor");
var someMessage = new FetchFeed() {Url = ...};

// first, set up the message so that it can be canceled
system
   .Scheduler
   .ScheduleTellRepeatedly(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30)
                 someActor, someMessage, ActorRefs.Nobody,
  			     cancellation); // add cancellation support

// here we actually cancel the message and prevent it from being delivered
cancellation.Cancel();
```

#### 대안: `ScheduleTellRepeatedlyCancelable`을 사용하여 `ICancelable` 작업 가져 오기
Akka.NET v1.0에서 소개한 새로운 `IScheduler` 메서드 중 하나는 [`ScheduleTellRepeatedlyCancelable` 확장 메서드](http://api.getakka.net/docs/stable/html/9B66375D.htm "Akka.NET API Docs - SchedulerExtensions.ScheduleTellRepeatedlyCancelable extension method")입니다. 이 확장 메서드는 반복 메시지에 대한 `ICancelable` 인스턴스를 생성하는 프로세스를 나타내고, 간단히 `ICancelable`을 반환합니다. 

```csharp
var system = ActorSystem.Create("MySystem");
var someActor = system.ActorOf<SomeActor>("someActor");
var someMessage = new FetchFeed() {Url = ...};

// cancellable recurring message send created automatically
var cancellation =  system
   .Scheduler
   .ScheduleTellRepeatedlyCancelable(TimeSpan.FromMinutes(30), 
                 TimeSpan.FromMinutes(30)
                 someActor, 
                 someMessage, 
                 ActorRefs.Nobody);

// here we actually cancel the message and prevent it from being delivered
cancellation.Cancel();
```
이것은 이전 예제에 대한 좀더 간결한 대안이며, 이 부트캠프에서는 사용하지 않더라도 앞으로 사용하는 것이 좋습니다. 

### 예약 된 메시지의 타이밍은 얼마나 정확한가요? 
***예약 된 메시지는 우리가 접한 모든 사용 사례에 대해 충분히 정확합니다.*** 

그렇긴 하지만, 우리가 알고 있는 부정확한 상황이 두 가지가 있습니다:

1. 예약 된 메시지는 CLR 스레드풀에 예약되고 내부적으로 `Task.Delay`를 사용합니다. CLR 스레드풀에 높은로드가있는 경우 작업이 계획보다 조금 늦게 완료 될 수 있습니다. 작업이 예상 한 밀리 초에 정확히 실행된다는 보장은 없습니다. 
2. 스케줄링 요구 사항이 15밀리초 미만의 정밀도를 요구하는 경우 `스케줄러`가 충분히 정확하지 않습니다. Windows, OSX 또는 Linux와 같은 일반적인 운영 체제도 마찬가지입니다. 이는 ~ 15ms가 Windows 및 기타 일반 OS가 시스템 클럭을 업데이트하는 간격("클럭 해상도")이기 때문에, 이러한 OS는 자체 시스템 클럭보다 정확한 타이밍을 지원할 수 없기 때문입니다. 

### 'Schedule'과 'ScheduleOnce'의 다양한 오버로드는 무엇이 있나요? 
다음은 메시지 예약에 사용할 수있는 모든 오버로드 옵션입니다. 

#### `ScheduleTellRepeatedly`의 오버로드
반복 메시지를 예약하기 위해 수행 할 수있는 다양한 API 호출이 있습니다. 

[`IScheduler` API 문서를 참조하세요](http://api.getakka.net/docs/stable/html/FB15E2E6.htm "Akka.NET Stable API Documentation - IScheduler Interface").

#### `ScheduleTellOnce`의 오버로드 
일회성 메시지를 예약하기 위해 만들 수있는 다양한 API 호출이 있습니다.

[`IScheduler` API 문서를 참조하세요](http://api.getakka.net/docs/stable/html/FB15E2E6.htm "Akka.NET Stable API Documentation - IScheduler Interface").

### Akka.NET 액터로 Pub / Sub를 어떻게 수행하나요?
사실 아주 간단합니다. 많은 사람들은 이것이 매우 복잡할 것으로 기대하고 더 많은 코드가 관련되지 않았는지 의심합니다. Akka.NET 액터를 사용하는 pub / sub에 대한 마법은 없습니다. 말 그대로 이렇게 간단 할 수 있습니다:

```csharp
public class PubActor : ReceiveActor
{
  // HashSet automatically eliminates duplicates
  private HashSet<IActorRef> _subscribers;

  PubActor()
  {
    _subscribers = new HashSet<IActorRef>();

    Receive<Subscribe>(sub =>
    {
      _subscribers.Add(sub.IActorRef);
    });

    Receive<MessageSubscribersWant>(message =>
    {
      // notify each subscriber
      foreach (var sub in _subscribers)
      {
        sub.Tell(message);
      }
    });

    Receive<Unsubscribe>(unsub =>
    {
      _subscribers.Remove(unsub.IActorRef);
    });
  }
}
```

Pub/sub는 Akka.NET에서 구현하기에 매우 간단한 것으로, 이에 잘 맞는 시나리오가 있을때 정기적으로 사용할 수 있는 패턴입니다.

이제 'Scheduler'가 작동하는 방식에 익숙해 졌으므로, 이를 사용하여 차트 UI를 반응형으로 만들 수 있습니다. 

## 실습
**주의:** 이번 실습에서 Unit 2의 모든 작업 중 90%가 이루어집니다. `PerformanceCounter` 데이터를 정기적으로 그래프로 표시하기 위해 `ChartingActor`와 pub/sub 관계 설정을 담당하는 몇 명의 새로운 액터를 추가 할 것입니다.

### 1단계: "Add Series" 버튼을 삭제하고 레슨 2에서 만든 핸들러를 클릭하세요. 

그것이 필요하지 않을 것입니다. `Main.cs`의 **[Design]** 뷰에서 **"Add Series" 버튼을 삭제**하고, 클릭 핸들러를 제거합니다:

```csharp
// Main.cs - Main
// DELETE THIS:
private void button1_Click(object sender, EventArgs e)
{
    var series = ChartDataHelper.RandomSeries("FakeSeries" +
        _seriesCounter.GetAndIncrement());
    _chartActor.Tell(new ChartingActor.AddSeries(series));
}
```

### 2단계: `Main.cs`에 3 개의 버튼 추가 

세 개의 버튼과 각각에 대한 클릭 핸들러를 추가 할 것입니다. 

* **CPU (ON)**
* **MEMORY (OFF)**
* **DISK (OFF)**

Visual Studio의 `Main.cs`에 대한 **[디자인]** 뷰는 다음과 같아야합니다:

![Add 3 buttons for tracking different performance counter metrics](images/add-3-buttons.png)

나중에 참조해야하므로 각 버튼에 설명이 포함 된 이름을 지정했는지 확인하세요. Visual Studio의 **속성** 창을 사용하여 각각에 대한 설명이 포함된 이름을 설정할 수 있습니다:

![Set a descriptive name for each button](images/button-properties-window.png)

참조 할 때 각 버튼에 사용할 이름은 다음과 같습니다: 

* **CPU (ON)** - `btnCpu`
* **MEMORY (OFF)** - `btnMemory`
* **DISK (OFF)** - `btnDisk`

버튼의 이름을 변경한 후 **[디자인]** 뷰에서 *버튼을 두 번 클릭하여 각 버튼에 대한 클릭 핸들러를 추가합니다.* 

```csharp
// Main.cs - Main
private void btnCpu_Click(object sender, EventArgs e)
{

}

private void btnMemory_Click(object sender, EventArgs e)
{

}

private void btnDisk_Click(object sender, EventArgs e)
{

}
```

이 핸들러는 나중에 채울 것입니다. 

### 3단계: 새 메시지 유형 추가
잠시 후, 프로젝트에 새로운 액터들을 추가할 것입니다. 그 전에 프로젝트의 `/Actors` 폴더에 새 파일을 만들고, 몇 가지 새로운 메시지 유형을 정의해 보겠습니다:

```csharp
// Actors/ChartingMessages.cs

using Akka.Actor;

namespace ChartApp.Actors
{
    #region Reporting

    /// <summary>
    /// Signal used to indicate that it's time to sample all counters
    /// </summary>
    public class GatherMetrics { }

    /// <summary>
    /// Metric data at the time of sample
    /// </summary>
    public class Metric
    {
        public Metric(string series, float counterValue)
        {
            CounterValue = counterValue;
            Series = series;
        }

        public string Series { get; private set; }

        public float CounterValue { get; private set; }
    }

    #endregion

    #region Performance Counter Management

    /// <summary>
    /// All types of counters supported by this example
    /// </summary>
    public enum CounterType
    {
        Cpu,
        Memory,
        Disk
    }

    /// <summary>
    /// Enables a counter and begins publishing values to <see cref="Subscriber"/>.
    /// </summary>
    public class SubscribeCounter
    {
        public SubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Counter = counter;
        }

        public CounterType Counter { get; private set; }

        public IActorRef Subscriber { get; private set; }
    }

    /// <summary>
    /// Unsubscribes <see cref="Subscriber"/> from receiving updates 
    /// for a given counter
    /// </summary>
    public class UnsubscribeCounter
    {
        public UnsubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Counter = counter;
        }

        public CounterType Counter { get; private set; }

        public IActorRef Subscriber { get; private set; }
    }

    #endregion
}
```

이제 이러한 메시지 정의에 의존하는 액터를 추가 할 수 있습니다. 

### 4단계: `PerformanceCounterActor` 만들기

`PerformanceCounterActor`는 Pub/Sub 와 `Scheduler`를 사용하여 `PerformanceCounter` 값을 `ChartingActor`에 게시 할 액터입니다. 

`/Actors` 폴더에 `PerformanceCounterActor.cs`라는 새 파일을 만들고 다음을 입력합니다:

```csharp
// Actors/PerformanceCounterActor.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for monitoring a specific <see cref="PerformanceCounter"/>
    /// </summary>
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly string _seriesName;
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private PerformanceCounter _counter;

        private readonly HashSet<IActorRef> _subscriptions;
        private readonly ICancelable _cancelPublishing;

        public PerformanceCounterActor(string seriesName,
            Func<PerformanceCounter> performanceCounterGenerator)
        {
            _seriesName = seriesName;
            _performanceCounterGenerator = performanceCounterGenerator;
            _subscriptions = new HashSet<IActorRef>();
            _cancelPublishing = new Cancelable(Context.System.Scheduler);
        }

        #region Actor lifecycle methods

        protected override void PreStart()
        {
            //create a new instance of the performance counter
            _counter = _performanceCounterGenerator();
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250), 
                Self,
                new GatherMetrics(), 
                Self, 
                _cancelPublishing);
        }

        protected override void PostStop()
        {
            try
            {
                //terminate the scheduled task
                _cancelPublishing.Cancel(false);
                _counter.Dispose();
            }
            catch
            {
                //don't care about additional "ObjectDisposed" exceptions
            }
            finally
            {
                base.PostStop();
            }
        }

        #endregion

        protected override void OnReceive(object message)
        {
            if (message is GatherMetrics)
            {
                //publish latest counter value to all subscribers
                var metric = new Metric(_seriesName, _counter.NextValue());
                foreach(var sub in _subscriptions)
                    sub.Tell(metric);
            }
            else if (message is SubscribeCounter)
            {
                // add a subscription for this counter
                // (it's parent's job to filter by counter types)
                var sc = message as SubscribeCounter;
                _subscriptions.Add(sc.Subscriber);
            }
            else if (message is UnsubscribeCounter)
            {
                // remove a subscription from this counter
                var uc = message as UnsubscribeCounter;
                _subscriptions.Remove(uc.Subscriber);
            }
        }
    }
}

```

*다음 단계로 넘어가기 전에, 방금한 일에 대해 이야기해 봅시다...*

#### 신뢰성을 위한 함수형 프로그래밍 
`PerformanceCounterActor`의 생성자에서 `PerformanceCounter`가 아닌 `Func<PerformanceCounter>`를 사용하는 방법을 알고 계셨습니까? 그렇지 않은 경우 돌아가서 지금보십시오. 무엇을 제공합니까? 

이것은 함수형 프로그래밍에서 차용한 기술입니다. 액터의 생성자에 `IDisposable` 객체를 주입해야 할 때마다 사용합니다. 왜냐고요? 

글쎄요, `IDisposable` 객체를 매개 변수로 취하는 액터를 가지고 있습니다. 이 객체가 실제로 어떤 시점에서 `Disposed`가 되어 더이상 사용할 수 없게 될 것이라고 가정 할 것입니다. 

`PerformanceCounterActor`를 다시 시작해야할 필요가 있다면 어떻게 합니까? 

**`PerformanceCounterActor`가 재시작하려 할 때마다 참조 유형을 포함하는 원래 생성자 인수를 재사용합니다**. 이미 `Disposed`된 `PerfomaceCounter`에 대해 같은 참조를 다시 사용하려면 부모 액터가 그 액터를 완전히 kill 하기로 결정할 때까지 반복적으로 오류(crash)가 납니다.

더 좋은 방법은 `PerformanceCounterActor`가 `PerformanceCounter`의 새 인스턴스를 가져 오는데 사용할 수있는 팩토리 함수를 전달하는 것입니다. 이것이 우리가 생성자에 `Func<PerformanceCounter>`를 사용하는 이유입니다. 액터의 `PreStart()` 라이프 사이클 메서드 안에서 호출됩니다. 

```csharp
// create a new instance of the performance counter from factory that was passed in
_counter = _performanceCounterGenerator();
```

`PerformanceCounter`는 `IDisposable`이기 때문에, 액터의 `PostStop` 라이프 사이클 메소드 내에서 `PerformanceCounter` 인스턴스도 정리해야합니다. 

이미 액터가 재시작할 때 카운터의 새로운 인스턴스를 얻을 수 있다는 것을 알고 있으므로, 리소스 낭비를 방지하고 싶습니다. 이것이 우리가 하는 방법입니다:

```csharp
// Actors/PerformanceCounterActor.cs
// prevent resource leaks by disposing of our current PerformanceCounter
protected override void PostStop()
{
    try
    {
        // terminate the scheduled task
        _cancelPublishing.Cancel(false);
        _counter.Dispose();
    }
    catch
    {
        // we don't care about additional "ObjectDisposed" exceptions
    }
    finally
    {
        base.PostStop();
    }
}
```

#### 손쉽게 Pub/Sub 만들기
`PerformanceCounterActor`는 `OnReceive` 메소드 내부에 `SubscribeCounter` 와 `UnsubscribeCounter` 메시지에 대한 핸들러를 통해 pub / sub가 내장되어 있습니다. 

```csharp
// Actors/PerformanceCounterActor.cs
// ...
else if (message is SubscribeCounter)
{
    // add a subscription for this counter (it is up to the parent
    // to filter by counter types)
    var sc = message as SubscribeCounter;
    _subscriptions.Add(sc.Subscriber);
}
else if (message is UnsubscribeCounter)
{
    // remove a subscription from this counter
    var uc = message as UnsubscribeCounter;
    _subscriptions.Remove(uc.Subscriber);
}
```

이 강의에서 `PerformanceCounterActor`에는 구독자가 한 명 (`Main.cs` 내부의 `ChartingActor`) 뿐이지만 약간의 재구성을 통해 이러한 액터가 여러 수신자에게 `PerformanceCounter` 데이터를 게시하도록 할 수 있습니다. 나중에 시도해 볼 만한 자습거리 이지요? ;) 

#### `PerformanceCounter` 데이터 게시를 어떻게 예약 했나요?
`PreStart` 라이프 사이클 메서드 내에서 `Context` 객체를 사용하여 `Scheduler`에 액세스 한 다음 `PerformanceCounterActor`가 250 밀리초 마다 한 번씩 `GatherMetrics` 메서드를 전송하도록 했습니다. 

이로 인해 `PerformanceCounterActor`는 250ms 마다 데이터를 가져와 `ChartingActor`에 게시하여 프레임 속도가 4FPS 인 라이브 그래프를 제공합니다. 

```csharp
// Actors/PerformanceCounterActor.cs
protected override void PreStart()
{
    // create a new instance of the performance counter
    _counter = _performanceCounterGenerator();
    Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMilliseconds(250),
    TimeSpan.FromMilliseconds(250), Self,
        new GatherMetrics(), Self, _cancelPublishing);
}
```

`PerformanceCounterActor`의 `PostStop` 메소드 내에서 이 반복 메시지를 취소하기 위해 생성한 `ICancelable`을 호출합니다. 

```csharp
 // terminate the scheduled task
_cancelPublishing.Cancel();
```

리소스 누수를 제거하고 `IScheduler`가 죽거나 재시작된 액터에게 되풀이 메시지를 보내는 것을 방지하기 위해 `PerformanceCounter`를 `Dispose`하는 것과 같은 이유로 이 작업을 수행합니다. 

### 5단계: `PerformanceCounterCoordinatorActor` 만들기 

`PerformanceCounterCoordinatorActor`는 `ChartingActor`와 모든 `PerformanceCounterActor` 인스턴스 간의 인터페이스입니다. 

다음과 같은 작업이 있습니다:

* 사용자가 요청한 모든 `PerformanceCounterActor` 인스턴스를 느리게 생성합니다. 
* 카운터 생성을 위한 팩토리 메서드(`Func<PerformanceCounter>`)와 함께 `PerformanceCounterActor`를 제공합니다. 
* `ChartingActor`에 대한 모든 카운터 구독 관리 와
* `ChartingActor`에게 각각의 개별 카운터 측정 항목을 렌더링하는 방법(`PerformanceCounter`에 해당하는 각 `Series`에 사용할 색상 및 플롯 유형)을 알려줍니다. 

복잡하게 들리죠? 아마도, 코드 양이 얼마나 작은지 보면 놀랄 것입니다! 

`/Actors` 폴더에 `PerformanceCounterCoordinatorActor.cs`라는 새 파일을 만들고 다음을 입력합니다:

```csharp
// Actors/PerformanceCoordinatorActor.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for translating UI calls into ActorSystem messages
    /// </summary>
    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {
        #region Message types

        /// <summary>
        /// Subscribe the <see cref="ChartingActor"/> to 
        /// updates for <see cref="Counter"/>.
        /// </summary>
        public class Watch
        {
            public Watch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }

        /// <summary>
        /// Unsubscribe the <see cref="ChartingActor"/> to 
        /// updates for <see cref="Counter"/>
        /// </summary>
        public class Unwatch
        {
            public Unwatch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }

        #endregion

        /// <summary>
        /// Methods for generating new instances of all <see cref="PerformanceCounter"/>s
        /// we want to monitor
        /// </summary>
        private static readonly Dictionary<CounterType, Func<PerformanceCounter>>
            CounterGenerators = new Dictionary<CounterType, Func<PerformanceCounter>>()
        {
            {CounterType.Cpu, () => new PerformanceCounter("Processor", 
                "% Processor Time", "_Total", true)},
            {CounterType.Memory, () => new PerformanceCounter("Memory", 
                "% Committed Bytes In Use", true)},
            {CounterType.Disk, () => new PerformanceCounter("LogicalDisk",
                "% Disk Time", "_Total", true)},
        };

        /// <summary>
        /// Methods for creating new <see cref="Series"/> with distinct colors and names
		/// corresponding to each <see cref="PerformanceCounter"/>
        /// </summary>
        private static readonly Dictionary<CounterType, Func<Series>> CounterSeries =
			new Dictionary<CounterType, Func<Series>>()
        {
            {CounterType.Cpu, () =>
			new Series(CounterType.Cpu.ToString()){ 
                 ChartType = SeriesChartType.SplineArea,
                 Color = Color.DarkGreen}},
            {CounterType.Memory, () =>
			new Series(CounterType.Memory.ToString()){ 
                ChartType = SeriesChartType.FastLine,
                Color = Color.MediumBlue}},
            {CounterType.Disk, () =>
			new Series(CounterType.Disk.ToString()){ 
                ChartType = SeriesChartType.SplineArea,
                Color = Color.DarkRed}},
        };

        private Dictionary<CounterType, IActorRef> _counterActors;

        private IActorRef _chartingActor;

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor) :
			this(chartingActor, new Dictionary<CounterType, IActorRef>())
        {
        }

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor,
            Dictionary<CounterType, IActorRef> counterActors)
        {
            _chartingActor = chartingActor;
            _counterActors = counterActors;

            Receive<Watch>(watch =>
            {
                if (!_counterActors.ContainsKey(watch.Counter))
                {
                    // create a child actor to monitor this counter if
                    // one doesn't exist already
                    var counterActor = Context.ActorOf(Props.Create(() =>
						new PerformanceCounterActor(watch.Counter.ToString(),
                                CounterGenerators[watch.Counter])));

                    // add this counter actor to our index
                    _counterActors[watch.Counter] = counterActor;
                }

                // register this series with the ChartingActor
                _chartingActor.Tell(new ChartingActor.AddSeries(
                    CounterSeries[watch.Counter]()));

                // tell the counter actor to begin publishing its
                // statistics to the _chartingActor
                _counterActors[watch.Counter].Tell(new SubscribeCounter(watch.Counter,
                    _chartingActor));
            });

            Receive<Unwatch>(unwatch =>
            {
                if (!_counterActors.ContainsKey(unwatch.Counter))
                {
                    return; // noop
                }

                // unsubscribe the ChartingActor from receiving any more updates
                _counterActors[unwatch.Counter].Tell(new UnsubscribeCounter(
                    unwatch.Counter, _chartingActor));

                // remove this series from the ChartingActor
                _chartingActor.Tell(new ChartingActor.RemoveSeries(
                    unwatch.Counter.ToString()));
            });
        }


    }
}
```
좋아요, 거의 다 왔습니다. 단 하나의 액터만 남았습니다! 

### 6단계: `ButtonToggleActor` 만들기
2단계에서 만든 버튼들을 관리할 액터을 추가하지 않고 그냥 시작하게 놔둘 거라고는 생각지 않으셨죠? ;)

이 단계에서는 `ChartingActor`처럼 UI 스레드에서 실행될 새로운 유형의 액터를 추가할 것입니다. 

`ButtonToggleActor`의 역할은 관리하는 `Button`의 클릭 이벤트를 `PerformanceCounterCoordinatorActor`에 대한 메시지로 바꾸는 것입니다. 또한 `ButtonToggleActor`는 `Button`의 시각적 상태가 `PerformanceCounterCoordinatorActor`가 관리하는 구독 상태를 정확하게 반영하는지 확인합니다 (예 : ON/OFF).

좋습니다. `/Actors` 폴더에 `ButtonToggleActor.cs`라는 새 파일을 만들고 다음을 입력합니다:

```csharp
// Actors/ButtonToggleActor.cs

using System.Windows.Forms;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for managing button toggles
    /// </summary>
    public class ButtonToggleActor : UntypedActor
    {
        #region Message types

        /// <summary>
        /// Toggles this button on or off and sends an appropriate messages
        /// to the <see cref="PerformanceCounterCoordinatorActor"/>
        /// </summary>
        public class Toggle { }

        #endregion

        private readonly CounterType _myCounterType;
        private bool _isToggledOn;
        private readonly Button _myButton;
        private readonly IActorRef _coordinatorActor;

        public ButtonToggleActor(IActorRef coordinatorActor, Button myButton,
				CounterType myCounterType, bool isToggledOn = false)
        {
            _coordinatorActor = coordinatorActor;
            _myButton = myButton;
            _isToggledOn = isToggledOn;
            _myCounterType = myCounterType;
        }

        protected override void OnReceive(object message)
        {
            if (message is Toggle && _isToggledOn)
            {
                // toggle is currently on

                // stop watching this counter
                _coordinatorActor.Tell(
                    new PerformanceCounterCoordinatorActor.Unwatch(_myCounterType));

                FlipToggle();
            }
            else if (message is Toggle && !_isToggledOn)
            {
                // toggle is currently off

                // start watching this counter
                _coordinatorActor.Tell(
                    new PerformanceCounterCoordinatorActor.Watch(_myCounterType));

                FlipToggle();
            }
            else
            {
                Unhandled(message);
            }
        }

        private void FlipToggle()
        {
            // flip the toggle
            _isToggledOn = !_isToggledOn;

            // change the text of the button
            _myButton.Text = string.Format("{0} ({1})",
                _myCounterType.ToString().ToUpperInvariant(),
                _isToggledOn ? "ON" : "OFF");
        }
    }
}
```

### 7단계: `ChartingActor` 업데이트
마지막 구간입니다! 거의 다 왔어요.

3단계에서 정의한 모든 새 메시지 유형을 `ChartingActor`에 통합해야 합니다. 또한 `차트`를 지속적으로 *실시간 업데이트* 할 예정이므로 `차트`를 렌더링하는 방식을 약간 변경해야 합니다. 

시작하려면 `ChartingActor` 클래스 맨 위에 다음 코드를 추가하십시오:

```csharp
// Actors/ChartingActor.cs

/// <summary>
/// Maximum number of points we will allow in a series
/// </summary>
public const int MaxPoints = 250;

/// <summary>
/// Incrementing counter we use to plot along the X-axis
/// </summary>
private int xPosCounter = 0;
```

다음으로, `ChartingActor`가 사용할 새 메시지 유형을 추가합니다. 이것을 `Actors/ChartingActor.cs`의 `Messages` 영역에 추가합니다:

```csharp
// Actors/ChartingActor.cs - inside the Messages region

/// <summary>
/// Remove an existing <see cref="Series"/> from the chart
/// </summary>
public class RemoveSeries
{
    public RemoveSeries(string seriesName)
    {
        SeriesName = seriesName;
    }

    public string SeriesName { get; private set; }
}
```

`ChartingActor` 클래스의 맨 아래에 다음 메서드를 추가합니다(세부 사항에 대해서는 걱정하지 마십시오. 액터와 직접 관련이 없는 UI 관리 코드를 추가합니다):

```csharp
// Actors/ChartingActor.cs

private void SetChartBoundaries()
{
    double maxAxisX, maxAxisY, minAxisX, minAxisY = 0.0d;
    var allPoints = _seriesIndex.Values.SelectMany(series => series.Points).ToList();
    var yValues = allPoints.SelectMany(point => point.YValues).ToList();
    maxAxisX = xPosCounter;
    minAxisX = xPosCounter - MaxPoints;
    maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;
    minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;
    if (allPoints.Count > 2)
    {
        var area = _chart.ChartAreas[0];
        area.AxisX.Minimum = minAxisX;
        area.AxisX.Maximum = maxAxisX;
        area.AxisY.Minimum = minAxisY;
        area.AxisY.Maximum = maxAxisY;
    }
}
```

> **NOTE**: `SetChartBoundaries()`메소드는 시간이 지남에 따라 차트의 시작 부분에서 오래된 점을 제거 할 때 차트의 경계 영역이 업데이트되도록하는 데 사용됩니다. 

다음으로, 새로운 `SetChartBoundaries()`메서드를 사용하도록 모든 메시지 핸들러를 재정의 할 것입니다. 

**`Individual Message Type Handlers`영역** 내의 모든 항목을 삭제 한 후 **다음으로 교체합니다**: 

```csharp
// Actors/ChartingActor.cs - inside the Individual Message Type Handlers region
private void HandleInitialize(InitializeChart ic)
{
    if (ic.InitialSeries != null)
    {
        // swap the two series out
        _seriesIndex = ic.InitialSeries;
    }

    // delete any existing series
    _chart.Series.Clear();

    // set the axes up
    var area = _chart.ChartAreas[0];
    area.AxisX.IntervalType = DateTimeIntervalType.Number;
    area.AxisY.IntervalType = DateTimeIntervalType.Number;

    SetChartBoundaries();

    // attempt to render the initial chart
    if (_seriesIndex.Any())
    {
        foreach (var series in _seriesIndex)
        {
            // force both the chart and the internal index to use the same names
            series.Value.Name = series.Key;
            _chart.Series.Add(series.Value);
        }
    }

    SetChartBoundaries();
}

private void HandleAddSeries(AddSeries series)
{
    if(!string.IsNullOrEmpty(series.Series.Name) &&
        !_seriesIndex.ContainsKey(series.Series.Name))
    {
        _seriesIndex.Add(series.Series.Name, series.Series);
        _chart.Series.Add(series.Series);
        SetChartBoundaries();
    }
}

private void HandleRemoveSeries(RemoveSeries series)
{
    if (!string.IsNullOrEmpty(series.SeriesName) &&
        _seriesIndex.ContainsKey(series.SeriesName))
    {
        var seriesToRemove = _seriesIndex[series.SeriesName];
        _seriesIndex.Remove(series.SeriesName);
        _chart.Series.Remove(seriesToRemove);
        SetChartBoundaries();
    }
}

private void HandleMetrics(Metric metric)
{
    if (!string.IsNullOrEmpty(metric.Series) && 
        _seriesIndex.ContainsKey(metric.Series))
    {
        var series = _seriesIndex[metric.Series];
        series.Points.AddXY(xPosCounter++, metric.CounterValue);
        while(series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
        SetChartBoundaries();
    }
}
```

마지막으로 다음 `Receive<T>` 핸들러를 `ChartingActor`의 생성자에 추가합니다:

```csharp
// Actors/ChartingActor.cs - add these below the original Receive<T>
// handlers in the ctor
Receive<RemoveSeries>(removeSeries => HandleRemoveSeries(removeSeries));
Receive<Metric>(metric => HandleMetrics(metric));
```

### 8단계: `Main.cs`의 `Main_Load` 핸들러 교체 
이제 실시간으로 플로팅하려는 실제 데이터가 있으므로, `ChartActor`에 가짜 데이터를 제공한 원래 `Main_Load` 이벤트 핸들러를 라이브 차트 작성을 위한 실제 데이터로 교체해야 합니다. 

`Main.cs` 내부의 `Main` 클래스 상단에 다음 선언을 추가합니다:

```csharp
// Main.cs - at top of Main class
private IActorRef _coordinatorActor;
private Dictionary<CounterType, IActorRef> _toggleActors = new Dictionary<CounterType,
    IActorRef>();
```

다음으로, `Init` 영역의 `Main_Load` 이벤트 핸들러를 다음과 일치하도록 변경합니다:

```csharp
// Main.cs - replace Main_Load event handler in the Init region
private void Main_Load(object sender, EventArgs e)
{
    _chartActor = Program.ChartActors.ActorOf(Props.Create(() =>
        new ChartingActor(sysChart)), "charting");
    _chartActor.Tell(new ChartingActor.InitializeChart(null)); //no initial series

    _coordinatorActor = Program.ChartActors.ActorOf(Props.Create(() =>
			new PerformanceCounterCoordinatorActor(_chartActor)), "counters");

    // CPU button toggle actor
    _toggleActors[CounterType.Cpu] = Program.ChartActors.ActorOf(
        Props.Create(() => new ButtonToggleActor(_coordinatorActor, btnCpu, 
        CounterType.Cpu, false))
        .WithDispatcher("akka.actor.synchronized-dispatcher"));

    // MEMORY button toggle actor
    _toggleActors[CounterType.Memory] = Program.ChartActors.ActorOf(
       Props.Create(() => new ButtonToggleActor(_coordinatorActor, btnMemory,
        CounterType.Memory, false))
        .WithDispatcher("akka.actor.synchronized-dispatcher"));

    // DISK button toggle actor
    _toggleActors[CounterType.Disk] = Program.ChartActors.ActorOf(
       Props.Create(() => new ButtonToggleActor(_coordinatorActor, btnDisk,
       CounterType.Disk, false))
       .WithDispatcher("akka.actor.synchronized-dispatcher"));

    // Set the CPU toggle to ON so we start getting some data
    _toggleActors[CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
}
```

#### 잠깐만요, `WithDispatcher`? 이 말도 안되는 소리는 뭔가요! 
`Props`에는 액터 배포를 프로그래밍 방식으로 구성할 수있는 유창한 인터페이스가 내장되어 있습니다. 이 인스턴스에서는 `Props.WithDispatcher` 메서드를 사용하여 각 `ButtonToggleActor` 인스턴스가 UI 스레드에서 실행되도록 보장합니다.

레슨 1과 같이 HOCON 구성을 통해 액터에 대한 `Dispatcher`도 구성할 수 있습니다. 그렇다면, HOCON에 'Dispatcher'가 설정되어 있는 액터와  'Props'의 인터페이스를 통해 프로그래밍 방식으로 선언한 액터 가 있다면, 어떤 액터가 이길까요?

*충돌의 경우 `Config`가 이기고 `Props`가 집니다.* `Props` 인터페이스에 의해 선언된 모든 충돌 설정은 항상 구성에 선언된 내용으로 재정의됩니다. 

### 9단계: 버튼 핸들러가 해당 `ButtonToggleActor`에 `Toggle` 메시지를 보내도록 지정 
**마지막 단계 입니다.** 함께 해주셔서 감사합니다. :)

마지막으로 3단계에서 만든 버튼 핸들러를 연결해야 합니다. 

`Main.cs`에서 버튼 핸들러를 연결합니다. 다음과 같이 표시되어야 합니다:

```csharp
// Main.cs - wiring up the button handlers added in step 3
private void btnCpu_Click(object sender, EventArgs e)
{
    _toggleActors[CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
}

private void btnMemory_Click(object sender, EventArgs e)
{
    _toggleActors[CounterType.Memory].Tell(new ButtonToggleActor.Toggle());
}

private void btnDisk_Click(object sender, EventArgs e)
{
    _toggleActors[CounterType.Disk].Tell(new ButtonToggleActor.Toggle());
}
```

### 마치고,
`ChartApp.csproj`를 빌드하고 실행하면 다음이 표시됩니다:

![Successful Lesson 3 Output (animated gif)](images/dothis-successful-run3.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요.](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson3/images/dothis-successful-run3.gif)

작성한 코드와 [Completed](Completed/)의 코드를 비교하며 샘플에 어떤 것이 추가 및 수정되었는지 확인 해봅시다.

## 수고하셨습니다!
*와우*. *정말 많은* 코드였습니다. 잘하셨습니다. 고마워요! 이제 액터로 구현된 완전히 작동하는 리소스 모니터앱이 있습니다. 

다른 모든 강의는 이 강의를 기반으로 진행합니다. 계속하기 전에 코드가 [Completed 폴더](Completed/)의 출력과 일치하는지 확인하세요. 

이 지점에서. `Scheduler`가 작동하는 방식과 Pub-sub와 같은 패턴과 함께 사용하여 비교적 작은 코드양을 가진 액터로 매우 반응적인 시스템을 만드는 방법을 이해해야 합니다. 

작업 시스템에 대한 개괄적인 개요는 다음과 같습니다:

![Akka.NET Bootcamp Unit 2 System Overview](images/system_overview_2_3.png)

**이제 [Akka 중급 2-4 : `BecomeStacked` 와 `UnbecomeStacked`를 사용하여 런타임에 액터 동작 전환](../lesson4/README.md)으로 넘어갑시다.**
