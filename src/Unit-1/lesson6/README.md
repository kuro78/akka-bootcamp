# Akka 기초 1-6 : 액터 라이프사이클(The Actor Lifecycle)
이 강의는 액터에 대한 중요한 개념 인 액터 라이프 사이클(the actor life sycle)을 끝으로 "기본 시리즈"를 마무리합니다.

## Key concepts / background
### 액터 라이프사이클(actor life cycle)이란?
액터에는 잘 정의 된 라이프사이클이 있습니다. 액터가 생성되고 시작후 대부분의 삶을 메시지를받는 데 보냅니다. 액터가 더 이상 필요하지 않은 경우 액터를 종료하거나 "중지"할 수 있습니다.

### 액터 라이프사이클의 단계는 무엇인가요?
Akka.NET의 액터 라이프사이클의 5 단계가 있습니다:

1. `Starting`
2. `Receiving`
3. `Stopping`
4. `Terminated`, or
5. `Restarting`

![Akka.NET actor life cycle steps with explicit methods](Images/lifecycle.png)

차례대로 알아보도록 합니다.

#### `Starting`
액터가 `ActorSystem`에 의해 초기화 될 때의 초기 상태입니다.

#### `Receiving`
이제 액터는 메시지를 처리 할 수 있습니다. `Mailbox`(나중에 자세히 설명하겠습니다)는 처리를 위해 액터의 `OnReceive`메소드로 메시지를 전달하기 시작합니다.

#### `Stopping`
액터는 상태(state)를 정리합니다. 이 단계에서 일어나는 일은 액터를 종료할지, 다시 시작할지 여부에 따라 다릅니다.

액터가 다시 시작될 예정 이라면, 액터가 다시 시작된 후 `Receiving` 상태로 돌아 오면 처리 할 상태 또는 메시지를 이 단계 동안 저장하는 것이 일반적입니다.

액터가 종료될 예정 이라면, `Mailbox`의 모든 메시지가 `ActorSystem`의 `DeadLetters` 메일 함으로 전송됩니다. `DeadLetters`는 보통 액터가 소멸되었기 때문에 배달 할 수 없는 메시지의 저장소입니다.

#### `Terminated`
액터가 소멸되었습니다. `IActorRef`를 통해 전송 된 모든 메시지는 이제 `DeadLetters`로 이동합니다. 액터는 다시 시작할 수 없지만 같은 주소를 가진 새로운 액터를 생성 할 수 있습니다(`IActorRef`가 다르지만 `ActorPath`가 같음).

#### `Restarting`
액터가 다시 시작되고 `Starting`상태로 돌아갑니다.

### 라이프사이클(Life cycle) 후크 방법(hook methods)
그렇다면, 어떻게 액터 라이프사이클에 연결할 수 있을까요? 연결할 수 있는 4개의 메소드가 있습니다.

#### `PreStart`
액터가 메시지 수신을 시작하기 전에 `PreStart` 로직이 실행됩니다. 초기화 로직을 배치하는 것이 좋습니다. 다시 시작하는 동안에도 호출됩니다.

#### `PreRestart`
액터가 실패하면 (예: 처리되지 않은 예외 발생) 부모 액터가 액터를 다시 시작합니다. `PreRestart`는 액터가 다시 시작되기 전에 정리를 수행하거나 나중에 재 처리하기 위해 현재 메시지를 저장하기 위해 연결할 수있는 메소드입니다.

#### `PostStop`
`PostStop`은 액터가 중지되고 더 이상 메시지를 수신하지 않으면 호출됩니다. 이것은 정리 로직을 포함하기에 좋은 곳입니다. PostStop은 `PreRestart` 중에도 호출되지만, 재시작 중에 이 동작을 피하려면 `PreRestart`를 재정의해서 `base.PreRestart`를 호출하지 않아도됩니다.

`DeathWatch`는 액터가 구독이 종료되면 알림을 받도록 구독 한 다른 액터에게 알릴 때도 사용됩니다. `DeathWatch`는 모든 액터가 다른 액터의 종료에 대해 알림을 받을 수 있도록 프레임 워크에 내장 된 게시 / 구독 시스템입니다.

#### `PostRestart`
`PostRestart`는 PreRestart 후 재시작 중 PreStart 이전에 호출됩니다. Akka.NET이 이미 수행한 작업을 넘어서 액터 충돌을 유발한 오류에 대한 추가보고 또는 진단을 수행 할 수있는 좋은 메소드입니다.

다음은 후크 메서드가 라이프사이클 단계에 적합한 위치입니다:

![Akka.NET actor life cycle steps with explicit methods](Images/lifecycle_methods.png)

### 라이프사이클을 어떻게 후킹하나요?
후킹하려면, 다음과 같이 후킹하려는 메서드를 재정의하면됩니다:

```csharp
 /// <summary>
/// Initialization logic for actor
/// </summary>
protected override void PreStart()
{
    // do whatever you need to here
}
```

### 가장 일반적으로 사용되는 라이프사이클 메소드는 무엇인가요?
#### `PreStart`
`PreStart`는 가장 많이 사용되는 후크 메소드입니다. 액터의 초기 상태를 설정하고 액터에 필요한 사용자 지정 초기화 로직을 실행하는 데 사용됩니다.

#### `PostStop`
두 번째 메소드는 사용자 지정 정리 논리를 수행하는 `PostStop`입니다. 예를 들어, 액터가 종료하기 전에 파일 시스템 핸들이나 시스템에서 소비하고있는 다른 리소스를 해제하도록 할 수 있습니다.

#### `PreRestart`
`PreRestart`는 위의 메소드보다 1/3 정도 떨어지지만 가끔 사용합니다. PreRestart를 사용하는 것은 액터가하는 일에 크게 의존하지만, 한 가지 일반적인 경우는 메시지를 숨기거나 액터가 다시 시작되면 재 처리를 하기위한 조치를 취하는 것입니다.

### 감시(supervision)와 관련이 있나요?
액터가 실수로 충돌하는 경우 (즉, 처리되지 않은 예외가 발생하는 경우) 액터의 감독자는 액터의 메일함에 남아있는 메시지를 잃지 않고 처음부터 액터의 라이프 사이클을 자동으로 다시 시작합니다.

레슨4에서 액터 계층 / 감시에 대해 언급했듯이 처리되지 않은 오류에 대한 동작은 부모 액터의 `감시 지침(SupervisionDirective)`에 의해 결정됩니다. 부모 액터는 자식 액터에게 오류에 대해 종료, 다시 시작 또는 무시하도록 지시하고 중단 된 부분부터 다시 시작할 수 있습니다. 기본값은 다시 시작하는 것이므로 잘못된 상태가 사라지고 액터가 깨끗하게 시작됩니다. 재시작이 저렴한 방법입니다.

## 실습
마지막 실습은 우리의 시스템이 이미 완성되어 있기에 매우 짧습니다. `TailActor`의 초기화 및 종료를 최적화하는 데 집중합니다.

### 초기화 로직을 `TailActor` 생성자에서 `PreStart()`로 이동
이 모든 것이 `TailActor`의 생성자안에서 보입니까?

```csharp
// TailActor.cs constructor
// start watching file for changes
_observer = new FileObserver(Self, Path.GetFullPath(_filePath));
_observer.Start();

// open the file stream with shared read/write permissions
// (so file can be written to while open)
_fileStream = new FileStream(Path.GetFullPath(_filePath), FileMode.Open, 
    FileAccess.Read, FileShare.ReadWrite);
_fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);

// read the initial contents of the file and send it to console as first message
var text = _fileStreamReader.ReadToEnd();
Self.Tell(new InitialRead(_filePath, text));
```

생성자의 초기화 로직은 실제로 `PreStart()`메소드에 속합니다.

첫 번째 라이프사이클 메소드를 사용할 시간입니다!

Pull all of the above initialization logic out of the `TailActor` constructor and move it into `PreStart()`. We'll also need to change `_observer`, `_fileStream`, and `_fileStreamReader` to non-readonly fields since they're moving out of the constructor.
위의 모든 초기화 로직을 `TailActor` 생성자에서 `PreStart()`로 이동 시킵니다. `_observer`, `_fileStream`과 `_fileStreamReader`가 생성자에서 이동하므로 읽기 readonly가 아닌 필드로 변경해야합니다.

이제 `TailActor.cs`의 상단을 다음과 같이 변경합니다:

```csharp
// TailActor.cs
private string _filePath;
private IActorRef _reporterActor;
private FileObserver _observer;
private Stream _fileStream;
private StreamReader _fileStreamReader;

public TailActor(IActorRef reporterActor, string filePath)
{
    _reporterActor = reporterActor;
    _filePath = filePath;
}

// we moved all the initialization logic from the constructor
// down below to PreStart!

/// <summary>
/// Initialization logic for actor that will tail changes to a file.
/// </summary>
protected override void PreStart()
{
    // start watching file for changes
    _observer = new FileObserver(Self, Path.GetFullPath(_filePath));
    _observer.Start();

    // open the file stream with shared read/write permissions
    // (so file can be written to while open)
    _fileStream = new FileStream(Path.GetFullPath(_filePath),
        FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);

    // read the initial contents of the file and send it to console as first message
    var text = _fileStreamReader.ReadToEnd();
    Self.Tell(new InitialRead(_filePath, text));
}
```

훨씬 낫네요! 좋아요, 다음은 뭐죠?

### `FileSystem` 리소스를 정리하고 잘 관리합시다.
`TailActor` 인스턴스는 OS 핸들을 `_fileStreamReader`와 `FileObserver`에 각각 저장합니다. `PostStop()`을 사용해 해당 핸들이 정리되었는지 확인하고 모든 리소스를 OS에 다시 릴리즈합니다.

다음을 `TailActor`에 추가합니다:

```csharp
// TailActor.cs
/// <summary>
/// Cleanup OS handles for <see cref="_fileStreamReader"/> 
/// and <see cref="FileObserver"/>.
/// </summary>
protected override void PostStop()
{
    _observer.Dispose();
    _observer = null;
    _fileStreamReader.Close();
    _fileStreamReader.Dispose();
    base.PostStop();
}
```

### 4단계: Build and Run!
끝났습니다! `F5`를 눌러 솔루션을 실행하면 조금 더 최적화되었지만 이전과 똑같이 작동합니다. :)

### 마치고,
작성한 코드와 [Completed](Completed/)의 코드를 비교하며 샘플에 어떤 것이 추가 및 수정되었는지 확인 해봅시다.

## 수고하셨습니다!
**더 배우고 싶은가요? [지금 Unit 2를 시작해 보세요](../../Unit-2/README.md "Akka.NET Bootcamp Unit 2").**
