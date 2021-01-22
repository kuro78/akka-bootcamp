# Akka 시작하기 1-4 : Child Actors, Actor Hierarchies, and Supervision
이번 레슨은 코드베이스의 기능과 액터 모델이 어떻게 작동하는지 이해하는 데 있어 큰 도움을 줄 것입니다.
이번 레슨이 지금까지 중 가장 어려운 수업이니, 바로 시작합니다!

## Key concepts / background
액터 계층 구조에 대해 깊게 들어가기 전에 알아봅시다: 왜 우리는 계층 구조가 필요할까요?

계층 구조를 사용하는 두 가지 중요한 키 포인트가 있습니다:
1. 작업을 원자화하고 대용량 데이터를 처리하기 쉬운 양으로 변환하기 위해
2. 에러를 억제하고 시스템을 회복력 있게 만들기 위해

### 계층 구조의 원자화 작업
계층 구조를 가지는 것은 아주 작은 조각으로 작업을 쪼개고, 서로 다른 계층의 레벨에서 다른 전문 기술을 활용할 수 있게 합니다.

액터시스템에서 이것이 실현되는 일반적인 방법은 큰 데이터 스트림을 원자화 하는 것입니다. 그것이 작고 쉽게 처리할 만한 코드 조각이 될 때까지 계속 원자화를 반복합니다.

트위터를 예로 들어봅시다(JVM Akka의 사용자들). Akka를 사용하면 그들의 대용량 데이터 수집을 작고, 처리하기 쉬운 정보의 으름으로 분해할 수 있습니다.

트위터의 경우에는 거대한 소방호스를 통해 뿜어져 나오는 것 같은 트윗들을 각 유저들의 타임라인에 작은 물줄기 하나하나로 나누어 분산할 수 있고, Akka를 사용하여 해당 유저의 스트림으로 도착한 메시지를 websoket 등을 통해 푸시할 수도 있습니다. 

### 계층 구조를 통해 복원력 있는 시스템 지원
계층 구조를 사용함으로써 위험의 계층화와 전문화가 가능합니다.

군대가 어떻게 일하는지를 생각해 봅시다. 군대에서 장군은 전략을 세우고 감독하지만, 보통 가장 큰 전투의 최전선에 서지는 않습니다. 그러나 폭넓은 영향력을 가지고 모든 것을 지휘합니다. 동시에, 낮은 계급의 병사들은 최전선에 서서 그들이 받은 명령과 위험한 작전을 수행합니다.

이것은 액터시스템의 동작과 정확히 일치합니다.

상위 계층의 액터는 본질적으로 좀더 감독적(supervisional)며, 이러한 점은 액터시스템의 리스크를 낮추고 가장자리로 밀어냅니다. 계층의 가장자리로 위험한 작업을 밀어냄으로써 시스템은 리스크를 고립시키고 전체 시스템의 크래싱 없이 오류로부터 복구할 수 있습니다.

이 두 개념이 모두 중요하지만, 이번 레슨의 나머지 부분에서는 액터시스템이 계층 구조를 사용해서 복원력을 발휘할 방법에 중점을 두겠습니다.

어떻게 이룰거냐고요? **감시(Supervision) 입니다**.

### 감시(Supervision)란? 왜 신경써야 하나?
감시(Supervision)는 액터 시스템이 오류(failures)에서 빠르고 신속하게 격리와 회복이 이뤄질 수 있게 하는 기본 개념입니다. 

모든 액터는 자신을 감시(supervision)하는 다른 액터를 가지고 있으며, 이 액터들은 에러가 발생했을 때 복구할 수 있도록 돕습니다. 이는 계층 구조의 최상단부터 최하단까지 모두에게 해당합니다.

이 감시(Supervision)는 애플리케이션이 예기치 않은 오류(unhandled exception, network timeout 등)가 발생할 경우, 액터 계층 구조에서 해당 계층에만 영향을 받도록 제어합니다.

모든 다른 액터들은 아무일도 없었던 것처럼 동작할 것입니다. 우리는 이것을 "오류 고립(failure isolation)", "봉쇄(containment)"라고 부릅니다.

어떻게 이게 이루어 질까요? 지금부터 알아봅시다.

### 액터의 계층 구조
첫 번째 키포인트: 모든 액터는 부모 액터를 가지고 있습니다. 그리고 일부 액터들은 자식을 가지고 있습니다. 부모 액터는 자식을 감독(supervise) 합니다.

부모 액터가 자식 액터를 감독한다는 것은 모든 **액터가 감독자(supervisor)를 가진다는 것이고, 모든 액터가 감독자(supervisor)가 될 수 있음을 말합니다.**

액터시스템 내에서 액터는 계층 구조로 정리됩니다. `ActorSystem`자체에 직접 보고하는 "최상위" 액터와 다른 액터에게 보고하는 "자식(child)" 액터가 있음을 의미합니다.

전반적인 계층 구조는 다음과 같습니다(잠시 후 하나씩 알아보겠습니다.):
![Petabridge Akka.NET Bootcamp Lesson 1.3 Actor Hierarchies](Images/hierarchy_overview.png)

### 계층 구조의 레벨이란?
#### 모든 것의 기본: "Guardians"
"Guardians"는 전체 시스템의 root 액터 입니다.

계층 구조에서 최상단에 있는 이 3개의 액터를 말합니다:
![Petabridge Akka.NET Bootcamp Lesson 1.3 Actor Hierarchies](Images/guardians.png)

##### `/` 액터
`/` 액터는 액터시스템 전제의 root 액터입니다. "The Root Guardian" 이라고 불립니다. 이 액터는 다른 "가디언(Guardinas)"(`/system`과 `/user` 액터)을 감독(supervises)합니다.

이 하나를 제외한 모든 액터는 그들의 부모 액터를 필요로 합니다. 이 액터는 일반 액터시스템의 바깥("out of the bubble")에 있기 때문에 때때로 "bubble-walker"라고 불립니다.

##### `/system` 액터
`/system` 액터는 "시스템 가디언(The Sytem Guardian)"이라고 불립니다. 이 액터의 주 역할은 정상적인 방법으로 시스템이 종료되고, 프레임워크 레벨의 기능 및 유틸리티(logging 등)로 구현된 다른 시스템 액터를 유지하고 감독합니다. `시스템 가디언(system guardian)`과 시스템 액터 계층 구조에 대해서는 다음에 포스팅 하겠습니다.

##### `/user` 액터
여기가 바로 파티가 시작되는 곳입니다! 당신이 개발자로서 모든 시간을 보내게 될 곳입니다.

`/user`액터는 "가디언 액터(The Guardian Actor)"로 불립니다. 사용자 관점에서 볼 때 `/user` 액터는 시스템의 root이기 때문에 "root actor" 라고 부릅니다.

> 일반적으로 "root actor"는 `/user`액터를 말합니다.

사용자는 `가디언(Guardians)`에 대해 걱정할 필요가 없습니다. 어떠한 예외도 `가디언(Guardians)`까지 거품을 일으켜 전체 시스템에 크래시가 발생하지 않도록 `/user`아래에서 적절하게 감독됩니다.

#### `/user` 액터의 계층 구조
액터 계층 구조의 주요 포인트입니다: 애플리케이션 내에서 당신이 정의한 모든 액터들이 속합니다.
![Akka: User actor hierarchy](Images/user_actors.png)

> `/user` 액터 바로 아래의 자식 액터를 "최상위 액터(top level actors)"라고 부릅니다.

액터는 항상 다른 액터의 자식 액터로 만들어집니다.

액터시스템 자체의 컨텍스트(context)를 활용해 직접 액터를 만들면, 새 액터는 최상위 액터(top level actor)가 됩니다:
```cs
// 다이어그램에 표현된 최상위 액터를 만듭니다.
IActorRef a1 = MyActorSystem.ActorOf(Props.Create<BasicActor>(), "a1");
IActorRef a2 = MyActorSystem.ActorOf(Props.Create<BasicActor>(), "a2");
```

`a2` 컨텍스트 안쪽에서 `a2`의 자식 액터들을 만들어 봅니다:
```cs
// a2의 자식 액터들을 만듭니다.
// a2의 내부에서 진행합니다.
IActorRef b1 = Context.ActorOf(Props.Create<BasicActor>(), "b1");
IActorRef b2 = Context.ActorOf(Props.Create<BasicActor>(), "b2");
```

#### 액터 주소(Actor Path) == 계층 구조 내에서 액터의 위치
모든 액터는 주소를 가집니다. 액터에서 다른 액터로 메시지를 보내려면, 주소("Actor Path")를 알아야 합니다. 완전한 액터 주소(Actor Path)는 다음과 같습니다:

![Akka.NET actor address and path](Images/actor_path.png)

> "Path"는 액터가 당신의 액터 계층 구조에서 어디에 위치하는지 알려줍니다. 슬래시('/')로 액터의 계층을 구분합니다.

만일 `localhost`에서 실행했다면, `b2` 액터의 완전한 주소는 `akka.tcp://MyActorSystem@localhost:9001/user/a2/b2` 입니다.

"내 액터 클래스가 꼭 계층 구조의 특정 위치에 있어야 하나요?" 라는 질문이 많습니다. 예를 들어, 내가 만든 `FooActor` 액터 클래스를 `BarActor`의 자식 액터로만 배포(deploy) 할 수 있을까요? 아니면 어디든 가능할까요?

이에 대한 대답은 **어떤 액터든 당신의 액터 계층 구조 안에서 어디에나 위치할 수 있습니다.**

> 어떤 액터든 당신의 액터 계층 구조 안에서 어디에나 위치할 수 있습니다.

자, 감독하는 것 같은 흥미로운 일을 해봅시다!

### 액터 계층 구조에서 감시(supervision)가 동작하는 방법
이제 액터들의 구성을 알게 되었습니다: 액터는 자식 액터를 감독(supervise)합니다. 하지만, 그들은 액터 계층 구조에서 바로 아래 단계만 감독합니다. (액터는 손자, 증손자 등을 감독하지 않습니다.)

> 액터는 계층 구조상 바로 아래 단계의 자식만 감독(supervise)합니다.

#### 언제 감시(supervison)가 시작되나요? 에러가 발생하면!
뭔가 잘못되었을 때, 그 때입니다. 자식 액터가 `unhandled exception`이나 크래시가 발생할 때, 부모 액터에게 도움을 청하고 무엇을 해야 하는지 알려줍니다.

구체적으로,  자식 액터는 `Failure` 클래스의 메시지를 부모 액터에게 보냅니다. 무엇을 해야할지 결정하는 것은 부모 액터에게 달려있습니다.

#### 부모 액터는 어떻게 오류를 해결할 수 있을까?
오류 해결 방법을 결정하는 두 가지 요소가 있습니다:

1. 자식 액터의 실패 요인 (자식 액터가 보낸 `Failure` 메시지에 어떤 타입의 `Exception`이 포함되었는가)
2. 자식 액터의 `Failure`에 대한 부모 액터가 실행하는 명령. 이는 부모 액터의 `감시 전략(SupervisionStrategy)`에 의해 결정됩니다.

##### 오류가 발생했을 때 이벤트의 순서:
1. 자식 액터(c1)에서 `Unhandled exception`이 발생하면, 부모 액터(b1)에 의해 관리(supervised by) 됩니다.
2. `c1`은 작업을 중지합니다.
3. 시스템은 `Failure` 메시지에 `Exception`을 담아 `c1`에서 `b1`으로 전달합니다.
4. `b1`은 `c1`에게 어떻게 해야하는 지시합니다.
5. 삶은 계속되고, 시스템의 영향 받은 부분은 집 전체를 태우지 않고 스스로 치유됩니다. 고양이와 유니콘이 푹신한 무지개 위에서 휴식을 취하며 즐길 수 있는 아이스크림과 커피를 무료로 나누어 주고 있습니다. 야호!

##### 감시 지침(Supervision directives)
자식 액터에게서 에러를 받으면, 부모 액터는 다음 동작 중 하나를 수행 할 수 있습니다("지침(directives)"). 감시 전략(supervision strategy)은 예외 타입에 따라 침을 매핑하므로, 여러 에러의 유형에 따라 적절하게 처리 할 수 있습니다.

감시 지침(supervsion directives)의 종류 (감독자가 할 수 있는 결정):
- **Restart** the child (default): 가장 일반적인 경우이며 기본값 입니다.
- **Stop** the chid: 자식 액터를 영구히 종료합니다.
- **Escalate** the error(and stop itself): 이건 부모 액터가 "뭘 해야 할지 모르겠어! 다 멈추고 '내' 부모 액터에게 물어봐야 겠어!" 라고 말하는 겁니다.
- **Resume** processing (ignores the error): 일반적으로는 사용하지 않습니다. 일단 무시합니다.

> 여기서 알아야 할 중요한 것은 **부모 액터에게 어떤 조치가 취해지든 자식 액터에게 전파된다는 것 입니다.** 부모 액터가 중단되면 모든 자식 액터가 중단됩니다. 다시 시작하면 모든 자식 액터가 다시 시작됩니다.

##### 감시 전략(Supervision strategies)
두 가지 기본 감시 전략(Supervision strategies)이 있습니다:

1. One-For-One Strategy (default)
2. All-For-One Strategy

두 가지의 기본적인 차이점은 에러 해결 지시 효과가 얼마나 널리 퍼지는가 입니다.

**One-For-One** 부모 액터의 지시가 실패한 자식 액터에게만 적용됩니다. 실패한 자식 액터의 형제 액터에게 영향을 끼치지 않습니다. 이것은 별도로 지정하지 않는 이상 기본각으로 동작합니다. (또한 당신은 커스텀 감시 전략을 정의할 수도 있습니다.)

**All-For-One** 부모 액터의 지시가 실패한 자식 액터와 그 형테 액터에게 적용됩니다.

감시 전략에서 또 다른 중요한 선택은, 자식 액터가 얼마의 시간 동안에 몇 번의 실패를 허용하는가 입니다.(ex. "60초 이내에 10번 이하의 실패를 허용하고, 초과할 경우 종료 합니다.")

다음은 감시 전략의 예시 입니다:
```cs
public class MyActor : UntypedActor
{
    // if any child of MyActor throws an exception, apply the rules below
    // e.g. REstart the child, if 10 exceptions occur in 30 seconds or 
    // less, then stop the actor
    protected override SupervisiorStrategy SupervisorStrategy()
    {
        return new OnForOneStrategy( // or AllForOneStrategy
            maxNrOfretries: 10,
            withinTimeRange: TimeSpan.FromSeconds(30),
            localOnlyDecider: x =>
            {
                // Maybe ArithmeticException is not application critical
                // so we just ignore the error and keep going.
                if (x is ArithmeticException) return Directive.resume;

                // Error that we have no idea what to do with
                else if (x is InsanelyBadException) return Directive.Escalate;

                // Error that we can't recover from, stop the failing child
                else if (x is notSupportedException) return Directive.Stop;

                // otherwise restart the failing child
                else return Directive.restart;   
            }
        );
    }
    ...
}
```

### 핵심은 봉쇄(Containment)
감시 전략과 지침의 전체적인 핵심은 시스템 내에서 오류를 포함하고 자가 치유하는 것이므로, 시스템 전체에 크래시가 발생하지 않는 것입니다. 이를 위해 어떻게 해야 할까요?

잠재적으로 위험한 작업을 부모 액터에서 자식 액터에게 전달합니다. 이 작업은 위험한 작업을 수행하는 것입니다.

예를 들어, 월드컵 기간 동안에 치뤄지는 수많은 경기에서 점수와 선수 통계들을 관리하는 시스템을 운영한다고 생각해 봅시다.

월드컵이 진행되는 동안 처리 한계에 다다를 정도의 엄청난 양의 API 요청이 발생할 겁니다. 때로는 크래시가 발생할 수도 있습니다.(FIFA에 대한 비하가 아닙니다. 우리는 FIFA와 월드컵을 사랑합니다.) 독일 - 가나전을 예로  들어 보겠습니다.

스코어 키퍼는 게임이 진행되는 동안 정기적으로 데이터를 업데이트해야 합니다. FIFA가 관리하는 외부 API를 호출해서 필요한 데이터를 가져온다고 가정합니다.

**네트워크 호출은 위험합니다!** 만약 요청이 오류를 발생시키면, 호출을 시작한 액터는 크래시가 발생합니다. 그러면 어떻게 보호해야 할까요?

부모 액터가 상태를 보관하고, 형편없는 네트워크 호출으 ㄴ자식 액터에게 밀어 넣습니다. 그렇게 하면, 자식 액터가 크래시 되더라도 중요한 데이터를 보관하고 있는 부모 액터에게 영향을 끼치지 않습니다. **오류를 지역화(localizing the failure)**하여 시스템 전체에 퍼지는 것을 방지합니다.

액터 계층 구조에서 안전성을 이루는 방법의 예시입니다:

![Akka: User actor hierarchy](Images/error_kernel.png)

추적 중인 게임마다 하나의 클론으로 이러한 구조를 만들어 병렬로 동작할 수 있음을 기억하세요. 새로운 코드를 작성할 필요가 없습니다.

> 사람들이 "오류 커널(error kernel)" 이라는 용어로 부르기도 합니다. 오류의 영향을 받는 시스템의 양을 나타냅니다. 또한 "오류 커널 패턴(error kernel pattern)"이라는 말도 있습니다. 부모를 격리/보호하기 위해 위험을 자식에게 푸시하는 방식의 약어입니다.

## 실습
시작하기 전에, 시스템을 약간 업그레이드 해야 합니다. 우리의 액터시스템이 파일의 변화를 모니터링 할 수 있게 구성 요소를 추가합니다. 필요한 대부분의 클래스를 이미 가지고 있지만, 몇 가지 유틸리티 코드를 추가해야 합니다:
`TailCoordinatorActor`, `TailActor` 그리고 `FileObserver`.

이번 실습의 목표는 부모/자식 액터 관계를 어더ㄷㅎ게 만드는지 보여주는 것입니다.

### 1단계: 첫 번째 부모/자식 액터 만들기
클래스에 부모/자식 관계를 만들 준비가 되었습니다.

우리가 바라는 계층 구조에서 `TailCoordinatorActor`가 자식 액터들이 파일을 모니터링하고 추적할 수 있게 만들 것입니다.

지금은 하나의 `TailActor`지만, 미래에는 쉽게 많은 자식으로 확장하여 여러 파일을 관찰하거나 `tailing` 할 수 있습니다.

#### `TailCoordinatorActor` 추가
`TailCoordinatorActor`라는 이름으로 파일과 클래스를 만듭니다.

그리고 다음 코드를 추가합니다. (첫 번째 부모 액터가 될) coordinator 액터입니다.
```cs
// TailCoordinatorActor.cs
using System;
using Akka.Actor;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        #region Message types

        /// <summary>
        /// Start tailing the file at user-specified path.
        /// </summary>
        public class StartTail
        {
            public StartTail(string filePath, IActorRef reporterActor)
            {
                FilePath = filePath;
                reporterActor = reporterActor;
            }

            public string FilePath { get; private set; }
            public IActorRef ReporterActor { get; private set; }
        }

        /// <summary>
        /// Stop tailing the file at user-specified path.
        /// </summary>
        public class StopTail
        {
            public StopTail(string filePath)
            {
                FilePath = filePath;
            }

            public string FilePath { get; private set; }
        }

        #endregion

        protected override void OnReceive(object message)
        {
            if (message is StartTail)
            {
                var msg = message as StartTail;
                // YOU NEED TO FILL IN HERE
            }
        }
    }
}
```

#### `TailActor` 추가
이제, `TailActor`라는 클래스를 같은 이름의 파일에 추가합니다. 이 액터는 실제로 주어진 파일에 tailing을 수행합니다. `TailActor`는 적절한 시기에 `TailCoordinatorActor`에 의해 만들어지고 지시를 받습니다.

`TailActor.cs`에 다음과 같은 코드를 작성해주세요:
```cs
// TailActor.cs
using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Monitors the file at <see cref="_filePath"/> for changes and sends
    /// file updates to console.
    /// </summary>
    public class TailActor : UntypedActor
    {
        #region Message types
        
        /// <summary>
        /// Signal that the file has changed, and we need to
        /// read the next line of the file.
        /// </summary>
        public class FileWrite
        {
            public FileWrite(string fileName)
            {
                FileName = fileName;
            }

            public string FileName { get; private set; }
        }

        /// <summary>
        /// Signal that the Os has an error accessing the file.
        /// </summary>
        public class FileError
        {
            public FileError(string fileName, string reason)
            {
                FileName = fileName;
                Reason = reason;
            }

            public string FileName { get; private set; }
            public string Reason { get; private set; }
        }

        /// <summary>
        /// Signal to read the initial contents of the file at actor startup.
        /// </summary>
        public class InitialRead
        {
            public InitialRead(string fileName, string text)
            {
                FileName = fileName;
                Text = text;
            }

            public string FileName { get; private set; }
            public string Text { get; private set; }
        }

        #endregion

        private readonly string _filePath;
        private readonly IActorRef _reporterActor;
        private readonly FileObserver _observer;
        private readonly Stream _fileStream;
        private readonly StreamReader _fileStreamReader;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            _reporterActor = reporterActor;
            _filePath = filePath;

            // start watching file for changes
            _observer = new FileObserver(Self, Path.GetFullPath(_filePath));
            _observer.Start();

            // open the file stream with shared read/write permissions
            // (so file can be written to while open)
            _fileStream = new FileStream(
                Path.GetFullPath(_filePath), 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.ReadWrite
            );
            _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);

            // read the initial contents of the file and send it to console as first message
            var text = _fileStreamReader.ReadToEnd();
            Self.Tell(new InitialRead(_filePath, text));
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                // move file cursor forward
                // pull results from cursor to end of file and write to output
                // (this is assuming a log file type format that is append-only)
                var text = _fileStreamReader.ReadToEnd();
                if(!string.IsNullOrEmpty(text))
                {
                    _reporterActor.Tell(text);
                }
            }
            else if (message is FileError)
            {
                var fe = message as FileError;
                _reporterActor.Tell(string.Format("Tail error: {0}", fe.Reason));
            }
            else if (message is InitialRead)
            {
                var ir = message as InitialRead;
                _reporterActor.Tell(ir.Text);
            }
        }
    }
}
```

#### `TailCoordinatorActor`의 자식 액터로 `TailActor` 추가
빠르게 정리해보면: `TailActor`는 `TailCoordinatiorActor`의 자식이 되고, `TailCoordinatorActor`에 의해 지시를 받습니다.

즉, `TailActor`는 `TailCoordinatorActor`의 컨텍스트(context)에서 생성되어야 합니다.

`TailCoordinatorActor.cs`로 가서 `OnReceive()`를 다음과 같이 고칩니다.
```cs
// TailCoordinatorActor.OnReceive
protected override void OnReceive(object message)
{
    if (message is StartTail)
    {
        var msg = message as StartTail;
        // here we are creating our first parent/child relationship!
        // the TailActor instance created here is a child
        // of this instance of TailCoordinatorActor
        Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterActor, msg.FilePath)));
    }
}
```

#### BAM!
당신의 첫 번째 부모/자식 액터 관계를 성립시켰습니다.

### 2단계: 간단한 준비
#### `ValidationActor`를 `FileValidatorActor`로 변경
파일을 찾고 있기 때문에, `ValidationActor`를 `FileValidatorActor`로 바꿉니다.

`FileValidatorActor`를 [이 코드](Completed/FileValidatorActor.cs) 처럼 바꿉니다:
```cs
// FileValidatiorActor.cs
using System.IO;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor that validates user input and signals result to others.
    /// </summary>
    public class FileValidatorActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;
        private readonly IActorRef _tailCoordinatorActor;

        public FileValidatorActor(IActorRef consoleWriterActor, IActorRef tailCoordinatorActor)
        {
            _consoleWriterActor = consoleWriterActor;
            _tailCoordinatorActor = tailCoordinatorActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                // signal that the user needs to supply an input
                _consoleWriterActor.Tell(new Messages.NullInputError("Input as blank. Please type again.\n"));

                // tell sender to continue doing its thing (whatever that may be,
                // this actor doesn't care)
                Sender.Tell(new Messages.ContinueProcessing());
            }
            else
            {
                var valid = IsFileUri(msg);
                if (valid)
                {
                    // signal successful input
                    _consoleWriterActor.Tell(new Messages.InputSuccess(string.Format("starting processing for {0}", msg)));

                    // start coordinator
                    _tailCoordinatorActor.Tell(new TailCoordinatorActor.StartTail(msg, _consoleWriterActor));
                }
                else
                {
                    // signal that input was bad
                    _consoleWriterActor.Tell(new Messages.ValidationError(string.Format("{0} is not an existing URI on disk.", msg)));


                    // tell sender to continue doing its thing (whatever that
                    // may be, this actor doesn't care)
                    Sender.Tell(new Messages.ContinueProcessing());
                }
            }
        }

        /// <summary>
        /// Checks if file exists at path provided by user.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool IsFileUri(string path)
        {
            return File.Exists(path);
        }
    }
}
```

#### `TailCoordinatorActor`의 `IActorRef` 만들기
`Main()`에서 `TailCoordinatorActor`를 위한 `IActorRef`를 만들고, `fileValidatorActorProps`에 넘겨줍니다:
```cs
// Program.Main
// make tailCoordinatorActor
Props tailCoordinatorProps = Props.Create(() => new TailCoordinatorActor());
IActorRef tailCoordinatorActor = MyActorSystem.ActorOf(tailCoordinatorProps, "tailCoordinatorActor");

// pass tailCoordinatorActor to fileValidatiorActorProps (just adding one extra arg)
Props fileValidatorActorProps = Props.Create(() => new FileValidatorActor(consoleWriterActor, tailCoordinatorActor));
IActorRef fileValidatorActor = MyActorSystem.ActorOf(fileValidatorActorProps, "ValidatorActor");
```

#### `DoPrintInstructions` 수정
약간의 수정만 하면 됩니다. 사용자의 입력을 받는 대신 디스크에서 텍스트 파일을 이용할 것이기 때문입니다.

`DoPrintInstructions()`를 다음과 같이 수정합니다:
```cs
// ConsolereaderActor.cs
private void DoPrintInstructions()
{
    Console.WriteLine("Please provide the URI of a log file on disk.\n");
}
```

#### `FileObserver` 추가
이건 우리가 당신에게 제공하는 유틸리티 클래스 입니다. 파일의 변화를 감시하는 낮은 레벨의 작업을 수행합니다.

`FileObserver`라는 새로운 클래스를 생성하고 [FileObserver.cs](Completed/FileObserver.cs)를 작성하세요. `Mono`에서 동작하는 경우, `Start()` 메소드에 있는 추가 환경 변수 주석을 해제하세요:
```cs
// FileObserver.cs
using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Turns <see cref="FileSystemWatcher"/> events about a specific file into
    /// messages for <see cref="TailActor"/>
    /// </summary>
    public class FileObserver : IDisposable
    {
        private readonly IActorRef _tailActor;
        private readonly string _absoluteFilePath;
        private FileSystemWatcher _watcher;
        private readonly string _fileDir;
        private readonly string _fileNameOnly;

        public FileObserver(IActorRef tailActor, string absoluteFilePath)
        {
            _tailActor = tailActor;
            _absoluteFilePath = absoluteFilePath;
            _fileDir = Path.GetDirectoryName(absoluteFilePath);
            _fileNameOnly = Path.GetFileName(absoluteFilePath);
        }

        /// <summary>
        /// Begin monitoring file.
        /// </summary>
        public void Start()
        {
            // Need this for Mono 3.12.0 workaround
            // uncomment netx line if you're running on Mono!
            // Environment.SetEnvironmentValiable("MONO_MANAGED_WATCHER", "enabled");

            // make watcher to observe our specific file
            _watcher = new FileSystemWatcher(_fileDir, _fileNameOnly);

            // watch our file for changes to the file name,
            // or new messages being written to file
            _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

            // assign callbacks for event types
            _watcher.Changed += OnFileChanged;
            _watcher.Error += OnFileError;

            // start watching
            _watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stop monitoring file.
        /// </summary>
        public void Dispose()
        {
            _watcher.Dispose();
        }

        /// <summary>
        /// Callback for <see cref="FileSystemWatcher"/> file error events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnFileError(object sender, ErrorEventArgs e)
        {
            _tailActor.Tell(new TailActor.FileError(_fileNameOnly, e.GetException().Message), ActorRefs.NoSender);
        }

        /// <summary>
        /// Callback for <see cref="FileSystemWatcher"/> file change events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // here we use special ActorRefs.NoSender
            // since this event can happen many tiems,
            // this is a little microoptimization
            _tailActor.Tell(new TailActor.FileWrite(e.Name), ActorRefs.NoSender);
        }
    }
}
```

### 3단계: `SupervisorStaritegy`
드디어 새로운 부모 `TailcoordinatorActor`에 감시 전략을 추가할 때입니다.

기본 `SupervisorStrategy`는 One-For-One에 Restart directive 입니다.

`TailCoordinatorActor`의 아래에 다음 코드를 추가해 주세요:
```cs
// TailCoordinatorActor.cs
protected override SupervisorStrategy SupervisorStrategy()
{
    return new OneForOneStrategy(
        10, // maxNumberOfretries
        TimeSpan.FromSeconds(30), // withinTimeRange
        x => // localOnlyDecider
        {
            // Maybe we consider ArithmeticException to not be application critical
            // so we just ignore the error and keep going.
            if (x is ArithmeticException) return Directive.Resume;

            // Error that we cannot recover from, sotp the failing actor
            else if (x is NotSupportedException) return Directive.Stop;

            // in all other cases, just restart the failing actor
            else return Directive.Restart;
        }
    );
}
```

### 4단계: Build and Run!
드디어 동작해 볼 시간입니다!

#### Tail동작을 수행할 텍스트 파일 준비
[이것](DoThis/sample_log_file.txt)과 같은 로그 파일을 추천합니다. 당신이 원하는 아무 텍스트 파일을 사용해도 괜찮습니다.

텍스트 파일을 스크린 한쪽에 열어주세요.

#### 동작
##### 화면에 나타나는 것을 확인
어플리케이션을 실행하면 콘솔 윈도우에 로그파일의 내용이 나타나야 합니다. 제공한 로그파일을 사용하는 경우 화면은 다음과 같아야 합니다:
![Petabridge Akka.NET Bootcamp Actor Hierarchies](Images/working_tail_1.png)

** 콘솔과 파일을 모두 열어둔 채로 둡니다. 그리고...**

텍스트를 추가하고 tail이 잘 작동하는지 확인합니다! 텍스트 몇 줄을 추가하고, 저장합니다. tail이 잘 동작하는지 지켜봅니다!

다음 처럼 보일겁니다:
![Petabridge Akka.NET Bootcamp Actor Hierarchies](Images/working_tail_2.png)

축하합니다! 당신은 .NET을 이용해 `tail`을 포팅했습니다. 

### 레슨을 마치고,
작성한 코드와 [Completed](Completed/)의 코드를 비교하며 샘플에 어떤 것이 추가 및 수정되었는지 확인 해봅시다.

## 수고하셨습니다! 이제 레슨5 차례입니다.
수고하셨습니다! 레슨4을 무사히 끝냈습니다. 이번 레슨을 통해 우리의 시스템과 당신의 이해에 큰 도약이 있었습니다. 

이제 [Akka 시작하기 1-5 : Looking up Actors by Address with `ActorSelection`](../lesson5/README.md)를 향해 나아가 봅시다.

## Supervision FAQ
### 자식 액터는 supervisor를 얼마나 기다리나요?
이 질문은 우리가 종종 받는 질문입니다: 자식 액터가 오류를 보고했을 때, 감독자의 메일박스에 처리 대기중이 메시지가 여러개 있다면 어떻게 합니까? 크래시가 발생중인 자식 액터는 반을이 올때까지 기다려야 하지 않을까요?

사실 그렇지 않습니다. 액터가 그들의 감시자에게 오류를 보고한 때에, 이 보고는 특별한 종류의 "시스템 메시지(system message)"로 보내어 집니다. **시스템 메시지는 감독자의 메일박스와 감독자의 일반 작업에 대한 반환을 기다리는 것을 건너뛰고 전해집니다.**

> 시스템 메시지는 감독자의 메일박스와 감독자의 일반 작업에 대한 반환을 기다리는 것을 건너뛰고 전해집니다.

### 액터가 실패했을때 현재 메시지는 어떻게 되나요?
액터가 현재 처리중인 메시지가 중지된 경우 (액터 자신 또는 그 부모 액터에 의해 실패가 발생하였는지 와는 관계 없이), 저장하고 다시 시작한 후 재처리 할 수 있습니다. 여러 가지 방법이 있는데, 가장 보편적인 접근방식은 `preRestart()` 입니다. 저장(stash)할(또는 저장되어 있는) 메시지가 있다면 다시 시작하면서 다른 액터에 메시지를 보낼 수 있습니다.  (Note: 액터에게 저장(stash)된 메시지가 있다면, 재시작을 성공한 경우 메시지가 자동으로 해제(unstash)됩니다.)