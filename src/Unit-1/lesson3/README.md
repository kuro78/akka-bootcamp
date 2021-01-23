# Akka 시작하기 1-3 : `Props`와 `IActorRef`
이번 레슨은 액터를 만들고 메시지를 보내는 여러 방법에 대해 복습 및 보강을 해보도록 합니다. 이번 강의는 코딩이 별로 없고, 더욱 개념적인 내용이지만 앞으로 만날 코드를 이해하기 위한 핵심요소 이자 필수 기반 입니다.

또한 코드가 조금 변경되었습니다. `ConsoleReaderActor`에선 더는 유효성 검사를 하지 않습니다. 대신 콘솔에서 받은 메시지를 유효성 검사를 담당할 액터(`ValidationActor`)에게 전달합니다.

## Key concepts / background
### `IActorRef`
#### `IActorRef`란?
[`IActorRef`](http://api.getakka.net/docs/stable/html/56C46846.htm "Akka.NET Stable API Docs - IActorRef")는 액터에 대한 참조(reference) 또는 핸들(handle)입니다. `IActorRef`의 목적은 `ActorSystem`을 통해 **액터에게 메시지를 전송하는 것을 지원하는 것** 입니다. 당신은 액터에게 직접적으로 말을 걸지 않습니다. `IActorRef`에게 메시지를 보내고, `ActorSystem`은 당신을 위해 메시지를 전달합니다.

#### 내 액터와 실제로 대화하지 않아요? 왜 안돼요?
직접적이지 않을 뿐, 액터와 대화를 나누고 있어요. :) 액터들과 대화를 할때는 `ActorSystem`의 중개를 통해 대화해야 합니다.

`IActorRef`로 메시지를 보내고, `ActorSystem`이 실제 액터에게 메시지를 전달하는 것이 왜 더 좋은지에 대한 두 가지 이유가 여기에 있습니다.
- 메시지 의미론적으로 당신이 작업하기에 더 좋은 정보를 제공합니다. `ActorSystem`은 모든 메시지를 각 메시지에 대한 메타 데이터를 포함하여 `Envelope`으로 포장합니다.
- **"위치 투명성"**을 허용합니다: 이것은 액터가 어느 프로세스나 머신에 올라와 있는지 걱정할 필요가 없다는 멋진 말입니다. 모든 추적 작업은 시스템이 할 일입니다. 이것은 원격 액터를 허용하는 데 필수적입이고, 방대한 양의 데이터를 처리하기 위해 액터의 시스템을 확장하는 방법입니다. (예: 클러스터 안의 여러 머신에서 동작하는 액터). 자세한 설명은 다음에 하겠습니다.

#### 내 메시지가 액터에게 절달되었음을 어떻게 알 수 있나요?
일단 지금은, 이것에 대해 고려하지 않아도 됩니다. Akka.NET의 `ActorSytstem`에서 이를 보장하는 매커니즘을 제공하지만, `GuaranteedDeliveryActors`는 고급 주제입니다.

지금은 단지 메시지 전달 작업이 당신이 아닌 `ActorSystem`이 할 일이라고 믿고 갑시다.

#### 그래서 `IActorRef`는 어떻게 얻을 수 있나요?
`IActorRef`를 얻는 두 가지 방법이 있습니다.
##### 1) 액터 생성
액터들은 본질적인 감독(supervision) 계층을 형성합니다. (자세한 내용은 5번째 강의에서 하겠습니다.) 이것은 `ActorSystem`에 직접 보고하는 **최상위** 액터와 다른 액터에게 보고하는 **"자식(child)"** 액터가 있음을 의미합니다.

액터를 만들려면, 이것의 컨텍스트에서 만들어야 합니다. 그리고 **당신은 이미 이것을 끝냈습니다!** 기억합니까?
```cs
// "MyActorSystem" 이라는 액터 시스템을 가지고 있다고 가정 합니다.
IActorRef myFirstActor = MyActorSystem.ActorOf(Props.Create(() => new MyActorClass()), "myfirstActor");
```
위의 예제에서 볼 수 있듯이, 항상 이것을 감독할 액터의 컨텍스트에서 액터를 만듭니다. 위와 같이 `ActorSystem`위에서 직접 액터를 만들면 최상위 액터가 됩니다.

자식 액터 또한 똑같은 방법으로 만듭니다. 다음과 같이 다른 액터에서 만드는 것을 제외하면 말이죠:
```cs
// MyActorClass 내부에서 child actor를 만들어야 할 때도 있습니다.
// 보편적으로 OnReceive 나 PreStart 안에서 일어납니다.
class MyActorClass : UntypedActor
{
    protected override void Prestart()
    {
        IActorRef myFirstChildActor = Context.ActorOf(Props.Create(() => new MyChildActorClass()), "myFirstChildActor");
    }
}
```
**\*주의* :** 액터를 만들기 위해 `Props`와 `ActorSystem`의 외부에서 ```new MyActorClass()```를 호출하지 마십시오. 자세한 사항을 여기에 다 적을 수 없지만, `ActorSystem`의 컨텍스트 외부에서 액터를 만든다면 완전히 쓸모없고 바람직하지 못한 오브젝트가 만들어질 것입니다.

##### 2) 액터 찾기
모든 액터는 시스템 계층 속에서 어디에 있는지를 나타내는 기술적인 (`ActorPath`라는 이름의) 주로를 가지고 있습니다. 당신은 주소로 액터를 찾아서(`IActorRef`를 입수하여) 핸들을 얻을 수도 있습니다.

이에 대한 자세한 내용은 다음 강의에서 더욱 자세히 다루겠습니다.

#### 액터들에게 꼭 이름을 지어야 하나요?
위에서 액터를 만들 때 `ActorSystem`에 이름을 같이 전달한 것을 알아차리셨을 수도 있겠습니다:
```cs
// ActorOf()를 호출할 때의 마지막 Argument가 이름입니다.
IActorRef myFirstActor = MyActorSystem.ActorOf(Props.Create(() => new MyActorClass()), "myFirstActor");
```
이름은 필수가 아닙니다. 아래와 같이 이름 없이 액터를 만들 수도 있습니다:
```cs
// ActorIf() 항수를 호출할 때, 마지막 argument로 이름을 주지 않았습니다.
IActorRef myFirstActor = MyActorSystem.ActorOf(Props.Create(() => new MyActorClass()));
```
**이름을 지어주는 것이 가장 좋은 방법입니다.** 당신의 액터 이름은 로그 메시지와 액터의 식별에 사용되기 때문입니다. 습관으로 만드세요. 디버깅을 해야하는 상황에서 훌륭한 라벨이 붙어 있을 때, 미래의 당신은 자기 자신에게 고마워할 것입니다.

#### 컨텍스트(`Context`)는 어디에 사용되나요?
모든 액터들은 컨텍스트가 존재하며, 모든 액터들에 내장된 `Context`속성으로 접근할 수 있습니다.

`Context`에는 현재 메시지의 `Sender`, 현재 액터의 `Parent`, `Children`과 같은 액터의 현재 상태에 관련된 메타 데이터가 있습니다.
`Parent`, `Children`, 그리고 `Sender` 모두 `IActorRef`를 제공하고 사용할 수 있습니다.

### Props
#### `Props`란?
[`Props`](http://api.getakka.net/docs/stable/html/CA4B795B.htm "Akka.NET Stable API Documentation - Props class")는 액터를 만들기 위한 레시피로 생각하십시오. 전문적으로 `Props`는 주어진 액터 타입의 인스턴스를 만들기 위해 필요한 정보를 캡슐화하는 **configuration class** 입니다.

#### 왜 `Props`가 필요합니까?
`Props` 오브젝트는 액터의 인스턴스를 생성하는 것에 사용하는 공유 가능한 레시피 입니다. `Props`는 `ActorSystem`으로 전달되어 당신이 사용할 액터를 생성합니다.

지금은 아마 `Props`가 조금 버겁게 느껴질 수 있습니다. (그렇다고 걱정할 필요는 없습니다.)

우리가 봐왔던 것과 같은 대부분의 기본적인 `Props`는 액터를 만들기 위해 필요한 구성요소들만 포함하는 것처럼 보입니다. 클래스와 필수 인자들, 생성자 처럼요.

**하지만**, 아직 보지 못한 점은 `Props`는 원격 작업을 수행하는데 필요한 배포(deployment) 정보와 다른 세부적인 설정을 포함하도록 확장된다는 것 입니다. 예를 들어 `Props`는 직렬화 할 수 있으므로(serializable), 네트워크 어딘가의 다른 머신에서 전체 액터 그룹을 원격으로 생성하고 배포할 수 있습니다.

이것은 진도보다 앞서가는 내용이지만, 짧게 정리하면 우리는 Akka.NET에서 제공하는 흥미를 꿀고 강한 힘을 가진 다양한 응용 기능들(클러스버링, 원격 액터, 등)을 지원하기 위해 `Props`를 필요로 합니다.

#### `Props`를 어떻게 만들 수 있나요?
`Props`를 만드는 법을 말하기 전에, 하면 **안 되는** 것을 말하겠습니다.
**PROPS를 만들기 위해 `new Props(...)`를 호출하지 마십시오.** 액터를 만들기 위해 `new MyActorClass()`를 호출하는 것과 처럼, 이것은 프레임워크와 싸우고 Akka의 `ActorSystem`에게 액터의 재시작, 라이프사이클 관리와 관련한 안전보장 제공을 시키지 못합니다.

여기에 올바를게 `Props`를 만드는 3가지 방법이 있으며, 전부 `Props.Create()` 호출을 포함하고 있습니다.
1. **`typeof` 문법**
   ```cs
   Props props1 = Props.Create(typeof(MyActor));
   ```
   단순해 보이지만, **이 방법을 추천하지 않습니다.** _이것은 타입 안정성(type safety)이 없으며, 컴파일을 통과하고 런타임에 터뜨리는 버그를 쉽게 만들게 합니다._
2. **lambda 문법**
   ```cs
   Props props2 = Props.Create(() => new MyActor(..), "...");
   ```
   이것은 훌륭한 문법이며, 우리가 좋아하는 방법입니다. 당신은 액터 클래스의 생성자에 필요한 인자들을 이름과 함께 전달할 수 있습니다.
3. **generic 문법**
   ```cs
   Props props3 = Props.Create<MyActor>();
   ```
   이것 또한 우리가 전적으로 권하는 또 다른 훌룡한 문법입니다.

#### `Props`는 어떻게 사용하나요?
당신은 이미 이것을 알고 있고, 사용해 봤습니다. `Props` - 액터 레시피 - 를 `Context.ActorOf()` 호출에 전달하고, `ActorSystem`은 이 레시피를 읽습니다. _et voila!_ 새 액터를 얻었습니다.

개념적인 이야기는 이미 충분하니, 직접 사용해 봅시다.

## 실습
이 강의의 핵심(`Props`와 `IActorref`)을 만나기 전에, 우리는 다소 정리 작업을 진행해야 합니다.

### 1단계: 유효성 검사를 별도의 액터로 만들기
모든 유효성 검사를 별도의 액터로 옮길 것입니다. 유효성 검사 코드들은 `ConsoleReaderAcotr`에 속하지 않습니다. 유효성 검사는 자체 액터를 가질 가치가 있습니다. (OOP에서 단일목적 - single-purpose - 오브젝트를 지향하는 것과 비슷합니다.)

#### `ValidationActor` 클래스 생성
`ValidationActor`라는 이름의 새로운 클래스와 파일을 만듭니다. `ConsoleReaderAcotr`에 있는 유효성 검사 로직을 모두 옮깁니다.
```cs
// ValidationActor.cs
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// 사용자의 입력을 유효성 검사하고 다른 액터로 결과 신호를 전달하는 액터
    /// </summary>
    public class ValidationActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;
        public ValidationActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                // signal that the user needs to supply an input
                _consoleWriterActor.Tell(
                  new Messages.NullInputError("No input received."));
            }
            else
            {
                var valid = IsValid(msg);
                if (valid)
                {
                    // send success to console writer
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Thank you! Message was valid."));
                }
                else
                {
                    // signal that input was bad
                    _consoleWriterActor.Tell(new Messages.ValidationError("Invalid: input had odd number of characters."));
                }
            }

            // tell sender to continue doing its thing
            // (whatever that may be, this actor doesn't care)
            Sender.Tell(new Messages.ContinueProcessing());
        }

        /// <summary>
        /// Determines if the message received is valid.
        /// Checks if number of chars in message received is even.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static bool IsValid(string msg)
        {
            var valid = msg.Length % 2 == 0;
            return valid;
        }
    }
}
```

### 2단계: 우리의 액터 레시피, `Pros` 만들기
좋습니다. 이제 우리는 좋은 물건을 엎을 수 있습니다! 우리가 배운 `Props`를 사용할 것이고, 액터들을 만드는 방법을 바꿀 것입니다.

다시 말하지만, `typeof` 문법의 사용을 권하지 않습니다. 연습 삼아 lambda와 generic 문법을 사용해 주세요!

> **기억하세요:** 절대 `new Props(...)`를 호출하여 `Props`를 만들지 마십시오. 
> 
> 이 방법을 사용하면 고양이가 죽고, 유니콘이 사라지고, 모르도르가 이기고 온갖 나쁜일이 벌어집니다. 그냥 하지 마세요.

이 섹션에서는 `Props` 오브젝트를 읽기 쉽게 만들기 위해 줄을 나눌 겁니다. 실제로는 우리는 대게 `ActorOf` 호출 안에 인라인으로 사용합니다.

#### 이전의 `Props`와 `IAcotrRef` 삭제
`Main()`에 존재하는 액터 선언을 지워 백지상태를 만듭니다.

다음과 같이 나타나게 될 것입니다:
```cs
// Program.cs
static void Main(string[] args)
{
    // initialize MyActorSystem
    MyActorSystem = ActorSystem.Create("MyActorSystem");

    // nothing here where our actors used to be!

    // tell console reader to begin
    consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

    // blocks the main thread from exiting until the actor system is shut down
    MyActorSystem.WhenTerminated.Wait();
}
```

#### `consoleWriterProps` 생성
`Program.cs`로 돌아갑니다. `Main()`안에 `consoleWriterProps`를 다음과 같이 추가합니다:
```cs
// Program.cs
Props consoleWriterProps = Props.Create(typeof(ConsoleWriterActor));
```
여기서 당신은 우리가 `typeof` 문법을 사용한 것을 볼 수 있습니다. 다시 한 번 강조하지만, **`typeof` 문법을 실제로 사용하지 않는 것이 좋습니다.**

앞으로, 우리는 `Props`에 lambda와 generic 문법만 사용할 것입니다.

#### `validationActorProps` 생성
`Main()`에 아래의 코드 또한 추가합니다:
```cs
// Program.cs
Props validationActorProps = Props.Create(() => new ValidationActor(consoleWriterActor));
```
보시는것 처럼, 여기에 우리는 lambda 문법을 사용했습니다.

#### `consoleReaderProps` 생성
`Main()`에 아래의 코드 또한 추가합니다:
```cs
// Program.cs
Props consoleReaderProps = Props.Create<ConsoleReaderActor>(validationActor);
```
여기에는 generic 문법을 사용했습니다. `Props`는 generic 타입의 액터 클래스 인자를 받고, 생성자에 필요한 것들을 전달합니다.

### 3단계: `Props`를 사용해서 `IActorRef` 만들기
이제 우리는 모든 액터의 Props를 가졌습니다, 이제 애터들을 만들어 봅시다.

**기억하세요:** 액터를 만들기 위해 `Props`오브젝트 밖에서, `ActorSystem`의 컨텍스트 혹은 기타 `IActorRef` 밖에서 `new Actor()`를 호출하지 마세요. 모르도르가 이기고... 기억하죠?

#### `consoleWriterActor`를 위한 `IActorRef` 만들기
`Main()` 에서 `consoleWriterProps` 밑에 다음 코드를 추가합니다:
```cs
// Program.cs
IActorRef consoleWriterActor = MyActorSystem.ActorOf(consoleWriterPros, "consoleWriterActor");
```

#### `validationActor`를 위한 `IActorRef` 만들기
`Main()`에서 `validationActiorProps` 밑에 다음 코드를 추가합니다:
```cs
// Program.cs
IActorRef validationActor = MyActorSystem.ActorOf(validationActorProps, "validationActor");
```

#### `consoleReaderActor`를 위한 `IActorRef` 만들기
`Main()`에서 `consoleReaderProps` 밑에 다음 코드를 추가합니다:
```cs
// Program.cs
IActorRef consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");
```

#### 특별한 `IActorRef` 호출: `Sender`
눈치채지 못했을 수도 있겠지만, 우리는 특별한 `IActorRef`를 이미 사용하고 있습니다: `Sender`.`ValidationActorcs`안의 여기를 봐주세요:
```cs
// tell sender to continue doing its thing
// (whatever that may be, this actor doesn't care)
Sender.Tell(new Messages.ContinueProcessing());
```
메시지를 처리할 때 액터의 컨텍스트 내부에서 만들어지는 특별한 `Sender` 핸들입니다.
그 컨텍스트는 항상 다른 메타 데이터와 함께 참조할수 있도록 만들어집니다.(자세한 내용은 다음에...)

### 4단계: 정리
클래스 구조를 바꾼 것과 관련해 약간 정리하고나면 다시 앱을 실행할 수 있습니다!

#### `consoleReaderActor` 수정
이제 `ValidationActor`가 유효성 검사 작읍을 수행하고 있으므로, `ConsoleReaderActor`의 크기를 줄여붑시다. `ConsoleReaderActor`를 정리하고 유효성 검사를 위해 `ValidationActor`에게 메세지를 전송하도록 해밥시다.

또한 우리는 `ConsoleReaerActor`안에 `ValidationActor`의 참조를 가지게 해야 하므로, `ConsoleWriterActor`에 대한 참조가 더이상 필요하지 않습니다. 바로 정리해 보죠.

당신의 `ConsoleReaderActor`를 아래와 같이 수정합니다:
```cs
// ConsoleRaderActor.cs
using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.shutdown"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        public const string StartCommand = "start";
        public const string ExitCommand = "exit";
        private IActorRef _validationActor;

        public ConsoleReaderActor(IActorRef validationActor)
        {
            _validationActor = validationActor;
        }

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }
            
            GetAndValidateInput();
        }

        #region Internal methods
        private void DoPrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.WriteLine("Some entries will pass validation, and some won't...\n\n");
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }
        
        /// <summary>
        /// Reads input form console, validate it, then signals appropriate response
        /// (continue processing, error, success, etc.)
        /// </summary>
        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();

            if (!string.IsNullOrEmpty(message) &&
                String.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // if user typed ExitCommand, shut down the entire actor
                // system (allows the process to exit)
                Context.System.Terminate();
                return;
            }

            //otherwise, just hand message off to validation actor
            // (by telling its actor ref)
            _validationActor.Tell(message);
        }
        #endregion
    }
}
```
보이는 바와 같이, 유효성 검사 및 결정을 위해 콘솔에서 들어오는 입력을 `ValidationActor`에게 전달합니다. `ConsoleReaderActor`는 이제 단지 콘솔에서 읽고 다른 복잡한 액터에게 데이터를 전달하는 일을 수행합니다.

#### 첫 번째 `Props` 호출 수정...
우리는 `typeof` 문법을 사용하는 것을 그대로 둘 수 없습니다. 당장 빨리, `Main()`으로 돌아가서 `consoleWriterProps`를 generic 문법을 사용하도록 수저하세요.
```cs
Props consoleWriterProps = Props.Create<ConsoleWriterActor>();
```

### 레슨을 마치고,
작성한 코드와 [Completed](Completed/)의 코드를 비교하며 샘플에 어떤 것이 추가 및 수정되었는지 확인해봅시다.
모든 작업이 정상적으로 작동된다면, 출력은 지난번과 같아야 합니다:

![Petabridge Akka.NET Bootcamp Lesson 1.2 Correct Output](Images/working_lesson3.jpg)

#### `Props`의 `typeof` 문법 위험성을 직접 경험해보고 싶을 경우
우린 앞서 지겹도록 말한 `typeof` `props`의 위험성과 왜 우리가 피해야 하는지 알아봅시다.
1. [Completed/Program.cs](Completed/Program.cs)을 엽니다.
2. `fakedActorProps`와 `fakeActor`가 포함된 라인을 찾습니다. (18번째 라인 근처에 있습니다.)
3. 주석을 해제합니다.
   - 우리가 무엇을 하는지 확인해 봅시다 - 의도적으로 액터가 아닌 클래스를 `Props` 오브젝트에 넣고 있습니다! 터무니 없는! 이런 끔찍한 짓을!
   - 솔직히 말도 안 되는 우스꽝스러운 예시지만, 정확하게 요점을 말해주고 있습니다. 고의적으로 실수를 저지르고 있는 겁니다.
4. 솔루션을 빌드합니다. 이 터무니없는 코드가 에러 없이 컴파일되므로 공포에 주의 하십시오!
5. 솔루션을 실행합니다.
6. 프로그램이 해당 라인에 도달하게 될 경우 어떤 상황이 발생하는지 관찰합니다.

좋아요, 핵심이 무었이었죠? 예시와 같이 `Props`의 `typeof` 문법을 사용하는 것은 타입 안정성(type safety)이 없으며 _사용해야 할 적절한 이유가 없다면 피하는 것이 가장 바람직합니다._

## 수고하셨습니다! 이제 레슨4 차례입니다.
수고하셨습니다! 레슨3을 무사히 끝냈습니다. 이번 레슨은 꾀나 큰 주제였습니다.

이제 [Akka 시작하기 1-4 : 자식 액터, 액터 계층 구조, 그리고 감시(Supervision)](../lesson4/README.md)를 향해 나아가 봅시다.