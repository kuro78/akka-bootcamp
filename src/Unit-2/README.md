# Akka.NET Bootcamp - Unit 2: Akka.NET 중급

**[Unit 1](../Unit-1/README.md)** 에서, Akka.NET과 Actor Model의 기본 사항을 배웠습니다.

Unit 2에서는 패턴 매칭, 기본 Akka.NET 구성, 예약된(scheduled) 메시지 등과 같은 Akka.NET의 보다 정교한 개념 중 일부를 배웁니다!

## 개념 학습

Unit 2에서는 Windows Forms, .NET에 내장 된 데이터 시각화 도구와 [Performance Counters](https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.aspx "PerformanceCounter Class - C#")를 사용하여 고유한 리소스 모니터를 빌드 할 것입니다.

레슨 5의 최종 출력은 다음과 같습니다:

![Akka.NET Bootcamp Unit 2 Output](lesson5/images/syncharting-complete-output.gif)

> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/blob/master/src/Unit-2/lesson5/images/syncharting-complete-output.gif).

**액터를 사용하여 이 모든 것을 구축할 것이며**, 작업이 끝나면 코드 풋프린트가 얼마나 작은지 놀랄 것입니다.

Unit 2에서 학습할 내용:

1. App.config 와 Web.config를 통해 액터를 구성하기 위해 [HOCON 구성(configuratio)](https://getakka.net/articles/concepts/configuration.html#what-is-hocon "Akka.NET HOCON Configurations")을 사용하는 방법
2. 액터의 [Dispatcher](https://getakka.net/articles/actors/dispatchers.html)가 Windows Forms UI 스레드에서 실행되도록 구성하여, 액터가 컨텍스트를 변경할 필요없이 UI 요소에서 직접 작업을 수행 할 수 있도록하는 방법
3. `ReceiveActor`를 사용해 보다 정교한 유형의 패턴 매칭을 처리하는 방법
4. `Scheduler`를 사용해 액터에게 반복 메시지를 보내는 방법
5.  액터간 [게시-구독 패턴(Publish-subscribe pattern)](http://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) 사용 방법
6.  런타임에 액터의 동작을 전환하는 방법과 이유
7.  지연된 처리를 위해 메시지를 `보관(Stash)`하는 방법

## Table of Contents

1. **[Lesson 1: `Config` 와 App.Config를 통한 액터 배포](lesson1/README.md)**
2. **[Lesson 2: 더 나은 메시지 처리를 위해 `ReceiveActor` 사용](lesson2/README.md)**
3. **[Lesson 3: `Scheduler`를 사용한 지연 메시지 보내기](lesson3/README.md)**
4. **[Lesson 4: `BecomeStacked` 와 `UnbecomeStacked`를 사용하여 런타임에 액터 동작 전환](lesson4/README.md)**
5. **[Lesson 5: `Stash`를 사용하여 메시지 처리 지연](lesson5/README.md)**

## 시작해 봅시다

시작하려면, [DoThis](DoThis/) 폴더로 이동해서 `ChartApp.csproj` 파일을 여세요.

다음으로 [Lesson 1](lesson1/README.md)으로 이동합니다.
