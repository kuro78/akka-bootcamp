# Akka 기초 1-1 : 액터와 `액터시스템(ActorSystem)`
시작합시다! 첫 번째 강의에 오신걸 환영합니다.

이번 시간에는 [Akka.NET](http://getakka.net/)의 기본을 소개하고, 액터를 직접 만들어볼 것입니다.

## Key concepts / background
간단한 액터를 포함한 콘솔 어플리케이션을 만들면서 액터 시스템의 기초를 배워봅니다.

이를 위해 두 가지 액터를 만듭니다.
  - 콘솔에서 Read를 수행하는 액터
  - 어떠한 프로세싱을 한 후에 Write를 하는 액터

### 액터(actor) 란?
"액터(actor)"는 실제로 시스템의 인간 참여자를 위한 유사점 일뿐입니다. 그것은 일을하고 의사 소통 할 수있는 독립체(entity), 객체(object)입니다.

> 당신이 객체 지향 프로그래밍(OOP)에 익숙하다고 가정하겠습니다. 액터 모델은 OOP(객체 지향 프로그래밍)와 매우 유사합니다. 마치 OOP에서 모든 것이 객체 인 것처럼 액터 모델에서는 ***모든 것이 액터입니다***.

이것만 생각하세요: 모든 것은 액터다. 모든 것은 액터다. 모든 것은 액터다! 당신의 시스템을 사람들의 계급제도처럼 하나의 액터에 의해 간결하게 다뤄질 수 있을 정도로 충분히 작아질 때까지 태스크를 나누고 위임하도록 설계하는 것을 생각해봅시다.

일단 당장은 이렇게 생각합시다: OOP에서 당신은 각각의 모든 객체에 대해 명확한 목적을 부여합니다. 맞죠? 액터 모델 또한 다르지 않습니다. 액터가 되는 것이라는 확실한 목적을 부여한 객체라는 점을 제외하면 말입니다.

**추가로 읽을 거리: [What is an Akka.NET Actor](http://petabridge.com/blog/akkadotnet-what-is-an-actor/)?**

### 액터 사이의 정보 교환
액터들의 소통은 사람들이 메세지를 교환하는 것과 같습니다. 이들의 메세지는 단순히 C# 클래스입니다.

```csharp
//this is a message!
public class SomeMessage{
	public int SomeValue {get; set}
}
```

We go into messages in detail in the next lesson, so don't worry about it for now. All you need to know is that you send messages by `Tell()`ing them to another actor.
메세지에 대한 자세한 내용은 다음에 깊게 설명할 것이니 지금은 생각하지 않아도 됩니다. 금 우리가 중요하게 알아야 할 것은 메시지를 다른 액터에게`Tell()`하여 보내는 것입니다.

```csharp
//send a string to an actor
someActorRef.Tell("this is a message too!");
```

### 액터가 할 수 있는 일은?
당신이 작성할 수 있는 모든 코드를 동작할 수 있습니다. 정말로! :)

당신이 액터를 자신에게 수신된 메세지를 처리하도록 작성하면, 액터는 메세지 처리에 따라 뭐든지 할 수 있습니다. 데이터베이스 처리, 파일 IO, 내부 변수 변경, 등 당신이 필요하다고 생각하는 뭐든지.

추가로 액터는 수신 메세지를 처리하는 것 외에도 다음과 같은 것들을 할 수 있습니다:

1. 다른 액터를 생성
1. 다른 액터에게 메세지 전송 (현재 메세지를 전송한 `Sender`에게 처럼)
1. 자신의 동작을 변경하여 다음 수신 메세지를 다른 방식으로 처리

Actors are inherently asynchronous (more on this in a future lesson), and there is nothing about the [Actor Model](https://en.wikipedia.org/wiki/Actor_model) that says which of the above an actor must do, or the order it has to do them in. It's up to you.
액터는 선천적으로 비동기 방식입니다(다음에 더 자세히 설명하겠습니다). 그리고 [액터모델(Actor Model)](https://en.wikipedia.org/wiki/Actor_model)에서는 어떤 액터가 명령을 내려야 하고, 수행을 해야 하는 지는 정해져 있지 않습니다. 당신에게 달려있습니다.

### 어떤 종류의 액터가 있는가?
모든 형태의 액터는 `UntypedActor`를 상속받습니다. 하지만, 이 점을 벌써 신경 쓰지 않아도 됩니다.
우리는 나중에 각양각색의 액터 타입을 다룰 것입니다.

일단 Unit 1에서 사용할 모든 액터는 [`UntypedActor`](https://getakka.net/articles/actors/untyped-actor-api.html?q=untyped%20actor)를 상속받을 것입니다.

### 액터는 어떻게 만들죠?
알아야 할 두 가지의 키 포인트가 있습니다:

1. 모든 액터들은 관련된 context 내에서 생성됩니다. 즉, 그들의 "actor of" 문맥(context)입니다.
1. 액터를 만들기 위해 `Props`가 필요합니다. `Props` 객체는 단지 주어진 종류의 액터를 만들기 위한 공식을 캡슐화한 객체입니다.

`Props`에 대한 자세한 설명은 세 번째 레슨에서 이어집니다. 지금 당장 큰 걱정 안 하셔도 됩니다. 제공한 코드에 `Props`가 포함되어 있기 때문에, 단지 어떻게 `Props`를 사용하여 액터를 만드는지 이해하면 됩니다.

우리가 당신에게 줄 힌트는 당신의 첫 액터들이 액터 시스템(ActorSystem) 자체의 `Context` 내에서 생성된다는 것입니다.

### `액터시스템(ActorSystem)`이 뭐죠?
`ActorSystem`은 Underlying System과 Akka.NET 프레임워크에 대한 참조(reference)입니다. 모든 액터들은 이 액터 시스템의 context 내에서 동작합니다. 당신은 이 `ActorSystem`의 context에서 첫 번째 액터들를 만들어야 합니다.

참고로, `ActorSystem`은 무거운 객체입니다: 각 어플리케이션에 하나만 생성합니다.

일단 지금은 이 정도 개념이면 충분합니다. 그러므로 당장 당신의 첫 번째 액터들을 만듭시다.

## 실습
Let's dive in!

> Note: 샘플 코드 내 `“YOU NEED TO FILL IN HERE”`라고 명시된 섹션이 있습니다. - 코드에서 해당 표시를 찾고 레슨에 따라 적절한 동작을 작성하세요.

### Launch the fill-in-the-blank sample
Go to the  folder and open  in Visual Studio. The solution consists of a simple console application and only one Visual Studio project file.
[DoThis](../DoThis/) 폴더로 가서 [WinTail](../DoThis/WinTail.csproj)을 Visual Studio로 여세요. 솔루션은 간단한 콘솔 어플리케이션과 하나의 비주얼 스튜디오 프로젝트 파일로 구성되어 있습니다.

당신은 이 솔루션 파일을 Unit 1 내내 사용할 것입니다.

### 최신 Akka.NET NuGet 패키지를 설치하자
패키지 매니저 콘솔에서 다음 명령어를 입력하세요:

```
Install-Package Akka
```

이 명령어는 당신이 샘플을 컴파일하기 위해 필요로 할 최신 Akka.NET 바이너리를 설치할 것입니다.

그리고, `Program.cs` 상단에 namespace `사용 선언`을 다음과 같이 추가합니다:

```csharp
// in Program.cs
using Akka.Actor;
```

### 당신의 첫 번째 `ActorSystem`을 만들어봅시다.
`Program.cs`로 가서 다음을 추가해 당신의 첫 번째 액터 시스템을 생성합니다:

```csharp
MyActorSystem = ActorSystem.Create("MyActorSystem");
```
>
> **NOTE:** `Props`, `ActorSystem`, `ActorRef`를 만들 때, 드물게 `new` 키워드를 볼 수 있습니다. 이 객체들은 반드시 Akka.NET에 포함된 팩토리 메소드를 통하여 생성되어야 합니다. 만약 당신이 `new`를 사용했다면, 실수한 것일지 모릅니다.

### ConsoleReaderActor & ConsoleWriterActor 만들기
액터 클래스들은 자체적으로 이미 정의되어 있지만, 당신의 첫 번째 액터들을 생성해야 합니다.

다시 `Program.cs`로 가서 당신이 생성한 `ActorSystem`코드 밑에 아래의 코드를 추가합니다:

```csharp
var consoleWriterActor = MyActorSystem.ActorOf(Props.Create(() =>
new ConsoleWriterActor()));
var consoleReaderActor = MyActorSystem.ActorOf(Props.Create(() =>
new ConsoleReaderActor(consoleWriterActor)));
```

`Props`와 `ActorRefs`에 관한 자세한 내용은 레슨3에서 설명할 것입니다, 그러니 이에 대해 지금 걱정하지 않아도 됩니다. 단지, 액터가 어떻게 만들어지는지만 알아둡시다.

### ConsoleReaderActor에서 ConsoleWriterActor로 메세지 보내기
당신의 첫 번째 액터들에게 작업을 줄 시간입니다!

따라해봅시다:

1. ConsoleReaderActor는 콘솔에서 읽어오도록 설정되어 있습니다. 읽어온 내용물을 담은 메세지를 ConsoleWriterActor에게 보냅니다.

	```csharp
	// in ConsoleReaderActor.cs
	_consoleWriterActor.Tell(read);
	```

2. ConsoleWriterActor에게 메세지를 보낸 다음, ConsoleReaderActor 자신에게 메세지를 보냅니다. 이 메세지는 Read 작업 루프를 반복하도록 만듭니다.

	```csharp
	// in ConsoleReaderActor.cs
	Self.Tell("continue");
	```
3. 그리고 ConsoleReaderActor의 읽기 동작이 시작할 수 있도록 초기화 메세지를 보냅니다.

	```csharp
	// in Program.cs
	consoleReaderActor.Tell("start");
	```

### 5단계: Build and Run!
모든 작업이 끝났으면, 비주얼 스튜디오에서 `F5`를 눌러 샘플을 컴파일 및 실행합니다.

화면이 다음과 같이 나타날 경우, 정상적으로 작동하는 것입니다:
![Petabridge Akka.NET Bootcamp Lesson 1.1 Correct Output](Images/example.png)

> **N.B.** Akka.NET 1.0.8 이상에서는, JSON.NET serializer가 Akka.NET (1.5)부터 사용되지 않을 것이라는 deprecated 경고가 나타날 것입니다. 기본 Newtonsoft.Json 시리얼 라이저가 [Hyperion](https://github.com/akkadotnet/Hyperion)으로 대체됩니다. 이는 주로 기본 serializer에 종속된 Akka.Persistence or Akka.Remote를 사용하는 Akka.NET 사용자에게 나타나는 경고입니다.

### 레슨을 마치고,
작성한 코드와 [Completed](Completed/)의 코드를 비교하며 샘플에 어떤 것이 추가 및 수정되었는지 확인 해봅시다.

## 수고하셨습니다! 이제 레슨2 차례입니다.
정말 잘했어요! 첫 수업을 잘 마쳤습니다.

이제 [Akka 기초 1-2 : 메시지 정의 및 핸들링](../lesson2/README.md)을 향해 나아가 봅시다.