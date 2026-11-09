# Xếp hạng Bradley–Terry phi tập trung

Mỗi trận đấu chỉ cần xếp hạng hiện tại của hai người chơi và kết quả là đủ để cập nhật. Không cần máy chủ trung tâm, không cần bảng trận đấu toàn cục, cũng không cần mã người chơi. Kho lưu trữ có hai bản triển khai tương đương về ngữ nghĩa — TypeScript và C# — cả hai đều dựa trên hàm thuần.

**Ngôn ngữ:** [English](../README.md) · [简体中文](zh-CN.md) · [繁體中文](zh-TW.md) · [Deutsch](de-DE.md) · [Español](es-ES.md) · [Français](fr-FR.md) · [Italiano](it-IT.md) · [日本語](ja-JP.md) · [한국어](ko-KR.md) · [Русский](ru-RU.md) · **Tiếng Việt**

## Gói

| Ngôn ngữ | Gói | Tài liệu |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](../typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](../csharp/README.md) |

## Mô hình

Xếp hạng `r ∈ (0, 1)` là tỷ lệ thắng tương đối của người chơi: xác suất đánh bại một đối thủ trung tính có xếp hạng `r = 0.5`. Nó tương ứng với độ mạnh Bradley–Terry

```
γ = r / (1 - r)
```

Dự đoán cho một trận giữa hai người chơi là

```
P(A thắng B) = γA / (γA + γB)
             = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

Trong đó `θ = logit(r) = log(γ)`, `p = P(A thắng B)`, `s = 1` nếu A thắng và `0` nếu ngược lại, còn `η` là tốc độ học:

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

Đây là một bước leo dốc trực tuyến trên log-hợp lý toàn cục của Bradley–Terry, tức Elo ở dạng log-odds. Giá trị mặc định `η = 0.25` xấp xỉ hệ số `K ≈ 43` của cờ vua (`η = K · ln 10 / 400`).

Khi tốc độ học cố định, cập nhật trực tuyến sẽ ổn định trong lân cận `O(η)` của nghiệm toàn cục. Muốn hội tụ chặt chẽ về nghiệm hợp lý cực đại, hãy giảm tốc độ học theo số trận đã chơi `t`, chẳng hạn `η_t = η0 / (t + τ)`. Thư viện không lưu trạng thái, nên lịch giảm tốc do phía gọi truyền vào.

## Tính chất

1. **Bị chặn** — đầu vào và đầu ra luôn nằm nghiêm ngặt trong `(0, 1)`. Không có tỷ lệ thắng âm, không có giá trị vượt quá 100%.
2. **Trung tính tại 0.5** — `r = 0.5` nghĩa là ngang sức với một đối thủ trung tính, và mọi người chơi mới đều bắt đầu từ đây.
3. **Đối xứng** — đổi chỗ hai người chơi và đảo kết quả thì hai xếp hạng mới cũng đổi chỗ cho nhau. `P(A thắng B) + P(B thắng A) = 1`.
4. **Đơn điệu** — xếp hạng của người thắng tăng, của người thua giảm.
5. **Kết quả bất ngờ có sức nặng hơn** — kết quả bất ngờ dịch chuyển xếp hạng mạnh hơn kết quả được dự đoán, nên thắng đối thủ mạnh mang lại nhiều hơn thắng đối thủ yếu.
6. **Tổng log-odds bằng không** — `θA' + θB' = θA + θB`. Một trận không bao giờ làm đổi tổng log-odds của cặp đấu, nên toàn bộ tập xếp hạng không bị trôi. Nếu mọi người khởi đầu từ 0.5, tổng `θ` luôn bằng 0 — đúng chuẩn hóa của nghiệm hợp lý cực đại Bradley–Terry toàn cục.
7. **Hội tụ** — mỗi cập nhật tiến theo hướng gradient của log-hợp lý Bradley–Terry toàn cục. Nếu tốc độ học giảm dần theo điều kiện Robbins–Monro, xếp hạng sẽ hội tụ về MLE toàn cục; các bộ kiểm thử xác minh điều này bằng cách so với MLE tham chiếu tính bằng thuật toán MM.

## Hòa

Một trận chỉ có hai kết cục: A thắng hoặc B thắng. Nếu lĩnh vực của bạn cần kết quả hòa, đừng truyền thẳng vào thư viện này.

## Bắt đầu nhanh

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

// alice thắng
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // dự đoán sau cập nhật
```

C#:

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// alice thắng
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

API đầy đủ của từng ngôn ngữ nằm trong README của gói tương ứng.

## Cấu trúc kho lưu trữ

```
.
├── typescript/   # gói npm
├── csharp/       # gói NuGet
└── docs/         # bản dịch của README này
```

## Chạy kiểm thử

TypeScript (Node ≥ 23.6, chạy thẳng tệp `.ts` nhờ cơ chế loại bỏ kiểu gốc — không cần bước build):

```bash
cd typescript
npm test
```

Tùy chọn:

```bash
npm install        # cài TypeScript và @types/node
npm run typecheck  # kiểm tra kiểu
npm run build      # xuất ra dist/
```

C# (SDK .NET 8):

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

Cả hai bộ kiểm thử đều bao phủ công thức dự đoán, tính đối xứng, tính đơn điệu, bảo toàn log-odds, ảnh hưởng của tốc độ học, đầu vào không hợp lệ và kiểm chứng đầu-cuối rằng cập nhật trực tuyến hội tụ về MLE Bradley–Terry toàn cục.

## Giấy phép

MIT
