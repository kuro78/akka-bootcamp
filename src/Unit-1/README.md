# Akka.NET Bootcamp - Unit 1: Akka.NET 시작하기

Unit 1에서는 액터 모델과 Akka.NET이 작동하는 방식의 기본을 배웁니다. 

## 학습 개념

*NIX 시스템에는 파일의 변경 사항 (예 : 테일링 로그 파일)을 모니터링하기 위한 `tail` 명령이 내장되어 있지만 Windows는 그렇지 않습니다. Windows 용 `tail`을 만들면서 기본개념을 학습합니다.

Unit 1에서는 다음을 배우게됩니다:

1. 자신 만의`ActorSystem`과 액터를 만드는 방법
2.  액터에게 메시지를 보내는 방법과 다양한 유형의 메시지를 처리하는 방법
3. `Props`와`IActorRef`를 사용하여 느슨하게 결합 된 시스템을 구축하는 방법
4. 액터 경로(path), 주소(address)와 `ActorSelection`을 사용하여 액터에게 메시지를 보내는 방법
5. 자식 액터 와 액터 계층 구조를 생성하는 방법과 `감시 전략(SupervisionStrategy)`으로 자식 액터를 감독하는 방법
6. 액터 라이프사이클을 사용하여 액터의 시작, 종료 및 재시작 동작을 제어하는 방법

## 자마린(Xamarin)을 사용하나요?
Unit 1은 콘솔에 크게 의존하기 때문에 시작하기 전에 약간의 조정이 필요합니다. **외부 콘솔**을 사용하려면 `WinTail` 프로젝트 파일(솔루션 아님)을 설정해야합니다.

다음처럼 셋업하세요:

1. `WinTail` 프로젝트(솔루션 아님)를 클릭합니다.
2. 메뉴에서 `Project> WinTail Options`로 이동합니다.
3. `WinTail Options` 내에서 `Run> General`로 이동합니다.
4. `Run on external console`을 선택합니다.
5. `OK`를 클릭합니다.

설정 방법에 대한 데모입니다:
![Configure Xamarin to use external console](../../images/xamarin.gif)


## Table of Contents

1. **[Lesson 1 - 액터와 `액터시스템(ActorSystem)`](lesson1/README.md)**
2. **[Lesson 2 - 메시지 정의 및 핸들링](lesson2/README.md)**
3. **[Lesson 3 - `Props` 와 `IActorRef` 사용하기](lesson3/README.md)**
4. **[Lesson 4 - 자식 액터, 액터 계층 구조, 그리고 감시(Supervision)](lesson4/README.md)**
5. **[Lesson 5 - `ActorSelection`과 함께 주소로 액터 찾기](lesson5/README.md)**
6. **[Lesson 6 - 액터 라이프사이클(The Actor Lifecycle)](lesson6/README.md)**

## 시작해 봅시다

시작하려면, [DoThis](DoThis/) 폴더로 이동해서 `WinTail.csproj` 파일을 여세요.

다음으로 [Lesson 1](lesson1/README.md)으로 이동합니다.