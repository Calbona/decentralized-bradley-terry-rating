# 탈중앙화 Bradley–Terry 레이팅

한 경기의 결과와 두 선수의 현재 레이팅만 있으면 갱신이 끝납니다. 중앙 서버도, 전체 대전 기록도, 선수 ID도 필요 없습니다. 저장소에는 TypeScript와 C# 구현이 각각 들어 있으며, 둘은 의미상 동일하고 핵심은 모두 순수 함수로 이루어져 있습니다.

**언어:** [English](../README.md) · [简体中文](zh-CN.md) · [繁體中文](zh-TW.md) · [Deutsch](de-DE.md) · [Español](es-ES.md) · [Français](fr-FR.md) · [Italiano](it-IT.md) · [日本語](ja-JP.md) · **한국어** · [Русский](ru-RU.md) · [Tiếng Việt](vi-VN.md)

## 패키지

| 언어 | 패키지 | 문서 |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](../typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](../csharp/README.md) |

## 모델

레이팅 `r ∈ (0, 1)`은 선수의 상대 승률입니다. 즉 레이팅이 `r = 0.5`인 중립적인 상대를 이길 확률입니다. 이는 다음과 같은 Bradley–Terry 강도에 대응합니다.

```
γ = r / (1 - r)
```

두 선수의 경기 예측은 다음과 같습니다.

```
P(A가 B를 이길 확률) = γA / (γA + γB)
                    = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

`θ = logit(r) = log(γ)`, `p = P(A가 B를 이길 확률)`, A가 이기면 `s = 1`, 지면 `0`, 학습률을 `η`라고 하면

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

이는 전역 Bradley–Terry 로그 우도에 대한 온라인 경사 상승 한 단계입니다. 즉 로그 오즈 형태의 Elo입니다. 기본값 `η = 0.25`는 체스의 `K ≈ 43`과 대략 같습니다(`η = K · ln 10 / 400`).

학습률을 고정하면 온라인 갱신은 전역 해 주변의 `O(η)` 범위에서 안정됩니다. 최대 우도 해로 엄밀하게 수렴시키려면 경기 수 `t`에 따라 학습률을 감쇠시키세요. 예를 들어 `η_t = η0 / (t + τ)`입니다. 라이브러리 자체는 상태를 저장하지 않으므로 감쇠 일정은 호출하는 쪽에서 넘깁니다.

## 속성

1. **범위 보장** — 입력과 출력은 항상 `(0, 1)` 안에 엄격히 머뭅니다. 음수 승률이나 100%를 넘는 값은 나오지 않습니다.
2. **0.5는 중립** — `r = 0.5`는 중립적인 상대와 맞붙었을 때 오십오십을 뜻하며, 새 선수도 여기서 시작합니다.
3. **대칭** — 두 선수를 바꾸고 결과를 뒤집으면 새 레이팅도 그대로 맞바뀝니다. `P(A가 B를 이길 확률) + P(B가 A를 이길 확률) = 1`.
4. **단조** — 이긴 선수의 레이팅은 오르고, 진 선수의 레이팅은 내립니다.
5. **이변일수록 더 크게 움직임** — 예상 밖의 결과는 예상된 결과보다 레이팅을 더 크게 움직입니다. 그래서 강한 상대를 이기는 쪽이 약한 상대를 이기는 것보다 이득이 큽니다.
6. **로그 오즈 제로섬** — `θA' + θB' = θA + θB`. 한 경기가 두 선수의 로그 오즈 합을 바꾸지 않으므로 레이팅 전체가 밀리지 않습니다. 모두 0.5에서 시작하면 전체 `θ`의 합은 항상 0이고, 이는 전역 Bradley–Terry 최대 우도 해의 정규화와 정확히 일치합니다.
7. **수렴** — 각 갱신은 전역 Bradley–Terry 로그 우도의 경사 방향으로 나아갑니다. Robbins–Monro 조건을 만족하도록 학습률을 감쇠시키면 레이팅은 전역 MLE로 수렴합니다. 테스트에서는 MM 알고리즘으로 구한 참조 MLE와 비교해 이를 확인합니다.

## 무승부

경기 결과는 'A 승' 또는 'B 승' 두 가지뿐입니다. 무승부가 필요한 도메인이라면 그대로 이 라이브러리에 넘기지 마세요.

## 빠른 시작

TypeScript:

```ts
import {
  INITIAL_RATING,
  updateRatings,
  winProbability,
} from 'decentralized-bradley-terry-rating';

const ratings = new Map<string, number>([
  ['alice', INITIAL_RATING],
  ['bob', INITIAL_RATING],
]);

const a = ratings.get('alice')!;
const b = ratings.get('bob')!;

// alice 승리
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // 갱신 후 예측 승률
```

C#:

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// alice 승리
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

언어별 전체 API는 각 패키지의 README에서 확인할 수 있습니다.

## 저장소 구조

```
.
├── typescript/   # npm 패키지
├── csharp/       # NuGet 패키지
└── docs/         # 이 README의 언어별 번역
```

## 테스트 실행

TypeScript(Node 23.6 이상. 네이티브 타입 제거로 `.ts` 파일을 바로 실행하므로 빌드 단계가 없습니다):

```bash
cd typescript
npm test
```

선택 사항:

```bash
npm install        # TypeScript와 @types/node 설치
npm run typecheck  # 타입 검사
npm run build      # dist/ 생성
```

C#(.NET 8 SDK):

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

두 테스트 스위트는 예측 공식, 대칭성, 단조성, 로그 오즈 보존, 학습률의 영향, 잘못된 입력, 그리고 온라인 갱신이 전역 Bradley–Terry MLE로 수렴하는지에 대한 엔드투엔드 검증을 다룹니다.

## 라이선스

MIT
