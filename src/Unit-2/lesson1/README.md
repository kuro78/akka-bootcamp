# Akka 중급 2-1 : HOCON Configuration을 사용하여 Akka.NET 구성
Unit 2에서 대부분의 시간을 차트의 모든 데이터를 실제로 플로팅하는 역할을 담당하는 액터인 `ChartingActor`와 함께 작업 할 것입니다:

![Pretty output](../lesson5/images/syncharting-complete-output.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요.](https://github.com/petabridge/akka-bootcamp/blob/master/src/Unit-2/lesson5/images/syncharting-complete-output.gif).

하지만 지금 당장 Unit 2의 ([/DoThis/ 폴더](../DoThi /) 안에 있는) `ChartApp.csproj`를 빌드하고 실행하려고 하면 다음과 같은 출력이 표시됩니다:

![No output?](images/dothis-failed-run.png)

뭐, 별로 신나지 않네요. Unit 2에서 실시간 데이터 시각화 애플리케이션을 구축해야 하는거 아닌가요? 왜 그러고 있어요?

잠깐만요, **디버그** 창에 예외가 있네요. 뭐라고 써 있는데요?

> [ERROR][2/24/2015 11-48-34 AM][Thread 0010][akka://ChartActors/user/charting] Cross-thread operation not valid: Control 'sysChart' accessed from a thread other than the thread it was created on.

원인 : System.InvalidOperationException : 크로스 스레드 작업이 유효하지 않음 : 컨트롤 'sysChart'가 생성 된 스레드가 아닌 다른 스레드에서 액세스되었습니다.

### 무슨 문제가 있나요?
차트로 표시하려는 이벤트가 그래프로 표시되지 않습니다. 음 ... 이벤트가 UI 스레드에 도달하지 않는 것 같습니다. 차트가 업데이트되도록 이벤트를 전달하고 UI 스레드와 동기화하는 방법을 찾아야합니다!

UI 스레드와 수동으로 동기화하려면 `ChartingActor`를 일부 악성 코드로 다시 작성해야 합니까?

아니에요! 긴장을 푸세요.

**`ChartingActor`를 정의하는 코드를 업데이트하지 않고도 [Akka.NET의 HOCON configuration](https://getakka.net/articles/concepts/configuration.html#what-is-hocon)을 사용하여이 문제를 해결할 수 있습니다.**

하지만 먼저 `Dispatcher`를 이해해야 합니다.

## Key Concepts / Background
### `Dispatcher`
#### `Dispatcher`란?
`Dispatcher`는 액터의 Mailbox에서 액터 인스턴스 자체로 메시지를 푸시하는 스택의 일종입니다. 즉,`Dispatcher`는 액터의 `OnReceive()`메소드로 메시지를 푸시하는 것입니다. 주어진 `Dispatcher`를 공유하는 모든 액터는 병렬 실행을 위해 `Dispatcher`의 스레드도 공유합니다.

Akka.NET의 기본 디스패처는 `ThreadPoolDispatcher`입니다. 짐작할 수 있듯이, 이 디스패처는 CLR `ThreadPool`을 기반으로 모든 액터를 실행합니다.

#### 어떤 종류의 `Dispatcher`가 있나요?
액터와 함께 사용할 수있는 여러 유형의`Dispatcher`가 있습니다:

##### `SingleThreadDispatcher`
이 `Dispatcher`는 단일 스레드에서 여러 액터를 실행합니다.

##### `ThreadPoolDispatcher` (default)
이 `Dispatcher`는 최대 동시성을 위해 CLR `ThreadPool` 위에서 액터를 실행합니다.

##### `SynchronizedDispatcher`
이 `Dispatcher`는 모든 액터 메시지가 호출자(Sender)와 동일한 동기화 컨텍스트에서 처리되도록 예약합니다. 99%의 경우 여기에서 클라이언트 애플리케이션과 같이 UI 스레드에 액세스해야하는 액터를 실행합니다.

`SynchronizedDispatcher`는 *현재* [SynchronizationContext](https://msdn.microsoft.com/en-us/magazine/gg598924.aspx)를 사용하여 실행을 예약합니다.

> **Note:** 일반적으로`SynchronizedDispatcher`에서 실행되는 액터는 많은 작업을 수행하지 않아야합니다. 다른 풀에서 실행중인 액터가 수행 할 수있는 추가 작업을 수행하지 마십시오.

이 레슨에서는 `SynchronizedDispatcher`를 사용하여 `ChartingActor`가 WinForms 애플리케이션의 UI 스레드에서 실행되도록 할 것입니다. 이렇게하면 `ChartingActor`는 크로스 스레드 마샬링(cross-thread marshalling) 없이 원하는 모든 UI 요소를 업데이트 할 수 있습니다. 액터의 `Dispatcher`가 자동으로 처리 할 수 있습니다!

##### [`ForkJoinDispatcher`](http://api.getakka.net/docs/stable/html/F0DC1571.htm "Akka.NET Stable API Docs - ForkJoinDispatcher")
이 `Dispatcher`는 조정 가능한 동시성을 위해 전용 스레드 그룹에서 액터를 실행합니다.

실행을 위해 전용 스레드가 필요한 액터(격리 보장이 필요한)를 위한 것입니다. 이것은 주로 `Sytem`액터가 사용하므로 많이 건드리지 않을 것입니다.

#### UI 스레드에서 액터를 실행하는 것이 나쁜 생각인가요?
짧게 말해서 "아니오" 입니다.

UI 스레드에서 액터를 실행하는 것은 해당 액터가 디스크 또는 네트워크 I/O와 같은 장기 실행 작업을 수행하지 않는 한 괜찮습니다. 사실, *UI 스레드에서 액터를 실행하는 것은 UI 이벤트 및 업데이트를 처리하는데 현명한 작업입니다*.

왜냐구요? *UI 스레드에서 액터를 실행하면 일반적인 동기화 문제*가 모두 제거되기 되는데, 그렇지 않으면 다중 스레드 WPF 또는 WinForms 앱에서해야 할 일이 있습니다.

> **기억하세요: [Akka.NET 액터는 게으르다](http://petabridge.com/blog/akkadotnet-what-is-an-actor/)**. 메시지를받지 않을 때는 아무 작업도하지 않습니다. 비활성 상태에서는 리소스를 소비하지 않습니다.

#### `Dispatcher`는 깨진 차트와 어떤 관련이 있나요?
앞에서 본 것처럼 그래프를 수행하는 액터(`ChartingActor`)가 이벤트를 UI 스레드와 동기화하지 않기 때문에 차트가 업데이트되지 않습니다.

문제를 해결하기 위해 우리가해야 할 일은 `ChartingActor`를 `CurrentSynchronizationContextDispatcher`를 사용하도록 변경하는 것 뿐입니다. 그러면 UI 스레드에서 자동으로 실행됩니다!

그러나: 우리는 실제 액터 코드를 건드리지 않고 이것을 하고 싶습니다. 액터 자체를 수정하지 않고 `CurrentSynchronizationContextDispatcher`를 사용하도록 `ChartingActor`를 배포하려면 어떻게해야합니까?

HOCON을 만날 시간입니다.

### HOCON
Akka.NET은 HOCON이라는 구성 형식을 활용하여, 원하는 세분화 수준으로 Akka.NET 응용 프로그램을 구성 할 수 있습니다.

#### HOCON이란?
[HOCON (Human-Optimized Config Object Notation)](https://getakka.net/articles/concepts/configuration.html#what-is-hocon)은 유연하고 확장 가능한 구성 형식입니다. 이를 통해 Akka.NET의 `IActorRefProvider`를 구현, 로깅, 네트워크 전송, 그리고 더 일반적으로 개별 액터가 배포되는 방식에서 모든 것을 구성 할 수 있습니다.

HOCON에서 반환하는 값은 유형이 강력합니다(`int`, `Timespan` 등을 가져올 수 있습니다).

#### HOCON으로 무엇을 할 수 있나요?
HOCON은 App.config 와 Web.config의 읽기 어려운 XML 내에 쉽게 읽을 수있는 구성을 포함 할 수 있습니다. HOCON은 섹션 경로로 구성을 쿼리할 수 있으며 해당 섹션은 애플리케이션 내에서 사용할 수있는 강력한 형식과 구문 분석된 값을 노출합니다.

HOCON은 구성 섹션을 중첩 하거나 아니면 연결하여 세분화 계층을 생성하고 의미상 네임스페이스 구성을 제공 할 수 있습니다.

#### HOCON은 일반적으로 어디에 사용되나요?
HOCON은 일반적으로 로깅 설정을 조정하고, 특수 모듈(예: `Akka.Remote`)을 활성화 하거나, 이 레슨에서 `ChartingActor`에 대한 `Dispatcher`와 같은 배포를 구성하는 데 사용됩니다.

예를 들어 HOCON으로 `ActorSystem`을 구성 해 보겠습니다:

```csharp
var config = ConfigurationFactory.ParseString(@"
akka.remote.helios.tcp {
    transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
    transport-protocol = tcp
    port = 8091
    hostname = ""127.0.0.1""
}");

var system = ActorSystem.Create("MyActorSystem", config);
```

예제에서 볼 수 있듯이, `ConfigurationFactory.ParseString` 메소드를 사용하여 `string`에서 HOCON `Config` 객체를 구문 분석 할 수 있습니다. 일단 `Config` 객체가 있으면 `ActorSystem.Create` 메소드 내의 `ActorSystem`에 이를 전달할 수 있습니다.

> NOTE: 이 예에서 우리는 Unit 2에서 다루는 개념을 훨씬 뛰어 넘는 개념 인 `Akka.Remote`와 함께 사용할 특정 네트워크 전송을 구성했습니다. 지금은 세부 사항에 대해 걱정하지 마십시오.

#### "배포(Deployment)"? 그건 뭔가요?
배포(Deployment)는 모호한 개념이지만 HOCON과 밀접한 관련이 있습니다. 액터는 인스턴스화되어 `ActorSystem` 어딘가에 서비스 될 때 "배포(deployed)"됩니다.

액터가 `ActorSystem` 내에서 인스턴스화되면 로컬 프로세스 내부 또는 다른 프로세스 (이것이 `Akka.Remote`가하는 일)의 두 위치 중 하나에 배치 될 수 있습니다.

액터가 `ActorSystem`에 의해 배포되면 다양한 구성 설정이 있습니다. 이 설정은 액터에 대한 광범위한 동작 옵션을 제어합니다. 예를 들면 다음과 같습니다. 이 액터가 라우터가 될까요? 어떤 `Dispatcher`를 사용합니까? 어떤 유형의 Mailbox가 있습니까? (이 개념에 대해서는 이후 레슨에서 자세히 설명합니다.)

모든 옵션의 의미를 살펴 보지는 않았지만 *지금 알아야 할 핵심 사항은 액터를 서비스에 배포하기 위해 `ActorSystem`이 사용하는 설정을 HOCON 내에서 설정할 수 있다는 것입니다.*

***이것은 실제로 액터 코드 자체를 건드리지 않고도 액터의 동작을 극적으로 (이러한 설정을 변경하여) 변경할 수 있음을 의미합니다.***

유연한 구성, 승리를 위하여!

#### HOCON은`App.config` 와 `Web.config` 내에서 사용할 수 있습니다

> **NOTE**
> App.config 와 Web.config는 .NET core에서 더 이상 사용되지 않는다는 것을 알고 있습니다. 독립 실행형 HOCON 3.0이 출시되면 HOCON 구성을 선언하는 새로운 방법으로 이 샘플을 업데이트 할 것입니다.

`string`에서 HOCON을 구문 분석하는 것은 작은 구성 섹션에 편리하지만 [`App.config` 와 `Web.config`에 대한 구성 변환](https://msdn.microsoft.com/en-us/library/dd465326.aspx) 및 기타 모든 유용한 도구를 활용하려면 어떻게해야합니까? `System.Configuration` 네임스페이스에 있습니까?

결과적으로 이러한 구성 파일 내에서도 HOCON을 사용할 수 있습니다!

다음은`App.config` 내에서 HOCON을 사용하는 예입니다:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <configSections>
    <section name="akka"
             type="Akka.Configuration.Hocon.AkkaConfigurationSection, Akka" />
  </configSections>

  <akka>
    <hocon>
      <![CDATA[
          akka {
            # here we are configuring log levels
            log-config-on-start = off
            stdout-loglevel = INFO
            loglevel = ERROR
            # this config section will be referenced as akka.actor
            actor {
              provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
              debug {
                  receive = on
                  autoreceive = on
                  lifecycle = on
                  event-stream = on
                  unhandled = on
              }
            }
            # here we're configuring the Akka.Remote module
            remote {
              helios.tcp {
                  transport-class =
            "Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote"
                  #applied-adapters = []
                  transport-protocol = tcp
                  port = 8091
                  hostname = "127.0.0.1"
              }
            log-remote-lifecycle-events = INFO
          }
      ]]>
    </hocon>
  </akka>
</configuration>
```

그리고 다음 코드를 통해 이 구성 섹션을 `ActorSystem`에 로드 할 수 있습니다:

```csharp
var system = ActorSystem.Create("Mysystem");
// Loads section.AkkaConfig from App or Web.config automatically
// FYI, section.AkkaConfig is built into Akka.NET for you
```

#### HOCON Configuration은 폴백을 지원합니다.
Unit 2에서 명시적으로 활용하는 개념은 아니지만 많은 프로덕션 사용 사례에서 유용하게 사용되는 `Config` 클래스의 강력한 특성입니다.

HOCON은 "대체(fallback)" 구성 개념을 지원합니다. 이 개념을 시각적으로 설명하는 것이 가장 쉽습니다.

![Normal HOCON Config Behavior](images/hocon-config-normally.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요.](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson1/images/hocon-config-normally.gif)

위의 다이어그램과 같은 것을 만들려면 다음과 같은 구문을 사용하여 세 개의 폴백이 연결된 `Config` 객체를 만들어야합니다:

```csharp
var f0 = ConfigurationFactory.ParseString("a = bar");
var f1 = ConfigurationFactory.ParseString("b = biz");
var f2 = ConfigurationFactory.ParseString("c = baz");
var f3 = ConfigurationFactory.ParseString("a = foo");

var yourConfig = f0.WithFallback(f1)
				   .WithFallback(f2)
				   .WithFallback(f3);
```

키가 "a"인 HOCON 개체의 값을 요청하는 경우 다음 코드를 사용합니다:

```csharp
var a = yourConfig.GetString("a");
```

그러면 내부 HOCON 엔진은 키 `a`에 대한 정의를 포함하는 첫 번째 HOCON 파일과 일치합니다. 이 경우 `f0`은 "bar"값을 반환합니다.

####  "foo"가 "a"의 값으로 반환되지 않은 이유는 무엇인가요?
HOCON은 `Config` 체인에서 일치하는 항목이 이전에 발견되지 않은 경우에만 대체 `Config` 객체를 검색하기 때문입니다. 최상위 `Config` 객체에 `a`와 일치하는 항목이 있으면 대체 항목이 검색되지 않습니다. 이 경우 `f0`에서 `a`에 대한 일치가 발견되었으므로 `f3`의 `a = foo`에는 도달하지 않았습니다.

#### HOCON 키 미스가 발생하면 어떻게되나요?
`c`가 `f0`또는 `f1`에 정의되어 있지 않은 경우 다음 코드를 실행하면 어떻게됩니까?

```csharp
var c = yourConfig.GetString("c");
```

![대체 HOCON 구성 동작](images/hocon-config-fallbacks.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요.](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson1/images/hocon-config-fallbacks.gif).

이 경우 `yourConfig`는 `f2`까지 두 번 폴백되고 `c` 키의 값으로 "baz"를 반환합니다.

이제 HOCON을 이해 했으니, 이것을 사용하여 `ChartingActor`의 `Dispatcher`를 수정 해 보겠습니다!

## 실습
UI 스레드에서 차트가 올바르게 작동하도록 하려면 `SynchronizedDispatcher`를 사용하도록 `ChartingActor`를 구성해야합니다.

### `App.config`에 Akka.NET 구성 섹션 추가
가장 먼저해야 할 일은`App.config`의 맨 위에 `AkkaConfigurationSection`을 선언하는 것입니다:

```xml
<!-- in App.config file -->
<!-- add this right after the opening <configuration> tag -->
<configSections>
 <section name="akka" type="Akka.Configuration.Hocon.AkkaConfigurationSection, Akka" />
</configSections>
```

다음으로, `AkkaConfigurationSection`의 내용을 `App.config`에 추가합니다:

```xml
<!-- in App.config file -->
<!-- add this anywhere after <configSections> -->
<akka>
  <hocon>
    <![CDATA[
        akka {
          actor {
            deployment {
              # this nested section will be accessed by akka.actor.deployment
              # used to configure our ChartingActor
              /charting {
				 # causes ChartingActor to run on the UI thread for WinForms
                dispatcher = akka.actor.synchronized-dispatcher
              }
            }
          }
        }
    ]]>
  </hocon>
</akka>
```


`akka.actor.synchronized-dispatcher`는 `CurrentSynchronizationContextDispatcher`에 대한 Akka.NET의 기본 구성에 내장된 축약형 이름이라는 점을 지적해야합니다. 따라서 정규화된 형식 이름을 사용할 필요가 없습니다.

또한 `ChartingActor`와 관련된 구성 섹션이 `/charting`으로 선언된 것을 눈치 채셨을 것입니다. **이것은 액터 배포가 액터 유형이 아닌 액터의 경로와 이름에 의해 수행되기 때문입니다**.

다음은 `Main.cs` 내에 `ChartingActor`를 만드는 방법입니다:

```csharp
 _chartActor = Program.ChartActors.ActorOf(Props.Create(() =>
  new ChartingActor(sysChart)), "charting");
```

`ActorSystem.ActorOf`를 호출하면 `ActorOf` 메소드는 이 액터의 경로에 해당하는 `akka.actor.deployment` 구성 섹션에서 선언된 모든 배포를 자동으로 찾습니다. 이 경우, 액터의 경로는`/user/charting`이며, 위 구성 섹션의 `/charting`에 대한 `akka.actor.deployment` 값에 해당합니다.

> Akka.NET 최종 사용자는 `/user/` 계층 구조 내에서 생성된 액터에 대한 배포 설정만 지정할 수 있습니다. 따라서 배포 설정을 선언 할 때 `/user`를 지정할 필요가 없습니다. **암시적**입니다.
>
> 확장으로 `/system` 액터를 배포하는 방법도 지정할 수 없습니다. 이것은 `ActorSystem`에 달려 있습니다.

그리고... 끝났습니다!

### 마치고,
`ChartApp.csproj`를 빌드하고 실행하면 다음이 표시됩니다:

![Successful Lesson 1 Output](images/dothis-successful-run.png)

작성한 코드와 [Completed](Completed/)의 코드를 비교하며 샘플에 어떤 것이 추가 및 수정되었는지 확인 해봅시다.

## 수고하셨습니다!
Unit 2의 첫 번째 수업을 완료했습니다! 우리는 많은 개념을 다루었으며 Akka.NET의 구성 모델이 실제로 얼마나 강력한지에 대한 감사 표시와 함께 이 문제에서 벗어나길 바랍니다.

이제 [Akka 중급 2-2 : 더 나은 메시지 처리를 위해 `ReceiveActor` 사용](../lesson2/README.md)을 향해 나아가 봅시다.


## 추가 읽기
위의 HOCON 구성을 읽으면서 짐작 하셨겠지만, 앞에 `#`이 있는 줄은 HOCON에서 주석으로 처리됩니다. [HOCON 구문에 대해 자세히 알아보기](https://getakka.net/articles/concepts/configuration.html#what-is-hocon)