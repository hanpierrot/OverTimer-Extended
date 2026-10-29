**TRƯỜNG ĐẠI HỌC GIAO THÔNG VẬN TẢI**

**KHOA CÔNG NGHỆ THÔNG TIN**

**……………**

\[Chèn logo trường\]

**BÁO CÁO BÀI TẬP NHÓM**

**HỌC PHẦN: KIỂM THỬ PHẦN MỀM**

**ĐỀ TÀI: XÂY DỰNG KẾ HOẠCH KIỂM THỬ GAME CASUAL 2D “OVERTIMER”**

Giảng viên hướng dẫn: \[Họ tên giảng viên\]

Lớp: \[Mã lớp\]

Nhóm sinh viên: \[Họ tên – MSV\]

\[Họ tên – MSV\]

\[Họ tên – MSV\]

**Hà Nội, tháng 10 năm 2026**

**LỜI NÓI ĐẦU**

Trong phát triển trò chơi điện tử, chất lượng và độ ổn định của sản phẩm quyết định trực tiếp trải nghiệm người chơi. Khác với phần mềm nghiệp vụ, trò chơi có nhiều yếu tố ngẫu nhiên, thời gian thực và cân bằng số liệu, nên cần một kế hoạch kiểm thử được xây dựng bài bản.

OverTimer là trò chơi 2D quản lý tài nguyên và rủi ro, phát triển trên Unity, trong đó người chơi chạy đua với đồng hồ đếm ngược. Báo cáo này trình bày cơ sở lý thuyết về kiểm thử phần mềm và kiểm thử đơn vị, giới thiệu Unity Test Framework, từ đó xây dựng kế hoạch kiểm thử cho trò chơi dựa trên bản đặc tả yêu cầu phần mềm (SRS) của dự án.

*\[Tự điền\] Lời cảm ơn ngắn đối với giảng viên (hoặc đặt ở cuối báo cáo như mẫu).*

**MỤC LỤC**

*Mở file trong Word, vào References → Table of Contents để chèn mục lục; khi sửa nội dung, nhấp chuột phải vào mục lục → Update Field → Update entire table để cập nhật số trang.*

**DANH MỤC HÌNH ẢNH**

*\[Tự điền\] Liệt kê Hình 1.1, 1.2 … (ví dụ: mô hình chữ V, giao diện Unity Test Runner, kết quả chạy test từng phân hệ).*

**DANH SÁCH BẢNG**

*\[Tự điền\] Liệt kê Bảng 2.1, 3.1, 4.1 … (ví dụ: bảng truy vết yêu cầu – kiểm thử, các bảng ca kiểm thử, bảng tổng hợp kết quả).*

**DANH MỤC TỪ VIẾT TẮT**

| Thuật ngữ | Giải thích |
|---|---|
| SRS | Software Requirements Specification – đặc tả yêu cầu phần mềm của OverTimer |
| UTF | Unity Test Framework |
| TC | Test Case – ca kiểm thử |
| EP / BVA | Equivalence Partitioning / Boundary Value Analysis |
| RNG | Random Number Generator – bộ sinh số ngẫu nhiên (RngService, có seed) |
| SO | ScriptableObject – asset dữ liệu cấu hình của Unity |
| EM / PM / TC / RS | Mức thực hiện ca kiểm thử: Edit Mode test / Play Mode test / kiểm thử thủ công / rà soát mã (kiểm thử tĩnh) |
| ƯT | Độ ưu tiên của ca kiểm thử: Cao / TB (trung bình) / Thấp |
| “Ứng dụng” (app) | Ứng dụng trên Laptop *trong game* (Mining, Blackjack…); “đóng app” là đóng cửa sổ ứng dụng đó, không phải tắt trò chơi |
| Invariants (I1–I10) | Các quy tắc bất biến của trò chơi nêu trong SRS mục 2.5 |
| Clock / Meter / Hand / Pawn | Xem bảng thuật ngữ trong SRS mục 1.3 |

# CHƯƠNG 1: GIỚI THIỆU VỀ KIỂM THỬ PHẦN MỀM

## 1.1 Khái niệm kiểm thử phần mềm

Kiểm thử phần mềm là quá trình thực thi chương trình nhằm tìm lỗi và đánh giá mức độ đáp ứng yêu cầu. Nêu ngắn gọn mục tiêu: phát hiện lỗi, đảm bảo đúng yêu cầu, giảm rủi ro và chi phí.

*\[Tự điền\] Viết lại theo ý nhóm, có thể giữ khung như mẫu (4 mục tiêu chính).*

## 1.2 Các khái niệm cơ bản về kiểm thử

### 1.2.1 Lỗi, sai, thất bại và sự cố

Error – Fault – Failure – Incident. Gợi ý ví dụ lấy từ OverTimer: số dư tiền bị âm do bỏ qua TrySpend (fault) dẫn tới hiển thị tiền âm (failure).

### 1.2.2 Kiểm chứng và thẩm định

Verification: sản phẩm có đúng đặc tả SRS không. Validation: trò chơi có thật sự vui và cân bằng như người chơi mong đợi không.

### 1.2.3 Chất lượng và độ tin cậy

Các tiêu chí liên quan: tính đúng đắn, hiệu năng (WebGL), khả năng dùng, khả năng bảo trì, độ ổn định qua nhiều ván liên tiếp.

## 1.3 Vai trò của kiểm thử phần mềm

Phát hiện lỗi sớm, đảm bảo ổn định, nâng cao trải nghiệm, hỗ trợ cân bằng trò chơi. Tóm tắt phân tích tĩnh (rà soát mã, SRS) và phân tích động (chạy chương trình, ca kiểm thử).

## 1.4 Ca kiểm thử

Một ca kiểm thử gồm: mã ca, mô tả/mục tiêu, kỹ thuật thiết kế, mức thực hiện (Edit Mode / Play Mode / thủ công / rà soát mã), độ ưu tiên, điều kiện tiên quyết, dữ liệu đầu vào hoặc thao tác, kết quả mong đợi, kết quả thực tế và trạng thái (Đạt/Lỗi). Đây là các cột của bảng ca kiểm thử dùng ở Chương 4. Kết quả mong đợi phải là giá trị cụ thể, kiểm tra được (ví dụ “Money = 15”), không phải công thức hay mô tả chung chung.

## 1.5 Xác định các ca kiểm thử

### 1.5.1 Kiểm thử chức năng (hộp đen)

Chỉ dựa vào đầu vào/đầu ra theo SRS. Ví dụ: nhấp Meter với số dư \$57 thì Clock tăng 57 giây.

### 1.5.2 Kiểm thử cấu trúc (hộp trắng)

Dựa vào cấu trúc mã nguồn, phủ nhánh điều kiện. Ví dụ: các nhánh Thắng/Hòa/Thua/Natural trong logic Blackjack.

## 1.6 Các mức kiểm thử

Mô hình chữ V: kiểm thử đơn vị, kiểm thử tích hợp, kiểm thử hệ thống, kiểm thử chấp nhận. Nêu mức nào sẽ áp dụng cho OverTimer (xem 4.1).

*\[Tự điền\] Chèn Hình 1.x – mô hình chữ V.*

## 1.7 Đặc thù kiểm thử trò chơi

- **Yếu tố ngẫu nhiên:** vé số, thẻ bài, Blackjack, Wire Task đều qua RngService; kiểm thử dùng seed cố định để tái lập (Invariant I10, SRS mục 2.5). Với các yêu cầu về tỷ lệ (độ hiếm thẻ, phân bố biểu tượng…), dùng *kiểm thử thống kê*: chạy số lần lớn và so tỷ lệ thu được với tỷ lệ đặc tả trong một ngưỡng sai lệch cho phép (xem 4.1.4).
- **Thời gian thực:** Clock chạy liên tục, không tạm dừng; cần điều khiển thời gian trong test (giả lập delta time).
- **Trạng thái và sự kiện:** nhiều hệ thống giao tiếp qua sự kiện (OnMoneyChanged, OnCountdownFinished, GameOver); cần kiểm tra trường hợp nhiều sự kiện xảy ra trong cùng một khung hình.
- **Cân bằng:** ngoài đúng/sai còn phải kiểm tra thông lượng quanh chuẩn 3 \$/giây và tần suất các kết thúc hiếm (1–3% số ván).
- **Giao diện và thao tác:** nhấp, kéo, giữ chuột; panel chặn input; camera cuộn dọc – khó tự động hóa hoàn toàn.
- **Nền tảng trình duyệt (WebGL):** trò chơi chạy trong tab trình duyệt nên cần kiểm tra các tình huống quen thuộc với kiểm thử web như chuyển tab, thu nhỏ cửa sổ, tải lại trang.

# CHƯƠNG 2: KIỂM THỬ ĐƠN VỊ (UNIT TESTING)

## 2.1 Giới thiệu về kiểm thử đơn vị

Kiểm thử các đơn vị nhỏ nhất (hàm, phương thức, lớp) một cách độc lập, thường do lập trình viên thực hiện. Với OverTimer, đơn vị tiêu biểu là MoneyService, ClockService, BlackjackGame (logic thuần), ScratchTicket, CardSO/EffectCardSO.

## 2.2 Vai trò kiểm thử đơn vị

Phát hiện lỗi sớm, hỗ trợ refactor (dự án có nhiệm vụ tái cấu trúc mã bản Game Jam), giảm chi phí bảo trì, cải thiện thiết kế (tách logic khỏi MonoBehaviour).

## 2.3 Nguyên tắc viết kiểm thử đơn vị hiệu quả

- Độc lập, một test kiểm một hành vi, tên test phản ánh điều kiện và mong đợi.
- Không phụ thuộc trạng thái ngoài luồng: tránh Scene, PlayerPrefs thật, thời gian thực – dùng mock hoặc dữ liệu giả. Nếu buộc phải chạm PlayerPrefs (ví dụ TC-WIN-8, TC-GAM-5), sao lưu ở \[SetUp\] và khôi phục ở \[TearDown\].
- Mô hình AAA (Arrange – Act – Assert).
- Dùng seed cố định cho RngService để kết quả lặp lại.

## 2.4 Các phương pháp kiểm thử đơn vị

### 2.4.1 Kiểm thử hộp trắng

Phủ câu lệnh/nhánh/điều kiện. Ví dụ: ClockService – nhánh thắng (≥ 600 s), nhánh thua (≤ 0 s), nhánh bị Final Countdown chặn.

Áp dụng đầy đủ cho chức năng Blackjack (đồ thị luồng điều khiển, độ phức tạp C(Vg), đường thi hành tuyến tính độc lập, phủ cấp 3, kiểm thử vòng lặp): mục 4.3.8.

### 2.4.2 Kiểm thử hộp đen

Thiết kế theo đặc tả SRS. Ví dụ: Meter từ chối khi số dư dưới \$10.

## 2.5 Các kỹ thuật kiểm thử đơn vị

- **2.5.1 Phân vùng tương đương:** ví dụ hạng vé (Triple Match / Lucky Nine), độ hiếm thẻ (C/R/SR/UR), kết quả ván Blackjack.
- **2.5.2 Phân tích giá trị biên:** ví dụ Clock 599/600 s, 0/1 s; số dư \$9/\$10 khi dùng Meter; Hand 4/5 khi mở gói; cào 59%/60% diện tích. Với giới hạn thời gian T, kiểm tra tại T − ε, T và T + ε.
- **2.5.3 Bảng quyết định:** ví dụ Final Countdown (giai đoạn None/Armed/Triggered × Clock tự về 0 / Place bản thứ hai); thắng/thua Blackjack.
- **2.5.4 Kiểm thử luồng điều khiển:** các nhánh if/switch trong state machine của Cat (Wandering, SittingIdle, SittingBlocking, Dragged).
- **2.5.5 Mocking và Stubbing:** giả lập RngService (seed/bộ số cố định), giả lập thời gian, stub MoneyService khi test CardBox. Lưu ý: mock/seed là *cách cài đặt* test, không phải kỹ thuật thiết kế ca kiểm thử; trong bảng ca kiểm thử, cột “Kỹ thuật” ghi kỹ thuật thiết kế (EP, BVA, bảng quyết định, luồng điều khiển, thống kê).

*\[Tự điền\] Có thể thêm 1 bảng quyết định mẫu (Bảng 2.1) cho Final Countdown hoặc Blackjack.*

## 2.6 Ưu, nhược điểm và ứng dụng của kiểm thử đơn vị

- **Ưu điểm:** phát hiện lỗi sớm, hỗ trợ refactor, tự động hóa, tích hợp CI.
- **Nhược điểm:** không phát hiện lỗi tích hợp/giao diện, tốn công bảo trì test, khó với mã gắn chặt MonoBehaviour.
- **Ứng dụng trong game:** kiểm tra luật chơi, kinh tế, RNG; bổ trợ bằng kiểm thử Play Mode ở Chương 4.

# CHƯƠNG 3: GIỚI THIỆU VỀ UNITY TEST FRAMEWORK

## 3.1 Khái niệm về Unity Test Framework

UTF là package kiểm thử tích hợp trong Unity, xây dựng trên NUnit, cho phép viết và chạy test ngay trong Editor (cửa sổ Test Runner) hoặc qua dòng lệnh.

## 3.2 Lịch sử phát triển

NUnit → Unity Test Runner → Unity Test Framework (package com.unity.test-framework). Tóm tắt 2–3 câu.

*\[Tự điền\] Tra cứu nguồn tài liệu chính thức của Unity để trích dẫn.*

## 3.3 Các tính năng chính

### 3.3.1 Edit Mode test và Play Mode test

Edit Mode: chạy trong Editor, không cần Play, phù hợp logic thuần C#. Play Mode: chạy trong vòng đời game, phù hợp Coroutine, MonoBehaviour, tương tác giữa các thành phần.

### 3.3.2 Attribute và Assertion

\[Test\], \[UnityTest\], \[SetUp\], \[TearDown\], \[TestCase\], \[Category\]; Assert.AreEqual, Assert.IsTrue, Assert.Throws, Assert.That.

### 3.3.3 Assembly Definition cho test

Tách assembly test khỏi assembly game để biên dịch độc lập và tham chiếu mã cần kiểm thử.

### 3.3.4 Đo độ phủ mã (Code Coverage)

Package Code Coverage cho báo cáo phủ câu lệnh/nhánh theo assembly.

### 3.3.5 Chạy dòng lệnh và tích hợp CI

Unity -batchmode -runTests ... xuất kết quả XML; có thể tích hợp GitHub Actions.

## 3.4 Các công cụ hỗ trợ khác

- Unity Console & Debug.Log: theo dõi log runtime, exception, null reference.
- Unity Profiler: đo CPU, bộ nhớ, rendering; kiểm tra Object Pooling không phát sinh cấp phát.
- Play Mode thủ công và menu gian lận (cấp tiền/thời gian/thẻ, xem thử từng kết thúc).
- Build WebGL: kiểm tra trên trình duyệt trước khi phát hành itch.io.

## 3.5 Ưu, nhược điểm của Unity Test Framework

- **Ưu điểm:** tích hợp sẵn, miễn phí, hỗ trợ cả Edit/Play Mode, chạy được trên CI.
- **Nhược điểm:** Play Mode test chậm, không hỗ trợ sẵn mock (cần thêm thư viện như NSubstitute), khó giả lập thao tác chuột phức tạp.

## 3.6 Cách sử dụng

### 3.6.1 Cài đặt

Package Manager → Unity Test Framework (thường đã có sẵn). Mở Window → General → Test Runner.

### 3.6.2 Tạo thư mục Tests và Assembly Definition

Tạo Assets/Tests/EditMode và Assets/Tests/PlayMode, mỗi nơi một Assembly Definition tham chiếu assembly game.

Lưu ý với OverTimer: ban đầu dự án không có Assembly Definition nào, toàn bộ mã game nằm trong assembly mặc định Assembly-CSharp. Một assembly test định nghĩa bằng .asmdef không tham chiếu được Assembly-CSharp, nên phải tạo thêm:

- OverTimer.asmdef cho Assets/Code/Scripts (tham chiếu UnityEngine.UI, Unity.TextMeshPro, Unity.InputSystem, Unity.Mathematics, DOTween.Modules);
- OverTimer.Editor.asmdef cho Assets/Code/Scripts/Editor (chỉ nền tảng Editor, để công cụ Editor không lọt vào bản build WebGL);
- DOTween.Modules.asmdef cho Assets/Plugins/Demigiant/DOTween/Modules (vì mã game dùng các phương thức mở rộng của DOTween);
- OverTimer.Tests.EditMode.asmdef và OverTimer.Tests.PlayMode.asmdef, tham chiếu OverTimer, nunit.framework.dll, UnityEngine.TestRunner, UnityEditor.TestRunner và có ràng buộc UNITY_INCLUDE_TESTS.

### 3.6.3 Viết test đơn giản

Ví dụ kiểm tra MoneyService.TrySpend (dạng AAA).

*\[Tự điền\] Chèn đoạn code C# ví dụ (Hình 3.x) và ảnh kết quả Test Runner.*

### 3.6.4 Chạy test và đọc kết quả

Run All / Run Selected; ý nghĩa dấu tick xanh/đỏ; xem log lỗi.

### 3.6.5 Cấu trúc thư mục dự án có test

*\[Tự điền\] Chèn sơ đồ cây thư mục thực tế của OverTimer – Extended (Assets/Scripts, Assets/Tests...).*

# CHƯƠNG 4: ỨNG DỤNG KIỂM THỬ CHO GAME OVERTIMER

## 4.1 Giới thiệu chung về chương trình kiểm thử

### 4.1.1 Giới thiệu đối tượng kiểm thử

OverTimer: game casual 2D trên Unity (URP), một Scene, điều khiển bằng chuột, phát hành WebGL trên itch.io. Người chơi bắt đầu với 120 giây, kiếm tiền, đổi tiền thành thời gian qua Meter và phải đưa Clock lên 600 giây để thắng.

### 4.1.2 Phạm vi và nguồn yêu cầu kiểm thử

- Căn cứ: SRS của dự án (19 nhóm chức năng, 10 quy tắc bất biến I1–I10, yêu cầu phi chức năng).
- Trong phạm vi: các chức năng đã cài đặt (Clock, Meter, Mining, Blackjack, Billiards, vé số, thẻ bài, Phone, ITRS, Cat, Egg Timer, Pawn, Wire Task, Hints & Tutorial, Run Save & Summary).
- Ở trạng thái dự kiến (SRS): Shop và 6/12 kết thúc ván (MotherLodeWin, CaveInLoss, BlackjackCharlieWin, BlackjackRuinLoss, Jackpot, Curse – enum EndReason trong mã hiện chỉ có 6 giá trị); chỉ lập ca kiểm thử khung (ghi nhãn **\[Dự kiến\]**), chạy khi hoàn thành. Ca \[Dự kiến\] không tính vào tỷ lệ Đạt/Lỗi ở mục 4.8.
- Phạm vi được đối chiếu với mã nguồn nhánh main ngày 18/10/2026: Billiards (Assets/Code/Scripts/Desk/Laptop/Billiards) và Hints & Tutorial (HintService, HintCatalog, TutorialPanel) đã được cài đặt nên chuyển từ \[Dự kiến\] sang phạm vi kiểm thử.
- **Đối chiếu lại ngày 28/10/2026 (main @ d4ad76f; mã nguồn đã chuyển sang Assets/Scripts, dữ liệu sang Assets/Data/Configs).** Các phần mã mới từ 18/10 được bổ sung ca kiểm thử: lõi Clock (PauseLock, SpeedModifiers, GameDelta – 4.2.4), cách tính thưởng vé mới và thời gian hồi Egg Timer (4.3.6), 4 kết thúc theo cơ chế TicketJackpotWin / TicketCurseLoss / MotherLodeWin / BlackjackCharlieWin (4.3.7; EndReason nay có 10 giá trị, CaveInLoss và BlackjackRuinLoss đã bị bỏ), phí bỏ debuff và giá theo khóa (4.4.4), Shop, Buyback, Phone mới, thuế mới, cửa – bưu kiện – thư (4.5.4 – 4.5.8), camera ba góc nhìn và DeviceLayer (4.7.8), âm thanh – Reward Popup – Highlight Hover (4.7.9), bổ sung vòng đời ván (4.6.2). Các ca cũ không còn khớp mã mới được liệt kê ở **Phụ lục A** (chưa sửa trực tiếp trong bảng cũ).

### 4.1.3 Chiến lược và mức kiểm thử

| Mức | Đối tượng | Công cụ | Ký hiệu | Mục |
|---|---|---|---|---|
| Đơn vị (Edit Mode) | Logic thuần, không cần vòng đời Unity: BlackjackGame, lõi Billiards (BilliardsReferee, BilliardsRound, BilliardsSim, BilliardsPayout), HintCatalog, MoneyService.Add/TrySpend | UTF + NUnit | EM | 4.2 – 4.3, 4.7.6 |
| Đơn vị và tích hợp (Play Mode) | Logic nằm trong MonoBehaviour hoặc phụ thuộc singleton/GameConfig/Update: ClockService, RngService, vé số, Card Box, hiệu ứng thẻ, Final Countdown, Annoyance, lưu ván; tương tác CardBox ↔ Hand ↔ Money, Cat ↔ PointerReceiver, ITRS ↔ Annoyance | UTF Play Mode | PM | 4.2 – 4.6 |
| Hệ thống | Vòng đời ván, các kết thúc, lưu ván, WebGL, hiệu năng | Play Mode thủ công, Profiler, menu gian lận | TC | 4.6 – 4.7 |
| Kiểm thử tĩnh | Quy tắc mã nguồn (không dùng UnityEngine.Random, không viết cứng số cân bằng) | Tìm kiếm mã, rà soát | RS | 4.2, 4.7 |
| Chấp nhận | Cân bằng, độ hiểu luật chơi | Playtest, khảo sát | – | 4.7 |

*Vì sao phần lớn ca đơn vị chạy Play Mode:* hầu hết service của OverTimer (MoneyService, ClockService, RngService, GameManager, ITRSService, AnnoyanceManager…) là MonoBehaviour dạng singleton, khởi tạo trong Awake()/Start() và đọc GameManager.Instance.GameConfig; Clock đếm ngược trong Update() bằng Time.deltaTime. Ở Edit Mode, Unity không gọi Awake/Start/Update, nên các giá trị khởi tạo, singleton và thời gian đều không có. Chỉ các lớp C# thuần (BlackjackGame, lõi mô phỏng và luật Billiards, HintCatalog) và hai hàm Add/TrySpend của MoneyService (không phụ thuộc vòng đời) kiểm thử được ở Edit Mode. Muốn đưa thêm ca về Edit Mode cần tách logic thuần (công thức thuế, bảng thưởng vé, roll độ hiếm, sinh vein, máy trạng thái Final Countdown) ra lớp C# riêng – đây là một hướng tái cấu trúc (xem 2.2).

### 4.1.4 Môi trường và tiêu chí

- **Môi trường:** Unity \[phiên bản\], Windows, trình duyệt \[Chrome/Edge/Firefox\] cho bản WebGL, Git/GitHub, \[công cụ quản lý công việc của nhóm\].
- **Tiêu chí đạt:** kết quả thực tế khớp kết quả mong đợi; không còn lỗi mức nghiêm trọng; mọi Invariant I1–I10 được xác nhận; 100% ca ưu tiên Cao đạt.
- **Quy ước kiểm thử:** dùng RngService với seed cố định; giá trị cân bằng đọc từ GameConfig/ScriptableObject, không viết cứng trong test. Các con số trong bảng dưới đây lấy theo SRS; khi viết test, đối chiếu lại với asset dữ liệu.
- **Tiêu chí cho ca thống kê:** với tỷ lệ đặc tả *p* và số mẫu *N*, ca đạt khi tỷ lệ thu được *p̂* thỏa \|*p̂* − *p*\| ≤ 3·√(*p*(1 − *p*)/*N*) cho từng hạng mục (hoặc kiểm định χ² với mức ý nghĩa 0,01 cho cả phân bố). Ví dụ với *N* = 10.000: *p* = 60% → sai lệch cho phép ±1,5%; *p* = 25% → ±1,3%; *p* = 3% → ±0,5%. Vì dùng seed cố định nên kết quả lặp lại, nhưng nên chạy thêm 3–5 seed khác nhau để tránh trường hợp một seed tình cờ “đẹp”.
- **Ô có nhãn \[Xác nhận SRS\]:** giá trị chưa có trong tài liệu hiện có; nhóm tra SRS/GameConfig để điền trước khi chạy.

### 4.1.5 Bảng truy vết yêu cầu – kiểm thử

| Chức năng SRS | Nội dung | Mục kiểm thử |
|---|---|---|
| 01, 02 | Clock, Tiền và Meter | 4.2 |
| 04, 05, 06, 07, 13, 17 | Wire Task, Mining, Blackjack, Vé số, Egg Timer và nhiệm vụ nhỏ, Billiards | 4.3 |
| 08, 09, 15 | Card Box, hiệu ứng thẻ, thẻ thắng/thua | 4.4 |
| 10, 11, 12, 14, 18 | Phone, ITRS, Cat, Pawn, Shop \[Dự kiến\] | 4.5 |
| 15, 16 | Kết thúc ván (12 EndReason), lưu ván, chơi lại | 4.6 |
| 03, 19 + phi chức năng | Nhập liệu/camera, Hints & Tutorial, hiệu năng, nền tảng WebGL | 4.7 |
| Bổ sung 28/10 | Lõi Clock; vé số mới, Egg Timer; 4 kết thúc theo cơ chế; phí bỏ debuff; Shop, Buyback, Phone, thuế, cửa/bưu kiện/thư; camera 3 góc nhìn, DeviceLayer; âm thanh, Reward Popup, Highlight Hover | 4.2.4, 4.3.6, 4.3.7, 4.4.4, 4.5.4 – 4.5.8, 4.6.2, 4.7.8, 4.7.9 |
| Bổ sung 29/10 | Kiểm thử luồng điều khiển (hộp trắng) chức năng Blackjack | 4.3.8 |

*Ghi chú: mỗi mục 4.2 – 4.7 bên dưới có bảng ca kiểm thử. Cột “Mức”: EM/PM/TC/RS (xem 4.1.3); cột “ƯT”: độ ưu tiên. Nhóm điền kết quả thực tế và trạng thái sau khi chạy.*

## 4.2 Kiểm thử Core Services

### 4.2.1 MoneyService

Mục tiêu: số dư không bao giờ âm; mọi khoản chi đi qua TrySpend. Tiền khởi đầu \$40.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-MON-1 | Số dư ban đầu | Phân vùng tương đương | PM | Cao | Ván mới | Khởi tạo MoneyService | Current = 40 | | |
| TC-MON-2 | Chi tiêu khi đủ tiền | Phân vùng tương đương | EM | Cao | Money = 40 | TrySpend(25) | Trả về true; Current = 15 | | |
| TC-MON-3 | Chi tiêu đúng bằng số dư | Giá trị biên | EM | Cao | Money = 25 | TrySpend(25) | Trả về true; Current = 0 | | |
| TC-MON-4 | Chi tiêu thiếu 1 đơn vị | Giá trị biên | EM | Cao | Money = 24 | TrySpend(25) | Trả về false; Current = 24 (không đổi) | | |
| TC-MON-5 | Phát sự kiện khi đổi số dư | Phân vùng tương đương | EM | TB | Money = 40; đăng ký lắng nghe OnMoneyChanged | Add(10) | OnMoneyChanged phát đúng 1 lần, mang giá trị 50 | | |
| TC-MON-6 | Chi tiêu số tiền bằng 0 | Giá trị biên | EM | Thấp | Money = 40 | TrySpend(0) | Không đổi số dư; không phát OnMoneyChanged; giá trị trả về theo SRS \[Xác nhận SRS\] | | |
| TC-MON-7 | Số tiền âm | Phân vùng tương đương (vùng không hợp lệ) | EM | TB | Money = 40 | TrySpend(-5); Add(-5) | Bị từ chối (trả về false hoặc ném ArgumentException); Current = 40 | | |

### 4.2.2 ClockService và Meter

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-CLK-1 | Giá trị khởi tạo | Phân vùng tương đương | PM | Cao | Ván mới | Khởi tạo ClockService | Clock = 120 s | | |
| TC-CLK-2 | Đếm ngược thường | Phân vùng tương đương | PM | Cao | Clock = 120; TickMultiplier = 1 | Giả lập trôi 1 s | Clock = 119 | | |
| TC-CLK-3 | Phone đang reo | Phân vùng tương đương | PM | Cao | Clock = 120; TickMultiplier = 1.5 | Giả lập trôi 1 s | Clock = 118.5 | | |
| TC-CLK-4 | Biên thắng | Giá trị biên | PM | Cao | (a) Clock = 599; (b) Clock = 598 | AddSeconds(1) | (a) Clock = 600, GameOver(ClockWin); (b) Clock = 599, chưa thắng | | |
| TC-CLK-5 | Biên thua | Giá trị biên | PM | Cao | Clock = 1; không có Final Countdown | Giả lập trôi 1 s | Clock = 0; phát OnCountdownFinished; GameOver(ClockLoss) | | |
| TC-CLK-6 | Trừ trực tiếp làm Clock về 0 khi Final Countdown Armed | Luồng điều khiển | PM | Cao | Clock = 20; Final Countdown = Armed | Spend(20) | GameOver(ClockLoss) ngay (Final Countdown chỉ chặn khi Clock tự đếm về 0) | | |
| TC-CLK-7 | Meter dưới mức tối thiểu | Giá trị biên | PM | Cao | Money = 9; Clock = 120 | Nhấp Meter | Từ chối; Money = 9; Clock = 120 | | |
| TC-CLK-8 | Meter đúng mức tối thiểu | Giá trị biên | PM | Cao | Money = 10; Clock = 120 | Nhấp Meter | Clock = 130; Money = 0 | | |
| TC-CLK-9 | Meter dùng toàn bộ số dư | Phân vùng tương đương | PM | Cao | Money = 57; Clock = 120 | Nhấp Meter | Clock = 177; Money = 0 | | |
| TC-CLK-10 | Meter bị khóa | Bảng quyết định | PM | Cao | Final Countdown = Triggered; Money = 50 | Nhấp Meter | Từ chối; Money = 50; Clock không cộng thêm | | |
| TC-CLK-11 | Meter đẩy Clock vượt 600 | Giá trị biên | PM | Cao | Clock = 590; Money = 57 | Nhấp Meter | Money = 0; GameOver(ClockWin) (Clock ≥ 600) | | |
| TC-CLK-12 | Nhấp đúp nhanh vào Meter | Phân vùng tương đương | PM | TB | Clock = 120; Money = 50 | Nhấp Meter 2 lần liên tiếp trong cùng khung hình / cách 50 ms | Clock = 170 (chỉ cộng một lần); Money = 0; lần nhấp 2 bị từ chối vì Money = 0 | | |

### 4.2.3 RngService

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-RNG-1 | Tái lập theo seed | Phân vùng tương đương | PM | Cao | Seed = 12345 | Khởi tạo hai RngService cùng seed, mỗi cái lấy 100 số | Hai dãy giống hệt nhau | | |
| TC-RNG-2 | Không dùng UnityEngine.Random | Rà soát mã (kiểm thử tĩnh) | RS | TB | Mã nguồn bản hiện tại | Tìm “UnityEngine.Random” và “Random.Range” trong Assets/Code/Scripts | Không còn lời gọi nào ngoài RngService | Rà soát ngày 18/10/2026: còn 1 lời gọi Random.Range tại Cutscene/ImageCycler.cs:42 (chọn ảnh ngẫu nhiên trong đoạn phim; chỉ ảnh hưởng hình ảnh, không ảnh hưởng kết quả ván) | Lỗi (nhẹ) |

### 4.2.4 Lõi Clock mới: PauseLock, SpeedModifiers, GameDelta (bổ sung 28/10)

Mục tiêu: tạm dừng Clock được đếm theo người giữ (cuộc gọi có lợi, đoạn phim, GameOver chồng nhau không mở khóa nhầm); tốc độ Clock là **tích** các hệ số theo khóa (Phone đang reo × thẻ tốc độ), kẹp trong \[0; 3\]. PauseLock và SpeedModifiers là C# thuần nên kiểm thử Edit Mode.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-CLC-1 | Cùng một người giữ tạm dừng hai lần | Phân vùng tương đương | EM | Cao | PauseLock mới | Acquire(A) hai lần | HolderCount = 1; IsPaused = true; PausedChanged phát đúng 1 lần (true); lần Acquire thứ 2 trả về false | | |
| TC-CLC-2 | Hai người giữ chồng nhau | Bảng quyết định | EM | Cao | PauseLock mới | Acquire(A), Acquire(B), Release(A), Release(B) | Sau Release(A): vẫn IsPaused = true, không phát sự kiện; sau Release(B): IsPaused = false, PausedChanged(false) phát đúng 1 lần | | |
| TC-CLC-3 | Giải phóng người không giữ / null | Phân vùng tương đương (vùng không hợp lệ) | EM | TB | PauseLock đang giữ A | Release(C); Release(null); Acquire(null) | Release(C), Release(null) trả về false, vẫn tạm dừng; Acquire(null) ném ArgumentNullException | | |
| TC-CLC-4 | Tốc độ là tích các hệ số | Phân vùng tương đương | EM | Cao | SpeedModifiers mới | Set(phone, 1,5); Set(thẻ, 0,75) | Effective = 1,125 (không còn “ghi sau cùng thắng”) | | |
| TC-CLC-5 | Kẹp hệ số tốc độ | Giá trị biên | EM | Cao | SpeedModifiers mới | (a) Ba khóa 1,5; (b) một khóa 0 (Time Scarab) cùng khóa 1,5 | (a) 3,375 → Effective = 3; (b) Effective = 0 | | |
| TC-CLC-6 | Đặt lại cùng khóa, xóa khóa, giá trị sai | Bảng quyết định | EM | TB | SpeedModifiers mới | Set(k, 1,5) rồi Set(k, 0,75); Remove(khóa không có); Set(k, −1); Set(k, NaN) | Effective = 0,75, Count = 1; Remove không lỗi; −1 và NaN ném ArgumentOutOfRangeException | | |
| TC-CLC-7 | GameDelta bằng 0 khi Clock tạm dừng | Luồng điều khiển | PM | Cao | Ván mới; Clock = 100 | ClockService.Pause(holder), chờ 2 s, Resume(holder), chờ 1 s | Trong 2 s: GameDelta = 0, Clock vẫn 100; sau Resume: Clock ≈ 99 | | |
| TC-CLC-8 | Phone reo chồng thẻ Snail Speed | Bảng quyết định | PM | Cao | Clock = 100; Phone đang reo (×1,5); thẻ Snail Speed (×0,75) đang hiệu lực | Chờ 4 s; sau đó Pawn thẻ Snail Speed và chờ thêm 2 s | Sau 4 s: Clock ≈ 95,5 (×1,125); sau khi Pawn: tốc độ ×1,5 (Clock giảm thêm ≈ 3 s) | | |
| TC-CLC-9 | Meter bỏ qua khi game đang đóng băng | Bảng quyết định | PM | Cao | Money = 50; CardPanel đang mở (Time.timeScale = 0) | Nhấp nút Feed của Meter | Money = 50; Clock không đổi; không hiện gợi ý. Đóng CardPanel rồi nhấp lại: Money = 0, Clock +50 | | |

## 4.3 Kiểm thử hệ thống kiếm tiền

### 4.3.1 Mining

Mục tiêu: vein 6–10 khối đúng thứ tự, tiền chưa xác nhận (unconfirmed) chỉ vào tài khoản khi hoàn thành vein, mất khi đóng ứng dụng Mining trên Laptop.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-MIN-1 | Độ dài vein hợp lệ | Thống kê | PM | Cao | Seed cố định | Sinh 1.000 vein | Mọi vein có độ dài trong \[6, 10\]; mỗi độ dài 6…10 đều xuất hiện ít nhất 1 lần | | |
| TC-MIN-2 | Thứ tự khối | Phân vùng tương đương | PM | TB | Seed cố định | Sinh một vein 10 khối | Dirt → Wood → Stone → Coal → … đúng thứ tự cố định trong BlockConfig | | |
| TC-MIN-3 | Hoàn thành vein 6 khối | Phân vùng tương đương | PM | Cao | Đã mở Laptop; Money = 0; vein 6 khối (HP 1, 2, 2, 3, 3, 4) | Phá hết 6 khối | Money = 29 (1+2+4+5+7+10 theo BlockConfig); vein mới xuất hiện sau 1,0 s | | |
| TC-MIN-4 | **[Dự kiến]** Kết thúc MotherLodeWin | Bảng quyết định | PM | Cao | Đang ở ứng dụng Mining | Hoàn thành liên tiếp 3 vein dài 10 khối, không đóng ứng dụng | GameOver(MotherLodeWin) | | |
| TC-MIN-5 | Đóng ứng dụng Mining giữa vein | Bảng quyết định | PM | Cao | Money = 20; đang ở giữa một vein | Phá 3 khối rồi đóng ứng dụng Mining | Tiền unconfirmed bị mất; Money = 20; Clock không bị trừ thêm | | |
| TC-MIN-6 | Giới hạn tốc độ nhát đào | Giá trị biên | PM | TB | T = khoảng nghỉ tối thiểu giữa hai nhát trong GameConfig \[Xác nhận SRS\] | Hai nhát cách nhau T − 0,01 s / T / T + 0,01 s | Nhát thứ hai: bị bỏ qua (HP khối không giảm) / hợp lệ / hợp lệ | | |
| TC-MIN-7 | Thẻ Lucky Ruby / Butter Finger | Bảng quyết định | PM | TB | Đã đặt thẻ tương ứng; vein kế tiếp trị giá \$29 | Hoàn thành vein kế | Lucky Ruby: +\$58; Butter Finger: +\$0 | | |
| TC-MIN-8 | **[Dự kiến]** Sập hầm (CaveInLoss) | Thống kê | PM | TB | Seed cố định | Mô phỏng 10.000 vein dài 10 khối | Tỷ lệ CaveInLoss trong 3% ± 0,5% mỗi vein; cảnh báo xuất hiện từ khối thứ 8; đóng ứng dụng trước khi sập thì không bị CaveInLoss | | |

### 4.3.2 Blackjack (BlackjackGame và BlackjackApp)

Mục tiêu: luật chơi đúng, ante bằng giây Clock, thưởng bằng tiền. BlackjackGame là logic thuần nên kiểm thử Edit Mode; các ca cần điều khiển lá bài dùng bộ bài giả lập (stub) xếp sẵn thứ tự.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-BJK-1 | Tính điểm Át mềm | Phân vùng tương đương | EM | Cao | – | Tay bài A + 6; sau đó rút thêm 9 | 17 (soft); sau khi rút 9: 16 (Át tính 1) | | |
| TC-BJK-2 | Natural | Phân vùng tương đương | EM | Cao | Bộ bài xếp sẵn | Hai lá đầu của người chơi: A + K | Ván kết thúc ngay; thưởng Natural (\$50) | | |
| TC-BJK-3 | Quá 21 | Giá trị biên | EM | Cao | Bộ bài xếp sẵn | (a) Tay 12, Hit được lá 10; (b) Tay 11, Hit được lá 10 | (a) Tổng 22 → thua ngay; (b) Tổng 21 → chưa thua, chuyển lượt hoặc chờ Stand | | |
| TC-BJK-4 | Nhà cái dừng ở 17 (kể cả soft 17) | Giá trị biên | EM | Cao | Bộ bài xếp sẵn | Nhà cái có (a) 16; (b) 17 cứng; (c) A + 6 | (a) Rút thêm; (b) Dừng; (c) Dừng | | |
| TC-BJK-5 | Double chỉ với hai lá đầu | Bảng quyết định | EM | TB | Người chơi đang có 3 lá | Chọn Double | Từ chối | | |
| TC-BJK-6 | Bảng thưởng | Bảng quyết định | EM | Cao | Bộ bài xếp sẵn cho từng kết quả | Thắng / Hòa / Natural / Double thắng | \$40 / \$20 / \$50 / \$80 (theo GameConfig) | | |
| TC-BJK-7 | Đủ thời gian để cược | Giá trị biên | PM | Cao | (a) Clock = 34; (b) Clock = 35 | Nhấn Deal | (a) Từ chối, hiện gợi ý không đủ thời gian; (b) Cho phép, Clock = 15 (ante 20 s + margin 15 s) | | |
| TC-BJK-8 | Đóng ứng dụng Blackjack giữa ván | Luồng điều khiển | PM | TB | Đang trong ván, ante đã trừ; Money = 30 | Đóng ứng dụng | Mất ante; Money = 30; không có thưởng | | |
| TC-BJK-9 | Tái lập bộ bài | Phân vùng tương đương | EM | TB | Seed cố định | Xào bài 2 lần cùng seed | Hai thứ tự bài giống nhau | | |
| TC-BJK-10 | **[Dự kiến]** Kết thúc bổ sung | Bảng quyết định | PM | TB | Bộ bài xếp sẵn | (a) 7 lá không quá 21; (b) 3 ván Double thua liên tiếp | (a) GameOver(BlackjackCharlieWin); (b) GameOver(BlackjackRuinLoss) | | |

### 4.3.3 Vé số cào (Tickets)

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-TKT-1 | Mua khi thiếu tiền | Giá trị biên | PM | Cao | Money = 24 | Mua Triple Match (giá \$25) | Không phát vé; Money = 24 | | |
| TC-TKT-2 | Mua khi đủ tiền | Phân vùng tương đương | PM | Cao | Money = 25 | Mua Triple Match | Money = 0; symbol được chốt ngay lúc mua; vé lên bàn | | |
| TC-TKT-3 | Bảng thưởng theo số ô trùng | Bảng quyết định | PM | Cao | Vé Triple Match; baseReward = B, payoutMultiplier = M lấy từ TicketSO \[Xác nhận SRS\] | maxMatch = 1 / 2 / 3 | \$0 / \$B×M / \$2×B×M (công thức baseReward × (maxMatch − 1) × payoutMultiplier); điền số cụ thể sau khi tra TicketSO | | |
| TC-TKT-4 | Hòa số ô trùng | Bảng quyết định | PM | TB | Vé có hai biểu tượng cùng số ô trùng lớn nhất | Tính thưởng | Thưởng tính theo biểu tượng có hệ số cao hơn | | |
| TC-TKT-5 | Ngưỡng tự lộ ô | Giá trị biên | PM | TB | Vé đang cào | Cào 59% / 60% diện tích một ô | Chưa lộ / tự lộ | | |
| TC-TKT-6 | Cash Out | Luồng điều khiển | PM | Cao | Vé mới cào một phần | Nhấn Cash Out | Lộ nốt các ô; trả thưởng theo kết quả đã chốt lúc mua; vé trở về pool | | |
| TC-TKT-7 | Phân bố symbol | Thống kê | PM | TB | Seed cố định | Sinh 10.000 vé mỗi loại | Triple Match: 75/20/5%; Lucky Nine: 40/27/18/10/5%; mỗi tỷ lệ nằm trong ngưỡng ở 4.1.4 (ví dụ 75% ± 1,3%) | | |
| TC-TKT-8 | Object Pool | Luồng điều khiển | PM | TB | Pool khởi tạo xong | Mua – cào – trả vé 50 lần | Không có Instantiate/Destroy (Profiler); số vé trong pool không đổi | | |
| TC-TKT-9 | **[Dự kiến]** Jackpot / Curse | Bảng quyết định | PM | TB | Vé Lucky Nine đã chốt toàn biểu tượng hiếm (Jackpot) hoặc toàn biểu tượng phổ biến (Curse) | (a) Tự cào lộ cả 9 ô; (b) Cash Out | (a) GameOver(Jackpot / Curse tương ứng); (b) Không kết thúc ván, chỉ trả thưởng | | |
| TC-TKT-10 | Nhấp đúp nút mua vé | Phân vùng tương đương | PM | TB | Money = 30 | Nhấp mua Triple Match 2 lần liên tiếp | Chỉ phát 1 vé; Money = 5 | | |

### 4.3.4 Egg Timer, nhiệm vụ nhỏ và Wire Task

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-TSK-1 | Egg Timer giữ đủ thời gian | Giá trị biên | PM | Cao | Clock = 100 | Giữ chuột 1,0 s | Clock tăng 3 s (bỏ qua phần trôi tự nhiên trong lúc giữ) | | |
| TC-TSK-2 | Egg Timer thả sớm | Giá trị biên | PM | TB | Clock = 100 | Giữ 0,9 s rồi thả | Không cộng thời gian; tiến trình đặt lại về 0 | | |
| TC-TSK-3 | Click Task | Phân vùng tương đương | PM | TB | Seed cố định; số lần yêu cầu n (3–8) | Nhấp đủ n lần | +\$5; hồi 3 s; sau đó tung lại n mới trong \[3, 8\] | | |
| TC-TSK-4 | Power Bank | Phân vùng tương đương | PM | TB | Power Bank sẵn sàng | Giữ 2 s | +\$8; vào thời gian hồi | | |
| TC-TSK-5 | Wire Task | Bảng quyết định | PM | Cao | Wire Task chưa giải; Money = 0 | (a) Nối đúng cả 5 cặp; (b) Nối một cặp sai | (a) Money = 20, Laptop được mở khóa; (b) Cặp sai bị từ chối, không cộng tiền | | |
| TC-TSK-6 | Laptop khi chưa giải Wire Task | Luồng điều khiển | PM | TB | Wire Task chưa giải | Nhấp vào Laptop | Không mở được ứng dụng nào | | |

### 4.3.5 Billiards (BilliardsApp và lõi mô phỏng)

Mục tiêu: luật bida theo thứ tự bi đúng (bi đầu hàng, bi cuối, bi cấm, lỗ đã gọi), số cú theo độ khó, tiền cược và tiền thưởng đúng công thức, thuế chỉ tính trên lợi nhuận, quy tắc đóng ứng dụng giữa ván. Khác với các ứng dụng khác, lõi Billiards là C# thuần (BilliardsSim, BilliardsReferee, BilliardsRound, BilliardsPayout) nên phần luật và tiền thưởng kiểm thử được ở Edit Mode; phần cược, tiền và thẻ đi qua BilliardsApp (MonoBehaviour) nên chạy Play Mode. Các ca dùng bảng có sẵn trong Assets/Resources/BilliardsPresets và GameConfig.asset; ca luật dùng ShotReport dựng tay (không cần bắn bi thật).

Ghi chú: dự án đã có bộ tự kiểm tra riêng của Billiards (Tools → OverTimer → Billiards → Run Self-Tests, 105 kiểm tra, không dùng NUnit). Các ca dưới đây đưa những luật chính vào Test Runner để chạy chung với các phân hệ khác.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-BIL-1 | Pot đúng bi đầu hàng | Phân vùng tương đương | EM | Cao | Thứ tự bi \[1, 2\] | Cú đánh chạm bi 1 trước, bi 1 rơi lỗ TR | Không phạm lỗi; bi 1 được tính; bi đầu hàng mới là 2; ván tiếp tục | | |
| TC-BIL-2 | Chạm sai bi trước | Bảng quyết định | EM | Cao | Thứ tự \[1, 2\]; luật chạm đúng bi đầu (billiardsStrictFirstContact = true) | Cú đánh chạm bi 2 trước, bi 1 rơi lỗ | Phạm lỗi; bi 1 được đặt lại bàn; bi đầu hàng vẫn là 1; ván tiếp tục | | |
| TC-BIL-3 | Không chạm bi / bi cái rơi lỗ | Bảng quyết định | EM | Cao | Thứ tự \[1, 2\] | (a) Không chạm bi nào; (b) chạm bi 1, cả bi 1 và bi cái rơi lỗ | (a) Phạm lỗi, ván tiếp tục; (b) Phạm lỗi; bi cái và bi 1 được đặt lại bàn | | |
| TC-BIL-4 | Luật bi cuối | Bảng quyết định | EM | Cao | Thứ tự \[1, 8\]; bi 8 gọi lỗ BR; billiardsFinalWrongPocketFails = true | (a) Bi 8 rơi khi bi 1 còn trên bàn; (b) chỉ còn bi 8, phạm lỗi và bi 8 rơi; (c) chỉ còn bi 8, bi 8 rơi lỗ TL; (d) chỉ còn bi 8, bi 8 rơi lỗ BR | (a) Thua: “8 dropped too early”; (b) Thua: “8 dropped on a foul”; (c) Thua: “8 went in the wrong pocket”; (d) Thắng | | |
| TC-BIL-5 | Bi cấm | Phân vùng tương đương | EM | TB | Thứ tự \[1, 2\]; bi 5 là bi cấm | Bi 5 rơi lỗ | Thua ngay: “5 is forbidden” | | |
| TC-BIL-6 | Bi thường vào sai lỗ đã gọi | Bảng quyết định | EM | TB | Thứ tự \[1, 8\]; bi 1 gọi lỗ TR | Chạm bi 1 trước, bi 1 rơi lỗ BL | Không thua; bi 1 được đặt lại bàn; bi đầu hàng vẫn là 1 | | |
| TC-BIL-7 | Số cú theo độ khó | Phân vùng tương đương | EM | TB | billiardsParOffsets = 4 / 2 / 0 | (a) Bảng par 2 không ghi số cú riêng; (b) bảng ghi đè số cú Easy = 7 | (a) Easy 6, Medium 4, Hard 2 cú; (b) Easy 7 cú, Medium vẫn 4 | | |
| TC-BIL-8 | Hết cú | Giá trị biên | EM | Cao | Bảng straight_pot_01, độ khó Hard (2 cú) | Bắn 2 cú đều không vào bi | Sau cú 1: ván tiếp tục, còn 1 cú; sau cú 2: thua, “Out of shots” | | |
| TC-BIL-9 | Tiền thưởng khi thắng | Bảng quyết định | EM | Cao | Bảng straight_pot_01 (par 2); GameConfig hiện tại | Tính thưởng ở cược \$10 và \$40, ba độ khó | Cược \$10: \$24 / \$40 / \$48; cược \$40: \$96 / \$158 / \$192 (Easy / Medium / Hard; công thức trong BilliardsPayout) | | |
| TC-BIL-10 | Tái lập mô phỏng | Phân vùng tương đương | EM | Cao | Bảng straight_pot_01 | Bắn cùng một cú (góc 27°, lực 0,45) trên hai bản mô phỏng độc lập | Cùng bi chạm đầu tiên, cùng danh sách bi rơi theo thứ tự, cùng vị trí cuối của mọi bi (Invariant I10) | | |
| TC-BIL-11 | Mọi bàn phát hành đều giải được | Phân vùng tương đương | EM | Cao | 10 bảng trong Resources/BilliardsPresets; vật lý lấy từ GameConfig.asset | Chạy lại lời giải lưu sẵn của từng bảng | Mọi bảng kết thúc ở trạng thái Thắng trong số cú par | | |
| TC-BIL-12 | Biên tiền cược tối thiểu | Giá trị biên | PM | Cao | Mở Billiards; cược tối thiểu \$10 | (a) Money = 9, nhấn Play; (b) Money = 10, nhấn Play | (a) Từ chối, Money = 9, bàn vẫn chờ; (b) Money = 0, ván bắt đầu | | |
| TC-BIL-13 | Thẻ High Risk, High Reward | Bảng quyết định | PM | TB | Thẻ đang hiệu lực (hệ số ×1,5 cho Billiards); Money = 50 | Cược \$10, Easy, bảng straight_pot_01, rồi thắng | Bị trừ \$15 (Money = 35); thưởng vẫn tính từ cược gốc: nhận \$24 | | |
| TC-BIL-14 | Thuế chỉ tính trên lợi nhuận | Bảng quyết định | PM | Cao | Money = 10; bảng straight_pot_01, Easy, cược \$10 | Thắng ván | Money = 24; khoản tính thuế của ITRS chỉ tăng 14 (không tính \$10 tiền cược hoàn lại); Money Earned của RunStats cũng chỉ tăng 14 | | |
| TC-BIL-15 | Đóng ứng dụng giữa ván | Bảng quyết định | PM | Cao | Ván đang ở (a) Racked; (b) Aiming; (c) Rolling; (d) Won/Lost | Đóng ứng dụng Billiards rồi mở lại | (a), (b) Bàn, tiền cược và số cú còn nguyên; (c) Mất tiền cược, hiện “You left mid-shot. That bet is lost.”; (d) Bàn được dọn | | |
| TC-BIL-16 | Thẻ Chalk Up | Bảng quyết định | PM | TB | Đã đặt 1 thẻ Chalk Up (+1 cú) | Chơi straight_pot_01 Easy, cược \$10 | Ván có 7 cú (thay vì 6); thưởng thắng vẫn \$24; thẻ lật úp sau khi dùng. Đặt 2 thẻ rồi Pawn 1 thẻ: còn 1 cú thêm đang chờ | | |
| TC-BIL-17 | Thẻ Cue Slip | Bảng quyết định | PM | TB | Seed cố định; đã đặt thẻ Cue Slip (lệch lực ±0,15) | Ngắm lực 0,80 rồi bắn; lặp lại với cùng seed | Lực thực tế nằm trong \[0,65; 0,95\]; hai lần chạy cùng seed cho cùng lực; thẻ kết thúc sau cú đánh | | |
| TC-BIL-18 | Thẻ Not Valid 8-Ball | Luồng điều khiển | PM | TB | Đang có ván ở trạng thái Aiming | Đặt thẻ, đóng ứng dụng, thử mở lại trong thời gian thẻ hiệu lực \[Xác nhận SRS: thời lượng theo asset thẻ\]; mở lại sau khi hết | Trong thời gian hiệu lực: không mở được Billiards; sau đó: mở được, ván cũ vẫn còn nguyên | | |
| TC-BIL-19 | Chơi lại xóa hiệu ứng đang chờ | Luồng điều khiển | PM | TB | Đã đặt 2 thẻ Chalk Up chưa dùng | Nhấn Restart | Không còn cú thêm nào đang chờ; bàn không còn thẻ | | |

### 4.3.6 Vé số (cách tính thưởng mới) và Egg Timer (bổ sung 28/10)

Luật mới (A23): thưởng = làm tròn của Σ trên mọi biểu tượng lặp lại của baseReward × (số ô trùng − 1) × hệ số, **chỉ tính các ô đã lộ**, và **chỉ trả khi nhấn Cash Out**. Số liệu hiện tại: Triple Match giá \$25, baseReward 55, ba biểu tượng trọng số 50/33/17, hệ số 0,25/0,5/1,2; Lucky Nine giá \$55, baseReward 26, năm biểu tượng trọng số 30/25/20/15/10, hệ số 0,15/0,25/0,5/0,8/1,2 (TripleMatchSymbols.asset / LuckyNineSymbols.asset). Mỗi loại vé có 2 vé trong pool.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-TKT-11 | Thưởng Triple Match theo công thức mới | Bảng quyết định | PM | Cao | Vé Triple Match đã chốt biểu tượng (seed hoặc gán sẵn); lộ cả 3 ô | Ba ô lần lượt: (a) ba biểu tượng khác nhau; (b) 2 ô hệ số 0,5 + 1 ô khác; (c) 3 ô hệ số 1,2; (d) 3 ô hệ số 0,25; rồi Cash Out | (a) \$0; (b) \$28 (55 × 1 × 0,5 = 27,5 → 28); (c) \$132; (d) \$28 | | |
| TC-TKT-12 | Thưởng Lucky Nine cộng mọi biểu tượng lặp | Bảng quyết định | PM | Cao | Vé Lucky Nine lộ đủ 9 ô: 3 ô hệ số 0,15; 2 ô 0,25; 2 ô 0,5; 1 ô 0,8; 1 ô 1,2 | Cash Out | \$27 (26 × (2 × 0,15 + 1 × 0,25 + 1 × 0,5) = 27,3 → 27) | | |
| TC-TKT-13 | Chỉ trả khi Cash Out, chỉ tính ô đã lộ | Bảng quyết định | PM | Cao | Money = 100; mua 1 vé Triple Match (Money = 75) có 3 ô hệ số 1,2 | (a) Cào lộ cả 3 ô, chưa Cash Out; (b) Cash Out khi mới lộ 2 ô; (c) Cash Out khi chưa cào ô nào | (a) Money vẫn 75 (cào hết không tự trả); (b) Money = 75 + 66 (55 × 1 × 1,2); (c) Money = 75 (\$0) | | |
| TC-TKT-14 | Pool 2 vé mỗi loại | Giá trị biên | PM | TB | Money = 100; chưa có vé nào trên bàn | Mua Triple Match 3 lần, không Cash Out | Lần 1, 2: phát vé, Money = 50; lần 3: không trừ tiền, hiện “Too many tickets on the desk. Cash one out first.” | | |
| TC-TKT-15 | Giá vé với hai thẻ Ticket Up Sale (nhân theo khóa) | Bảng quyết định | PM | TB | Hệ số thẻ ×1,5 (asset TicketCostModifier) | Đặt thẻ 1 → đọc giá; đặt thẻ 2 → đọc giá; Pawn thẻ 1; Pawn thẻ 2 | \$38 (37,5 → 38) → \$56 (56,25 → 56) → \$38 → \$25; Lucky Nine cùng lúc: \$83 → \$124 → \$83 → \$55 | | |
| TC-TKT-16 | Bảng thưởng cạnh panel khớp asset | Phân vùng tương đương | TC | TB | Mở NinePanel và TriplePanel | So từng dòng TicketPayoutTable với asset biểu tượng | Mỗi biểu tượng một dòng, tỷ lệ = trọng số / tổng trọng số, hệ số đúng asset; Lucky Nine có thêm dòng “9x rarest symbol = Win the game” | | |
| TC-TKT-17 | Giá trị kỳ vọng (RTP) | Thống kê | PM | TB | Seed cố định (chạy thêm 3–5 seed); cheat tắt Jackpot/Curse | Mua và lộ hết 10.000 vé mỗi loại, Cash Out | Trung bình thưởng Triple Match ≈ \$22,25 (RTP 89,0%), Lucky Nine ≈ \$48,62 (RTP 88,4%), sai lệch trong ngưỡng 4.1.4; Lucky Nine không bao giờ trả \$0 (tối thiểu \$16), tối đa \$250; Triple Match \$0 ≈ 17% | | |
| TC-TSK-7 | Thời gian hồi Egg Timer | Giá trị biên | PM | Cao | Clock = 100; eggTimerCooldown = 0,75 s | Giữ 1 s (nhận +3 s); thả; giữ lại sau (a) 0,70 s; (b) 0,75 s; (c) 0,80 s | (a) Không có tiến trình, không cộng giờ; (b), (c) Bắt đầu giữ bình thường, giữ đủ 1 s được +3 s | | |
| TC-TSK-8 | Egg Timer không cầm đồ được | Phân vùng tương đương | PM | TB | Money = 0 | Kéo Pawn lên Egg Timer | Money = 0; Egg Timer vẫn trên bàn; hiện gợi ý CannotPawn | | |

### 4.3.7 Kết thúc theo cơ chế (bổ sung 28/10)

Bốn kết thúc mới đã cài đặt (A22), thay thế các ca \[Dự kiến\] TC-MIN-4, TC-BJK-10 (phần Charlie) và TC-TKT-9. CaveInLoss (TC-MIN-8) và BlackjackRuinLoss (TC-BJK-10 phần b) **đã bị bỏ khỏi thiết kế**. Các ca dùng menu gian lận (tab Endings, Games) để ép kết quả: *next Lucky Nine = Jackpot / Cursed*, *Mother Lode streak = N*, *next hand = Charlie*. Giá trị: luckyNineJackpotChance 0,006, luckyNineCurseChance 0,01, luckyNineCurseOmenWindows 6, curseBeatSeconds 1,2 s, miningStreakTarget 3, miningFullVeinStreakChance 0,2, blackjackCharlieCards 7, blackjackCharlieChance 0,04.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-ALT-1 | Jackpot khi cào hết 9 ô | Luồng điều khiển | PM | Cao | Đã có Lucky Nine stack (mua ở Shop); cheat next Lucky Nine = Jackpot | Mua vé; cào lộ đủ 9 ô, không nhấn Cash Out | 9 ô cùng biểu tượng hiếm nhất; ngay khi ô thứ 9 lộ: GameOver(TicketJackpotWin), mưa xu (EndingFlourish), RunRecord ghi “TicketJackpotWin” | | |
| TC-ALT-2 | Cash Out sớm làm mất Jackpot | Bảng quyết định | PM | Cao | Vé Jackpot | Cào 8 ô rồi Cash Out | Không kết thúc ván; Money + \$218 (26 × 7 × 1,2 = 218,4 → 218) | | |
| TC-ALT-3 | Vé bị nguyền: cảnh báo và kết thúc | Giá trị biên | PM | Cao | Cheat next Lucky Nine = Cursed | Cào lộ lần lượt 5, 6, rồi 9 ô | 5 ô: chưa có gợi ý; 6 ô: hiện “Something is wrong with this ticket. Cash Out now.”; 9 ô: vé rung, nhãn đỏ, nút Cash Out bị khóa, sau 1,2 s GameOver(TicketCurseLoss) | | |
| TC-ALT-4 | Thoát vé bị nguyền bằng Cash Out | Bảng quyết định | PM | Cao | Vé Cursed, đã lộ 7 ô | Cash Out | Không kết thúc ván; Money + \$23 (26 × 6 × 0,15 = 23,4 → 23) | | |
| TC-ALT-5 | Tỷ lệ Jackpot/Curse | Thống kê | PM | TB | Seed cố định (thêm 3–5 seed); cheat tắt | Mua 100.000 vé Lucky Nine (chỉ đọc kết quả roll) | Jackpot 0,6% ± 0,07%; Curse 1,0% ± 0,09%; không vé nào vừa Jackpot vừa Curse; Triple Match không bao giờ ra Jackpot/Curse | | |
| TC-ALT-6 | Mother Lode: chuỗi 3 vein dài tối đa | Luồng điều khiển | PM | Cao | Mining đang mở; cheat Mother Lode streak = 2 và ép vein kế dài tối đa (10 khối) | Phá hết vein 10 khối | Vein thắng vẫn được trả tiền (Money + \$94, DESIGN §5.14 D), rồi GameOver(MotherLodeWin), mưa xu | | |
| TC-ALT-7 | Chuỗi Mother Lode bị đặt lại | Bảng quyết định | PM | Cao | Chuỗi = 2 | (a) Vein kế có 6–9 khối, phá hết; (b) vein 10 khối nhưng đang có thẻ Butter Finger; (c) đóng ứng dụng Mining rồi mở lại | Cả 3 trường hợp: chuỗi = 0, không kết thúc ván; ván chỉ thắng sau 3 vein tối đa liên tiếp mới | | |
| TC-ALT-8 | Gợi ý trước Mother Lode | Phân vùng tương đương | PM | TB | Chuỗi = 1 | Hoàn thành thêm 1 vein tối đa (chuỗi = 2) | Hiện “The ore is singing. One more full vein.”; không có nhãn tiến trình | | |
| TC-ALT-9 | Xác suất vein tối đa khi đang có chuỗi | Thống kê | PM | TB | Seed cố định; chuỗi > 0 | Sinh 10.000 vein | Vein 10 khối 20% ± 1,2%; các vein còn lại dài 6–9; khi chuỗi = 0 độ dài vẫn đều trong \[6, 10\] | | |
| TC-ALT-10 | Blackjack Charlie | Luồng điều khiển | PM | Cao | Có license Blackjack; cheat next hand = Charlie; Clock ≥ 35 s | Deal, Hit liên tục | Lá rút toàn 2 và 3, không quá 21; từ lá thứ 4 mỗi lần Hit hiện “99% of gamblers quit right before the big win.” (bỏ qua thời gian chờ gợi ý); đến lá thứ 7: GameOver(BlackjackCharlieWin) ngay | | |
| TC-ALT-11 | Double hủy Charlie | Bảng quyết định | EM | TB | BlackjackGame với tay đã gian lận (rigged) | Double ở hai lá đầu | Hàng đợi lá gian lận bị xóa; ván kết thúc bình thường sau 1 lá, không có Charlie | | |
| TC-ALT-12 | Tỷ lệ tay Charlie | Thống kê | PM | Thấp | Seed cố định | 10.000 lần Deal | Tay được gian lận 4% ± 0,6%; các tay còn lại phân phối bài như cũ | | |
| TC-ALT-13 | Điểm RunRating cho kết thúc cơ chế | Bảng quyết định | PM | TB | Trọng số 0,5/0,3/0,2; ratingMechanicWin 0,95; ratingMechanicLoss 0; mốc tiền \$300 | (a) TicketJackpotWin, kiếm \$300; (b) TicketCurseLoss, Clock cao nhất 300 s, kiếm \$0 | (a) 87,5 (100 × (0,5 × 0,95 + 0,3 × 1 + 0,2 × 0,5)); (b) 15 | | |
| TC-ALT-14 | Lịch sử ván với kết thúc mới | Phân vùng tương đương | TC | TB | Đã chơi 4 ván kết thúc lần lượt bằng 4 EndReason mới (cheat tab Endings) | Mở bảng lịch sử ván | 4 dòng; 3 ván thắng (Jackpot, Mother Lode, Charlie) hiện “WIN”, ván Curse hiện “LOSE”; tên kết thúc hiển thị đúng; tải lại trang vẫn còn | | |

### 4.3.8 Kiểm thử luồng điều khiển chức năng Blackjack (hộp trắng, bổ sung 29/10)

Mục tiêu: áp dụng kiểm thử hộp trắng dựa trên luồng điều khiển cho một chức năng tiêu biểu là Blackjack (một ván chơi). Với mỗi hàm: đánh số câu lệnh, dựng đồ thị luồng điều khiển nhị phân (điều kiện ghép `&&`, `||` tách thành nút a, b; `switch` và `?:` chuyển thành chuỗi nút quyết định nhị phân), tính độ phức tạp C(Vg) = P + 1 (kiểm tra lại bằng E − N + 2), xác định C đường thi hành tuyến tính độc lập và xây dựng một ca kiểm thử cho mỗi đường để đạt phủ cấp 3. Vòng lặp được kiểm theo nguyên tắc lặp 0, 1, 2, k, n − 1, n, n + 1 lần.

Chọn hàm theo ba tiêu chí: C(Vg) ≥ 3; chứa luật chơi hoặc tính tiền; kiểm thử độc lập được. Các hàm xử lý nút bấm của BlackjackApp (OnDeal, OnHit…) cần scene và đồng hồ đang chạy nên đã được kiểm ở mức PM (TC-BJK-7, 8). Mã nguồn đánh số, đồ thị và bảng đường thi hành chi tiết: thư mục DOCS/KiemThuLuongDieuKhien_Blackjack (Hình CFG_*.svg, sơ đồ tổng SoDoTong_VanBlackjack.svg).

| Hàm | Vai trò trong ván | P | C(Vg) | Số ca |
|---|---|---|---|---|
| BlackjackGame.Value | Tính điểm tay bài | 4 | 5 | 5 + 5 vòng lặp |
| BlackjackGame.Deal | Chia bài | 3 | 4 | 4 |
| BlackjackGame.Hit | Rút thêm | 3 | 4 | 4 |
| BlackjackGame.DoubleDown | Gấp đôi | 3 | 4 | 4 |
| BlackjackGame.Stand | Nhà cái chơi, phân thắng thua | 5 | 6 | 6 |
| BlackjackApp.SettleHand | Trả thưởng, Hot Table | 6 | 7 | 7 |
| BlackjackGame.RigPlayerCards | Xếp bài cho kết thúc Charlie | 6 | 7 | 7 + 6 vòng lặp |

Cột “Mô tả” ghi đường thi hành theo số nút trên đồ thị của hàm đó. Bộ bài xếp sẵn rút theo thứ tự: người chơi, nhà cái, người chơi, nhà cái, rồi các lá rút sau. Giá trị thưởng lấy từ GameConfig: thắng \$100, hòa \$25, Natural \$130, bước Hot Table 0,1, trần 0,5.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-BJK-VAL-1 | Value – đường 1; 2; 7a; 9 | Luồng điều khiển | EM | Cao | – | Tay bài rỗng | 0 | | |
| TC-BJK-VAL-2 | Value – đường 1; 2; 3; 5; 6; 2; 7a; 9 | Luồng điều khiển | EM | Cao | – | Tay [10] | 10 | | |
| TC-BJK-VAL-3 | Value – đường 1; 2; 3; 4; 6; 2; 7a; 9 | Luồng điều khiển | EM | Cao | – | Tay [A] | 11 | | |
| TC-BJK-VAL-4 | Value – đường 1; (2; 3; 5; 6)×3; 2; 7a; 7b; 9 | Luồng điều khiển | EM | Cao | – | Tay [K, Q, 5] | 25 (quá 21, không có Át để hạ) | | |
| TC-BJK-VAL-5 | Value – đường 1; (2; 3; 4; 6)×2; 2; 7a; 7b; 8; 7a; 9 | Luồng điều khiển | EM | Cao | – | Tay [A, A] | 12 (22 → 12) | | |
| TC-BJK-VAL-LP0 | Value – vòng while lặp 0 lần | Luồng điều khiển (vòng lặp) | EM | TB | n = 4 Át | Tay [A, 6] | 17 | | |
| TC-BJK-VAL-LP1 | Value – vòng while lặp 1 lần | Luồng điều khiển (vòng lặp) | EM | TB | n = 4 Át | Tay [A, 6, 9] | 16 | | |
| TC-BJK-VAL-LP2 | Value – vòng while lặp 2 lần | Luồng điều khiển (vòng lặp) | EM | TB | n = 4 Át | Tay [A, A, K] | 12 | | |
| TC-BJK-VAL-LP3 | Value – vòng while lặp n − 1 = 3 lần | Luồng điều khiển (vòng lặp) | EM | TB | n = 4 Át | Tay [A, A, A, K] | 13 | | |
| TC-BJK-VAL-LP4 | Value – vòng while lặp n = 4 lần | Luồng điều khiển (vòng lặp) | EM | TB | n = 4 Át | Tay [A, A, A, A, K] | 14 | | |
| TC-BJK-DEAL-1 | Deal – đường 1; 2; 4; 5; 10 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Deal(0); người chơi 10 + 7, nhà cái 9 + 8 | PlayerTurn; Result = None; mỗi bên 2 lá | | |
| TC-BJK-DEAL-2 | Deal – đường 1; 2; 3; 4; 5; 10 | Luồng điều khiển | EM | Cao | Seed cố định | Deal(7) (ván Charlie) | Hai lá của người chơi đều là 2 hoặc 3; còn 5 lá chờ; PlayerTurn | | |
| TC-BJK-DEAL-3 | Deal – đường 1; 2; 4; 5; 6; 8; 9; 10 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi A + K, nhà cái 5 + 9 | Resolved; PlayerNatural | | |
| TC-BJK-DEAL-4 | Deal – đường 1; 2; 4; 5; 6; 7; 9; 10 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi A + K, nhà cái A + Q | Resolved; Push | | |
| TC-BJK-HIT-1 | Hit – đường 1a; 2; 6 | Luồng điều khiển | EM | Cao | Ván chưa chia (Idle) | Hit | Không rút; tay rỗng; Idle | | |
| TC-BJK-HIT-2 | Hit – đường 1a; 1b; 2; 6 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi 5 + 6, Hit được 10 (= 21); Hit lần nữa | Vẫn 3 lá; PlayerTurn | | |
| TC-BJK-HIT-3 | Hit – đường 1a; 1b; 3; 4; 6 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi 10 + 2, Hit được 5 | 17; PlayerTurn; Result = None | | |
| TC-BJK-HIT-4 | Hit – đường 1a; 1b; 3; 4; 5; 6 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi 10 + 2, Hit được 10 | 22; PlayerBust; Resolved | | |
| TC-BJK-DBL-1 | DoubleDown – đường 1a; 2; 7 | Luồng điều khiển | EM | TB | Ván chưa chia (Idle) | DoubleDown | DoubledDown = false; tay rỗng | | |
| TC-BJK-DBL-2 | DoubleDown – đường 1a; 1b; 2; 7 | Luồng điều khiển | EM | TB | Bộ bài xếp sẵn | Người chơi 2 + 3, Hit được 4 (3 lá); DoubleDown | Từ chối; vẫn 3 lá; PlayerTurn | | |
| TC-BJK-DBL-3 | DoubleDown – đường 1a; 1b; 3; 4; 6; 7 | Luồng điều khiển | EM | TB | Bộ bài xếp sẵn; nhà cái 10 + 7 | Người chơi 5 + 6, Double được 10 | 21; tự Stand; PlayerWin; DoubledDown = true | | |
| TC-BJK-DBL-4 | DoubleDown – đường 1a; 1b; 3; 4; 5; 7 | Luồng điều khiển | EM | TB | Bộ bài xếp sẵn; nhà cái 10 + 7 | Người chơi 10 + 6, Double được 10 | 26; PlayerBust; nhà cái không rút thêm | | |
| TC-BJK-STD-1 | Stand – đường 1; 3; 4; 6; 8; 9; 13; 14 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi 10 + 7, nhà cái 10 + 8; Stand | Nhà cái không rút; DealerWin | | |
| TC-BJK-STD-2 | Stand – đường 1; 2; 14 | Luồng điều khiển | EM | Cao | Ván chưa chia (Idle) | Stand | Không đổi: Idle; Result = None | | |
| TC-BJK-STD-3 | Stand – đường 1; 3; 4; 5; 4; 6; 8; 9; 13; 14 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi 10 + 7, nhà cái 10 + 6, lá kế 2; Stand | Nhà cái 18 (3 lá); DealerWin | | |
| TC-BJK-STD-4 | Stand – đường 1; 3; 4; 5; 4; 6; 7; 13; 14 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi 10 + 7, nhà cái 10 + 6, lá kế 10; Stand | Nhà cái 26; DealerBust | | |
| TC-BJK-STD-5 | Stand – đường 1; 3; 4; 6; 8; 10; 11; 13; 14 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi 10 + 9, nhà cái 10 + 8; Stand | PlayerWin | | |
| TC-BJK-STD-6 | Stand – đường 1; 3; 4; 6; 8; 10; 12; 13; 14 | Luồng điều khiển | EM | Cao | Bộ bài xếp sẵn | Người chơi 10 + 8, nhà cái 10 + 8; Stand | Push | | |
| TC-BJK-SET-1 | SettleHand – đường 1; 3; 4a; 4b; 5; 7; 8; 11 | Luồng điều khiển | EM | Cao | GameConfig.asset | PlayerWin, không Double, chuỗi thắng 0 | \$100; chuỗi 1 | | |
| TC-BJK-SET-2 | SettleHand – đường 1; 2; 4a; 4b; 5; 7; 8; 11 | Luồng điều khiển | EM | Cao | GameConfig.asset | PlayerWin, Double, chuỗi 0 | \$200; chuỗi 1 | | |
| TC-BJK-SET-3 | SettleHand – đường 1; 3; 4a; 5; 6; 8; 11 | Luồng điều khiển | EM | Cao | GameConfig.asset | PlayerNatural, không Double, chuỗi 0 | \$130; chuỗi 1 | | |
| TC-BJK-SET-4 | SettleHand – đường 1; 3; 4a; 4b; 4c; 5; 7; 8; 11 | Luồng điều khiển | EM | Cao | GameConfig.asset | DealerBust, không Double, chuỗi 2 | \$120 (100 × 1,2); chuỗi 3 | | |
| TC-BJK-SET-5 | SettleHand – đường 1; 3; 4a; 4b; 4c; 4d; 9; 11 | Luồng điều khiển | EM | Cao | GameConfig.asset | Push, không Double, chuỗi 2 | \$25; chuỗi giữ nguyên 2 | | |
| TC-BJK-SET-6 | SettleHand – đường 1; 3; 4a; 4b; 4c; 4d; 10; 11 | Luồng điều khiển | EM | Cao | GameConfig.asset | DealerWin, không Double, chuỗi 3 | \$0; chuỗi về 0 | | |
| TC-BJK-SET-7 | SettleHand – đường 1; 2; 4a; 4b; 4c; 4d; 9; 11 | Luồng điều khiển | EM | Cao | GameConfig.asset | Push, Double, chuỗi 0 | \$50; chuỗi 0 | | |
| TC-BJK-RIG-1 | RigPlayerCards – đường 1; 2; 6; 7; 9; 10; 15 | Luồng điều khiển | EM | TB | Gọi trực tiếp hàm (private) với bộ bài đặt sẵn | Bộ bài rỗng, count = 1 | Không xếp lá nào; bộ bài rỗng | | |
| TC-BJK-RIG-2 | RigPlayerCards – đường 1; 2; 3a; 3b; 5; 2; 6; 7; 9; 10; 15 | Luồng điều khiển | EM | TB | Như trên | Bộ bài [K], count = 1 | Không xếp lá nào; bộ bài [K] | | |
| TC-BJK-RIG-3 | RigPlayerCards – đường 1; 2; 3a; 4; 5; 2; 6; 7; 9; 10; 11; 12; 14; 10; 15 | Luồng điều khiển | EM | TB | Như trên | Bộ bài [2], count = 1 | Xếp [2]; bộ bài rỗng | | |
| TC-BJK-RIG-4 | RigPlayerCards – đường 1; 2; 3a; 3b; 4; 5; 2; 6; 7; 9; 10; 11; 12; 14; 10; 15 | Luồng điều khiển | EM | TB | Như trên | Bộ bài [3], count = 1 | Xếp [3]; bộ bài rỗng | | |
| TC-BJK-RIG-5 | RigPlayerCards – đường …; 6; 7; 8; 7; 9; (10; 11; 12; 14)×2; 10; 15 | Luồng điều khiển | EM | TB | Như trên | Bộ bài [2, 3], count = 2 | Xếp cả 2 lá; bộ bài rỗng | | |
| TC-BJK-RIG-6 | RigPlayerCards – đường …; 9; 10; 11; 13; 14; 10; 15 | Luồng điều khiển | EM | TB | Như trên | Bộ bài [2], count = 0 | Không xếp; lá 2 trả lại bộ bài | | |
| TC-BJK-RIG-7 | RigPlayerCards – đường dài (cả 3 vòng lặp; nút 11 đi cả T và F) | Luồng điều khiển | EM | TB | Như trên | Bộ 52 lá, count = 7 (như khi chơi) | Xếp 7 lá 2/3; bộ bài còn 45 lá, trong đó 1 lá thấp được trả lại | | |
| TC-BJK-RIG-LP0 | RigPlayerCards – vòng lặp 10 lặp 0 lần | Luồng điều khiển (vòng lặp) | EM | TB | n = 8 lá 2 và 3; count = 7 | Bộ bài [K] | Xếp 0; bộ bài 1 lá | | |
| TC-BJK-RIG-LP1 | RigPlayerCards – vòng lặp 10 lặp 1 lần | Luồng điều khiển (vòng lặp) | EM | TB | Như trên | [K] + 1 lá thấp | Xếp 1; bộ bài 1 lá | | |
| TC-BJK-RIG-LP2 | RigPlayerCards – vòng lặp 10 lặp 2 lần | Luồng điều khiển (vòng lặp) | EM | TB | Như trên | [K] + 2 lá thấp | Xếp 2; bộ bài 1 lá | | |
| TC-BJK-RIG-LPk | RigPlayerCards – vòng lặp 10 lặp k = 4 lần | Luồng điều khiển (vòng lặp) | EM | TB | Như trên | [K] + 4 lá thấp | Xếp 4; bộ bài 1 lá | | |
| TC-BJK-RIG-LP7 | RigPlayerCards – vòng lặp 10 lặp n − 1 = 7 lần | Luồng điều khiển (vòng lặp) | EM | TB | Như trên | [K] + 7 lá thấp | Xếp 7; bộ bài 1 lá | | |
| TC-BJK-RIG-LP8 | RigPlayerCards – vòng lặp 10 lặp n = 8 lần | Luồng điều khiển (vòng lặp) | EM | TB | Như trên | [K] + 8 lá thấp | Xếp 7; bộ bài 2 lá (lá thấp thứ 8 trả lại) | | |

Ghi chú:

- **Đường không khả thi:** Deal 2(T) → 5(T) (ván Charlie chỉ chia lá 2, 3 nên không thể Natural); Stand 4(F) → 6(T) (nhà cái không rút thì 2 lá không thể quá 21); SettleHand 4a(T) → 5(F) (kết quả Natural thì nút 5 luôn đúng). RigPlayerCards chỉ được Deal gọi với bộ bài đủ 52 lá, nên muốn đi được từng đường phải gọi trực tiếp hàm với bộ bài đặt sẵn.
- **Ca lặp k và n + 1:** Value có n = 4 nên không có k nguyên nào thỏa 2 < k < n − 1; ca n + 1 của cả hai vòng lặp không sinh được (bộ bài chỉ có 4 Át, 8 lá 2 và 3).
- **Tiền thưởng thật** do BlackjackApp.SettleHand tính (có Hot Table); BlackjackGame.Payout mà TC-BJK-6 đang dùng không được game gọi (xem Phụ lục A).

## 4.4 Kiểm thử hệ thống thẻ bài

### 4.4.1 Card Box và Hand

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-CRD-1 | Mua gói khi đủ điều kiện (biên) | Giá trị biên | PM | Cao | Money = 35; Hand = 4 | Mua gói | Money = 0; ra 3 thẻ (Hand tạm thời có thể vượt 5 – hành vi này cần đối chiếu SRS \[Xác nhận SRS\]) | | |
| TC-CRD-2 | Hand đầy | Giá trị biên | PM | Cao | Money = 50; Hand = 5 | Mua gói | Từ chối; Money = 50 | | |
| TC-CRD-3 | Thiếu tiền | Giá trị biên | PM | Cao | Money = 34; Hand = 0 | Mua gói | Từ chối; Money = 34 | | |
| TC-CRD-4 | Phân bố độ hiếm | Thống kê | PM | TB | Seed cố định (chạy thêm 3–5 seed) | Sinh 10.000 thẻ | C 60% ± 1,5%; R 25% ± 1,3%; SR 12% ± 1,0%; UR 3% ± 0,5% | | |
| TC-CRD-5 | Debuff bắt buộc Đặt | Bảng quyết định | PM | Cao | Hand = 5 | Lật một thẻ debuff | Chỉ hiện lựa chọn Place; Place thành công dù vượt handCap | | |
| TC-CRD-6 | Bán thẻ | Phân vùng tương đương | PM | TB | Money = 0 | Bán (a) thẻ còn úp; (b) thẻ đã lật | (a) Money = 10; (b) Money = pawnValue của thẻ | | |
| TC-CRD-7 | Add to Collection | Bảng quyết định | PM | TB | – | Thẻ (a) chưa có trong Collection; (b) đã có; (c) debuff | (a) Hiện nút và thêm được; (b) Không hiện nút; (c) Không hiện nút | | |
| TC-CRD-8 | Pool CardItem cố định | Luồng điều khiển | PM | TB | Pool khởi tạo xong | Mở nhiều gói liên tiếp (bán thẻ giữa các lần) | Luôn đúng 7 thực thể CardItem; không tạo thêm | | |
| TC-CRD-9 | Nhấp đúp vào Card Box | Phân vùng tương đương | PM | TB | Money = 40; Hand = 0 | Nhấp mua gói 2 lần liên tiếp | Chỉ mở 1 gói; Money = 5 | | |
| TC-CRD-10 | **[Treo – chờ Han xác nhận]** Clock khi xem chi tiết thẻ | Bảng quyết định | PM | Cao | Đang trong ván; Clock = 100; có thẻ trên bàn | Mở bảng chi tiết thẻ (CardPanel), chờ 5 s, đóng | Theo Invariant I2, I5 (mở panel không dừng Clock): Clock ≈ 95 khi đóng bảng. Ghi chú: mã hiện tại gọi GameManager.PauseGame() khi bảng bật (CardPanel.cs) nên dự đoán ca này Lỗi | | |

### 4.4.2 Vòng đời hiệu ứng thẻ và Collection

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-EFX-1 | Hiệu ứng kết thúc tự nhiên | Luồng điều khiển | PM | Cao | Thẻ có hiệu ứng đang chạy | Chờ hết thời hạn | Thẻ lật úp, nằm lại bàn, vẫn tính vào Hand | | |
| TC-EFX-2 | OnRemoved chỉ gọi một lần | Luồng điều khiển | PM | Cao | Thẻ có hiệu ứng | (a) Hết hiệu ứng rồi Pawn; (b) Pawn sớm | OnRemoved được gọi đúng 1 lần trong cả hai trường hợp (cờ \_hasEnded) | | |
| TC-EFX-3 | Ghi Collection | Bảng quyết định | PM | TB | Thẻ chưa có trong Collection | (a) Hết hiệu ứng tự nhiên; (b) Pawn sớm | (a) Được ghi; (b) Không được ghi | | |
| TC-EFX-4 | Hiệu ứng tốc độ không cộng dồn | Bảng quyết định | PM | TB | Phone đang reo (TickMultiplier = 1.5) | Đặt thẻ đổi TickMultiplier; sau đó hiệu ứng kết thúc | Giá trị ghi sau cùng được dùng (không nhân dồn); hết hiệu ứng TickMultiplier = 1 | | |
| TC-EFX-5 | Chi phí qua CostModifierService | Bảng quyết định | PM | TB | Đặt thẻ Bet More Money / Ticket Up Sale | Mua đối tượng bị ảnh hưởng (ví dụ vé Triple Match \$25) | Giá ×1,5 (vé \$25 → \$37,5 hoặc làm tròn theo SRS \[Xác nhận SRS\]); đối tượng khác không đổi giá | | |

### 4.4.3 Thẻ thắng/thua (Set Completion, Final Countdown, Creepy Jester)

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-WIN-1 | Set completion đủ bộ | Bảng quyết định | PM | Cao | Đã đặt Mei + 3 nhóm mảnh ghép | Đặt nhóm thứ 4 | GameOver(SetCompletionWin) | | |
| TC-WIN-2 | Set completion thiếu một nhóm | Bảng quyết định | PM | Cao | – | Đặt Mei + 3 nhóm | Không thắng | | |
| TC-WIN-3 | Final Countdown: Armed | Bảng quyết định | PM | Cao | Phase = None | Place Final Countdown | Phase = Armed | | |
| TC-WIN-4 | Final Countdown: chặn lần thua đầu | Luồng điều khiển | PM | Cao | Phase = Armed; Clock = 1 | Giả lập trôi 1 s (Clock tự về 0) | Không GameOver; Clock = 180; Phase = Triggered; Meter bị khóa | | |
| TC-WIN-5 | Bản thứ hai khi Armed | Bảng quyết định | PM | Cao | Phase = Armed | Place bản thứ 2 | GameOver(FinalCountdownLoss) | | |
| TC-WIN-6 | Bản thứ hai khi Triggered | Bảng quyết định | PM | Cao | Phase = Triggered | Place bản thứ 2 | GameOver(FinalCountdownWin) | | |
| TC-WIN-9 | **[Treo – chờ Han xác nhận]** Clock trong đoạn phim Final Countdown | Bảng quyết định | PM | TB | Final Countdown = Armed; Clock = 1 | Để Clock tự về 0 (Final Countdown kích hoạt, đặt lại 180 s, phát đoạn phim); đo Clock ngay khi đoạn phim kết thúc | Theo Invariant I2: Clock = 180 − thời lượng đoạn phim (Clock vẫn chạy trong lúc phim). Ghi chú: mã hiện tại gọi ClockService.PauseCountdown() trong CutsceneController.PlayFinalCountdownIntro nên dự đoán Clock = 180 (ca Lỗi) | | |
| TC-WIN-7 | Pawn Final Countdown | Bảng quyết định | PM | TB | (a) Phase = Armed; (b) Phase = Triggered | Pawn thẻ | (a) Cho phép; (b) Từ chối | | |
| TC-WIN-8 | Creepy Jester | Luồng điều khiển | PM | Cao | Đã sao lưu PlayerPrefs (\[SetUp\]) và sẽ khôi phục ở \[TearDown\] – tránh xóa dữ liệu thật của máy kiểm thử | (a) Place và để chạy hết các hình; (b) Pawn giữa chừng | (a) PlayerPrefs.DeleteAll() được gọi; GameOver(CreepyJesterLoss); (b) Mối đe dọa dừng, không xóa dữ liệu | | |

### 4.4.4 Phí bỏ debuff, giá theo khóa, thẻ theo license (bổ sung 28/10)

Lưu ý chung: Card Box giờ là vật phẩm Shop (\$40), **không có trên bàn khi bắt đầu ván**; các ca thẻ bài cần mua Card Box trước (hoặc dùng GameSceneTest.GiveItem / cheat Give Card). debuffDismissFee = \$6.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-CRD-11 | Pawn debuff đang chạy khi đủ phí | Giá trị biên | PM | Cao | Thẻ “Clock Up!” đang hiệu lực; Money = 6 | Kéo Pawn lên thẻ | Money = 0 (trừ \$6, không cộng pawnValue); thẻ về pool; Clock trở lại tốc độ thường; thẻ **không** được ghi vào Collection | | |
| TC-CRD-12 | Pawn debuff đang chạy khi thiếu phí | Giá trị biên | PM | Cao | Thẻ “Clock Up!” đang hiệu lực; Money = 5 | Kéo Pawn lên thẻ (thả chuột) | Money = 5; hiện “Not enough money to dismiss it. Pawning an active debuff costs \$6.”; thẻ được dời sang chỗ khác trong vùng đặt thẻ; hiệu ứng vẫn chạy | | |
| TC-CRD-13 | Pawn debuff đã hết hiệu ứng | Bảng quyết định | PM | TB | Thẻ “Clock Up!” đã hết hạn (úp); Money = 0 | Kéo Pawn lên thẻ | Money = pawnValue của thẻ (\$6 theo asset); không thu phí | | |
| TC-CRD-14 | Pawn Final Countdown theo giai đoạn | Bảng quyết định | PM | Cao | (a) Phase = Armed, Money = 10; (b) Armed, Money = 5; (c) Triggered | Kéo Pawn lên thẻ Final Countdown | (a) Money = 4, Phase = None, Clock không còn được chặn; (b) Từ chối, hiện gợi ý phí \$6, thẻ được dời chỗ; (c) Từ chối (CannotPawn) | | |
| TC-CRD-15 | Hai thẻ tăng giá gói thẻ nhân theo khóa | Bảng quyết định | PM | TB | Thẻ “Rarest Card” (CardCostModifier.asset: ×1,5, 20 s) | Đặt thẻ 1; đặt thẻ 2 sau 5 s; thẻ 1 hết hạn (t = 20 s) | Giá gói: \$52 (52,5 → 52) → \$79 (78,75 → 79) → \$52; thẻ hết hạn trước không đặt lại giá của thẻ kia | | |
| TC-CRD-16 | Thẻ của ứng dụng chưa có license không ra trong gói | Thống kê | PM | Cao | Seed cố định; chưa có license Blackjack/Billiards | Roll 10.000 thẻ; sau đó kích hoạt license Blackjack và roll thêm 10.000 | Lượt 1: không có “No More Gambling”, “Not Valid 8-Ball”, “Bet More Money”, “High Risk, High Reward”; lượt 2: “No More Gambling” và “Bet More Money” xuất hiện, hai thẻ Billiards vẫn không có | | |
| TC-CRD-17 | Hai thẻ “No More Mining” chồng nhau | Luồng điều khiển | PM | TB | Thời lượng thẻ 30 s (DisableMining.asset) | Đặt thẻ A ở t = 0, thẻ B ở t = 5 s | Icon Mining ẩn từ t = 0 đến t = 35 s (không hiện lại khi thẻ A hết hạn ở t = 30 s) | | |
| TC-CRD-18 | Hết thẻ ẩn không mở ứng dụng chưa mua | Bảng quyết định | EM | Cao | IconGate: “Blackjack” chưa có license | CardHide("Blackjack"); CardRelease("Blackjack") | Sau CardRelease: IsVisible = false, IsLicensed = false (chỉ hiện khi có license) | | |
| TC-CRD-19 | Thẻ “Close The Shop” | Luồng điều khiển | PM | TB | Cửa sổ Shop trên Phone đang mở | Đặt thẻ; chờ hết hạn | Cửa sổ Shop đóng, icon Shop ẩn ngay (tên “Shop” khớp icon “shop” không phân biệt hoa thường); hết hạn: icon hiện lại | | |
| TC-CRD-20 | Thẻ Cable Eater | Luồng điều khiển | PM | Cao | Phone đang bật; Laptop đang mở ứng dụng Mining | Đặt thẻ; nhấn nút nguồn Phone; chờ 20 s; nhấn nút nguồn | Phone tắt ngay, nút nguồn không phản ứng; Laptop bị khóa (cuốc không đào được); sau 20 s Laptop mở khóa, **Phone vẫn tắt**; nhấn nguồn: khởi động 5 s rồi bật. Đã chốt theo code (Phụ lục B, B3) | | |
| TC-CRD-22 | Lớp mờ của panel mở gói che Meter | Bảng quyết định | PM | TB | Money = 50; Clock = 100 | Mua gói, khi panel mở gói đang hiện thì nhấp nút Feed của Meter; chờ 5 s | Meter không phản hồi (lớp Dim chặn cả HUD); Money vẫn 50 (trừ giá gói); Clock vẫn đọc được và giảm ≈ 5 s (DESIGN §5.4) | | |
| TC-CRD-21 | Thẻ Spam Robocall | Thống kê | PM | TB | Seed cố định | Đặt thẻ, ghi 200 khoảng chờ giữa các cuộc gọi trong lúc thẻ hiệu lực (lặp nhiều ván) | Mọi khoảng chờ nằm trong \[15, 30\] s (×0,5); khoảng cách tối thiểu 12 s giữa hai cuộc gọi không đổi | | |

## 4.5 Kiểm thử gây nhiễu, Pawn, Shop

### 4.5.1 Phone, ITRS và AnnoyanceManager

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-ANN-1 | Chu kỳ reo của Phone | Thống kê | PM | TB | Seed cố định | Ghi nhận 200 lần reo | Mọi khoảng cách nằm trong \[30, 60\] s; trung bình ≈ 45 s; khi reo TickMultiplier = 1.5; mỗi lần reo tự dừng sau ≤ 10 s | | |
| TC-ANN-2 | Chu kỳ hóa đơn ITRS | Luồng điều khiển | PM | TB | AnnoyanceManager đang bận ở giây thứ 45 | Clock chạy 45 s | ITRS xin ô, thử lại mỗi 1 s cho đến khi rảnh, rồi phát hóa đơn | | |
| TC-ANN-3 | Một phiền nhiễu một lúc | Bảng quyết định | PM | Cao | Không có sự kiện nào đang hoạt động | Phone và ITRS cùng yêu cầu trong một khung hình | Chỉ một sự kiện hoạt động; yêu cầu còn lại thử lại sau | | |
| TC-ANN-4 | Khoảng cách tối thiểu | Giá trị biên | PM | Cao | Một sự kiện vừa kết thúc | Sự kiện tiếp theo yêu cầu sau 11 s / 12 s | Bị từ chối / cho phép | | |
| TC-ANN-5 | Giá trị hóa đơn thuế | Giá trị biên | PM | Cao | Kỳ trước kiếm được (a) \$50; (b) \$100; (c) \$200 | Phát hóa đơn | (a) \$20; (b) \$20 (điểm chuyển 20% = 20); (c) \$40 — theo max(20, 20% × tiền kiếm được) | | |
| TC-ANN-6 | Pay khi thiếu tiền | Giá trị biên | PM | TB | Hóa đơn \$30; Money = 29 / 30 | Mở hóa đơn | Nút Pay bị vô hiệu / được bật | | |
| TC-ANN-7 | Ignore | Phân vùng tương đương | PM | Cao | Hóa đơn \$30; (a) Clock = 100; (b) Clock = 50 | Nhấn Ignore | (a) Clock = 40 (phạt 30 × 2 = 60 s); (b) GameOver(ClockLoss) | | |
| TC-ANN-8 | Thẻ Fvck The Tax / Tax Increase | Bảng quyết định | PM | TB | (a) Không có hóa đơn; (b) Đang có hóa đơn \$30 | Đặt Fvck The Tax / Tax Increase | Fvck The Tax: hóa đơn kế = \$0. Tax Increase: (a) hóa đơn xuất hiện ngay; (b) hóa đơn hiện tại thành \$60 | | |

### 4.5.2 Mèo Lang Thang (Cat)

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-CAT-1 | Chuyển trạng thái cơ bản | Luồng điều khiển | PM | TB | Cat = Wandering; seed cố định | Mèo đến điểm đích | Chuyển SittingIdle; sau 3–8 s quay lại Wandering | | |
| TC-CAT-2 | Cơ hội chặn vật | Thống kê | PM | TB | Seed cố định | 1.000 lần chạm vật | Tỷ lệ chặn 40% ± 4,6% | | |
| TC-CAT-3 | Thời gian chặn | Giá trị biên | PM | TB | Cat = SittingBlocking; seed cố định | Đo thời gian khóa qua nhiều lần | Mọi lần khóa nằm trong \[4, 8\] s rồi tự mở | | |
| TC-CAT-4 | Kéo mèo gỡ chặn | Bảng quyết định | PM | Cao | Cat đang chặn một vật | Kéo mèo khỏi vật rồi thả chuột | Khi kéo: Dragged; khi thả: Wandering; vật được gỡ chặn (không đếm số lần nhấp) | | |
| TC-CAT-5 | Miễn AnnoyanceManager | Luồng điều khiển | PM | TB | Phone đang reo | Mèo chạm vật và chặn | Mèo hoạt động độc lập, không bị giới hạn 12 s | | |

### 4.5.3 Cầm đồ (Pawn)

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-PWN-1 | Cầm đồ vật hợp lệ | Phân vùng tương đương | PM | Cao | Money = 0 | Kéo Pawn lên Laptop | Money = 300; Laptop biến mất; không hoàn tác được | | |
| TC-PWN-2 | Vật không thể cầm đồ | Bảng quyết định | PM | Cao | – | Kéo Pawn lên Meter, Clock, Phone, Mail Tray, Cat | Từ chối cả 5; tiền không đổi | | |
| TC-PWN-3 | Hủy hiệu ứng đang chạy | Luồng điều khiển | PM | TB | Thẻ đang có hiệu ứng | Pawn thẻ | OnRemoved được gọi; CardItem trở về pool | | |

### 4.5.4 Shop (bổ sung 28/10 – đã cài đặt)

Shop là một ứng dụng trên Phone. Lõi là C# thuần (Ownership, ShopCart, Checkout, PawnMath, IconGate, ShopValidation trong Shop/Economy.cs, Shop/ShopCart.cs) nên kiểm thử Edit Mode; mua hàng, giao hàng, cầm đồ qua ShopService (MonoBehaviour) chạy Play Mode. Giá trong Assets/Data/Configs/Items: Power Bank \$25, Card Box \$40, Lucky Nine stack \$30, license Blackjack \$60, license Billiards \$60; Laptop catalogValue \$135. Phí giao: Standard \$0 / 20 s, Express \$8 / 5 s, “Leave it opened & ready” +\$3. Cầm đồ = floor(80% × giá đã trả).

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-SHP-1 | Trạng thái đầu ván | Phân vùng tương đương | PM | Cao | Ván mới | Quan sát bàn, Laptop và Shop | Power Bank, Card Box, Lucky Nine stack không có trên bàn; icon Blackjack và Billiards ẩn; Shop liệt kê 5 mặt hàng với giá 25/40/30/60/60 | | |
| TC-SHP-2 | Báo giá giao Standard | Phân vùng tương đương | EM | Cao | Giỏ: Power Bank + Card Box | Quote(Standard, leaveOpen = false) | subtotal 65, shipping 0, total 65, eta 20 s; pricePaid: 25 và 40 | | |
| TC-SHP-3 | Báo giá Express + để mở sẵn | Bảng quyết định | EM | Cao | Giỏ như TC-SHP-2 | Quote(Express, leaveOpen = true) | total 76 (65 + 8 + 3); eta 5 s | | |
| TC-SHP-4 | Đơn chỉ có license | Bảng quyết định | EM | Cao | Giỏ: license Blackjack | Quote(Express, leaveOpen = true) | total 60; shipping 0; leaveOpenFee 0; eta 0 (hàng số không tính phí giao) | | |
| TC-SHP-5 | Đơn trộn license và hàng vật lý | Bảng quyết định | EM | TB | Giỏ: license Billiards + Power Bank | Quote(Express, false) | total 93 (60 + 25 + 8); eta 5 s | | |
| TC-SHP-6 | Quy tắc thêm vào giỏ | Bảng quyết định | EM | TB | Ownership mới | Thêm Power Bank 2 lần; thêm Egg Timer (không bán); thêm Card Box đang Ordered | Lần 2 Power Bank: false; Egg Timer: false; Card Box: false; giỏ chỉ có 1 Power Bank | | |
| TC-SHP-7 | Mua khi thiếu / đủ tiền | Giá trị biên | PM | Cao | Giỏ total \$65 | (a) Money = 64, BUY; (b) Money = 65, BUY | (a) Trả về false; Money = 64; hiện “Not enough money.”; giỏ giữ nguyên; (b) Money = 0; hai mặt hàng chuyển Ordered; giỏ trống | | |
| TC-SHP-8 | Mua và kích hoạt license | Luồng điều khiển | PM | Cao | Money = 60 | Mua license Blackjack; mở Inbox; nhấn Activate; nhấn Activate lần 2 | Sau khi mua: Inbox có “Your Blackjack license” kèm nút Activate, gợi ý “The license is in your Inbox. Press Activate to unlock the app.” (một lần), icon vẫn ẩn; sau Activate: trạng thái Owned, icon Blackjack hiện trên Laptop; Activate lần 2: không có tác dụng | | |
| TC-SHP-9 | Thời gian giao chỉ chạy khi Clock chạy | Luồng điều khiển | PM | TB | Vừa mua Power Bank giao Standard (20 s) | Sau 5 s nhận cuộc gọi Client (Clock tạm dừng) kéo dài 10 s thực, rồi chờ tiếp | Hàng đến sau khoảng 30 s thực (20 s Clock chạy); không đến sớm trong lúc Clock tạm dừng | | |
| TC-SHP-10 | Shop sau khi hết ván | Phân vùng tương đương | PM | Thấp | Ván đã GameOver; giỏ có hàng, Money đủ | Gọi TryBuy / nhấn BUY | Trả về false; Money không đổi | | |
| TC-SHP-11 | Kiểm tra catalog | Bảng quyết định | EM | TB | Danh sách ItemDefCheck dựng tay | (a) Trùng id; (b) hàng số có pawnable; (c) hàng cầm được nằm layer ≠ 3; (d) binding dùng id lạ; (e) catalog thật trong Scene | (a)–(d) Mỗi trường hợp trả đúng 1 thông báo lỗi tương ứng; (e) FindProblems rỗng, Console không có cảnh báo “\[Shop\]” | | |
| TC-SHP-12 | Giá cầm đồ theo catalog | Bảng quyết định | PM | Cao | Money = 0 trước mỗi lần; Power Bank, Card Box, Lucky Nine stack đã mua đúng giá | Kéo Pawn lên: Power Bank / Card Box / Lucky Nine stack / Laptop / Egg Timer / Bear / Picture / Triple stack / Phone | +\$20 / +\$32 / +\$24 / +\$108; 5 vật còn lại: \$0 và gợi ý CannotPawn, vật vẫn trên bàn | | |
| TC-SHP-13 | Chặn cầm đồ khi đang dùng | Bảng quyết định | PM | TB | (a) Panel mở gói của Card Box đang mở; (b) có một vé Lucky Nine đang trên bàn | Kéo Pawn lên Card Box / Lucky Nine stack | Cả hai bị từ chối; tiền không đổi | | |
| TC-SHP-14 | Vật đã cầm đồ trong Shop | Phân vùng tương đương | TC | TB | Đã cầm đồ Power Bank | Mở tab SHOP | Power Bank hiện “PAWNED”, không thêm vào giỏ được; tab PAWNED có Power Bank | | |
| TC-SHP-15 | Gợi ý lần đầu cầm đồ | Luồng điều khiển | PM | Thấp | PlayerPrefs đã sao lưu, chưa có HintSeen_FirstPawn | Cầm đồ 2 vật | Lần 1 hiện “The Printer pays for what you drop on it. A pawned item is gone.”; lần 2 không hiện | | |

### 4.5.5 Mua lại vật đã cầm đồ (Buyback)

Giá mua lại = làm tròn lên của giá gốc × hệ số theo thời gian kể từ lúc cầm đồ (thời gian game): ×1,05 / 1,08 / 1,12 / 1,15 tại 0 / 30 / 60 / 90 s. Giao hàng như một đơn mua.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-BUY-1 | Bậc giá mua lại Laptop | Giá trị biên | EM | Cao | basis = 135 | BuybackPrice tại t = 0 / 29,9 / 30 / 59,9 / 60 / 90 / 500 s | \$142 / \$142 / \$146 / \$146 / \$152 / \$156 / \$156 | | |
| TC-BUY-2 | Mua lại luôn lỗ | Phân vùng tương đương | EM | Cao | – | Với mọi basis 1…500 và t ∈ {0, 30, 60, 90} | BuybackPrice(basis, t) > floor(0,8 × basis) | | |
| TC-BUY-3 | Thời gian đến bậc giá kế | Giá trị biên | EM | TB | – | NextStepIn(10); NextStepIn(30); NextStepIn(95) | 20; 30; −1 (đã ở bậc cao nhất) | | |
| TC-BUY-4 | Mua lại Power Bank | Luồng điều khiển | PM | Cao | Power Bank đã mua \$25 và cầm đồ (+\$20); Money = 27 | Trong 30 s đầu, tab PAWNED → BUY BACK (Standard); chờ giao; mở bưu kiện ở cửa | Money = 0 (\$27); sau 20 s bưu kiện ở cửa; mở xong Power Bank hiện lại đúng vị trí ban đầu (không nằm trên Printer), tiến trình sạc về 0; trạng thái Owned; không còn trong tab PAWNED | | |
| TC-BUY-5 | Mua lại Laptop | Luồng điều khiển | PM | Cao | Đã giải Wire Task, có license Blackjack, đang mở Mining thì cầm đồ Laptop (+\$108) | Mua lại Laptop, mở bưu kiện, mở Laptop | Laptop hiện lại ở màn Home; vẫn có điện (Wire Task không phải làm lại); icon Blackjack vẫn còn | | |
| TC-BUY-6 | Mua lại khi thiếu tiền | Giá trị biên | PM | TB | Giá mua lại \$27, Money = 26 | BUY BACK | Trả về false; Money = 26; hiện “Not enough money.”; vật vẫn ở tab PAWNED | | |
| TC-BUY-7 | Giá mua lại không tăng khi Clock dừng | Luồng điều khiển | PM | TB | Vừa cầm đồ Laptop (giá \$142) | Nhận cuộc gọi Client kéo dài 40 s thực rồi xem giá | Giá vẫn \$142 (tính theo thời gian game, không theo thời gian thực) | | |

### 4.5.6 Phone mới: nguồn, cuộc gọi, Inbox

AnnoyanceManager và lớp Phone cũ **đã bị xóa**; thay bằng CallScheduler, PhonePower, CallerRoller, CallDialogue, Inbox, BannerQueue (C# thuần, kiểm thử Edit Mode) và PhoneController (Play Mode). Trọng số người gọi 25/25/40/10 (Telemarketer / Client / Unknown / ITRS), Unknown là telemarketer với xác suất 0,6; reo 30–60 s một lần, tối đa 10 s, cách nhau ≥ 12 s; khởi động máy 5 s.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-PHN-1 | Bật/tắt và khởi động | Giá trị biên | EM | Cao | PhonePower(5) ở On | Press; Press; Tick(4,5); Tick(0,5) | Off → Booting → vẫn Booting → On; Changed phát 3 lần | | |
| TC-PHN-2 | Khởi động chỉ đếm thời gian game | Phân vùng tương đương | EM | TB | Đang Booting | Tick(0) nhiều lần; Tick(−1) | Vẫn Booting, BootProgress không đổi | | |
| TC-PHN-3 | Bị tắt cưỡng bức | Bảng quyết định | EM | Cao | PhonePower ở Booting | ForceOff(A); ForceOff(B); Press; ReleaseForceOff(A); Press; ReleaseForceOff(B); Press | Off; Press trả false cả khi còn B; sau khi nhả B máy vẫn Off; Press cuối → Booting | | |
| TC-PHN-4 | Chu kỳ reo – nhỡ – nghỉ | Giá trị biên | EM | Cao | CallScheduler với PhoneConfig mặc định, seed cố định | Tick đến khi reo; không trả lời, Tick 10 s; Tick 11,5 s; Tick 0,5 s | Khoảng chờ đầu trong \[30, 60\]; reo → sau 10 s RingStopped(Missed), chuyển Gap; sau 11,5 s vẫn Gap; sau 12 s chuyển Waiting với khoảng chờ mới | | |
| TC-PHN-5 | Cuộc gọi đến khi máy tắt | Bảng quyết định | EM | Cao | CanRing = false | Tick qua thời điểm gọi | RingStopped(Missed) phát; RingStarted không phát; trạng thái vẫn Waiting với khoảng chờ mới (không xếp hàng chờ máy bật). Đã chốt theo code (Phụ lục B, B2) | | |
| TC-PHN-6 | Thao tác sai trạng thái | Bảng quyết định | EM | TB | Scheduler ở Waiting / Ringing / InCall | Answer, Decline, EndCall ở từng trạng thái | Answer/Decline chỉ thành công khi Ringing; EndCall chỉ có tác dụng khi InCall; còn lại trả false / không đổi trạng thái | | |
| TC-PHN-7 | Robocalls rút ngắn khoảng chờ | Thống kê | EM | TB | IntervalScale = 0,5; seed cố định | Ghi 1.000 khoảng chờ | Mọi giá trị trong \[15, 30\] s; khoảng nghỉ Gap vẫn 12 s | | |
| TC-PHN-8 | Phân bố người gọi | Thống kê | EM | TB | Seed cố định; N = 100.000 | (a) Có hóa đơn (ITRS eligible); (b) không có hóa đơn | (a) Hiển thị 25/25/40/10%; bản chất telemarketer 49%, client 41%, ITRS 10%; (b) không có ITRS; hiển thị 27,8/27,8/44,4%; bản chất telemarketer 54,4%. Đã chốt theo trọng số của code (Phụ lục B, B1) | | |
| TC-PHN-9 | Đếm câu thoại | Giá trị biên | EM | Thấp | – | CallDialogue(3): Advance 3 lần; CallDialogue(0) | 3 lần trả true, true, false; IsFinal sau lần 2; CallDialogue(0) coi như 1 câu, IsFinal ngay | | |
| TC-PHN-10 | Cuộc gọi Client | Luồng điều khiển | PM | Cao | Clock = 100; Money = 0; cheat ép cuộc gọi Client | Mở Phone, ACCEPT, bấm trả lời từng câu, câu cuối “Hang up.” | Từ lúc ACCEPT: Clock dừng, Laptop bị khóa, vật trên bàn không nhấp được (Invariant I5), không đóng được Phone, DECLINE xám; câu cuối: Money = 22; Clock chạy lại; lịch sử cuộc gọi có dòng “+\$22” | | |
| TC-PHN-11 | Client chờ quá lâu | Giá trị biên | PM | TB | Đang trong cuộc gọi Client, câu đầu | Không bấm gì trong 8 s thực | Hiện “Hello? Are you there?” 2 s, nút trả lời xám, rồi cúp máy; Money không đổi; Clock chạy lại | | |
| TC-PHN-12 | Cuộc gọi Telemarketer | Phân vùng tương đương | PM | TB | Clock = 100; ép cuộc gọi Telemarketer | ACCEPT, nghe hết | Clock vẫn chạy; nút trả lời đầu xám trong 4 s game; không có thưởng | | |
| TC-PHN-13 | Cuộc gọi ITRS | Bảng quyết định | PM | Cao | (a) Đang có hóa đơn còn 10 s; (b) không có hóa đơn | Nghe hết cuộc gọi ITRS | (a) Hóa đơn còn 25 s (+15 s); (b) hóa đơn kế đến sau 60 s thay vì 45 s; hạn trả 30 s không đổi | | |
| TC-PHN-14 | Từ chối cuộc gọi | Phân vùng tương đương | PM | Cao | Phone đang reo, Clock ×1,5 | DECLINE | Hết reo, Clock về ×1; không mất tiền, không mất giờ; lịch sử và Inbox có cuộc gọi bị từ chối | | |
| TC-PHN-15 | Người chơi im lặng giữa cuộc gọi | Giá trị biên | PM | Thấp | Cuộc gọi Client; Money = 0 | Bấm đến khi câu cuối hiện ra, rồi không bấm gì 5 s thực | Money = 22 ngay khi câu cuối hiện (PhoneController.Payoff), trước khi cúp; sau 5 s máy tự cúp, không cộng thêm; Clock chạy lại | | |
| TC-PHN-16 | Lịch sử cuộc gọi | Giá trị biên | PM | TB | Ván đang chạy | Tạo 31 cuộc gọi (trả lời, từ chối, nhỡ) bằng cheat | Mọi cuộc gọi có CallRecord, mới nhất ở đầu; chỉ giữ 30 dòng; cuộc nhỡ và từ chối cũng vào Inbox, cuộc đã trả lời thì không | | |
| TC-PHN-17 | Inbox | Phân vùng tương đương | EM | TB | Inbox mới | Add 3 tin; MarkRead tin thứ 2 | All\[0\] là tin mới nhất; Unread = 2; id tăng dần | | |
| TC-PHN-18 | Hàng đợi banner | Giá trị biên | EM | Thấp | BannerQueue(4 s) | Push 4 tin; Tick(4); Clear | Sau Push: 3 hiện, 1 chờ; sau 4 s: 3 tin đầu ẩn, tin 4 hiện; Clear: không còn gì. **Sẽ bỏ**: logic banner sẽ bị xóa (Phụ lục B, B4); khi đó loại ca này | | |
| TC-PHN-19 | Nhãn “Incoming call” khi đang ở ứng dụng Laptop | Luồng điều khiển | TC | TB | Đang mở Mining | Phone reo; nhấp nhãn | Nhãn hiện ở trên cùng; nhấp mở màn chỉ có Phone, hiện ACCEPT / DECLINE | | |
| TC-PHN-20 | GameOver/Restart giữa cuộc gọi | Luồng điều khiển | PM | Cao | Đang trong cuộc gọi Client (Clock dừng, Laptop khóa) | Cheat GameOver; Restart | Ván mới: Clock chạy, Laptop không khóa, Phone bật, không còn cuộc gọi treo | | |

### 4.5.7 Thuế mới: hạn trả, ứng dụng Taxes, thư thuế

Nút Ignore **đã bị bỏ**. Mỗi hóa đơn có TaxDeadline 30 s Clock chạy; quá hạn bị trừ số tiền × 2 giây Clock **một lần** (ClockService.Spend, bỏ qua Final Countdown). Không phát hóa đơn mới khi còn hóa đơn mở. Trả được ở ứng dụng Taxes (Phone) hoặc bản giấy (panel Tax, mở từ thư thuế ở cửa).

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-TAX-1 | Hạn trả hóa đơn | Giá trị biên | EM | Cao | TaxDeadline(30, 30) | Tick(29,5); Tick(0,5) | Sau 29,5 s: Pending, còn 0,5 s; sau 30 s: Penalized, SecondsLeft = 0; Resolved phát đúng 1 lần | | |
| TC-TAX-2 | Hóa đơn đã xử lý không đổi nữa | Bảng quyết định | EM | TB | TaxDeadline đã Penalized | MarkPaid; Extend(15); Double; Tick(1) | Vẫn Penalized, Amount = 30, Resolved không phát lại | | |
| TC-TAX-3 | Gia hạn và nhân đôi | Phân vùng tương đương | EM | TB | TaxDeadline(30, 30), đã Tick(10) | Extend(15); Double | SecondsLeft = 35; Amount = 60 | | |
| TC-TAX-4 | Không trả hóa đơn | Luồng điều khiển | PM | Cao | Clock = 100; hóa đơn \$30 vừa phát; Phone tắt (để không reo) | Không trả, chờ 30 s | Clock ≈ 10 (100 − 30 − 60); chỉ bị trừ một lần; ITRS bắt đầu đếm 45 s cho hóa đơn kế | | |
| TC-TAX-5 | Phạt thuế bỏ qua Final Countdown | Bảng quyết định | PM | TB | Final Countdown = Armed; Clock = 50; hóa đơn \$30 còn 1 s | Để hết hạn | Clock về 0 → GameOver(ClockLoss) (không chặn) | | |
| TC-TAX-6 | Hạn trả dừng khi cuộc gọi tạm dừng Clock | Luồng điều khiển | PM | Cao | Hóa đơn còn 20 s | Nghe cuộc gọi Client 15 s thực | Sau cuộc gọi hóa đơn vẫn còn 20 s | | |
| TC-TAX-7 | Không phát hóa đơn chồng | Luồng điều khiển | PM | Cao | Hóa đơn đang mở | Chờ 45 s Clock chạy (trả hóa đơn ở giây thứ 25 của hạn) | Không có hóa đơn thứ hai khi hóa đơn đầu còn mở; hóa đơn kế đến 45 s sau khi hóa đơn đầu được xử lý | | |
| TC-TAX-8 | Trả ở một nơi, cả hai nơi đều xong | Bảng quyết định | PM | Cao | Hóa đơn \$30; thư thuế đã nằm ở cửa | (a) Trả ở ứng dụng Taxes; (b) trả ở panel Tax giấy | Cả hai: Money − 30; thư thuế biến mất (puff); badge Taxes tắt; panel Tax nếu đang mở tự đóng | | |
| TC-TAX-9 | Nút Pay theo số dư | Giá trị biên | PM | TB | Hóa đơn \$30 | Money = 29 / 30 | Nút Pay bị vô hiệu / được bật (cả ở ứng dụng Taxes và panel giấy) | | |
| TC-TAX-10 | Thông báo hóa đơn | Phân vùng tương đương | PM | TB | Phone bật | ITRS phát hóa đơn \$20 | Inbox có “Tax bill \$20” với hành động “Open Taxes”; icon Taxes có badge đến khi trả | | |
| TC-TAX-11 | Fvck The Tax và Tax Increase | Bảng quyết định | PM | TB | (a) Không có hóa đơn; (b) hóa đơn \$30 đang mở | Đặt Fvck The Tax; đặt Tax Increase; Pawn Fvck The Tax trước khi hóa đơn kế đến | Fvck The Tax: hóa đơn kế \$0, xử lý như hóa đơn thường; Pawn trước: hóa đơn kế tính bình thường. Tax Increase: (a) hóa đơn xuất hiện ngay; (b) thành \$60 và panel Tax hiện \$60 ngay | | |

### 4.5.8 Cửa, bưu kiện và thư

Góc nhìn Door có tháp bưu kiện (tối đa 4, nhấp 3 lần để mở) và đống thư (tối đa 8; thư thuế 10 s sau khi có hóa đơn; thư rác mỗi 60–120 s, không có trong 60 s đầu). DoorDepot, LetterPile, JunkMailTimer, DeliveryScheduler là C# thuần.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-DOR-1 | Tháp bưu kiện đầy | Giá trị biên | EM | Cao | DoorDepot(4, 3) | Receive 5 bưu kiện; Click bưu kiện đáy 3 lần | Tower = 4, Waiting = 1; hai lần Click đầu trả false, lần 3 true; bưu kiện chờ được đưa vào tháp (Added phát) | | |
| TC-DOR-2 | Nhấp bưu kiện không có trong tháp | Phân vùng tương đương (vùng không hợp lệ) | EM | Thấp | DoorDepot có 1 bưu kiện | Click(null); Click(bưu kiện đang chờ) | Cả hai trả false, không đổi | | |
| TC-DOR-3 | Đống thư đầy | Bảng quyết định | EM | TB | LetterPile(8) | (a) 7 thư rác + 1 thư thuế, thêm 1 thư rác; (b) 8 thư thuế, thêm 1 thư | (a) Thư rác cũ nhất bị đẩy ra (Removed phát), thư thuế còn; (b) Add trả null | | |
| TC-DOR-4 | Lịch thư rác | Thống kê | EM | TB | JunkMailTimer(seed, 60, 120, grace 60) | Mô phỏng 1.000 ván | Thư đầu tiên trong \[60, 120\] s, không bao giờ trước 60 s; các thư sau cách nhau \[60, 120\] s; Tick(0) không đến hạn | | |
| TC-DOR-5 | Hàng đến cửa | Luồng điều khiển | PM | Cao | Mua Power Bank, giao Standard | Chờ 20 s; chuyển góc nhìn Door; nhấp bưu kiện 3 lần; chuyển về Desk | Chuông cửa reo; bưu kiện ở tháp; gợi ý “A package is at the door. Click the door.” rồi “Click the box to open it.” (mỗi gợi ý một lần); mở xong, khi về Desk, Power Bank hiện trong làn khói kèm gợi ý “Your item is on the desk.” (một lần); Inbox có “Power Bank delivered” | | |
| TC-DOR-6 | “Leave it opened & ready” | Bảng quyết định | PM | TB | Mua Power Bank Standard + để mở sẵn (\$28) | Chờ 20 s | Không có bưu kiện ở cửa; không hiện gợi ý PackageArrived; Power Bank lên thẳng bàn | | |
| TC-DOR-7 | Thư thuế | Bảng quyết định | PM | Cao | Hóa đơn vừa phát | (a) Chờ 10 s; (b) trả hóa đơn sau 5 s | (a) Thư thuế dấu đỏ nằm trên thảm; nhấp mở panel Tax; (b) thư không bao giờ xuất hiện | | |
| TC-DOR-8 | Thư rác | Phân vùng tương đương | PM | Thấp | Ván đã chạy > 60 s | Chờ thư rác dấu xanh; nhấp vào | Chuông cửa reo; nhấp hiện tờ rơi đùa (gợi ý JunkFlyer), thư bị bỏ đi | | |
| TC-DOR-9 | Restart xóa thư, bưu kiện, đơn hàng | Luồng điều khiển | PM | TB | Có bưu kiện ở cửa, thư trên thảm, một đơn đang giao | Restart | Cửa trống; không còn đơn đang giao; Shop như đầu ván | | |
| TC-DOR-10 | **\[Dự kiến\]** Hộp rỗng sau khi mở bưu kiện | Phân vùng tương đương | PM | Thấp | Vừa mở một bưu kiện (giao Standard) | Kéo hộp rỗng lên Printer | Theo DESIGN §5.7, §5.11: hộp rỗng là Junk nhỏ, Money + \$5. Mã chưa có hộp rỗng, đang được bổ sung (Phụ lục B, B5) | | |

## 4.6 Kiểm thử vòng đời ván chơi và lưu dữ liệu

### 4.6.1 Kết thúc ván, lưu ván và chơi lại

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-GAM-1a | Kết thúc ván chỉ qua GameOver (đã cài đặt) | Luồng điều khiển | PM | Cao | Bản build có menu gian lận | Kích hoạt lần lượt từng EndReason đã cài đặt (F1/F2) | Mỗi kết thúc đều đi qua GameManager.GameOver; input bị vô hiệu; Clock dừng; Time.timeScale = 0 | | |
| TC-GAM-1b | Kết thúc ván chỉ qua GameOver \[Dự kiến\] | Luồng điều khiển | PM | TB | 6 EndReason còn lại đã cài đặt | Như TC-GAM-1a | Như TC-GAM-1a | | |
| TC-GAM-2 | Màn hình kết thúc | Phân vùng tương đương | TC | TB | – | Kết thúc một ván | Sau ~1 s hiện màn hình (mờ dần 0,6 s) với Best Time, Money Earned, Cards Opened, New Cards | | |
| TC-GAM-3 | Ghi RunRecord | Phân vùng tương đương | PM | Cao | PlayerPrefs trống (đã sao lưu) | Kết thúc ván thứ 1 | Ghi vào RunHistory_0; NextSlot = 1 | | |
| TC-GAM-4 | Bộ đệm vòng 20 ô | Giá trị biên | PM | TB | PlayerPrefs trống (đã sao lưu) | Ghi 20 / 21 RunRecord | Sau 20 ván: NextSlot = 0; ván 21 ghi đè ô 0, NextSlot = 1 | | |
| TC-GAM-5 | Lưu và nạp Collection | Phân vùng tương đương | PM | Cao | PlayerPrefs đã sao lưu | Thêm thẻ vào Collection → khởi động lại | Collection còn nguyên (khóa CardCollection_UnlockedIds) | | |
| TC-GAM-6 | Chơi lại | Phân vùng tương đương | PM | Cao | Đang ở màn hình kết thúc | Nhấn Restart | Scene nạp lại; Clock = 120, Money = 40, Hand = 0; dữ liệu lưu giữ nguyên | | |
| TC-GAM-7 | Hai điều kiện kết thúc trong cùng khung hình | Bảng quyết định | PM | Cao | Clock = 1; một điều kiện thắng khác (ví dụ Set Completion) sẵn sàng | Clock về 0 và đặt mảnh cuối trong cùng khung hình | GameOver được gọi đúng 1 lần; EndReason theo thứ tự ưu tiên trong SRS \[Xác nhận SRS\] | | |
| TC-GAM-8 | Gọi GameOver lần thứ hai | Luồng điều khiển | PM | TB | Ván đã kết thúc | Gọi GameOver với EndReason khác | Bị bỏ qua; màn hình kết thúc và RunRecord không đổi | | |

### 4.6.2 Bổ sung vòng đời ván (28/10)

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-GAM-9 | Đủ 10 EndReason | Phân vùng tương đương | PM | Cao | PlayerPrefs đã sao lưu; menu gian lận tab Endings | Ép lần lượt 10 kết thúc (Restart giữa các lần) | Mỗi lần đi qua GameManager.GameOver đúng 1 lần; RunRecord lưu đúng tên EndReason; ba thắng cơ chế phát EndingFlourish, các kết thúc có Timeline phát cutscene; không có kết thúc nào treo | | |
| TC-GAM-10 | Thứ tự enum EndReason chỉ thêm cuối | Rà soát mã (kiểm thử tĩnh) | EM | TB | – | So enum EndReason với danh sách đã lưu: ClockWin, SetCompletionWin, FinalCountdownWin, ClockLoss, FinalCountdownLoss, CreepyJesterLoss, rồi 4 giá trị mới | 6 giá trị cũ giữ nguyên vị trí; 4 giá trị mới nằm sau; RunRecord cũ (lưu theo tên) vẫn đọc được | | |
| TC-GAM-11 | Cờ gian lận bị xóa khi chơi lại | Luồng điều khiển | PM | TB | Bản Development | Bật “next Lucky Nine = Jackpot” và “next hand = Charlie”; Restart; mua 1 vé Lucky Nine, chơi 1 tay | Vé và tay bài bình thường (CheatFlags đã đặt lại khi nạp scene) | | |
| TC-GAM-12 | Trạng thái tĩnh được đặt lại khi chơi lại | Luồng điều khiển | PM | Cao | Đang có: Laptop bị khóa (Cable Eater), icon Mining bị ẩn, đơn hàng đang giao, cuộc gọi đang reo | Restart | Laptop không khóa; mọi icon theo license như đầu ván; không còn đơn, không còn cuộc gọi; Console không có MissingReferenceException | | |

## 4.7 Kiểm thử tích hợp, hệ thống và phi chức năng

### 4.7.1 Kịch bản tích hợp (Play Mode)

- Kịch bản chiến thắng: giải Wire Task → mở Laptop → Mining → Meter → Clock đạt 600 s → ClockWin.
- Kịch bản thua do thuế: bỏ qua nhiều hóa đơn ITRS → Clock về 0 → ClockLoss.
- Kịch bản thẻ: mua gói → đặt thẻ → thẻ hết hiệu ứng → Pawn → kiểm tra Hand/Collection.
- Kịch bản gây nhiễu chồng lấp: Phone + ITRS + Cat cùng thời điểm.

### 4.7.2 Kiểm thử các quy tắc bất biến (I1–I10)

| Mã | Quy tắc (SRS 2.5) | Cách kiểm thử |
|---|---|---|
| I1 | Thắng ≥ 600 s, thua ≤ 0 s; kết thúc bổ sung 1–3% số ván | Unit (TC-CLK-4, 5, 11) + mô phỏng nhiều ván (seed) |
| I2 | Clock thời gian thực, không pause; chỉ Phone/thẻ đổi tick | Unit + Play Mode (TC-CLK-2, 3; TC-SYS-1; TC-HNT-7, TC-CRD-10, TC-WIN-9 – mã hiện có 3 chỗ dừng Clock: hướng dẫn đầu game, bảng chi tiết thẻ, đoạn phim Final Countdown) |
| I3, I6 | Chỉ Meter quy đổi tiền → thời gian, \$1 = 1 s | Unit + rà soát mã |
| I4 | Không cộng giờ ngẫu nhiên vào Clock | Rà soát mã/ghi log AddSeconds |
| I5 | Cào vé, nối dây, mở panel không dừng Clock | Play Mode (TC-CRD-10) |
| I7 | Debuff không trừ thẳng Clock; thoát được bằng Pawn | Unit + Play Mode |
| I8 | Một phiền nhiễu một lúc, cách ≥ 12 s (Cat miễn) | Unit/Play Mode (TC-ANN-3, 4; TC-CAT-5) |
| I9 | Camera chỉ cuộn dọc \[minY, maxY\] bằng hai nút giữ, không kéo-pan/zoom/xoay; Physics2D chỉ dùng cho hit-testing | Play Mode + rà soát mã |
| I10 | Giá trị cân bằng nằm trong asset dữ liệu; ngẫu nhiên tái lập được theo seed | Rà soát mã (tìm “số cứng”); TC-RNG-1, TC-BJK-9, TC-BIL-10 |

### 4.7.3 Kiểm thử phi chức năng

- **Hiệu năng:** dùng Unity Profiler kiểm tra không cấp phát liên tục khi cào vé, mở gói thẻ (Object Pooling); khung hình ổn định trên WebGL.
- **Nền tảng:** chạy bản WebGL trên các trình duyệt chính; lưu PlayerPrefs (IndexedDB) giữa các lần tải trang (chi tiết ở 4.7.5).
- **Giao diện và thao tác:** cửa sổ Laptop không chiếm toàn màn hình (Phone, Cat, Mail Tray luôn hiển thị); kiểm tra hiển thị và bố cục ở toàn bộ khoảng cuộn của camera; panel mở thì input thế giới bị chặn nhưng Clock vẫn chạy.
- **Bảo mật/phát hành:** menu gian lận (F1/F2) phải tắt ở bản phát hành (TC-SYS-5).

### 4.7.4 Kiểm thử cân bằng và hồi quy

- Thông lượng: Mining 2.8–3.6 \$/s; Blackjack kỳ vọng 19–21 \$ mỗi ván so với ante; Egg Timer +2 s mỗi giây giữ; so với chuẩn 3 \$/s.
- Mô phỏng nhiều ván với seed cố định để đo tần suất kết thúc bổ sung (1–3%).
- Hồi quy các lỗi/phản hồi từ bản Jam: hoán đổi nhầm ứng dụng trên Laptop, thao tác cào vé (giữ chuột), Blackjack áp đảo, người chơi không hiểu luật.
- Billiards: báo cáo cân bằng BILLIARDS_BALANCE.md (sinh bằng Tools → OverTimer → Billiards → Balance Report) ước tính người chơi trung bình kiếm +4,3 \$/s ở Medium và +4,4 \$/s ở Hard, cao hơn cả Mining đào nhanh tối đa (3,17 \$/s). Cần playtest ít nhất 20 ván thật, đọc nhật ký ván (cheat menu “Billiards: round log”) để xác nhận, rồi quyết định có áp dụng giá trị khuyến nghị billiardsTargetRate = 0,6 / 1,8 / 2,3 hay không.
- Billiards: người chơi được xem bàn trước khi chọn độ khó, trong khi tiền thưởng chỉ phụ thuộc độ khó, nên chọn độ khó theo từng bàn lời thêm khoảng +1,0 \$/s (BILLIARDS_BALANCE.md mục 7). Ghi nhận là rủi ro cân bằng.

*\[Tự điền\] Playtest với người chơi thật và điền câu trả lời cho các câu hỏi cần xác nhận ở cuối SRS.*

### 4.7.5 Ca kiểm thử hệ thống trên nền tảng WebGL

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-SYS-1 | Chuyển sang tab khác | Phân vùng tương đương | TC | Cao | Bản WebGL; Clock = 200 | Chuyển sang tab khác 30 s rồi quay lại | Hành vi Clock khớp quyết định trong SRS: giảm đúng 30 s (nếu chạy nền) hoặc giữ nguyên (nếu dừng khi mất focus) \[Xác nhận SRS\]; không có bước nhảy thời gian bất thường, không lỗi | | |
| TC-SYS-2 | Thu nhỏ / đổi kích thước cửa sổ | Phân vùng tương đương | TC | TB | Bản WebGL đang chơi | Thu nhỏ trình duyệt, đổi kích thước, bật/tắt toàn màn hình | Bố cục không vỡ; Phone, Cat, Mail Tray vẫn hiển thị; thao tác chuột vẫn đúng vị trí | | |
| TC-SYS-3 | Tải lại trang (F5) giữa ván | Phân vùng tương đương | TC | Cao | Đã có thẻ trong Collection; đang giữa ván | Nhấn F5 | Ván mới bắt đầu (Clock = 120, Money = 40); Collection và RunHistory còn nguyên | | |
| TC-SYS-4 | Trình duyệt khác nhau | Phân vùng tương đương | TC | TB | Bản WebGL trên itch.io | Chạy kịch bản chiến thắng 4.7.1 trên Chrome, Edge, Firefox | Chạy được và cho kết quả giống nhau trên cả 3 trình duyệt | | |
| TC-SYS-5 | Menu gian lận ở bản phát hành | Phân vùng tương đương | TC | Cao | Bản build Release | Nhấn F1, F2 | Không có phản ứng; không hiện menu | | |

### 4.7.6 Gợi ý (Hints) và hướng dẫn đầu game (Tutorial)

Mục tiêu: gợi ý hiện đúng nội dung, đúng lúc, không lặp quá dày; gợi ý “một lần” chỉ hiện một lần cho mỗi máy; hướng dẫn đầu game chỉ hiện ở lần chơi đầu. Các giá trị lấy từ GameConfig.asset: hintHoldSeconds = 2,5 s, hintFadeSeconds = 0,5 s, hintRepeatCooldown = 3 s. Các ca có chạm PlayerPrefs phải sao lưu ở \[SetUp\] và khôi phục ở \[TearDown\].

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-HNT-1 | Mọi gợi ý đều có nội dung | Phân vùng tương đương | EM | TB | – | Gọi HintCatalog.Get với từng giá trị của HintId | Không giá trị nào trả về chuỗi rỗng; chuỗi có “{0}” định dạng được với 1 tham số | | |
| TC-HNT-2 | Gợi ý chỉ hiện một lần | Luồng điều khiển | PM | Cao | PlayerPrefs đã sao lưu; khóa HintSeen_TicketOpen chưa có | Gọi ShowOnce(TicketOpen) hai lần; tải lại scene rồi gọi lần ba | Lần 1: hiện “Scratch with the mouse. Cash Out to collect.” và HintSeen_TicketOpen = 1; lần 2 và lần 3: không hiện | | |
| TC-HNT-3 | Thời gian chờ giữa hai lần hiện | Giá trị biên | PM | TB | hintRepeatCooldown = 3 s | Gọi Show(NotEnoughMoney), gọi lại sau 2,9 s / 3,0 s | Sau 2,9 s: bị bỏ qua; sau 3,0 s: hiện lại | | |
| TC-HNT-4 | Không hiện gợi ý sau khi hết ván | Phân vùng tương đương | PM | TB | Ván đã kết thúc (GameOver) | Gọi Show với một HintId bất kỳ | Không hiện gì | | |
| TC-HNT-5 | Gợi ý khi thao tác bị từ chối | Bảng quyết định | PM | TB | (a) Money = 9; (b) Hand = 5, Money = 50 | (a) Nhấp Meter; (b) mua gói thẻ | (a) “The Meter needs at least \$10.”; (b) “Your hand is full (5). Pawn a card to make room.” | | |
| TC-HNT-6 | Hướng dẫn chỉ ở lần chơi đầu | Luồng điều khiển | PM | Cao | PlayerPrefs đã sao lưu và xóa khóa đã xem hướng dẫn | Mở game, nhấp qua đủ 5 bước; tải lại scene | Lần đầu: hiện bước 1/5, mỗi lần nhấp sang bước kế, bước 5 nhấp thì đóng; lần sau: không hiện | | |
| TC-HNT-7 | **[Treo – chờ Han xác nhận]** Clock khi đang xem hướng dẫn | Bảng quyết định | PM | Cao | Hướng dẫn đang mở ở đầu ván; Clock = 120 | Chờ 5 s rồi đóng hướng dẫn | Theo Invariant I2 (Clock không pause): Clock ≈ 115 khi đóng hướng dẫn. Ghi chú: mã hiện tại gọi GameManager.PauseGame() (TutorialPanel.cs) nên dự đoán ca này Lỗi; nếu SRS bổ sung ngoại lệ cho màn hướng dẫn thì sửa kết quả mong đợi thành Clock = 120 | | |

### 4.7.7 Kiểm thử chức năng thủ công (hộp đen)

Các mục trước chủ yếu kiểm thử ở mức mã nguồn: điều kiện tiên quyết được gán thẳng vào biến, kết quả mong đợi là trạng thái nội bộ (sự kiện, biến cờ, PlayerPrefs). Mục này kiểm thử trò chơi **từ góc nhìn người chơi**: chỉ thao tác bằng chuột như người chơi thật, và kết quả mong đợi là thứ **nhìn thấy hoặc nghe thấy được** (HUD, nút, thông báo gợi ý, cutscene, âm thanh).

Quy ước:

- Chạy trên bản build Development (hoặc Play Mode trong Editor) để có menu gian lận. Nhấn F1 mở menu; các nút dùng để đặt điều kiện tiên quyết: *Give Money*, *Give Time*, nút tên thẻ (đặt thẻ lên bàn), *Preview \<EndReason\>*, *GameOver \<EndReason\>*, *FC Intro*. Nhấn F2 dừng cutscene.
- HUD hiển thị Clock dạng mm:ss (120 s → “02:00”) và tiền dạng “\$40”. Các con số lấy từ GameConfig.asset và asset thẻ trong Resources/CardSO tại ngày lập kế hoạch.
- Báo cáo lỗi trò chơi cần thêm so với phần mềm nghiệp vụ: **tỷ lệ tái hiện** (ví dụ 3/10 lần), **video quay màn hình**, phiên bản build, trình duyệt và seed (nếu có).

#### a) HUD, Clock và Meter

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-HUD-1 | HUD đầu ván | Phân vùng tương đương | TC | Cao | Ván mới, đã đóng hướng dẫn | Quan sát HUD | Clock hiện “02:00”; tiền hiện “\$40” | | |
| TC-HUD-2 | Clock đếm ngược trên màn hình | Phân vùng tương đương | TC | Cao | Clock “02:00”; Phone không reo | Chờ 10 s (bấm giờ ngoài) | Clock hiện “01:50” (sai lệch ≤ 1 s); số giây giảm đều từng giây, không nhảy cóc | | |
| TC-HUD-3 | Tiền cập nhật ngay khi nhận thưởng | Phân vùng tương đương | TC | Cao | Tiền “\$40” | Hoàn thành Click Task | Tiền đổi thành “\$45” ngay sau lần nhấp cuối, không cần thao tác khác | | |
| TC-HUD-4 | Meter khi thiếu tiền | Giá trị biên | TC | Cao | Tiền “\$9”; Clock khoảng “01:30” | Nhấp Meter | Tiền vẫn “\$9”; Clock không tăng; hiện gợi ý “The Meter needs at least \$10.” | | |
| TC-HUD-5 | Meter đổi toàn bộ tiền | Phân vùng tương đương | TC | Cao | Tiền “\$57” | Nhấp Meter, đọc Clock trước và sau | Tiền “\$0”; Clock tăng đúng 57 s (ví dụ “01:30” → “02:27”) | | |
| TC-HUD-6 | Meter bị khóa | Phân vùng tương đương | TC | TB | Final Countdown đã kích hoạt (xem TC-E2E-6); tiền “\$50” | Nhấp Meter | Tiền và Clock không đổi; hiện gợi ý “The Meter is locked.” | | |
| TC-HUD-7 | Phone reo làm Clock chạy nhanh | Phân vùng tương đương | TC | TB | Phone vừa bắt đầu reo; không nhấc máy | Bấm giờ 4 s thực | Clock giảm khoảng 6 s (×1,5); khi Phone ngừng reo, Clock trở lại tốc độ thường | | |
| TC-HUD-8 | Biên thắng trên HUD | Giá trị biên | TC | Cao | Dùng Give Time đưa Clock lên “09:50” | Chờ, rồi nạp Meter đủ để vượt “10:00” | Clock chạm “10:00” thì ván kết thúc thắng (cutscene ClockWin), không hiện giá trị vượt quá “10:00” | | |
| TC-HUD-9 | Hiển thị số tiền lớn | Giá trị biên | TC | Thấp | – | Give Money 99999 | HUD hiện “\$100039” đầy đủ, không tràn khung, không bị cắt chữ | | |

#### b) Laptop và chuyển ứng dụng

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-LAP-1 | Laptop chưa có điện | Phân vùng tương đương | TC | Cao | Wire Task chưa giải | Nhấp Laptop trên bàn | Laptop không mở; hiện “The laptop has no power. Check the wiring.” | | |
| TC-LAP-2 | Chọn icon bằng nhấp đơn | Phân vùng tương đương | TC | TB | Laptop đã mở, đang ở màn Home | Nhấp 1 lần vào icon Mining, rồi 1 lần vào icon Blackjack | Icon được nhấp đổi nền xanh nhạt, icon trước trở lại trắng; app chưa mở; lần đầu hiện “Double-click an icon to open it.” | | |
| TC-LAP-3 | Mở app bằng nhấp đúp | Phân vùng tương đương | TC | Cao | Màn Home; Clock đang chạy | Nhấp đúp icon Mining | Hiện màn chờ chuyển app khoảng 1 s rồi mở Mining; Clock vẫn chạy trong lúc chờ | | |
| TC-LAP-4 | Spam nhấp đúp trong lúc chuyển app | Phân vùng tương đương | TC | Cao | Màn Home | Nhấp đúp Mining rồi nhấp đúp ngay Blackjack trong lúc màn chờ đang hiện | Chỉ một app mở (Mining); không có hai app chồng nhau; màn chờ biến mất sau ~1 s *(lỗi hoán đổi nhầm app ở bản Jam)* | | |
| TC-LAP-5 | Đóng Laptop giữa lúc chuyển app | Luồng điều khiển | TC | Cao | Màn Home | Nhấp đúp Mining, trong lúc màn chờ đang hiện nhấn nút đóng Laptop; mở lại Laptop rồi nhấp đúp Mining | Lần mở lại hiện màn Home; Mining mở được bình thường (không bị “kẹt” không mở được app nào) | | |
| TC-LAP-6 | Nút đóng app và nút đóng Laptop | Bảng quyết định | TC | TB | Mining đang mở | (a) Nhấn nút đóng của app Mining; (b) mở lại Mining rồi nhấn nút đóng của Laptop | (a) Về màn Home, Laptop vẫn mở; (b) Laptop đóng hẳn, về bàn làm việc; mở lại thấy màn Home, không còn app nổi lơ lửng | | |
| TC-LAP-7 | Laptop không che bàn | Phân vùng tương đương | TC | Cao | Laptop đang mở một app | Chờ Phone reo / có hóa đơn ở Mail Tray | Phone, Cat, Mail Tray vẫn nhìn thấy và nhấp được khi Laptop đang mở | | |
| TC-LAP-8 | Thẻ ẩn icon app | Luồng điều khiển | TC | TB | Laptop đang ở màn Home | Give Card “No More Mining”; quan sát; chờ hết 30 s (đóng/mở Laptop trong lúc chờ) | Icon Mining biến mất ngay; vẫn ẩn sau khi đóng/mở Laptop; hiện lại sau 30 s; thẻ lật úp | | |

#### c) Cutscene và màn hình kết thúc

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-END-1 | Cutscene thắng theo Clock | Phân vùng tương đương | TC | Cao | Clock “09:58” | Chờ Meter/Give Time đưa Clock lên 600 s | Hiện đồng hồ lớn đếm “09:57” → “10:00”, mỗi bước 1 s, dừng ngắn rồi khoảng 1 s sau màn hình kết thúc mờ dần hiện ra | | |
| TC-END-2 | Cutscene thua theo Clock | Phân vùng tương đương | TC | Cao | Clock “00:05” | Chờ Clock về 0 | Đồng hồ lớn đếm “00:03” → “00:00”; sau đó hiện màn hình kết thúc | | |
| TC-END-3 | Cutscene từng kết thúc khác | Bảng quyết định | TC | Cao | Menu gian lận | Lần lượt nhấn GameOver SetCompletionWin / FinalCountdownWin / FinalCountdownLoss / CreepyJesterLoss (Restart giữa các lần) | Mỗi kết thúc phát đúng cutscene của nó, giữ khung hình cuối, rồi hiện màn hình kết thúc; không có kết thúc nào bỏ qua cutscene hoặc treo | | |
| TC-END-4 | Không thao tác được khi đang cutscene | Phân vùng tương đương | TC | Cao | Cutscene kết thúc đang phát | Nhấp Meter, Phone, Laptop, thẻ; giữ nút cuộn camera | Không có phản ứng; tiền và Clock không đổi; Phone không reo | | |
| TC-END-5 | Cutscene giới thiệu Final Countdown | Luồng điều khiển | TC | Cao | Đã đặt Final Countdown; Clock “00:03” | Chờ Clock về 0 | Phát cutscene Final Countdown; Clock đứng yên trong lúc phát; sau cutscene Clock hiện “03:00” và bắt đầu đếm | | |
| TC-END-6 | Hình và âm thanh Creepy Jester | Phân vùng tương đương | TC | TB | Menu gian lận | Preview CreepyJesterLoss | Hình đổi liên tục khoảng 2 s, dừng ở hình cuối, phát tiếng cười | | |
| TC-END-7 | Số liệu trên màn hình kết thúc | Phân vùng tương đương | TC | Cao | Ván mới; ghi lại thao tác | Kiếm tổng \$60, mở 1 gói thẻ (3 thẻ) trong đó 1 thẻ chưa có trong Collection và chờ thẻ đó hết hiệu ứng, rồi để thua | Hiện “Money Earned: \$60”, “Cards Opened: 3”, “New Cards: 1”; “Best Time” khớp thời gian sống sót | | |
| TC-END-8 | Chơi lại từ màn hình kết thúc | Phân vùng tương đương | TC | Cao | Ván trước đã cầm đồ Laptop và mua vé | Nhấn Restart | HUD “02:00”, “\$40”; Laptop có lại trên bàn; không còn vé/thẻ của ván trước; không còn cutscene hay màn hình kết thúc | | |

#### d) Lịch sử ván và Collection

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-HIS-1 | Lịch sử trống | Phân vùng tương đương | TC | TB | PlayerPrefs trống (máy/trình duyệt mới) | Mở bảng lịch sử ván | Hiện thông báo danh sách trống; không có dòng nào | | |
| TC-HIS-2 | Hiển thị thắng/thua | Bảng quyết định | TC | Cao | Đã chơi 3 ván: 1 ClockWin, 1 ClockLoss, 1 FinalCountdownLoss | Mở bảng lịch sử | 3 dòng; ván thắng có chữ “WIN” màu xanh và “Survived 10:00”; ván thua “LOSE” màu đỏ với “Time Out” / “Countdown Lost”; ván vừa chơi được tô nền nổi bật | | |
| TC-HIS-3 | Xoay vòng cách sắp xếp | Phân vùng tương đương | TC | TB | Có ≥ 3 ván | Nhấn nút Sort 4 lần | Nhãn lần lượt “SORT: LONGEST RUN” → “MOST MONEY” → “FASTEST END” → “BEST SCORE”; thứ tự dòng đổi đúng theo tiêu chí | | |
| TC-HIS-4 | Điểm của ván | Bảng quyết định | TC | TB | (a) Ván ClockWin, kiếm \$300; (b) ván ClockLoss sống 300 s, kiếm \$0 | Mở bảng lịch sử | Cột điểm: (a) 90; (b) 15 (công thức RunRating với trọng số 0,5 / 0,3 / 0,2 và mốc tiền \$300) | | |
| TC-HIS-5 | Định dạng tiền | Giá trị biên | TC | Thấp | Một ván kiếm ≥ \$1.000 | Mở bảng lịch sử | Tiền hiện dạng “\$1,234” (dấu phẩy ngăn nghìn) | | |
| TC-HIS-6 | Thẻ ẩn trong Collection | Bảng quyết định | TC | TB | Có thẻ được đánh dấu ẩn đến khi sưu tầm | Mở sổ Collection trước và sau khi sưu tầm thẻ đó | Trước: thẻ không xuất hiện; sau: thẻ xuất hiện ở trạng thái đã sưu tầm | | |
| TC-HIS-7 | Lật trang Collection | Giá trị biên | TC | Thấp | Sổ Collection có > 2 trang | Mở sổ; nhấn Next đến cuối; nhấn Prev về đầu | Trang đầu: không có nút Prev; trang cuối: không có nút Next; nếu chỉ có ≤ 2 trang thì không có nút nào | | |

#### e) Thẻ chưa có ca kiểm thử ở 4.4

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-FCD-1 | Thẻ đổi tốc độ Clock | Bảng quyết định | TC | TB | Phone không reo | Give Card (a) “Snail Speed”; (b) “Clock Up!”; (c) “Time Scarab”; bấm giờ trong thời gian hiệu lực | (a) Clock chạy chậm (×0,75) trong 15 s; (b) chạy nhanh (×1,5) trong 20 s; (c) Clock đứng yên trong 20 s; hết hạn thì trở lại bình thường và thẻ lật úp | | |
| TC-FCD-2 | Cable Eater (Do Not Disturb) | Luồng điều khiển | TC | TB | Phone đang reo | Give Card “Cable Eater”; theo dõi 30 s | Phone ngừng reo và tắt chuông ngay; không reo lại trong 30 s; Clock về tốc độ thường | | |
| TC-FCD-3 | Spam Robocall | Phân vùng tương đương | TC | TB | – | Give Card “Spam Robocall”; đếm số lần reo trong 30 s | Phone reo dày hơn bình thường (khoảng gấp đôi); sau 30 s trở lại chu kỳ thường | | |
| TC-FCD-4 | Ticket Hunter (Insider Tip) | Luồng điều khiển | TC | TB | Tiền ≥ \$25 | Give Card “Ticket Hunter”; mua 1 vé; mua thêm 1 vé | Vé đầu có sẵn một ô đã lộ; vé thứ hai bình thường; thẻ lật úp sau vé đầu | | |
| TC-FCD-5 | Trick Shot **[Dự kiến]** | Luồng điều khiển | TC | Thấp | Có asset thẻ Trick Shot (hiện chưa có trong Resources/CardSO) | Đặt thẻ, vào Billiards, ngắm | Khi ngắm hiện bi mờ ở vị trí dừng dự đoán; sau cú đánh thì tắt và thẻ lật úp | | |
| TC-FCD-6 | Pawn debuff để thoát hiệu ứng | Bảng quyết định | TC | Cao | Đang có “Spam Robocall” / “Clock Up!” / “No More Mining” hiệu lực | Cầm đồ từng thẻ | Hiệu ứng dừng ngay (Phone về chu kỳ thường / Clock về tốc độ thường / icon Mining hiện lại); tiền tăng đúng giá trị pawn của thẻ (Invariant I7) | | |

#### f) Vật dụng trên bàn, cầm đồ và thao tác chuột

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-DSK-1 | Picture Click Task | Phân vùng tương đương | TC | TB | Khung ảnh sẵn sàng | Nhấp khung ảnh đến khi nhận thưởng | Mỗi lần nhấp khung ảnh đổi hình phản ứng rồi trở lại; sau 3–8 lần nhấp tiền +\$5 | | |
| TC-DSK-2 | Nhấp khi đang hồi | Phân vùng tương đương | TC | TB | Click Task vừa trả thưởng (đang hồi 3 s) | Nhấp tiếp | Không cộng tiền; hiện “Still cooling down.” | | |
| TC-DSK-3 | Power Bank đang sạc lại | Phân vùng tương đương | TC | Thấp | Power Bank vừa trả \$8 | Giữ chuột lên Power Bank | Không cộng tiền; hiện “Recharging...” | | |
| TC-DSK-4 | Giá cầm đồ từng vật | Bảng quyết định | TC | TB | Tiền “\$0” trước mỗi lần | Kéo Pawn lên: rác nhỏ / vừa / lớn, Egg Timer, Card Box, Power Bank, Click Task, khung ảnh, Wire Task, chồng vé Triple, chồng vé Nine | Tiền lần lượt +\$5 / \$15 / \$30 / \$150 / \$180 / \$45 / \$40 / \$40 / \$15 / \$50 / \$100; vật biến khỏi bàn | | |
| TC-DSK-5 | Thả Pawn vào chỗ trống | Phân vùng tương đương | TC | TB | – | Kéo Pawn (nghe âm thanh) rồi thả vào chỗ trống trên bàn | Âm thanh phát trong lúc kéo và tắt khi thả; Pawn trở về vị trí ban đầu; tiền không đổi | | |
| TC-DSK-6 | Bàn hết chỗ để vé | Giá trị biên | TC | TB | Tiền đủ lớn; số vé tối đa N = kích thước pool vé trong Scene \[Xác nhận\] | Mua vé liên tục N + 1 lần, không Cash Out | N vé đầu lên bàn; lần N + 1 không trừ tiền, hiện “Too many tickets on the desk. Cash one out first.” | | |
| TC-DSK-7 | Mua vé khi thiếu tiền | Giá trị biên | TC | TB | Tiền “\$24” | Nhấp chồng vé Triple Match | Không có vé; tiền vẫn “\$24”; hiện “Not enough money.” | | |
| TC-DSK-8 | Cuộn camera | Giá trị biên | TC | Cao | Camera ở vị trí đầu ván | Giữ nút cuộn lên đến hết; giữ nút cuộn xuống đến hết; thử lăn chuột và kéo nền | Camera cuộn dọc mượt khi giữ, dừng khi thả; ở đỉnh nút lên biến mất, ở đáy nút xuống biến mất; lăn chuột/kéo nền không làm camera di chuyển (Invariant I9) | | |
| TC-DSK-9 | Panel chặn thao tác phía sau | Phân vùng tương đương | TC | Cao | Đang mở một panel (vé, gói thẻ hoặc hóa đơn) | Nhấp vào Phone, Cat, Meter nằm phía sau; nhìn nút cuộn camera | Vật phía sau không phản ứng; nút cuộn camera bị ẩn; Clock vẫn chạy; đóng panel thì mọi thứ hoạt động lại | | |
| TC-DSK-10 | Thả chuột ngoài cửa sổ trò chơi | Phân vùng tương đương | TC | TB | Bản WebGL; Cat đang chặn một vật | Kéo Cat ra ngoài khung trò chơi rồi thả chuột ở ngoài; đưa chuột trở lại | Cat không bị kẹt dính theo chuột; vẫn kéo/thả lại được bình thường | | |
| TC-DSK-11 | Gợi ý khi Cat chặn | Phân vùng tương đương | TC | TB | Cat đang chặn một vật | Nhấp vào vật bị chặn | Vật không phản ứng; hiện “The cat is in the way. Pick it up and move it.” | | |
| TC-DSK-12 | Hiệu ứng khi nhấp | Phân vùng tương đương | TC | Thấp | – | Nhấp nhanh 10 lần ở nhiều chỗ khác nhau | Hiệu ứng hạt xuất hiện tại đúng vị trí nhấp; không lỗi khi nhấp dồn dập | | |
| TC-DSK-13 | Gợi ý lần đầu của từng tính năng | Bảng quyết định | TC | TB | PlayerPrefs trống | Lần đầu: mở Mining, mở Blackjack, mở Wire Task, Phone reo, có hóa đơn, Cat chặn; sau đó làm lại từng việc | Lần đầu hiện đúng gợi ý của tính năng đó (theo HintCatalog); lần sau không hiện lại | | |

#### g) Âm thanh

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-AUD-1 | Chuông Phone | Luồng điều khiển | TC | TB | – | Để Phone reo rồi (a) nhấc máy; (b) để tự hết 10 s; (c) đặt “Cable Eater” khi đang reo | Chuông phát lặp khi reo và tắt ngay trong cả 3 trường hợp; không còn tiếng chuông sót lại | | |
| TC-AUD-2 | Âm thanh khi chuyển tab (WebGL) | Phân vùng tương đương | TC | Thấp | Bản WebGL; Phone đang reo | Chuyển sang tab khác rồi quay lại | Âm thanh dừng/tiếp tục nhất quán với hành vi Clock ở TC-SYS-1; không phát chồng hai lần | | |

#### h) Kịch bản đầu – cuối (end-to-end)

Chơi trọn ván như người chơi, không dùng menu gian lận trừ khi ghi rõ. Các kịch bản này chi tiết hóa mục 4.7.1.

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-E2E-1 | Thắng bằng Clock | Kịch bản (use case) | TC | Cao | Ván mới | Giải Wire Task → mở Laptop → Mining → nạp Meter mỗi khi đủ tiền → lặp đến khi Clock đạt “10:00” | Cutscene ClockWin; màn hình kết thúc với số liệu đúng; bảng lịch sử có thêm 1 dòng WIN | | |
| TC-E2E-2 | Thua do thuế | Kịch bản (use case) | TC | Cao | Ván mới | Kiếm tiền bình thường nhưng nhấn Ignore mọi hóa đơn ITRS | Mỗi lần Ignore, Clock bị trừ gấp đôi số tiền hóa đơn; cuối cùng Clock về 0 → cutscene ClockLoss | | |
| TC-E2E-3 | Vòng đời thẻ | Kịch bản (use case) | TC | Cao | Tiền ≥ \$35; Hand trống | Mua gói → lật cả 3 thẻ → Place 1 thẻ có hiệu ứng, Pawn 1 thẻ, bán 1 thẻ còn úp → chờ thẻ đặt hết hiệu ứng → mở Collection | Hand còn 1 thẻ (đã lật úp); tiền đúng theo giá pawn/bán; thẻ hết hiệu ứng tự nhiên có trong Collection | | |
| TC-E2E-4 | Phiền nhiễu chồng lấp | Kịch bản (use case) | TC | TB | Ván đã chạy > 45 s | Chờ đến khi Phone reo, có hóa đơn và Cat chặn vật cùng lúc (hoặc gần nhau); xử lý lần lượt | Phone và hóa đơn không xuất hiện cùng lúc (cách nhau ≥ 12 s); Cat có thể chặn bất cứ lúc nào; xử lý xong không còn UI nào bị kẹt | | |
| TC-E2E-5 | Cầm đồ Laptop sớm | Kịch bản (use case) | TC | TB | Đã giải Wire Task | Pawn Laptop (+\$300) → nạp Meter → chơi tiếp bằng vé, thẻ, nhiệm vụ nhỏ | Clock tăng 300 s; không còn cách mở app; ván vẫn chơi tiếp được đến khi kết thúc, không lỗi | | |
| TC-E2E-6 | Thắng bằng Final Countdown | Kịch bản (use case) | TC | Cao | Có 2 thẻ Final Countdown (Give Card nếu cần) | Place thẻ thứ nhất → để Clock về 0 → xem cutscene → Clock “03:00”, Meter khóa → Place thẻ thứ hai | Cutscene giới thiệu Final Countdown phát một lần; sau đó GameOver FinalCountdownWin với cutscene tương ứng | | |
| TC-E2E-7 | Chơi nhiều ván liên tiếp | Kịch bản (use case) | TC | TB | Bản WebGL | Chơi và Restart 5 ván liên tục (mỗi ván có thể thua nhanh) | Không chậm dần, không tăng bộ nhớ bất thường (quan sát FPS/Task Manager); bảng lịch sử có đủ 5 dòng, số thứ tự ván tăng dần | | |

#### i) Phiên kiểm thử thăm dò (exploratory)

Ngoài các ca có kịch bản, nhóm chạy các phiên kiểm thử thăm dò có giới hạn thời gian. Mỗi phiên ghi lại thao tác đã thử, lỗi tìm được và video. Các phiên này không tính vào bảng 4.8.

| Chủ đề (charter) | Thời lượng | Gợi ý |
|---|---|---|
| Phá Laptop | 60 phút | Nhấp đúp liên tục, đóng/mở giữa lúc chuyển app, cầm đồ Laptop khi app đang mở |
| Thao tác dồn dập | 60 phút | Spam nhấp/kéo ở mọi vật, nhấp trong khung hình GameOver, giữ chuột rồi Alt-Tab |
| Kinh tế | 90 phút | Tìm cách để tiền âm, Clock tăng không qua Meter, nhận thưởng hai lần cho một việc |
| Thẻ chồng hiệu ứng | 60 phút | Đặt nhiều thẻ cùng loại/khác loại cùng lúc, Pawn giữa chừng, Restart khi thẻ đang hiệu lực |
| WebGL | 60 phút | Đổi kích thước, toàn màn hình, chuyển tab, F5 ở các thời điểm khác nhau, trình duyệt khác |
| Shop và mua lại (bổ sung 28/10) | 60 phút | Mua – cầm đồ – mua lại liên tục để tìm cách lời; cầm đồ Laptop khi đang có cuộc gọi; mua khi đang chuyển góc nhìn |

### 4.7.8 Camera ba góc nhìn và DeviceLayer (bổ sung 28/10)

Camera cuộn dọc cũ **đã bị xóa** (Invariant I9 mới): chỉ có ba góc nhìn cố định Room (ortho 14), Desk (6), Door (7), chuyển bằng nhấp, tween 0,5 s. Laptop và Phone nằm trong DeviceLayer (làm mờ nền, Phone gắn bên phải Laptop).

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-CAM-1 | Chuyển ba góc nhìn | Phân vùng tương đương | PM | Cao | Ván mới (bắt đầu ở Room) | Nhấp bàn; nhấp vùng Back; nhấp cửa; nhấp Back | CurrentView: Room → Desk → Room → Door → Room; kích thước camera 14 / 6 / 7; mỗi lần chuyển mất ≈ 0,5 s; ViewChanged phát sau khi tween xong; từ Desk không sang thẳng Door được, luôn qua Room (DESIGN §4.1) | | |
| TC-CAM-2 | Gọi chuyển khi đang chuyển hoặc trùng góc nhìn | Bảng quyết định | PM | TB | Đang ở Room | GoTo(Desk) rồi GoTo(Door) ngay khi đang tween; ở Desk gọi GoTo(Desk) | GoTo(Door) và GoTo(Desk) thứ hai trả false; camera dừng ở Desk | | |
| TC-CAM-3 | Chặn input khi đang chuyển, Clock vẫn chạy | Bảng quyết định | PM | Cao | Clock = 100 | Trong lúc tween, nhấp vào Egg Timer / Phone | Không có phản ứng; InputGate bị chặn đến khi tween xong; Clock giảm bình thường | | |
| TC-CAM-4 | Vật chỉ phản hồi ở góc nhìn cho phép | Bảng quyết định | PM | TB | Đang ở góc nhìn Room | Nhấp lần lượt: bàn, cửa, Picture, Egg Timer, Phone, Printer, Bear, Triple Match stack | Theo DESIGN §4.1: chỉ bàn, cửa, Picture, Egg Timer phản hồi; các vật còn lại không phản hồi (chỉ dùng được ở Desk). Highlight Hover theo cùng quy tắc. Ghi chú: danh sách ViewGate thực tế trong Scene chưa được xác minh trong Editor (PROJECT.md §20) | | |
| TC-CAM-5 | Không cuộn, không zoom, không kéo | Phân vùng tương đương | TC | Cao | Ở mỗi góc nhìn | Lăn chuột; kéo nền; giữ chuột ở mép màn hình | Camera không di chuyển; không có nút cuộn (Invariant I9) | | |
| TC-CAM-6 | Bố cục DeviceLayer | Luồng điều khiển | TC | TB | Laptop có điện | Mở Laptop; nhấp Phone gắn bên phải; đóng Phone; mở Mining; đóng Mining | Mở Laptop: nền mờ, Phone gắn bên phải; nhấp Phone: Laptop trượt sang trái, Phone ra giữa (0,25 s); mở Mining: Laptop ra giữa, Phone trượt khỏi màn hình; đóng app: cả hai trở về. Đã chốt theo code (Phụ lục B, B6) | | |
| TC-CAM-7 | Khóa Laptop theo khóa | Bảng quyết định | PM | Cao | Cable Eater đang hiệu lực (còn 15 s) | Nhận cuộc gọi Client rồi cúp sau 5 s | Sau cuộc gọi Laptop vẫn khóa (khóa của Cable Eater còn); mở khóa khi Cable Eater hết hạn | | |
| TC-CAM-8 | Khóa Laptop chặn thao tác trong ứng dụng | Phân vùng tương đương | PM | TB | (a) Đang đào Mining; (b) cú bida đang lăn | Khóa Laptop (cuộc gọi Client) | (a) Cuốc không gây sát thương khi khóa; (b) bi dừng mô phỏng đến khi mở khóa, rồi chạy tiếp, kết quả cú đánh như không bị ngắt | | |
| TC-CAM-9 | Làm mờ nền trên WebGL | Phân vùng tương đương | TC | Thấp | Bản WebGL | Mở Laptop | Nền phía sau bị làm mờ; nếu deviceBlurEnabled = false thì chỉ phủ màu tối; Meter phía sau không nhấp được | | |

### 4.7.9 Âm thanh, Reward Popup và Highlight Hover (bổ sung 28/10)

| Mã | Mô tả ca kiểm thử | Kỹ thuật | Mức | ƯT | Điều kiện tiên quyết | Đầu vào / Thao tác | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| TC-FBK-1 | Phát âm thanh khi không có AudioService | Phân vùng tương đương | PM | TB | Scene trống, không có AudioService | AudioService.Play(SfxId.Click) | Không lỗi, không ngoại lệ | | |
| TC-FBK-2 | Danh mục âm thanh đầy đủ | Phân vùng tương đương | EM | TB | AudioCatalog.asset | Duyệt mọi giá trị SfxId | Mỗi id có tối đa một mục; id chưa có clip thì im lặng, không lỗi; volume, pitch hợp lệ | | |
| TC-FBK-3 | Âm thanh nhận tiền theo bậc | Giá trị biên | PM | TB | AudioCatalog.asset: moneySmallMax = 10, moneyMediumMax = 40 | Nhận \$10, \$11, \$40, \$41; nhận tiền hoàn cược Billiards | \$10: MoneyGainSmall; \$11 và \$40: MoneyGainMedium; \$41: MoneyGainLarge; tiền hoàn cược không phát âm thanh | | |
| TC-FBK-4 | Chặn phát lặp quá nhanh | Giá trị biên | PM | Thấp | Một id có minInterval = T > 0 trong AudioCatalog.asset (các giá trị hiện có 0,03 / 0,05 / 0,1 s) | Gọi Play hai lần cách nhau T − 0,01 s và T | Lần thứ hai: bị bỏ / được phát; id có minInterval = 0 thì luôn phát | | |
| TC-FBK-5 | Nhạc thắng/thua khi GameOver | Bảng quyết định | PM | TB | AudioListener.pause = true sau GameOver | Kết thúc ván (a) thắng; (b) thua | (a) WinStinger phát dù listener đang tạm dừng; (b) LoseStinger (hiện chưa có clip: im lặng, không lỗi) | | |
| TC-FBK-6 | Âm thanh không làm lệch dãy ngẫu nhiên | Phân vùng tương đương | PM | Cao | RngService seed cố định | Chạy kịch bản 60 s hai lần: lần 1 tắt âm thanh, lần 2 phát nhiều âm thanh nhiều clip | Dãy số từ RngService (vein, vé, người gọi) giống hệt nhau giữa hai lần (Invariant I10) | | |
| TC-FBK-7 | Reward Popup | Bảng quyết định | TC | TB | – | (a) Hoàn thành Click Task; (b) Egg Timer; (c) thắng Billiards; (d) nhận hai khoản trong 0,25 s; (e) nhận tiền sau GameOver; (f) nạp Meter, mua vé, trả thuế; (g) 9 khoản liên tiếp | (a) “+\$5” xanh lá tại con trỏ; (b) “+3s” xanh dương; (c) chỉ hiện phần lời, không hiện tiền hoàn cược; (d) hai popup xếp chồng; (e) không hiện; (f) không hiện (chỉ khoản thu mới có popup); (g) tối đa 8 popup cùng lúc, popup cũ nhất được tái dùng; popup không chặn nhấp chuột (DESIGN §5.17) | | |
| TC-FBK-8 | Highlight Hover | Bảng quyết định | TC | Thấp | – | Rê chuột lên: vật tương tác được; vật bị Cat chặn; vật khi có panel mở; vùng Back; vật vừa bị cầm đồ; ở Room rê lên Egg Timer và Phone; kéo Cat rồi rê ra ngoài | Chỉ vật tương tác được phát sáng viền (hiện 0,1 s, tắt 0,15 s); ở Room Egg Timer sáng, Phone không; Cat đang kéo vẫn sáng đến khi thả (DESIGN §4.2); các trường hợp còn lại không phát sáng | | |

## 4.8 Tổng hợp kết quả kiểm thử

| Phân hệ | Số ca | Đạt | Lỗi | Chưa chạy / \[Dự kiến\] | Ghi chú |
|---|---|---|---|---|---|
| Core Services (4.2) | 30 | | | | 21 cũ + 9 lõi Clock (4.2.4) |
| Kiếm tiền (4.3) | 76 | | | 4 cũ không chạy (xem Phụ lục A) | 53 cũ (gồm 19 ca Billiards) + 23 mới: vé số/Egg Timer 9 (4.3.6), kết thúc cơ chế 14 (4.3.7). TC-MIN-4, TC-TKT-9, TC-BJK-10 được thay bằng 4.3.7; TC-MIN-8 bị bỏ (CaveInLoss đã cắt) |
| Thẻ bài (4.4) | 36 | | | 2 treo (TC-CRD-10, TC-WIN-9) | 24 cũ + 12 mới (4.4.4). Treo: xem Phụ lục A, DESIGN v2.1 (I5) đã cho phép panel thông tin và cutscene đóng băng Clock |
| Gây nhiễu, Pawn, Shop (4.5) | 79 | | | 1 (TC-DOR-10) | 16 cũ + Shop 15, Buyback 7, Phone 20, thuế 11, cửa/thư 10. Các ca từng lệch đặc tả: xem Phụ lục B (đã chốt) |
| Vòng đời, lưu ván (4.6) | 13 | | | | 9 cũ (4.6.1) + 4 mới (4.6.2); TC-GAM-1b nay chạy được (cả 10 EndReason đã cài đặt) |
| Hệ thống WebGL (4.7.5) | 5 | | | | |
| Hints & Tutorial (4.7.6) | 7 | | | 1 treo (TC-HNT-7) | Treo: xem Phụ lục A |
| Chức năng thủ công (4.7.7) | 60 | | | 1 (TC-FCD-5) | HUD 9, Laptop 8, kết thúc 8, lịch sử/Collection 7, thẻ 6, vật dụng/thao tác 13, âm thanh 2, end-to-end 7; không tính các phiên thăm dò. TC-FCD-5 bị bỏ (thẻ Trick Shot đã xóa) |
| Camera, DeviceLayer (4.7.8) | 9 | | | | mới |
| Âm thanh, Popup, Hover (4.7.9) | 8 | | | | mới |
| Luồng điều khiển Blackjack (4.3.8) | 48 | | | | mới 29/10: 7 hàm, 35 ca đường thi hành + 13 ca vòng lặp |
| Tổng | 371 | | | | 195 ca cũ + 128 ca bổ sung ngày 28/10/2026 + 48 ca ngày 29/10/2026 |

### Nhận xét và đề xuất

*\[Tự điền\] Các lỗi phát hiện, mức độ nghiêm trọng, cách khắc phục; độ phủ mã (nếu đo được); hạn chế (chưa tự động hóa được thao tác chuột/UI); hướng phát triển (CI, mở rộng sang Shop/Billiards).*

### Phụ lục A. Ca kiểm thử cũ không còn khớp mã nguồn (đối chiếu 28/10/2026)

Các ca dưới đây được viết theo mã ngày 18/10. Mã đã thay đổi nên kết quả mong đợi cũ sẽ báo Lỗi giả. Nhóm sửa hoặc loại bỏ trước khi chạy; ca thay thế (nếu có) ghi ở cột cuối.

| Mã cũ | Điều đã thay đổi trong mã | Đề xuất | Ca thay thế |
|---|---|---|---|
| TC-BJK-2, TC-BJK-6 | Thưởng Blackjack nay \$100 thắng / \$25 hòa / \$130 Natural (×2 khi Double), cộng thêm Hot Table (+10% mỗi ván thắng liên tiếp, tối đa +50%) | Sửa số: \$130; \$100 / \$25 / \$130 / \$200 với chuỗi thắng = 0 | – |
| TC-BJK-6 (bổ sung 29/10) | Tiền thưởng thật do BlackjackApp.SettleHand tính (có Hot Table); BlackjackGame.Payout chỉ còn công cụ Editor gọi | Chuyển ca sang kiểm SettleHand | TC-BJK-SET-1 – 7 |
| TC-BJK-10 | Charlie đã cài đặt; BlackjackRuinLoss bị bỏ | Bỏ ca | TC-ALT-10 – 12 |
| TC-MIN-4 | MotherLodeWin đã cài đặt | Bỏ \[Dự kiến\] | TC-ALT-6 – 9 |
| TC-MIN-8 | CaveInLoss bị bỏ khỏi thiết kế | Bỏ ca | – |
| TC-TKT-3, TC-TKT-4, TC-TKT-6 | Công thức thưởng mới (cộng mọi biểu tượng lặp, chỉ ô đã lộ, trả khi Cash Out); không còn quy tắc “biểu tượng hệ số cao hơn khi hòa” | Thay bằng ca mới | TC-TKT-11 – 13 |
| TC-TKT-7 | Trọng số mới: Triple Match 50/33/17; Lucky Nine 30/25/20/15/10 | Sửa tỷ lệ mong đợi | – |
| TC-TKT-9 | Jackpot/Curse đã cài đặt | Bỏ \[Dự kiến\] | TC-ALT-1 – 5 |
| TC-DSK-6 | Pool vé N = 2 mỗi loại | Điền N = 2 | TC-TKT-14 |
| TC-EFX-4 | Hệ số tốc độ nay **nhân** với nhau (Phone 1,5 × thẻ), không còn “ghi sau cùng thắng” | Sửa kết quả mong đợi | TC-CLC-4, TC-CLC-8 |
| TC-ANN-1 – 4 | AnnoyanceManager và lớp Phone cũ đã xóa; CallScheduler đảm nhận một cuộc gọi một lúc, cách nhau 12 s; hóa đơn không còn là “phiền nhiễu” chặn ô | Thay bằng ca mới | TC-PHN-4 – 8 |
| TC-ANN-7, TC-E2E-2 | Nút Ignore đã bỏ; phạt tự động khi quá hạn 30 s | Thay bằng ca mới | TC-TAX-4 |
| TC-FCD-2 | Cable Eater nay tắt cưỡng bức Phone **và khóa Laptop** 20 s; Phone vẫn tắt sau đó | Sửa kết quả mong đợi | TC-CRD-20 |
| TC-FCD-5, TC-BIL-16, TC-BIL-17, TC-BIL-19 | Ba thẻ Billiards (Chalk Up, Trick Shot, Cue Slip) đã xóa | Bỏ ca | – |
| TC-WIN-7 | Pawn Final Countdown khi Armed nay tốn phí \$6 | Sửa kết quả mong đợi | TC-CRD-14 |
| TC-PWN-1, TC-DSK-4, TC-E2E-5 | Giá cầm đồ theo catalog: Laptop \$108; Power Bank \$20, Card Box \$32, Lucky Nine stack \$24; Egg Timer, Bear, Picture, Triple stack, Wire Task không cầm được | Sửa số | TC-SHP-12 |
| TC-PWN-2, TC-LAP-7, TC-SYS-2 | Không còn Mail Tray; thư thuế nằm ở cửa (góc nhìn Door) | Bỏ “Mail Tray” khỏi ca | TC-DOR-7 |
| TC-DSK-8, TC-DSK-9 (nút cuộn), Invariant I9 ở 4.7.2 | Camera cuộn dọc đã xóa; ba góc nhìn cố định | Thay bằng ca mới | TC-CAM-1 – 5 |
| TC-CRD-1 – 9, TC-TSK-4, TC-E2E-3 | Card Box và Power Bank là hàng Shop, không có trên bàn đầu ván | Thêm điều kiện tiên quyết “đã mua ở Shop” | TC-SHP-1 |
| TC-HNT-2 | HintId.TicketOpen đã bị xóa | Đổi sang một gợi ý ShowOnce còn tồn tại (ví dụ FirstPawn) | TC-SHP-15 |
| TC-CRD-10, TC-HNT-7, TC-WIN-9 (treo) | DESIGN v2.1, Invariant I5: panel thông tin (CardPanel, hướng dẫn đầu game) và cutscene được phép đóng băng Clock | Gỡ treo, sửa kết quả mong đợi: Clock = 100 / 120 / 180 (không giảm) | – |
| 4.1.2 (“enum EndReason hiện chỉ có 6 giá trị”), TC-GAM-1b | EndReason nay có 10 giá trị, cả 10 đã cài đặt | Bỏ \[Dự kiến\] | TC-GAM-9 |

### Phụ lục B. Chỗ mã nguồn lệch đặc tả DESIGN.md v2.2 (đối chiếu 28/10/2026)

Nguyên tắc: kết quả mong đợi lấy từ đặc tả. Các ca bổ sung ở trên được đối chiếu với DESIGN.md v2.2 (bản đặc tả hiện hành của dự án). Những chỗ dưới đây mã nguồn làm khác đặc tả; quyết định ngày 29/10/2026 ghi ở cột cuối. Với B1, B2, B3, B6 hành vi của mã là đúng, ca kiểm thử giữ theo mã (DESIGN.md cần cập nhật theo).

| # | Nội dung | DESIGN.md nói | Mã nguồn làm | Ca liên quan | Quyết định |
|---|---|---|---|---|---|
| B1 | Trọng số người gọi | §5.9: Telemarketer 40 / Client 25 / Unknown 25 / ITRS 10 | 25 / 25 / 40 / 10 (PhoneConfig, GameConfig). PROJECT.md ghi là vấn đề mở N16, chưa quyết | TC-PHN-8 | Theo code (đã chốt 29/10) |
| B2 | Cuộc gọi đến hạn khi máy tắt | §5.9: một cuộc gọi bị từ chối “được thử lại sau, không bao giờ bị bỏ”; có hàng đợi | Ghi là cuộc gọi nhỡ và lên lịch cuộc mới, không xếp hàng (CallScheduler) | TC-PHN-5 | Theo code (đã chốt 29/10) |
| B3 | Phone sau khi Cable Eater hết hạn | §5.4: “Phone và Laptop cùng tắt trong 20 s” | Laptop mở lại sau 20 s, còn Phone vẫn tắt đến khi người chơi bấm nguồn (PhonePower.ReleaseForceOff) | TC-CRD-20 | Theo code (đã chốt 29/10) |
| B4 | Banner thông báo | §5.9: banner hiện tối đa 3, mỗi cái 4 s | Logic BannerQueue còn, nhưng giao diện banner đã bị gỡ (quyết định A12 của Ari) | TC-PHN-18 | Bỏ logic banner; TC-PHN-18 sẽ bị loại |
| B5 | Hộp rỗng sau khi mở bưu kiện | §5.7, §5.11: để lại hộp các-tông là Junk nhỏ, cầm đồ được \$5 | Chưa cài đặt (PROJECT.md A16: “làm sau khi có hình”) | TC-DOR-10 \[Dự kiến\] | Đang bổ sung vào code; TC-DOR-10 chạy khi xong |
| B6 | Bố cục Laptop và Phone | §5.9: hai thiết bị đặt cạnh nhau | Phone gắn ở mép phải Laptop, nhấp Phone thì Laptop trượt đi (quyết định A3 của Ari); DESIGN chưa cập nhật | TC-CAM-6 | Theo code (đã chốt 29/10) |
| B7 | Gợi ý hóa đơn đầu tiên | §5.16: MailTrayFirstBill nhắc “click the mail tray” | Mail Tray đã bỏ, thư thuế ở cửa; câu gợi ý có thể gây hiểu nhầm | (kiểm tra cùng TC-TAX-10) | Sửa câu gợi ý; TC-TAX-10 kiểm theo câu mới |

### Phụ lục C. Mã kiểm thử tự động cho các ca mới (cập nhật 29/10/2026)

Quy ước: mỗi ca là một hàm `TC_<MÃ>_<số>_MoTa` trong Assets/Tests. Thời gian trong các ca biên dùng giá trị biểu diễn chính xác trong float (ví dụ 29,5 s + 0,5 s thay cho 29,9 s + 0,1 s), vì cộng trừ số thực có sai số làm tròn có thể làm lệch đúng điểm biên.

**Edit Mode – 33 ca, 36 hàm test** (nhánh test/new-cases-2810):

| File | Ca |
|---|---|
| EditMode/ClockCoreTests.cs | TC-CLC-1 – 6 |
| EditMode/TaxDeadlineTests.cs | TC-TAX-1 – 3 |
| EditMode/PhoneLogicTests.cs | TC-PHN-1 – 9, TC-PHN-17 |
| EditMode/ShopLogicTests.cs | TC-SHP-2 – 6, TC-SHP-11, TC-BUY-1 – 3, TC-CRD-18 |
| EditMode/DoorMailTests.cs | TC-DOR-1 – 4 |

**Đã có test từ trước, không viết lại:**

| Ca | Test có sẵn |
|---|---|
| TC-ALT-11 | BlackjackCharlieTests.DoubleDown_XoaRig_LaThuBaLayTuBoBaiThuong |
| TC-GAM-10 | RunRatingTests.EndReason_ThuTuCu_KhongDoi_BonGiaTriMoiNamCuoi |
| TC-FBK-2 | AudioCatalogTests.MoiSfxIdCoDungMotDong, ThongSoCuaTungDongHopLe |

Ngoài ra, PhoneShopSelfTests (chạy trong Test Runner qua SelfTestRunnerTests) đã kiểm phần lớn logic thuần của Phone, Shop, thuế và cửa/thư dưới dạng một hàm tổng; các hàm mới ở trên tách theo từng mã ca để truy vết được kết quả.

**Không viết:** TC-PHN-18 (logic banner sẽ bị bỏ, B4). TC-ALT-13 chuyển sang Play Mode vì RunRating.Score đọc GameManager.Instance.

**Kết quả chạy:** trong **Unity Test Runner** (Unity 6000.3.14f1, Edit Mode, 29/10/2026): 121/121 Đạt, 0 Lỗi, 0 Bỏ qua, 17,5 s; gồm 36 hàm mới ở trên và 85 hàm có sẵn. Console không có lỗi biên dịch mới. Trước đó cũng đã chạy 36/36 Đạt ngoài Unity bằng .NET 9 + NUnit 3.14. Kiểm tra độ nhạy: trên bản sao mã có cài lỗi cố ý (đổi bậc giá mua lại từ 30 s thành 31 s; cho máy reo quá 10 s), 3 test báo Lỗi đúng chỗ (TC-BUY-1, TC-BUY-3, TC-PHN-4).

**Play Mode – đợt 1 (Shop, mua lại): 13 ca, 14 hàm test** (PlayMode/ShopTests.cs, chạy trong cảnh SampleScene thật):

| Ca | Hàm test |
|---|---|
| TC-SHP-1 | TC_SHP_1_TrangThaiDauVan |
| TC-SHP-7 | TC_SHP_7_MuaKhiThieuVaDuTien |
| TC-SHP-8 | TC_SHP_8_MuaVaKichHoatLicense |
| TC-SHP-9 | TC_SHP_9_ThoiGianGiaoChiChayKhiClockChay |
| TC-SHP-10 | TC_SHP_10_ShopSauKhiHetVan |
| TC-SHP-12 | TC_SHP_12_GiaCamDoTheoCatalog (8 vật), TC_SHP_12b_CamDoPhone_BiTuChoiCoGoiY (Phone) |
| TC-SHP-13 | TC_SHP_13a_PanelMoGoiDangMo_KhongCamDoCardBox, TC_SHP_13b_VeLuckyNineTrenBan_KhongCamDoChongVe |
| TC-SHP-15 | TC_SHP_15_GoiYLanDauCamDo |
| TC-BUY-4 – 7 | TC_BUY_4_MuaLaiPowerBank, TC_BUY_5_MuaLaiLaptop, TC_BUY_6_MuaLaiKhiThieuTien, TC_BUY_7_GiaMuaLaiKhongTangKhiClockDung |

Mỗi test đưa camera về góc nhìn Desk trước khi chạy (ván bắt đầu ở góc nhìn Room, hàng giao chỉ hiện lên bàn khi đang nhìn Desk) và đặt Clock = 500 s để không hết giờ giữa chừng. Số tiền tính bằng số nguyên (80% làm tròn xuống, giá mua lại làm tròn lên), giá đọc từ ItemCatalog, không ghi cứng.

**Kết quả chạy:** trong **Unity Test Runner** (Unity 6000.3.14f1, Play Mode, 29/10/2026): **13/14 Đạt, 1 Lỗi**. Console không có lỗi hay cảnh báo mới.
- **Lỗi – TC_SHP_12b (TC-SHP-12, phần Phone): Không đạt.** Kéo Printer lên Phone: tiền không đổi, Phone vẫn trên bàn (đúng), nhưng **không hiện gợi ý CannotPawn**. DESIGN.md §5.11 yêu cầu “Trying to Pawn a protected object shows a hint”. Nguyên nhân: PhoneController không có IPawnable nên Pawn.OnTriggerEnter2D bỏ qua mà không báo. Đã ghi lỗi N19 trong PROJECT.md §15 (Meter, Clock, Cat có thể bị cùng lỗi; TC-PWN-2 chưa kiểm gợi ý). Test để nguyên trạng thái Lỗi cho tới khi game được sửa.
- Kiểm tra độ nhạy: tạm đổi làm tròn giá mua lại từ lên thành xuống (PawnMath.BuybackPrice), TC-BUY-4 (27 → 26) và TC-BUY-7 (142 → 141) báo Lỗi đúng chỗ; đã hoàn tác.
- Ghi chú khi chạy qua Unity MCP: công cụ run_tests tạm bật “Enter Play Mode Options: tắt domain reload” trong lúc chạy và trả lại sau; nếu phiên MCP rớt giữa chừng, ProjectSettings/EditorSettings.asset bị để lại m_EnterPlayModeOptions: 1, cần hoàn tác, không commit.

**Kiểm thử luồng điều khiển Blackjack (4.3.8) – 48 ca, 48 hàm test:**

| File | Ca |
|---|---|
| EditMode/BlackjackControlFlowTests.cs | TC-BJK-VAL-1 – 5, LP0 – LP4; TC-BJK-DEAL-1 – 4; TC-BJK-HIT-1 – 4; TC-BJK-DBL-1 – 4; TC-BJK-STD-1 – 6; TC-BJK-SET-1 – 7; TC-BJK-RIG-1 – 7, LP0 – LP8 |

Các ca RigPlayerCards gọi hàm private bằng reflection để đặt bộ bài tùy ý (cùng cách với TestHooks). Kết quả chạy ngày 29/10/2026 ngoài Unity (.NET 9, bản build OverTimer.dll, NUnit của Unity, GameConfig đọc từ asset): 48/48 Đạt; kiểm tra độ nhạy bằng một kỳ vọng cố ý sai cho kết quả Lỗi đúng.

**LỜI CẢM ƠN**

*\[Tự điền\] Cảm ơn giảng viên, các thành viên trong nhóm (xem mẫu ở file báo cáo Kiểm thử phần mềm).*

**TÀI LIỆU THAM KHẢO**

1\. Giáo trình Kiểm thử phần mềm (giáo trình của học phần).

2\. Tài liệu đặc tả yêu cầu phần mềm (SRS) và tài liệu thiết kế DESIGN.md của dự án OverTimer.

3\. Unity Technologies, Unity Test Framework documentation.

4\. NUnit Documentation.

*\[Tự điền\] Bổ sung đường dẫn, ngày truy cập.*
