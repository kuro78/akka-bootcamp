# Akka 기초 1-5: `ActorSelection`과 함께 주소로 액터 찾기
이번 레슨에서는 액터들을 서로 조금씩 분리하는 방법과 액터들 간의 새로운 소통 방법을 배워보겠습니다: [`ActorSelection`](http://api.getakka.net/docs/stable/html/CC0731A6.htm "Akka.NET Stable API Docs - ActorSelection class"). 이번 레슨은 이전의 수업들 보다 짧습니다. 이제 우리는 탄탄한 개념적 토대를 마련했습니다.

## Key concepts / background
`ActorSelection`은 지난 수업에서 다룬 액터 계층 구조와 자연스럽게 연결됩니다. 이제 우리는 액터들이 계층구조 속에서 있다는 것을 이해하게 되면서, 이런 의문을 품게 됩니다: 액터들은 모두 같은 수준에 있지 않습니다. 이것이 이것이 액터들의 의사소통 방식을 변화시킬까요?

액터에게 메시지를 보내고 일을 시키기 위해 handle이 필요하다는 것을 알고 있습니다. 이제 우리는 모든 계층의 액터들을 가지고 있지만, 메시지를 보내고자 하는 액터에게 항상 직접적인 링크(`IActorRef`)가 있는 것은 아닙니다.

**그럼 계츰 구조의 다른 어딘가에 있을 저장된 `IActorRef`도 없는 액터에게 어떻게 메시지를 보낼 수 있을까요? 어쩌지?**

`ActorSelection` 속으로 들어가 봅시다.

### `ActorSelection`이란?
`ActorSelection`은 `IActorRef`를 저장해 두지 않은 상태에서 액터에 메시지를 보낼 수 있도록 액터의 handle을 찾기 위해 `액터 주소(ActorPath)`를 이용한 것에 불과합니다.

액터가 알고 있는 `IActorRef` 통해 액터를 생성하거나 소멸시키는 작업을 하는 대신, `ActorPath`를 통해 액터의 handle을 "찾아(looking up)"서 할 수 있습니다(`ActorPath`가 시스템 계층에서 액터가 있는 위치에 대한 주소라는 것을 기억하세요.). 스카이프에서 username을 모르는 상태에서 email을 이용해 누군가를 찾는 것과 비슷합니다. 

`ActorSelection`이 'IActorRef'를 찾는 방식이지만, 본질적으로 단일 액터를 1대1로 찾는 것은 아니라는 점을 유념해야 합니다.

기술적으로, 조회할 때 표시되는 `ActorSelection` 객체는 특정 `IActorRef`를 가리키지 않습니다. 검색(look up)한 표현식(expression)과 일치하는 모든 `IActorRef`를 가르키는 handle 입니다. 이 표현식에서는 와일드 카드가 지원되므로 0개 이상의 액터가 선택될 수 있습니다. (나중에 좀 더 다루도록 하겠습니다.)

`ActorSelection`에 의해 이름이 같은 - 첫 번째 액터가 소멸된 후 같은 이름으로 다시 생성한 - 서로 다른 두  `IActionRef`가 매칭될 수 있습니다. (재시작 하지 않은 상태에서, 이 경우 같은 `IActorRef`가 됩니다.)

#### 오브젝트(object) 인가요? 프로세스(process)? 둘 다?
`ActorSelect`이 프로세스(process)와 오브젝트(Object) 둘 모두라고 생각합니다: `ActorPath`로 액터를 찾는 프로세스와 그 과정에서 되돌아온 오브젝트는 우리가 찾던 표현과 일치하는 액터에게 메시지를 보낼 수 있게 해줍니다.

### 왜 `ActorSelection`에 대해 신경써야 하나요?
일반적으로 항상 `IActorRef`를 대신 사용해야 합니다. 그러나 `ActorSelection`이 작업에 적합한 도구인 몇 가지 시나리오가 있으며 여기에서 더 자세히 다룹니다: "[When Should I Use ActorSelection](https://petabridge.com/blog/when-should-I-use-actor-selection/)."

#### 동적 행동(Dynamic behavor)
동적 행동(Dyanmic behavor)은 Unit 2 초반에 파고드는 상급 개념이지만, 지금은 주어진 액터의 행동이 매우 유연할 수 있다는 것만 알아두시면 됩니다. 이를 통해 액터는 유한 상태 기계(Finite State Machines, FSM)와 같은 것을 쉽게 표현하여 작은 코드 설치 공간으로 복잡한 상황을 쉽게 처리할 수 있다.

`ActorSelection`은 어디에서 활약을 할까요? 당신이 매우 역동적이고 적응력이 뛰어난 시스템을 원한다면, 아마도 많은 액터가 계층 구조에서 들어오고 나가는 가운데  그 모두에게 핸들(handle)을 저장 / 전달하는 것은 정말 고통 스러울 것입니다. `ActorSelection`을 사용하면 통신해야하는 키 액터의 잘 알려진 주소로 메시지를 쉽게 보낼 수 있고, 필요한 항목에 대한 핸들을 가져 오거나 / 전달 / 저장하는 것에 대해 걱정할 필요가 없습니다.

또한 `ActorSelection`을 수행하는 데 필요한 `ActorPath` 조차 하드 코딩되지 않는 대신에 액터에 전달되는 메시지로 대표 될 수있는 극도로 동적인 액터를 빌드할 수 있습니다.

#### 유연한 커뮤니케이션 패턴(Flexible communication patterns) == 적응형 시스템(adaptable system)
개발자로서의 행복, 시스템의 탄력성 및 조직이 이동할 수있는 속도에 중요하기 때문에 이러한 적응성 아이디어를 가지고 실행 해 봅시다.

작동하기 위해 모든 것을 결합 할 필요가 없기 때문에 개발주기가 빨라집니다. 이미 작성한 모든 것을 다시 돌아가서 변경할 필요없이 새로운 액터와 완전히 새로운 섹션을 액터 계층에 도입 할 수 있습니다. 당신의 시스템에는 새로운 액터(및 요구 사항)를 쉽게 확장하고 수용 할 수있는 훨씬 더 유연한 통신 구조를 가지고 있습니다..


#### 간단히 말해서: `ActorSelection`은 시스템을 변경에 훨씬 더 적합하게 만들고 더 강력하게 만듭니다.

### `ActorSelection`을 언제 사용해야 하나요?
Petabridge는 "[When Should I Use `ActorSelection`?](https://petabridge.com/blog/when-should-I-use-actor-selection/)"라는 제목으로 이 주제에 대한 자세한 게시물을 게시했습니다.

Short version: 가능하다면 `ActorSelection`을 사용하지 마십시오. 하지만 때로는 이것이 현재 `IActorRef`가 없는 다른 액터와 통신 할 수있는 유일한 방법입니다.

### 주의: `ActorSelection`을 전달하지 마십시오.
`IActorRef`처럼`ActorSelection`을 매개 변수로 전달하지 않는 것을 권합니다. `ActorSelection`이 절대적이 아니라 상대적 일 수 있기 때문인데, 이 경우 계층 구조에서 다른 위치를 가진 액터에 전달 될 때 의도 한 효과를 내지 못할 것입니다.

### `ActorSelection`은 어떻게 만드나요?
매우 간단합니다: `var selection = Context.ActorSelection("/path/to/actorName");`

> NOTE: **액터 경로에는 액터 클래스명이 아닌 액터를 인스턴스화 할때 액터에 할당한 이름을 사용합니다. 액터를 만들 때 이름을 지정하지 않으면 시스템에서 고유한 이름을 자동으로 생성합니다.**

예제:

```csharp
class FooActor : UntypedActor {}
Props props = Props.Create<FooActor>();

// the ActorPath for myFooActor is "/user/barBazActor"
// NOT "/user/myFooActor" or "/user/FooActor"
IActorRef myFooActor = MyActorSystem.ActorOf(props, "barBazActor");

// if you don't specify a name on creation as below, the system will
// auto generate a name for you, so the actor path will
// be something like "/user/$a"
IActorRef myFooActor = MyActorSystem.ActorOf(props);
```

### `ActorSelection`과 `IActorRef`에 다르게 메시지를 보내나요?
Nope. You `Tell()` an `ActorSelection` a message just the same as an `IActorRef`:
아니요. `ActorSelection`도 `IActorRef`와 똑같이 메시지를 보낼때 `Tell ()`을 사용합니다:

```csharp
var selection = Context.ActorSelection("/path/to/actorName");
selection.Tell(message);
```

## 실습
좋아요, 시작해봅시다. 이번 실습은 짧습니다. 우리는 시스템을 약간만 최적화하면 됩니다.

### 1단계: `ConsoleReaderActor`와 `FileValidatorActor` 분리
우리의 `ConsoleReaderActor`는 검증을 위해 콘솔에서 읽은 메시지를 보낼 수 있도록 `IActorRef`를 제공해야합니다. 지금의 디자인에서는 충분히 쉽습니다.

그러나 `ConsoleReaderActor`가 `FileValidatorActor` 인스턴스가 생성 된 계층 (현재`Program.cs`)에서 멀리 떨어져 있다고 가정할 경우 필요한`IActorRef`를 모든 중개자를 먼저 통과하지 않고 `ConsoleReaderActor`로 전달할 수있는 깔끔하고 쉬운 방법이 없습니다.

`ActorSelection`이 없으면 핸들이 생성되고 사용되는 위치 사이의 모든 객체를 통해 필요한`IActorRef`를 전달해야합니다. 이는 객체사이에 점점더 높은 결합도를 요구합니다. -- **우웩**!

**전달중인 `validationActor` `IActorRef`를 제거**하여 문제를 해결해 보겠습니다. 이제`ConsoleReaderActor`의 상단이 다음과 같이 보일 것입니다:

```csharp
// ConsoleReaderActor.cs
// note: we don't even need our own constructor anymore!
public const string StartCommand = "start";
public const string ExitCommand = "exit";

protected override void OnReceive(object message)
{
    if (message.Equals(StartCommand))
    {
        DoPrintInstructions();
    }

    GetAndValidateInput();
}
```

그런 다음, `ConsoleReaderActor` 안에 메시지 유효성 확인을 위한 호출 내용을 업데이트해서 특정 `IActorRef`를 알 필요 없이 콘솔에서 읽은 메시지를 `ActorPath`를 통해 유효성 검사를 하는 액터에 전달할 수 있도록 합니다.

```csharp
// ConsoleReaderActor.GetAndValidateInput

// otherwise, just send the message off for validation
Context.ActorSelection("akka://MyActorSystem/user/validatorActor").Tell(message);
```

마지막으로, 생성자가 더 이상 인수를 받지 않으므로 `Program.cs`에서 `consoleReaderProps`를 적절히 업데이트하겠습니다:
```csharp
// Program.Main
Props consoleReaderProps = Props.Create<ConsoleReaderActor>();
```

### 2단계: `FileValidatorActor`와 `TailCoordinatorActor` 분리
`ConsoleReaderActor`와 `FileValidatorActor`처럼, `FileValidatorActor`는 필요하지 않은`TailCoordinatorActor`에 대한 `IActorRef`가 필요합니다. 수정하겠습니다.

First, **remove the `tailCoordinatorActor` argument to the constructor of `FileValidatorActor` and remove the accompanying field on the class**. The top of `FileValidatorActor.cs` should now look like this:
먼저 **`FileValidatorActor`의 생성자와 멤버에서 `tailCoordinatorActor`를 제거합니다**. 이제 `FileValidatorActor.cs`의 상단이 다음과 같이 보일 것입니다.

```csharp
// FileValidatorActor.cs
// note that we're no longer storing _tailCoordinatorActor field
private readonly IActorRef _consoleWriterActor;

public FileValidatorActor(IActorRef consoleWriterActor)
{
    _consoleWriterActor = consoleWriterActor;
}
```

다음으로, `ActorSelection`을 사용하여 `FileValidatorActor`와 `TailCoordinatorActor` 간의 통신을합시다! 다음과 같이 `FileValidatorActor`를 업데이트합니다:
```csharp
// FileValidatorActor.cs
// start coordinator
Context.ActorSelection("akka://MyActorSystem/user/tailCoordinatorActor").Tell(
    new TailCoordinatorActor.StartTail(msg, _consoleWriterActor));
```

마지막으로, `Program.cs`의 `fileValidatorProps`를 업데이트하여 변경된 생성자 인수를 반영해 보겠습니다:

```csharp
// Program.Main
Props fileValidatorActorProps = Props.Create(() =>
    new FileValidatorActor(consoleWriterActor));
```

### 3단계: Build and Run!
이제 우리의 시스템을 실행해 볼 시간입니다.

지난 강의에서와 마찬가지로 `F5`키를 눌러 로그/텍스트 파일을 실행하면 추가 내용이 콘솔에 표시되는 것을 볼 수 있습니다.

![Petabridge Akka.NET Bootcamp Actor Selection Working](Images/selection_working.png)

### 이봐, 기다려, 돌아가! 'FileValidatorActor'에 전달 된 'consoleWriterActor'는 어때요? 불필요하게 액터를 연결하지 않았나요?
오! 훌륭합니다!!

이 얘기를 하는 것 같군요, `FileValidatorActor`로 전달는 `IctorRef`:

```csharp
// FileValidatorActor.cs
private readonly IActorRef _consoleWriterActor;

public FileValidatorActor(IActorRef consoleWriterActor)
{
    _consoleWriterActor = consoleWriterActor;
}
```

*이것은 좀 직관적이지 않네요*. 제안을 하나 하겠습니다.

이 경우 `consoleWriterActor`의 핸들을 사용하여 직접 대화하지 않습니다. 대신 `IActorRef`를 시스템의 다른 곳으로 전송되는 메시지 안에 넣어 처리합니다. 메시지가 수신되면 수신 액터는 작업을 수행하기 위해 필요한 모든 것을 알게됩니다.

실제로 전달되는 메시지를 완전히 독립적으로 만들고, 이 한 액터(`FileValidatorActor`)에 `IActorRef`가 전달되어야하고, 시스템을 전체적으로 유연하게 유지하기 때문에 액터 모델에서 낮은 결합도를 가진 좋은 디자인 패턴입니다.

메시지를 받는 `TailCoordinatorActor`에서 무슨 일이 일어나고 있는지 생각해보십시오: `TailCoordinatorActor`의 역할은 실제로 파일 변경 사항을 관찰하고보고 할 `TailActor`를 관리하는 것입니다... 어딘가에 그것을 지정해야 합니다.

'TailActor'에는 리포트 출력 위치가 직접 기록되어서는 안됩니다. 리포트 출력 위치는 들어오는 메시지 내에서 명령으로 캡슐화되어야할 작업 수준의 세부 정보입니다. 이 경우 대상 작업은 사용자 지정 `StartTail` 메시지이며, 실제로 이전에 언급한 `consoleWriterActor`에 대한 `IActorRef`를 `reporterActor`로 포함합니다.

반 직관적으로, 이 패턴은 실제로 느슨한 결합(loose coupling)을 촉진합니다. 특히 이벤트를 메시지로 바꾸는 패턴이 널리 사용되는 경우를 Akka.NET을 통해 많이 볼 수 있습니다.

### 마치고,
작성한 코드와 [Completed](Completed/)의 코드를 비교하며 샘플에 어떤 것이 추가 및 수정되었는지 확인 해봅시다.

## 수고하셨습니다! 이제 레슨6 차례입니다.
수고하셨습니다! 레슨5를 무사히 끝냈습니다. Unit 1의 홈스테이지를 멋지게 진행하고 계십니다. 

이제 [Akka 기초 1-6 : 액터 라이프 사이클(The Actor Lifecycle)](../lesson6/README.md)을 향해 나아가 봅시다.