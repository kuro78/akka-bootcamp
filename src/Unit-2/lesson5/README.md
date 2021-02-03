# Akka 중급 2-5 : `Stash`를 사용하여 메시지 처리 지연

[레슨 4](../lesson4/README.md)의 마지막에 아래에서 볼 수 있듯이 라이브 차트에서 **Pause / Resume** 기능을 구현하는 방법에서 중대한 버그를 발견했습니다:

![Lesson 4 Output Bugs](../lesson4/images/dothis-fail4.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson4/images/dothis-fail4.gif).

버그는 `ChartingActor`가 동작을 `Paused`로 변경하면 더 이상 특정 성능 카운터에 대해 토글 버튼을 누를 때마다 생성되는 `AddSeries` 및 `RemoveSeries` 메시지를 처리하지 않는다는 것입니다. 

현재 폼에서는, 버튼의 시각적 상태가 라이브 차트와 완전히 동기화되지 않는데 많은 시간이 걸리지 않습니다. 그래프가 일시 중지되고 즉시 동기화되지 않을 때 토글 버튼을 누르기 만하면됩니다.

어떻게 이 문제를 고칠까요?

대답은 `ChartingActor`가 `Charting` 동작으로 돌아올 때까지 `AddSeries` 및 `RemoveSeries` 메시지의 처리를 연기하는 것입니다. 이때 실제로 해당 메시지로 무언가를 할 수 있습니다. 

이를위한 메커니즘이 [`Stash`](https://getakka.net/articles/actors/receive-actor-api.html#stash)입니다. 

## Key Concepts / Background
액터에 대한 전환 가능한 동작의 부작용 중 하나는 일부 동작이 특정 유형의 메시지를 처리 할 수 없다는 것입니다. 예를 들어 [레슨 4](../lesson4/README.md)에서 동작 전환에 사용한 인증 예제를 살펴 보겠습니다. 

### `Stash`가 뭔가요?
`Stash`는 나중에 처리하기 위해 메시지를 연기하기하는 액터에 구현된 스택형(stack-like) 데이터 구조입니다. 

### 액터에 `Stash`를 추가하는 방법
액터에 `Stash`를 추가하려면 다음과 같이 [`IWithBoundedStash`](http://api.getakka.net/docs/stable/html/683AD26A.htm "Akka.NET Stable API Docs - IWithBoundedStash interface") 또는 [`IWithUnboundedStash`](http://api.getakka.net/docs/stable/html/BB4565A9.htm "Akka.NET Stable API Docs - IWithUnboundedStash interface")를 인터페이스로 장식하기 만하면 됩니다:

```csharp
public class UserActor : ReceiveActor, IWithUnboundedStash {
	private readonly string _userId;
	private readonly string _chatRoomId;

	// added along with the IWithUnboundedStash interface
	public IStash Stash {get;set;}

	// constructors, behaviors, etc...
}
```

#### 언제 `BoundedStash`와 `UnboundedStash`가 필요한가요? 
99%의 시간 동안 `UnboundedStash`를 사용하기를 원할 것입니다 - `Stash`가 무제한의 메시지를 수락 할 수 있도록합니다. `BoundedStash`는 주어진 시간에 보관할 수있는 최대 메시지 수를 설정하려는 경우에만 사용해야합니다. `Stash`가 `BoundedStash` 제한을 초과 할 때마다 액터가 충돌합니다. 

#### `Stash`를 초기화해야 하나요?
잠시만 기다려주세요. `UserActor`에 공개 getter 및 setter를 포함하는 새로운 `Stash`속성이 있습니다. 이것이 바로 `Stash`를 초기화해야 한다는 의미인가요? **아니요!** 

`Stash` 속성은 액터가 로컬에서 생성 될 때마다 사용되는 "Actor Construction Pipeline"이라는 Akka.NET 기능에 의해 자동으로 채워집니다(자세한 내용은 강의의 범위를 벗어납니다). 

`ActorSystem`이 액터에서 `IWithBoundedStash` 인터페이스를 볼 때 `Stash` 속성 안에 `BoundedStash`를 자동으로 채우는 것을 알고 있습니다. 마찬가지로 `IWithUnboundedStash` 인터페이스가 표시되면 대신 해당 속성에 `UnboundedStash`를 채우는 것을 알고 있습니다. 

### `Stash` 사용 방법
이제 `UserActor`에 `Stash`를 추가했으니까 나중에 처리할 메시지를 저장하고, 처리할 이전에 저장된 메시지를 해제하는 데 실제로 어떻게 사용합니까? 

#### 메시지 보관
액터의 `OnReceive` 또는 `Receive<T>`핸들러 내에서 `Stash.Stash()`를 호출하여 현재 메시지를 `Stash` 상단에 배치 할 수 있습니다. 


지금 처리하고 싶지 않은 메시지만 보관할 필요가 있습니다 - 아래 시각화에서, 우리 액터는 메시지 1을 기꺼이 처리하지만 메시지 2와 0은 보관합니다. 

Note: `Stash()`를 호출하면 현재 메시지가 자동으로 보관되므로, `Stash.Stash()`호출에 메시지가 전달되지 않습니다. 

메시지를 보관하는 전체 순서는 다음과 같습니다:

![Stashing Messages with Akka.NET Actors](images/actors-stashing-messages.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson5/images/actors-stashing-messages.gif).

훌륭합니다! 이제 나중에 처리하기 위해 메시지를 `Stash`를 사용하는 방법을 알았으니, `Stash`에서 메시지를 다시 가져 오려면 어떻게 해야합니까? 

#### 단일 메시지 보관 해제
`Stash` 상단에서 메시지를 빼내기 위해 `Stash.Unstash()`를 호출합니다. 

**`Stash.Unstash()`를 호출하면 `Stash`가이 메시지를 *다른 대기열에있는 사용자 메시지보다 앞서 액터의 메일박스 앞에 배치합니다*.** 

##### VIP 라인
메일 박스 안에는 액터가 처리 할 `사용자` 메시지를 위한 별도의 두 대기열이 있는 것과 같습니다. 일반 메시지 대기열이 있고 VIP 라인이 있습니다. 

이 VIP 라인은 `Stash`에서 오는 메시지용으로 예약되어 있으며 VIP 라인의 모든 메시지는 일반 대기열의 메시지보다 먼저 점프하여 액터가 처리합니다. (참고로, 모든 `사용자` 메시지보다 먼저 잘라내는 `시스템` 메시지에 대한 "슈퍼 VIP" 라인도 있습니다. 하지만 이 강의의 범위를 벗어납니다.) 

메시지의 보관 해제 순서는 다음과 같습니다:

![Unstashing a Single Message with Akka.NET Actors](images/actor-unstashing-single-message.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson5/images/actor-unstashing-single-message.gif).

#### 한 번에 전체 Stash 해제
액터의 `Stash`에있는 *모든 것*을 한꺼번에 보관 해제 해야 한다면 `Stash.UnstashAll()` 메서드를 사용하여 `Stash`의 전체 내용을 메일박스 전면으로 푸시 할 수 있습니다. 

다음은 `Stash.UnstashAll()`을 호출하는 모습입니다:
![Unstashing all stashed messages at once with Akka.NET Actors](images/actor-unstashing-all-messages.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson5/images/actor-unstashing-all-messages.gif).

### 메시지가 `Stash`에서 나올 때 원래 순서대로 유지 되나요?
`Stash`에서 꺼내는 방법에 따라 다릅니다. 

#### `Stash.UnstashAll()`은 FIFO 메시지 순서를 유지
`Stash.UnstashAll()`을 호출 할 때 `Stash`는 `Stash`에있는 메시지의 원래 FIFO 순서가 액터의 메일박스 앞에 추가 될 때 보존되도록 합니다. (`Stash.UnstashAll()`애니메이션에 표시된대로) 

#### `Stash.Unstash()`는 메시지 순서를 변경 가능
`Stash.Unstash()`를 반복해서 호출하면 메시지의 원래 FIFO 순서를 변경할 수 있습니다. 

메일 함 내부에있는 VIP 라인을 기억 하시나요? `Stash`는 메시지가 풀릴 때 메시지를 넣는 곳입니다. 

음, ***단일*** 메시지를 `Unstash()`하면 해당 VIP 라인의 뒷면으로 이동합니다. 여전히 일반 `사용자`메시지보다 앞서 있지만 이전에 보관되지 않았고 VIP 라인에서 앞서있는 다른 메시지 뒤에 있습니다. 

*왜?* 이런 일이 일어날 수 있는지에 대해 더 많은 것이 있지만, 이 강의의 범위를 훨씬 벗어납니다. 

### `Stash`에 보관한 메시지는 데이터를 잃어 버리나요? 
절대로 그렇지 않아요. 메시지를 `Stash`에 보관할 때 메시지 와 메시지에 대한 모든 메타데이터(`Sender` 등)가 포함된 'Envelope' 메시지를 기술적으로 저장해야 합니다.

### 다시 시작하는 동안 액터의 `Stash`에있는 메시지는 어떻게 되나요? 
훌륭한 질문입니다! `Stash`는 액터의 짧은 상태 중 일부입니다. 다시 시작하는 경우 저장소가 파괴되고 가비지가 수집됩니다. 이것은  재시작 중에도 메시지가 지속되는 액터의 메일박스와 반대입니다.

**그러나 액터의 `PreRestart` 수명주기 메서드** 내에서 `Stash.UnstashAll()`을 호출하여 재시작하는 동안 `Stash`의 내용을 보존할 수 있습니다. 이렇게하면 모든 숨김 메시지가 다시 시작될 때까지 유지되는 액터 메일박스로 이동합니다:

```csharp
// move stashed messages to the mailbox so they persist through restart
protected override void PreRestart(Exception reason, object message){
	Stash.UnstashAll();
}
```

### 실제 시나리오: 메시지 버퍼링을 사용한 인증
이제 `Stash`가 무엇이며 어떻게 작동하는지 알았으니 채팅방 예제에서 `UserActor`를 다시 찾아가 사용자가 `Authenticated` 되기 전에 메시지를 버리는 문제를 해결해 보겠습니다. 

이것이 레슨 4의 개념 영역에서 디자인한 UserActor이며, 다양한 인증 상태에 대한 동작 전환이 있습니다: 

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
        // switch to Authenticating
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

레슨 4에서 채팅방 `UserActor` 예제를 처음 보았을 때, 처음에 인증을 활성화하기 위해 동작을 전환하는 데 중점을 두었습니다. 그러나 우리는 `UserActor`의 주요 문제를 무시했습니다. `Authenticating` 단계에서 시도한 `OutgoingMessage` 와 `IncomingMessage` 인스턴스를 모두 버리기만 하면 됩니다. 

메시지 처리를 지연시키는 방법을 알지 못했기 때문에 아무런 이유 없이 사용자에게 메시지가 손실되고 있습니다. **윽!** 고치죠.

이러한 메시지를 처리하는 올바른 방법은 `UserActor`가 `Authenticated` 또는 `Unauthenticated` 상태가 될 때까지 임시로 저장하는 것입니다. 이때 `UserActor`는 사용자와 주고받는 메시지로 무엇을 할 것인지에 대해 현명한 결정을 내릴 수 있습니다. 

사용자가 인증되었는지 여부를 알 때까지 메시지 처리를 지연하도록 `UserActor`의 `Authenticating` 동작을 업데이트하면 다음과 같습니다:

```csharp
public class UserActor : ReceiveActor, IWithUnboundedStash {
	// constructors, fields, etc...

	private void Authenticating() {
		Receive<AuthenticationSuccess>(auth => {
			Become(Authenticated); // switch behavior to Authenticated
            // move all stashed messages to the mailbox for processing in new behavior
			Stash.UnstashAll();
		});
		Receive<AuthenticationFailure>(auth => {
			Become(Unauthenticated); // switch behavior to Unauthenticated
            // move all stashed messages to the mailbox for processing in new behavior
			Stash.UnstashAll();
		});
		Receive<IncomingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// save this message for later
				Stash.Stash();
			});
		Receive<OutgoingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => {
				// save this message for later
				Stash.Stash();
			});
	}

	// other UserActor behaviors...
}
```

이제, `UserActor`가 `Authenticating` 동안 수신하는 모든 메시지는 동작을 `Authenticated` 또는 `Unauthenticated`로 전환 할 때 처리 할 수 있습니다. 

훌륭합니다! `Stash`를 이해했으니, 시스템 그래프를 수정하기 위해 작업을 진행하겠습니다.

## 실습
이번 섹션에서는 `UnboundedStash`를 사용하여 레슨 4의 끝에서 발견한 `ChartingActor` 내부의 **Pause / Resume** 버그를 수정합니다. 

### 1단계: `ChartingActor`가 `IWithUnboundedStash` 인터페이스를 구현
`Actors/ChartingActor.cs` 내에서 `ChartingActor` 클래스 선언을 업데이트하고 `IWithUnboundedStash` 인터페이스를 구현하도록 합니다:

```csharp
// Actors/ChartingActor.cs
public class ChartingActor : ReceiveActor, IWithUnboundedStash
```

또한, 인터페이스를 구현하고 `ChartingActor` 내부 어딘가에 다음 속성을 추가하도록합니다:

```csharp
// Actors/ChartingActor.cs - inside ChartingActor class definition
public IStash Stash { get; set; }
```

### 2단계: `Paused()` 동작 내부의 `Receive<T>` 핸들러에 `Stash` 메서드 호출 추가 
`ChartingActor` 내부에 선언된 `Paused()` 메서드로 이동합니다. 

`AddSeries` 와 `RemoveSeries` 메시지를 `Stash()`로 업데이트 합니다: 

```csharp
// Actors/ChartingActor.cs - inside ChartingActor class definition
private void Paused()
{
	// while paused, we need to stash AddSeries & RemoveSeries messages
    Receive<AddSeries>(addSeries => Stash.Stash());
    Receive<RemoveSeries>(removeSeries => Stash.Stash());
    Receive<Metric>(metric => HandleMetricsPaused(metric));
    Receive<TogglePause>(pause =>
    {
        SetPauseButtonText(false);
        UnbecomeStacked();

        // ChartingActor is leaving the Paused state, put messages back
        // into mailbox for processing under new behavior
        Stash.UnstashAll();
    });
}
```

이게 다입니다! `ChartingActor`는 이제 `AddSeries` 또는 `RemoveSeries` 메시지를 저장하고 `Paused()` 상태에서 `Charting()` 상태로 전환되는 즉시 수신된 순서대로 재생합니다. 

이제 버그가 수정됐습니다!

### 마치고,
`ChartApp.csproj`를 빌드하고 실행하면 다음이 표시됩니다:

![Successful Unit 2 Output](images/syncharting-complete-output.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson5/images/syncharting-complete-output.gif).

작성한 코드와 [Completed/ 폴더](Completed/)의 코드를 비교하고 최종 출력을 강사가 작성한 것과 비교합니다.

## 수고하셨습니다.

### 우와! 해냈어요! Unit 2를 완료했습니다! 이제 충분한 휴식을 취하고 Unit 3을 준비하세요!

**더 배우고 싶은가요? [지금 Unit 3을 시작해 보세요](../../Unit-3/README.md "Akka.NET Bootcamp Unit 3").**
