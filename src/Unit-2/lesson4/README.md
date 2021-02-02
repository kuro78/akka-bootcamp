# Akka 중급 2-4 : `BecomeStacked` 와 `UnbecomeStacked`를 사용하여 런타임에 액터 동작 전환

이번 레슨에서 액터가 할 수 있는 정말 멋진 일 중 하나에 대해 배울 것입니다: [런타임에 동작 변경하기](https://getakka.net/articles/actors/receive-actor-api.html#becomeunbecome "Akka.NET - Actor behavior hotswap")!

## Key Concepts / Background
액터의 행동을 바꿀 시 있는 능력이 필요한 실제 시나리오부터 시작해 볼까요?

### 실제 시나리오: 인증
Akka.Net 액터를 사용해 간단한 채팅 시스템을 구축한다고 가정해 보겠습니다. 특정 사용자와의 모든 통신을 담당하는 액터가 당신이 원하는 `UserActor`의 모습입니다.

```csharp
public class UserActor : ReceiveActor {
	private readonly string _userId;
	private readonly string _chatRoomId;

	public UserActor(string userId, string chatRoomId) {
		_userId = userId;
		_chatRoomId = chatRoomId;
		Receive<IncomingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// print message for user
			});
		Receive<OutgoingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// send message to chatroom
			});
	}
}
```

자, 기본적인 채팅 액터를 만들었습니다. 예! 하지만 지금 당장은 사용자 자신이 말하는 사람인지 보장 할 수 없습니다. 시스템에 뭔가 인증수단이 필요합니다.

아래와 같이 같은 타입의 채팅 메시지를 다르게 처리할 수 있도록 액터를 다시 작성할 수 있을까요?

* **인증 중인** 사용자
* **인증된** 사용자이거나
* **인증할 수 없는** 사용자?

간단합니다: 액터의 전환 가능 동작(switchable behavior)을 이용해 할 수 있습니다.

### 전환 가능 동작(siwtchable behavior)이 무엇인가요?
[Actor Model](https://en.wikipedia.org/wiki/Actor_model)에서 액터의 핵심 속성중 하나는 액터가 처리하는 메시지간에 행동을 변경할 수 있다는 것입니다.

이 기능을 사용하면, [유한 상태 머신](http://en.wikipedia.org/wiki/Finite-state_machine) 빌드나 액터가 수신한 메시지에 따라 메시지를 처리하는 방식을 변경하는 것과 같은 모든 종류의 작업을 수행할 수 있습니다.

전환 가능 동작은 진정한 액터 시스템의 가장 강력하고 기본적인 기능 중 하나입니다. 액터의 재사용을 가능하게하는 주요 기능 중 하나이며, 매우 작은 코드 풋프린트로 엄청난 양의 작업을 수행할 수 있도록 도와줍니다. 

전환 가능 동작은 어떻게 동작합니까?

#### 동작 스택(Behavior Stack)
Akka.NET 액터는 "동작 스택(behavior stack)"이라는 개념을 가지고 있습니다. 동작 스택의 맨 위에있는 방법은 액터의 현재 동작을 정의합니다. 현재 동작은`Authenticating()`입니다:

![Initial Behavior Stack for UserActor](images/behaviorstack-initialization.png)

#### `Become` 과 `BecomeStacked`를 사용하여 새로운 동작을 채택
[`BecomeStacked`](http://api.getakka.net/docs/stable/html/33B96712.htm "Akka.NET Stable API - BecomeStacked method")를 호출할 때마다 `ReceiveActor`에게 새로운 동작을 스택에 푸시하도록 지시합니다. 이 새로운 동작은 액터에게 전달된 메시지를 처리하는 데 사용할 `Receive` 메소드를 지정합니다. 

예제 액터가 `BecomeStacked`를 통해 `Authenticated`가 될 때 동작 스택에 일어나는 일입니다:

![Become Authenticated - push a new behavior onto the stack](images/behaviorstack-become.gif)

> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson4/images/behaviorstack-become.gif).

> NOTE: [`Become`](http://api.getakka.net/docs/stable/html/1DBD4D33.htm "Akka.NET Stable API - Become method")은 스택에서 이전 동작을 삭제하므로 스택에는 한 번에 하나 이상의 동작이 포함되지 않습니다. 
>
> 동작을 스택에 푸시하려면 [`BecomeStacked`](http://api.getakka.net/docs/stable/html/33B96712.htm "Akka.NET Stable API Docs - BecomeStacked method")를 사용하고 이전 동작으로 되돌리려면 [`UnbecomeStacked`](http://api.getakka.net/docs/stable/html/7D8311A9.htm "Akka.NET Stable API Docs - UnbecomeStacked method")를 사용합니다. 사용자는 대부분 `Become`만 사용하면됩니다. 


#### `UnbecomeStacked`를 사용하여 이전 동작으로 되돌리기
액터가 동작 스택의 이전 동작으로 되돌아 가도록하려면 `UnbecomeStacked`를 호출하기만 하면됩니다. 

`UnbecomeStacked`를 호출할 때마다 스택에서 현재 동작을 꺼내서 이전 동작으로 대체합니다(다시 말하지만이 새로운 동작은 들어오는 메시지를 처리하는 데 사용되는 `Receive` 메서드를 말합니다). 

다음은 예제 액터 `UnbecomeStacked`가 작동할 때 동작 스택에 일어나는 일입니다:

![Unbecome - pop the current behavior off of the stack](images/behaviorstack-unbecome.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson4/images/behaviorstack-unbecome.gif).


#### 동작을 변경하는 API는 무엇입니까? 
동작을 변경하는 API는 정말 간단합니다:

* `Become` - 현재 수신 루프를 지정된 루프로 바꿉니다. 동작 스택을 제거합니다. 
* `BecomeStacked` - 지정된 메서드를 동작 스택의 맨 위에 추가하고 그 아래의 이전 메서드를 유지합니다.
* `UnbecomeStacked` - 스택에서 이전에 수신한 메서드로 되돌립니다 (`BecomeStacked`에서만 작동). 

`BecomeStacked`는 이전 동작을 보존하므로 `UnbecomeStacked`를 호출하여 이전 동작으로 돌아갈 수 있다는 것이 차이점 입니다. 다른 무엇보다 당신의 필요에 달려 있습니다. 필요한만큼 `BecomeStacked`를 호출할 수 있으며, `BecomeStacked`를 호출한 횟수만큼 `UnbecomeStacked`를 호출할 수 있습니다. 현재 동작이 스택의 유일한 동작 인 경우 `UnbecomeStacked`에 대한 추가 호출은 아무 작업도 수행하지 않습니다. 


### 액터가 행동을 바꾸는 것이 문제가되지 않나요?
아니요, 실제로 안전하며 `ActorSystem`에 엄청난 유연성과 코드 재사용을 제공하는 기능입니다. 

다음은 전환 가능 동작에 대한 몇 가지 일반적인 질문입니다:

#### 새로운 동작은 언제 적용되나요? 
[Akka.NET 액터는 한 번에 하나의 메시지 만 처리](http://petabridge.com/blog/akkadotnet-async-actors-using-pipeto/)하므로 액터 메시지 처리 동작을 안전하게 전환 할 수 있습니다. 새 메시지 처리 동작은 다음 메시지가 도착할 때까지 적용되지 않습니다. 

#### `Become`이 동작 스택을 날려 버리는 것이 나쁘지 않나요? 
아니요, 그렇진 않아요. 지금까지 가장 일반적으로 사용되는 방식입니다. 한 동작에서 다른 동작으로 명시적으로 전환하는 것이 동작 전환에 사용되는 가장 일반적인 방식입니다. 간단하고 명시적인 스위치를 사용하면 코드를 훨씬 쉽게 읽고 추론할 수 있습니다.

실제로 동작 스택을 활용해야 하는 경우 - 단순하고, 명시적인 `Become (YourNewBehavior)`이 상황에 맞지 않는 경우 - 동작 스택을 사용할 수 있습니다.

이 레슨에서는 `BecomeStacked`와 `UnbecomeStacked`를 사용하여 시연합니다. 보통 우리는 그냥 `Become`을 사용합니다. 

#### 동작 스택의 깂이는 얼마나 되나요? 
스택은 *정말* 깊을 수 있지만, 무제한은 아닙니다. 

또한 액터가 다시 시작될 때마다 동작 스택이 지워지고 사용자가 코드화한 초기 동작부터 액터가 시작됩니다.

#### `UnbecomeStacked`를 호출하고 동작 스택에 아무것도 남지 않으면 어떻게 되나요?
*아무것도요* - `UnbecomeStacked`는 안전한 방법이며 현재 동작이 스택의 유일한 동작인 경우 아무 작업도 수행하지 않습니다. 

### 실제 사례로 돌아 가기
이제 전환 가능 동작을 이해 했으므로, 실제 시나리오로 돌아가서 어떻게 사용되는지 살펴 보겠습니다. 채팅 시스템 액터에 인증을 추가해야 합니다. 

따라서 다음과 같은 경우 채팅 메시지를 다르게 처리하도록이 액터를 어떻게 다시 작성할 수 있습니까? 

* **인증 중인** 사용자
* **인증된** 사용자이거나
* **인증할 수 없는** 사용자?

다음은 기본 인증을 처리하기 위해 `UserActor`에서 전환 가능 메시지 동작을 구현할 수있는 한 가지 방법입니다: 

```csharp
public class UserActor : ReceiveActor {
	private readonly string _userId;
	private readonly string _chatRoomId;

	public UserActor(string userId, string chatRoomId) {
		_userId = userId;
		_chatRoomId = chatRoomId;

		// start with the Authenticating behavior
		Authenticating();
	}

	protected override void PreStart() {
		// start the authentication process for this user
		Context.ActorSelection("/user/authenticator/")
			.Tell(new AuthenticatePlease(_userId));
	}

	private void Authenticating() {
		Receive<AuthenticationSuccess>(auth => {
			Become(Authenticated); //switch behavior to Authenticated
		});
		Receive<AuthenticationFailure>(auth => {
			Become(Unauthenticated); //switch behavior to Unauthenticated
		});
		Receive<IncomingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// can't accept message yet - not auth'd
			});
		Receive<OutgoingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// can't send message yet - not auth'd
			});
	}

	private void Unauthenticated() {
		//switch to Authenticating
		Receive<RetryAuthentication>(retry => Become(Authenticating));
		Receive<IncomingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// have to reject message - auth failed
			});
		Receive<OutgoingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// have to reject message - auth failed
			});
	}

	private void Authenticated() {
		Receive<IncomingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// print message for user
			});
		Receive<OutgoingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// send message to chatroom
			});
	}
}
```

헐! 이게 다 뭐야? 얼른 검토해 봅시다. 

먼저 `ReceiveActor`에 정의된 `Receive<T>`핸들러를 가져와서 세 가지 별도의 메서드로 옮겼습니다. 이러한 각 메소드는 액터가 메시지를 처리하는 방법을 제어하는 상태를 나타냅니다: 

* `Authenticating()`: 이 동작은 사용자가 인증을 시도 할 때 메시지를 처리하는 데 사용됩니다(초기 동작). 
* `Authenticated()`: 이 동작은 인증 작업이 성공할 때 메시지를 처리하는 데 사용됩니다. 그리고,
* `Unauthenticated()`: 이 동작은 인증 작업이 실패 할 때 메시지를 처리하는 데 사용됩니다.

생성자에서 `Authenticating()`을 호출했으므로, 액터는 `Authenticating()`상태에서 시작했습니다. 

*즉, `Authenticating()`메서드에 정의된 `Receive <T>`핸들러만 메시지 처리에 사용됩니다. (초기)* 

그러나, `AuthenticationSuccess` 또는 `AuthenticationFailure` 유형의 메시지를 수신하면 `Become` 메소드 ([docs](https://getakka.net/articles/actors/receive-actor-api.html#becomeunbecome "Akka.NET - ReceiveActor Become"))를 사용하여 동작을 각각 `Authenticated` 또는 `Unauthenticated`로 전환합니다. 

### `UntypedActor`에서 동작을 전환 할 수 있나요? 
예, 하지만 `UntypedActor` 내부의 구문은 약간 다릅니다. `UntypedActor`에서 동작을 전환하려면 직접 호출하는 대신 `ActorContext`를 통해 `BecomeStacked` 와 `UnbecomeStacked`에 액세스해야 합니다. 

`UntypedActor` 내부의 API 호출입니다:

* `Context.Become(Receive rec)` - 스택의 이전 동작을 보존하지 않고 동작을 변경합니다.
* `Context.BecomeStacked(Receive rec)` - 스택에 새로운 동작을 푸시하거나 
* `Context.UnbecomeStacked()` - 현재 동작을 팝하고 이전 동작으로 전환합니다. (해당되는 경우)

`Context.Become`의 첫 번째 인수는 `Receive` 델리게이트로, 다음 서명을 가진 모든 메서드입니다:

```csharp
void MethodName(object someParameterName);
```

이 대리자(delegate)는 메시지를 수신하고 새 동작 상태를 나타내는 액터의 다른 메서드를 나타내는 데만 사용됩니다.

다음은 예입니다 (`OtherBehavior`는 `Receive` 대리자입니다):

```csharp
public class MyActor : UntypedActor {
	protected override void OnReceive(object message) {
		if(message is SwitchMe) {
			// preserve the previous behavior on the stack
			Context.BecomeStacked(OtherBehavior);
		}
	}

	// OtherBehavior is a Receive delegate
	private void OtherBehavior(object message) {
		if(message is SwitchMeBack) {
			// switch back to previous behavior on the stack
			Context.UnbecomeStacked();
		}
	}
}
```


이러한 구문상의 차이를 제외하고, 동작 전환은 `UntypedActor`와 `ReceiveActor` 모두에서 정확히 동일한 방식으로 작동합니다. 

이제, 동작 전환을 적용해 보겠습니다! 

## 실습
이 레슨에서는 전환 가능 액터 동작을 통해 `ChartingActor`에 라이브 업데이트를 일시 중지하고 재개하는 기능을 추가 할 것입니다. 

### 1단계: `Main.cs`에 새로운 `Pause / Resume` 버튼 추가
이게 당신이 추가할 마지막 버튼입니다. 약속할께요.

`Main.cs`의 **[Design]** 뷰로 이동하여 다음 텍스트가 포함 된 새 버튼을 추가합니다: `PAUSE ||` 

![Add a Pause / Resume Button to Main](images/design-pauseresume-button.png)

Visual Studio의 **속성** 창으로 이동하여 이 단추의 이름을 `btnPauseResume`으로 변경합니다. 

![Use the Properties window to rename the button to btnPauseResume](images/pauseresume-properties.png)

`btnPauseResume`을 더블 클릭하여 `Main.cs`에 클릭 핸들러를 추가합니다. 

```csharp
private void btnPauseResume_Click(object sender, EventArgs e)
{

}
```

이 클릭 핸들러를 곧 채우겠습니다.

### 2단계: `ChartingActor`에 전환 가능 동작 추가
`ChartingActor`에 몇 가지 동적 동작을 추가 할 것입니다. 하지만 먼저 약간의 정리 작업이 필요합니다. 

먼저, `Actors/ChartingActor.cs`의 맨 위에 Windows Forms 네임 스페이스에 대한 `using` 참조를 추가합니다. 

```csharp
// Actors/ChartingActor.cs

using System.Windows.Forms;
```

`ChartingActor`의 `Messages` 영역 내에 새 메시지 유형을 선언해야합니다. 

```csharp
// Actors/ChartingActor.cs - add inside the Messages region
/// <summary>
/// Toggles the pausing between charts
/// </summary>
public class TogglePause { }
```

다음으로, `ChartingActor` 생성자 선언 바로 위에 다음 필드 선언을 추가합니다:

```csharp
// Actors/ChartingActor.cs - just above ChartingActor's constructors

private readonly Button _pauseButton;
```

`ChartingActor`의 기본 생성자에서 모든 `Receive<T>`선언을 `Charting()`이라는 새 메서드로 이동합니다. 

```csharp
// Actors/ChartingActor.cs - just after ChartingActor's constructors
private void Charting()
{
    Receive<InitializeChart>(ic => HandleInitialize(ic));
    Receive<AddSeries>(addSeries => HandleAddSeries(addSeries));
    Receive<RemoveSeries>(removeSeries => HandleRemoveSeries(removeSeries));
    Receive<Metric>(metric => HandleMetrics(metric));

	//new receive handler for the TogglePause message type
    Receive<TogglePause>(pause =>
    {
        SetPauseButtonText(true);
        BecomeStacked(Paused);
    });
}
```

`HandleMetricsPaused`라는 새 메서드를 `ChartingActor`의 `Individual Message Type Handlers` 영역에 추가합니다. 

```csharp
// Actors/ChartingActor.cs - inside Individual Message Type Handlers region
private void HandleMetricsPaused(Metric metric)
{
    if (!string.IsNullOrEmpty(metric.Series) 
        && _seriesIndex.ContainsKey(metric.Series))
    {
        var series = _seriesIndex[metric.Series];
        // set the Y value to zero when we're paused
        series.Points.AddXY(xPosCounter++, 0.0d);
        while (series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
        SetChartBoundaries();
    }
}
```

`ChartingActor` 클래스의 *가장* 아래에 `SetPauseButtonText`라는 새 메서드를 정의합니다:

```csharp
// Actors/ChartingActor.cs - add to the very bottom of the ChartingActor class
private void SetPauseButtonText(bool paused)
    {
        _pauseButton.Text = string.Format("{0}", !paused ? "PAUSE ||" : "RESUME ->");
    }
```

`ChartingActor` 내부의 `Charting` 메서드 바로 뒤에 `Paused`라는 새 메서드를 추가합니다:

```csharp
// Actors/ChartingActor.cs - just after the Charting method
private void Paused()
{
    Receive<Metric>(metric => HandleMetricsPaused(metric));
    Receive<TogglePause>(pause =>
    {
        SetPauseButtonText(false);
        UnbecomeStacked();
    });
}
```

마지막으로, **`ChartingActor`의 생성자를 모두 교체**하겠습니다:

```csharp
public ChartingActor(Chart chart, Button pauseButton) :
    this(chart, new Dictionary<string, Series>(), pauseButton)
{
}

public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex,
    Button pauseButton)
{
    _chart = chart;
    _seriesIndex = seriesIndex;
    _pauseButton = pauseButton;
    Charting();
}
```

### 3단계: Main.cs에서 `Main_Load` 와 `Pause / Resume` 클릭 핸들러 업데이트 
2단계에서 `ChartingActor`의 생성자 인수를 변경 했으므로 `Main_Load` 이벤트 핸들러 내에서 이를 수정해야합니다.

```csharp
// Main.cs - Main_Load event handler
_chartActor = Program.ChartActors.ActorOf(Props.Create(() => 
    new ChartingActor(sysChart, btnPauseResume)), "charting");
```

마지막으로, `btnPauseResume` 클릭 이벤트 핸들러를 업데이트하여 `ChartingActor`가 라이브 업데이트를 일시 중지하거나 다시 시작하도록 지시해야 합니다. 

```csharp
//Main.cs - btnPauseResume click handler
private void btnPauseResume_Click(object sender, EventArgs e)
{
    _chartActor.Tell(new ChartingActor.TogglePause());
}
```

### 마치고,
`ChartApp.csproj`를 빌드하고 실행하면 다음이 표시됩니다:

![Successful Lesson 4 Output](images/dothis-successful-run4.gif)

> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson4/images/dothis-successful-run4.gif).

작성한 코드와 [Completed/ 폴더](Completed/)의 코드를 비교하며 샘플에 어떤 것이 추가 및 수정되었는지 확인 해봅시다.

## 수고하셨습니다!
예아아아아아아아! 시간이 지남에 따라 일시 중지 할 수있는 실시간 업데이트 차트가 있습니다! 

이 시점에서 작업 시스템에 대한 개괄적인 개요는 다음과 같습니다:

![Akka.NET Bootcamp Unit 2 System Overview](images/system_overview_2_4.png)

***잠깐만요!***

`ChartingActor`가 일시 중지 상태일 때 차트를 켜거나 끄면 어떻게됩니까? 

![Lesson 4 Output Bugs](images/dothis-fail4.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson4/images/dothis-fail4.gif).

### 뜨헉!!!!! 작동하지 않아요!

  다음 강의에서 해결해야 할 문제입니다*. `Stash`메시지를 사용하여 준비가 될 때까지 메시지 처리를 연기합니다. 

**이제 [Akka 중급 2-5 : `Stash`를 사용하여 메시지 처리 지연](../lesson5/README.md)으로 넘어갑시다.**
