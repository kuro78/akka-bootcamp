# Akka 중급 2-2 : 더 나은 메시지 처리를 위해 `ReceiveActor` 사용

첫 번째 단원에서는 `UntypedActor`(["Akka.NET - UntypedActor"](http://api.getakka.net/docs/stable/html/6300028C.htm))를 사용하여 첫 번째 액터를 빌드하고 몇 가지 간단한 메시지 유형을 처리하는 방법을 배웠습니다.

이번 레슨에서는 `ReceiveActor`(["Akka.NET - ReceiveActor"](http://api.getakka.net/docs/stable/html/B124B2AF.htm))를 사용하여 Akka.NET에서 보다 정교한 유형의 패턴 일치 및 메시지 처리를 쉽게하는 방법을 보여줄 것입니다.

## Key Concepts / Background
### 패턴 매칭(Pattern matching)
Akka.NET의 액터는 [.NET 타입](https://msdn.microsoft.com/en-us/library/ms173104.aspx)이나 값에 따라 메시지를 선택적으로 처리 할 수있는 패턴 일치의 개념에 크게 의존합니다.

첫 번째 모듈에서는 `UntypedActor`를 사용하여 다음과 같은 코드 블록을 사용하여 메시지를 처리하고 수신하는 방법을 배웠습니다:

```csharp
protected override void OnReceive(object message){
	if(message is Foo) {
		var foo = message as Foo;
		// do something with foo
	}
	else if(message is Bar) {
		var bar = message as Bar;
		// do something with bar
	}
	//.... other matches
	else {
		// couldn't match this message
		Unhandled(message);
	}
}
```

Akka.NET에서 이 패턴 일치 방법은 단순 일치에 적합하지만 일치 요구가 더 복잡하다면 어떻게 해야할까요?

지금까지 살펴본 `UntypedActor`로 이러한 사용 사례를 어떻게 처리할지 고려하십시오:

1. `message`가 `string`이고 "AkkaDotNet"으로 시작하는 경우 또는
2. `message`가 `Foo` 타입이고 `Foo.Count`가 4보다 작고 `Foo.Max`가 10보다 큰 경우

흠... `UntypedActor` 내에서 모든 작업을 수행하려고 하면 다음과 같은 결과가 나올 것입니다:

```csharp
protected override void OnReceive(object message) {
	if(message is string
		&& message.AsInstanceOf<string>()
			.BeginsWith("AkkaDotNet")){
		var str = message as string;
		// do some work with str...
	}
	else if(message is Foo
			&& message.AsInstanceOf<Foo>().Count < 4
			&& message.AsInstanceOf<Foo>().Max > 10){
		var foo = message as Foo;
		// do something with foo
	}
	// ... other matches ...
	else {
		// couldn't match this message
		Unhandled(message);
	}
}
```

#### *우웩!* 더 나은 방법이 *있어*야 하겠군요?
네, 있습니다! `ReceiveActor`를 사용합니다.

### `ReceiveActor` 소개
`ReceiveActor`는 `UntypedActor` 위에 구축되어 정교한 패턴 매칭과 메시지 처리를 쉽게 수행 할 수 있습니다.

몇 분 전의 지저분한 코드 샘플이 `ReceiveActor`로 다시 작성된 모습입니다:

```csharp
public class FooActor : ReceiveActor
{
    public FooActor()
    {
        Receive<string>(s => s.StartsWith("AkkaDotNet"), s =>
        {
            // handle string
        });

        Receive<Foo>(foo => foo.Count < 4 && foo.Max > 10, foo =>
        {
            // handle foo
        });
    }
}
```

#### *훨씬 났군요*.

그렇다면 이전의 모든 패턴 일치 코드를 단순화하고 정리하는 데 도움이 된 비밀 소스는 무엇입니까?

### `ReceiveActor`의 비밀 소스
패턴 일치 코드를 모두 정리하는 비밀 소스는 **`Receive<T>`핸들러**입니다.

```csharp
// ReceiveActor 를 파워풀하게 만드는 이유입니다!
Receive<T>(Predicate<T>, Action<T>);
```

`ReceiveActor`를 사용하면 액터에 맞는 강력한 타입의 컴파일 타임 패턴 레이어를 쉽게 추가 할 수 있습니다.

타입에 따라 메시지를 쉽게 일치시킨 다음, 액터가 특정 메시지를 처리 할 수 있는지 여부를 결정할 때 추가 검사 또는 유효성 검사를 수행하기 위해 유형화 된 술어를 사용할 수 있습니다.


#### 다른 종류의`Receive<T>`핸들러가 있나요?
네, `Receive<T>`핸들러를 사용하는 다양한 방법은 다음과 같습니다:

##### 1) `Receive<T>(Action<T> handler)`
메시지가 'T'타입인 경우에만 메시지 핸들러를 실행합니다.

##### 2) `Receive<T>(Predicate<T> pred, Action<T> handler)`
이것은 메시지가 `T` 유형**이고** [predicate 함수](https://msdn.microsoft.com/ko-kr/library/bfcke1bz.aspx)가 `T` 인스턴스에 대해 true를 반환하는 경우에만 메시지 핸들러를 실행합니다.

##### 3) `Receive<T>(Action<T> handler, Predicate<T> pred)`
이전과 같습니다.

##### 4) `Receive(Type type, Action<object> handler)`
이것은 이전의 typed + predicate 메시지 핸들러의 구체적인 버전입니다(더 이상 일반이 아님).

##### 5) `Receive(Type type, Action<object> handler, Predicate<object> pred)`
이전과 같습니다.

##### 6) `ReceiveAny()`
이것은 모든 `object` 인스턴스를 받아들이는 catch-all 핸들러입니다. 일반적으로 이 기능은 이전 `Receive()` 처리기에서 처리되지 않은 메시지를 처리하는 데 사용됩니다.

### `Receive<T>`핸들러를 선언하는 순서가 중요합니다.
겹치는 메시지 유형을 처리해야하는 경우 어떻게됩니까?

아래 메시지를 고려하십시오. 동일한 하위 문자열로 시작하지만 다르게 처리해야한다고 가정합니다.

1. `AkkaDotNetSuccess`로 시작하는`string` 메시지, 그리고
2. `AkkaDotNet`으로 시작하는`string` 메시지

`ReceiveActor`가 이렇게 작성되면 어떻게 될까요?

```csharp
public class StringActor : ReceiveActor
{
    public StringActor()
    {
        Receive<string>(s => s.StartsWith("AkkaDotNet"), s =>
        {
            // handle string
        });

        Receive<string>(s => s.StartsWith("AkkaDotNetSuccess"), s =>
        {
            // handle string
        });
    }
}
```

이 경우에 일어나는 일은 두 번째 핸들러(for `s.StartsWith("AkkaDotNetSuccess")`)가 호출되지 않는다는 것입니다. 왜 안됄까요?

***`Receive<T>`핸들러의 순서가 중요합니다!***

이는 **`ReceiveActor`가 *최 상위* 일치 핸들러**가 아닌 *첫 번째* 일치 핸들러를 사용하여 메시지를 처리하고 [선언 된 순서대로 각 메시지에 대한 핸들러를 평가하기 때문입니다.](https://getakka.net/articles/actors/receive-actor-api.html#handler-priority)

그렇다면 "AkkaDotNetSuccess"로 시작하는 문자열에 대한 핸들러가 트리거되지 않는 위의 문제를 어떻게 해결합니까?

간단합니다 : *더 구체적인 핸들러가 먼저 나오도록하여이 문제를 해결합니다*.

```csharp
public class StringActor : ReceiveActor
{
    public StringActor()
    {
		// Now works as expected
        Receive<string>(s => s.StartsWith("AkkaDotNetSuccess"), s =>
        {
            // handle string
        });

        Receive<string>(s => s.StartsWith("AkkaDotNet"), s =>
        {
            // handle string
        });
    }
}
```

### `ReceiveActor`에서 메시지 핸들러는 어디에서 정의합니까?
`ReceiveActor`에는 `OnReceive()`메소드가 없습니다.

대신, `ReceiveActor` 생성자 또는 해당 생성자가 호출 한 메서드에서 직접 `Receive` 메시지 핸들러를 연결해야합니다.

이것을 알면 `ReceiveActor`를 사용하여 작업 할 수 있습니다.

## 실습
이번 실습에서는 차트에 여러 데이터 시리즈를 추가하는 기능을 추가하고, `ChartingActor`를 수정하여 이를 수행하는 명령을 처리 할 것입니다.

### 1단계 - UI에 "시리즈 추가" 버튼 추가

가장 먼저 할 일은 "시리즈 추가"라는 새 버튼을 양식에 추가하는 것입니다. `Main.cs`의 **[Design]**보기로 이동하여 도구 상자에서 `Button`을 UI로 드래그합니다. 여기에 버튼을 놓았습니다:

![Adding a 'Add Series' button in Design view in Visual Studio](images/button.gif)

**[Design]**보기에서 버튼을 두 번 클릭하면 Visual Studio가 자동으로 `Main.cs` 내에 클릭 핸들러를 추가합니다. 생성 된 핸들러는 다음과 같아야합니다:

```csharp
// automatically added inside Main.cs if you double click on button in designer
private void button1_Click(object sender, EventArgs e)
{

}
```

**지금은 비워 둡시다**. 조만간 이 버튼을 `ChartingActor`에 연결하겠습니다.

### 2단계 - `ChartingActor`에 `AddSeries` 메시지 타입 추가

`ChartingActor`가 관리하는 `Chart`에 추가 `Series`를 넣는 새 메시지 클래스를 정의해 보겠습니다. 새로운 `Series`를 추가할 것임을 나타 내기 위해 `AddSeries` 메시지 타입을 생성합니다.

`Messages` 영역 내의 `ChartingActor.cs`에 다음 코드를 추가합니다:

```csharp
// Actors/ChartingActor.cs, inside the #Messages region

/// <summary>
/// Add a new <see cref="Series"/> to the chart
/// </summary>
public class AddSeries
{
    public AddSeries(Series series)
    {
        Series = series;
    }

    public Series Series { get; private set; }
}
```

### 3단계 - `ChartingActor`는 `ReceiveActor`를 상속 받기
이제 중요한 부분은 - `ChartingActor`를 `UntypedActor`에서 `ReceiveActor`로 변경하는 것입니다.

이제 `ChartingActor`의 선언을 변경해 보겠습니다.

변경 전:

```csharp
// Actors/ChartingActor.cs
public class ChartingActor : UntypedActor
```

변경 후:

```csharp
// Actors/ChartingActor.cs
public class ChartingActor : ReceiveActor
```

#### `ChartingActor`에서 `OnReceive` 메소드를 제거
**`ChartingActor`에서 `OnReceive` 메소드를 삭제하는 것을 잊지 마십시오**. 이제 `ChartingActor`가 `ReceiveActor`이므로 `OnReceive()` 메소드가 필요하지 않습니다.

### 4단계 - `ChartingActor`에 대한 `Receive<T>` 핸들러 정의

지금 `ChartingActor`는 전송된 메시지를 처리할 수 없습니다. 따라서 수락하려는 메시지 유형에 대한 일부 `Receive<T>` 핸들러를 정의하여 문제를 해결하겠습니다.

먼저 다음 메서드를 `ChartingActor`의 `Individual Message Type Handlers` 영역에 추가합니다:

```csharp
// Actors/ChartingActor.cs in the ChartingActor class
// (Individual Message Type Handlers region)

private void HandleAddSeries(AddSeries series)
{
    if(!string.IsNullOrEmpty(series.Series.Name) &&
    !_seriesIndex.ContainsKey(series.Series.Name))
    {
        _seriesIndex.Add(series.Series.Name, series.Series);
        _chart.Series.Add(series.Series);
    }
}
```

`ChartingActor`의 생성자를 수정하여 `InitializeChart`와 `AddSeries`에 대한 `Receive<T>` 후크를 설정해 보겠습니다.

```csharp
// Actors/ChartingActor.cs in the ChartingActor constructor

public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex)
{
    _chart = chart;
    _seriesIndex = seriesIndex;

    Receive<InitializeChart>(ic => HandleInitialize(ic));
    Receive<AddSeries>(addSeries => HandleAddSeries(addSeries));
}
```


> **NOTE**: `ChartingActor`의 다른 생성자인 `ChartingActor(Chart chart)`는 `ChartingActor(Chart chart, Dictionary <string, Series> seriesIndex)`를 호출하므로 수정할 필요가 없습니다.

이를 통해 `ChartingActor`는 이제 두 가지 유형의 메시지를 쉽게 수신하고 처리할 수 있습니다.

### 5단계 - "시리즈 추가"버튼에 대한 버튼 클릭 핸들러를 사용하여 `ChartingActor`에 `AddSeries` 메시지 보내기

1단계에서 버튼에 추가한 클릭 핸들러로 돌아가 보겠습니다.

`Main.cs`에서 다음 코드를 클릭 핸들러의 본문에 추가합니다:

```csharp
// Main.cs - class Main
private void button1_Click(object sender, EventArgs e)
{
    var series = ChartDataHelper.RandomSeries("FakeSeries" +
        _seriesCounter.GetAndIncrement());
    _chartActor.Tell(new ChartingActor.AddSeries(series));
}
```

그러면 됩니다!

### 마치고,
`ChartApp.csproj`를 빌드하고 실행하면 다음이 표시됩니다:

![Successful Lesson 2 Output](images/dothis-successful-run2.gif)

> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요.](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-2/lesson2/images/dothis-successful-run2.gif).

작성한 코드와 [Completed](Completed/)의 코드를 비교하며 샘플에 어떤 것이 추가 및 수정되었는지 확인 해봅시다.

## 수고하셨습니다!
다시 한 번 수고하셨습니다. 이 레슨를 마치면 Akka.NET의 패턴 매칭에 대해 훨씬더 잘 이해하고 `ReceiveActor`가 `UntypedActor`와 어떻게 다른지 이해해야합니다.

이제 [Akka 중급 2-3 : `Scheduler`를 사용한 지연 메시지 보내기](../lesson3/README.md)을 향해 나아가 봅시다.
