# Akka.NET Bootcamp - Unit 3: Akka.NET 고급

**[Unit 1](../Unit-1/README.md)**에서는 Akka.NET의 기본 사항과 액터 모델을 배웠습니다. 

**[Unit 2](../Unit-2/README.md)**에서는 패턴 매칭, 기본 Akka.NET 구성, 예약된(scheduled) 메시지 등과 같은 Akka.NET의 보다 정교한 개념 중 일부를 배웠습니다! 

Unit 3에서는 [태스크 병렬 라이브러리 (TPL)](https://msdn.microsoft.com/en-us/library/dd537609.aspx) 와 Akka.NET 라우터(router)를 활용해 액터 시스템을 확장하여 병렬 처리를 통한 대규모 성능 향상을 실현하는 방법에 대해 배우겠습니다. 

## 개념 학습
Unit 3에서는 여러 GitHub 저장소에서 동시에 데이터를 검색할 수 있는 정교한 GitHub scaper를 만들어 보겠습니다.

![Unit 3 GithubScraper App Live Run](lesson5/images/lesson5-live-run.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-3/lesson5/images/lesson5-live-run.gif).


이 시스템은 또한 해당 저장소에 참여한 GitHubber에 대한 정보(예 : 별표 수(stared) 또는 분기 수(forked))를 가져올 수 있습니다. 결국 우리는 엄청난 양의 데이터 검색을 병렬로 조정할 수있는 GitHub API에서 데이터를 검색할 수있는 확장성이 뛰어난 시스템을 갖게 될 것입니다(물론 [API의 제한 허용 속도](https://developer.github.com/v3/rate_limit/)까지 가능한)!

Unit 3에서 학습할 내용:

1. `Group` 라우터를 사용해 액터간에 작업을 나누는 방법 
2. `Pool` 라우터를 사용해 액터 풀을 자동으로 생성하고 관리하는 방법 
3. HOCON을 사용해 라우터를 구성하는 방법 
4. `Ask`를 사용해 액터가 메시지에 응답할 때까지 인라인 대기하는 방법 
5. `PipeTo`를 사용해 액터 내부에서 비동기식으로 작업을 수행하는 방법 

### .NET의 공식 GitHub SDK인 Octokit와 협력
이 강의에서는 [.NET 용 공식 GitHub SDK인 Octokit](http://octokit.github.io/)(과 다른 언어!)도 소개합니다. 

> **OCTOKIT NOTE:** 프록시 서버안에서 작업하는 경우 Octokit를 사용하여 Github API에 연결하는 데 문제가 있는 경우 구성 파일에 이 문제를 추가하여 해결되는지 확인하십시오:
>
>  `<system.net><defaultProxy useDefaultCredentials="true" /></system.net>`

![Octokit .NET Logo](../../images/gundam-dotnet.png)

Octokit에 대해 궁금한 점이 있거나 더 자세히 알고 싶다면 [GitHub의 Octokit.NET](https://github.com/octokit/octokit.net)을 확인하세요! 

## Table of Contents

1. **[Lesson 1: `Group`라우터를 사용해 액터간에 작업 분할](lesson1/README.md)**
2. **[Lesson 2: `Pool` 라우터를 사용해 자동으로 액터 풀 생성 및 관리](lesson2/README.md)** 
3. **[Lesson 3: HOCON을 사용해 라우터를 구성하는 방법](lesson3/README.md)**
4. **[Lesson 4: `PipeTo`를 사용해 액터 내부에서 비동기식으로 작업을 수행하는 방법](lesson4/README.md)**
5. **[Lesson 5: `ReceiveTimeout`으로 교착 상태(deadlock)를 방지하는 방법](lesson5/README.md)**

## 준비할 것들
**API 용 GitHub OAuth 액세스 토큰을 만들어야합니다**. 

[이 지시사항을 따라서](https://help.github.com/articles/creating-an-access-token-for-command-line-use/) 이 앱에만 사용되는 계정의 일회용 토큰을 만들고 OAuth 토큰을 기록합니다. 액세스 토큰을 만들 때 범위로 repo를 선택할 수 있습니다. 이 프로젝트에는 이 범위만 있으면 됩니다.
![GitHub OAuth Scope settings page](../../images/OAuth_Scope.png)

샘플을 실행해 GitHub에서 데이터를 가져 오면 두 개의 팝업 창이 표시됩니다:
1. 첫 번째 팝업 창에서 GitHub 토큰을 요청합니다. 앞서 만든 읽기 전용 액세스 토큰입니다. 
2. 두 번째 창은 정보를 가져올 실제 저장소의 URL을 입력하는 곳입니다. 

여기에서 코드를 실행할 때 액세스 토큰을 입력합니다:

![Unit 3 GithubScraper App Live Run Token](lesson5/images/enter-access-token.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-3/lesson5/images/enter-access-token.gif).
> 
그러면 검사 할 저장소의 URL을 입력하는 두 번째 창이 나타납니다:

![Unit 3 GithubScraper App Live Run](lesson5/images/lesson5-live-run.gif)
> NOTE: eBook / .ePub를 사용하여 따라하는 경우 애니메이션이 표시되지 않습니다. [여기를 눌러 확인하세요](https://github.com/petabridge/akka-bootcamp/raw/master/src/Unit-3/lesson5/images/lesson5-live-run.gif).

> N.B. Github API로 작업 할 때 팔로워 수가 적은 저장소를 선택하십시오. 그렇지 않으면 API 토큰이 다소 빨리 소모 될 수 있습니다. API 토큰이 부족한 경우 위 단계를 반복하여 다른 OAuth 토큰을 가져옵니다. 

## 시작해 봅시다
시작하려면, [DoThis](DoThis/) 폴더로 이동해서 `GithubActors.csproj` 파일을 여세요.

다음으로 [Lesson 1](lesson1/README.md)으로 이동합니다.
