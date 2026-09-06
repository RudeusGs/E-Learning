# DATABASE.md — Data Model và Query Contract

## 1. Mục tiêu

Database phải:

- phản ánh đúng invariant nghiệp vụ;
- chống duplicate/race bằng constraint khi có thể;
- hỗ trợ query phổ biến bằng index phù hợp;
- không lưu derived state dễ lệch;
- giữ lịch sử quan trọng đủ để audit/debug;
- dễ migration và bàn giao.

Engine: PostgreSQL.

ORM: Entity Framework Core 10.

## 2. Nguyên tắc chung

- Primary key dùng `long`/bigint identity cho MVP.
- Timestamp dùng UTC, trong code ưu tiên `DateTimeOffset`.
- Không dùng lazy loading.
- Read-only query dùng `AsNoTracking()`.
- FK phải explicit và có index nếu query/join thường xuyên.
- Unique constraint dùng để bảo vệ invariant chứ không chỉ dựa vào pre-check trong code.
- Không lưu `CourseProgressPercent`.
- Không lưu `IsUnlocked` cho lesson.

## 3. Identity tables

Dùng ASP.NET Core Identity làm identity store.

Application user mở rộng Identity user với business fields tối thiểu:

```text
ApplicationUser
- Id: long
- UserName / NormalizedUserName
- Email / NormalizedEmail
- PasswordHash
- SecurityStamp
- FullName
- Status: ACTIVE | DISABLED
- CreatedAtUtc
- UpdatedAtUtc
```

Role dùng Identity role:

```text
ADMIN
STUDENT
```

Không tạo bảng `users` thứ hai chứa password song song với Identity.

### Constraints

- NormalizedEmail unique nếu hệ thống yêu cầu 1 email/1 account.
- Role assignment không được phép tạo Student đồng thời Admin trừ khi Product Owner đổi requirement.

## 4. Course

```text
Course
- Id: long PK
- Title: varchar(200), required
- Description: varchar/text, nullable
- ThumbnailUrl: varchar(2048), nullable
- Status: DRAFT | PUBLISHED | ARCHIVED
- SortOrder: int
- CreatedAtUtc
- UpdatedAtUtc
```

### Index đề xuất

```text
INDEX(Status, SortOrder)
INDEX(SortOrder)
```

Nếu danh sách admin search title thường xuyên, cân nhắc index phù hợp sau khi đo; không tạo index text tùy tiện từ đầu.

### Delete semantics

Không hard-delete Course có history. API delete của MVP map sang `ARCHIVED`.

Archived Course:

- không Student access;
- không enrollment mới;
- history vẫn còn.

## 5. Lesson

```text
Lesson
- Id: long PK
- CourseId: long FK -> Course
- Title: varchar(200), required
- Description: varchar/text, nullable
- ContentHtml: text, nullable
- VideoUrl or normalized video fields, nullable
- SortOrder: int, required
- Status: DRAFT | PUBLISHED | ARCHIVED
- CreatedAtUtc
- UpdatedAtUtc
```

### Video representation

MVP có thể chọn một trong hai cách, nhưng chỉ một cách trong codebase:

**Option A — đơn giản:**

```text
VideoUrl
```

Backend validate/normalize trước khi trả.

**Option B — an toàn rõ hơn:**

```text
VideoProvider = YOUTUBE
VideoExternalId = <YouTube video id>
```

Khuyến nghị B nếu implementation cost không đáng kể vì frontend không phải xử lý raw arbitrary URL.

### Index/constraint

```text
INDEX(CourseId, Status, SortOrder)
```

Thứ tự lesson Published trong cùng Course phải deterministic.

Preferred invariant:

```text
(CourseId, SortOrder) unique cho lesson không Archived
```

Nếu implement partial unique index làm reorder phức tạp, service phải ít nhất reject duplicate effective SortOrder. Không được để learning path có thứ tự mơ hồ.

## 6. Enrollment

```text
Enrollment
- Id: long PK
- StudentId: long FK -> ApplicationUser
- CourseId: long FK -> Course
- Status: ACTIVE | INACTIVE
- EnrolledAtUtc
- UpdatedAtUtc
```

### Required constraints

```text
UNIQUE(StudentId, CourseId)
INDEX(CourseId, Status)
INDEX(StudentId, Status)
```

### Semantics

Enroll lần đầu -> insert.

Enroll lại cùng pair:

- nếu INACTIVE -> reactivate;
- nếu ACTIVE -> idempotent/no duplicate.

Không tạo nhiều row cho cùng Student/Course trong MVP.

## 7. Question

```text
Question
- Id: long PK
- LessonId: long FK -> Lesson
- Text: text, required
- Type: MULTIPLE_CHOICE | TRUE_FALSE
- Explanation: text, nullable
- SortOrder: int
- CreatedAtUtc
- UpdatedAtUtc
```

### Index

```text
INDEX(LessonId, SortOrder)
```

## 8. QuestionOption

```text
QuestionOption
- Id: long PK
- QuestionId: long FK -> Question
- Content: text, required
- IsCorrect: bool, required
- SortOrder: int, required
```

### Index

```text
INDEX(QuestionId, SortOrder)
```

### Validation invariant

Mỗi Question phải có **chính xác 1** option `IsCorrect=true`.

DB relational constraint thông thường khó enforce “exactly one correct child row”; application service phải validate trong transaction trước commit.

MULTIPLE_CHOICE:

- min 2 options;
- MVP UI max 4 options;
- exactly 1 correct.

TRUE_FALSE:

- exactly 2 semantic options;
- exactly 1 correct.

## 9. StudentAnswer

```text
StudentAnswer
- Id: long PK
- StudentId: long FK -> ApplicationUser
- QuestionId: long FK -> Question
- OptionId: long FK -> QuestionOption
- IsCorrect: bool
- AnsweredAtUtc
```

Mỗi submit tạo một attempt mới.

`IsCorrect` được lưu snapshot ở thời điểm submit.

Lý do giữ snapshot:

- Admin có thể sửa correct option sau này.
- Kết quả lịch sử không bị retroactively đổi.

### Index

```text
INDEX(StudentId, QuestionId, AnsweredAtUtc)
INDEX(QuestionId, AnsweredAtUtc)
```

Nếu PostgreSQL index direction được cấu hình, có thể ưu tiên descending timestamp cho query latest attempt; chỉ làm nếu ORM/migration rõ ràng.

### Integrity

Application phải đảm bảo:

- OptionId thuộc QuestionId.
- Question thuộc lesson/course Student được access.
- StudentId lấy từ authenticated principal, không tin body.

## 10. LessonProgress

```text
LessonProgress
- Id: long PK
- StudentId: long FK -> ApplicationUser
- LessonId: long FK -> Lesson
- Status: IN_PROGRESS | COMPLETED
- StartedAtUtc
- CompletedAtUtc nullable
- UpdatedAtUtc
```

`NOT_STARTED` ưu tiên là trạng thái suy ra khi chưa có record, tránh tạo row cho mọi Student/Lesson.

### Required constraint

```text
UNIQUE(StudentId, LessonId)
INDEX(LessonId, Status)
INDEX(StudentId, Status)
```

### State transitions

```text
(no row) -> IN_PROGRESS -> COMPLETED
(no row) -> COMPLETED  // được phép khi complete hợp lệ mà chưa start explicit
COMPLETED -> COMPLETED // idempotent
```

Không cho:

```text
COMPLETED -> IN_PROGRESS
```

`CompletedAtUtc` set lần đầu khi transition sang Completed và không đổi khi complete lại.

## 11. Course progress query

Source of truth:

- Lesson `Status=PUBLISHED`.
- LessonProgress `Status=COMPLETED` của current Student.

Conceptual SQL:

```sql
SELECT
    COUNT(*) FILTER (WHERE lp.status = 'COMPLETED') AS completed,
    COUNT(*) AS total
FROM lessons l
LEFT JOIN lesson_progress lp
    ON lp.lesson_id = l.id
   AND lp.student_id = @studentId
WHERE l.course_id = @courseId
  AND l.status = 'PUBLISHED';
```

Percentage:

```text
if total == 0 -> 0
else round(completed * 100.0 / total)
```

Exact rounding convention phải thống nhất trong backend test. Với số liệu đơn giản như 2/5 -> 40.

## 12. Learning path query

Để Student access Lesson L:

1. Load lesson scope + course status + active enrollment theo current student.
2. Xác định lesson Published đứng ngay trước theo `SortOrder`.
3. Nếu không có predecessor -> open.
4. Nếu có -> check LessonProgress Completed cho predecessor.

Có thể thực hiện trong 1-3 bounded queries. Không được query toàn danh sách rồi lặp query progress từng lesson.

## 13. Admin course list query

Mỗi row cần:

- Course Id/Title/Status.
- Lesson count.
- Active enrollment count.

Dùng aggregate projection trong SQL.

Không:

```text
1 query courses
+ N query lesson count
+ N query enrollment count
```

Query count phải gần như hằng số theo số course page.

## 14. Student course list query

Scope từ Enrollment trước:

```text
current Student
+ Enrollment Active
+ Course Published
```

Mỗi item trả:

- course summary.
- total Published lessons.
- completed Published lessons.
- percentage.

Không load `ContentHtml`, full question/options hoặc answer history ở list endpoint.

## 15. Admin progress query

Danh sách có thể lớn; bắt buộc pagination.

Projection tối thiểu:

```text
StudentId
StudentName
CourseId
CourseTitle
CompletedLessons
TotalLessons
ProgressPercent
```

Không materialize toàn progress matrix cho tất cả student/course nếu chỉ hiển thị 20 rows.

## 16. Delete/archive và referential integrity

### Course

Archive, không hard-delete history.

### Lesson

Nếu chưa có progress/answers có thể cân nhắc hard delete, nhưng để behavior nhất quán MVP ưu tiên archive.

### Question

Admin delete question có thể hard-delete nếu chưa có answer history.

Nếu đã có StudentAnswer:

- ưu tiên soft-delete/disable question hoặc restrict delete;
- không cascade xóa answer history vô tình.

Nếu implementation chọn một policy khác, phải ghi rõ migration/data-retention impact trong `DECISIONS.md`.

## 17. Cascade rules

Không bật cascade delete rộng mà không review.

Khuyến nghị:

- Course -> Lesson: Restrict nếu archive semantics.
- Course -> Enrollment: Restrict.
- Lesson -> Question: Restrict nếu có history.
- Question -> Option: Restrict khi StudentAnswer tham chiếu option.
- User -> Enrollment/Progress/Answers: Restrict; account disable thay vì delete.

Mục tiêu là tránh một `DELETE` xóa sạch lịch sử học tập.

## 18. Migration policy

- Migration là source-controlled artifact.
- Không sửa migration đã apply shared/production DB; tạo migration mới.
- Migration destructive phải review dữ liệu bị ảnh hưởng.
- Production không auto-migrate trong app startup.
- Deployment chạy migration step có log và backup/rollback plan phù hợp.

Test bắt buộc:

- database mới apply tất cả migrations thành công;
- seed dev chạy được;
- app start sau migration.

## 19. Seed policy

Seed chia hai loại:

### Structural seed

- roles ADMIN/STUDENT.

Có thể áp dụng ở mọi environment nếu idempotent.

### Demo seed

- admin demo.
- student demo.
- Python Basic.
- lessons/questions.

Chỉ Development/Demo.

Production không tạo default credential.

## 20. Query performance checklist

Trước khi merge query mới:

- Có pagination nếu list tăng được?
- Có projection chỉ lấy field cần?
- Có `AsNoTracking` nếu read-only?
- Có query trong loop?
- Có hidden lazy loading?
- Có `Include` nhiều collection gây cartesian explosion?
- Filter diễn ra trong DB hay memory?
- Index support predicate/join/order chính?
- Query count có tăng theo N record không?
- Response có load blob/text lớn không cần thiết?

## 21. No N+1 regression test

Integration test cho endpoint quan trọng nên có command-count interceptor hoặc query capture.

Ví dụ contract:

```text
GET /api/admin/courses?page=1&pageSize=20
```

Với 5 course và 100 course, số SQL command cho request không tăng theo số row trong page.

Không cần ép mọi endpoint đúng 1 query; mục tiêu là **bounded query count** và không O(N) roundtrip.

## 22. Data integrity race cases cần test

- Hai request enroll cùng Student/Course -> chỉ 1 enrollment.
- Hai request start lesson đồng thời -> chỉ 1 LessonProgress.
- Hai request complete cùng lesson -> 1 progress, CompletedAt ổn định.
- Reorder lesson không tạo duplicate effective order.
- Option submit thuộc question khác -> reject.

## 23. Backup/restore expectation

Production handover phải ghi rõ:

- ai backup DB;
- retention policy;
- cách restore test;
- migration compatibility.

Repo không tự giả định backup đã tồn tại chỉ vì dùng managed database.
## Learning Guardrails — 2026-08-29

Added lesson media metadata:

- `Lessons.VideoDurationSeconds` nullable integer, 1..43200 when present.

Added question placement metadata:

- `Questions.Placement`: `Reinforcement | VideoCheckpoint`;
- `Questions.VideoTimestampSeconds` nullable integer.

Added trusted watch progress to LessonProgress:

- `VideoMaxPositionSeconds`;
- `VideoLastPositionSeconds`;
- `VideoHeartbeatAtUtc`;
- `VideoCompletedAtUtc`.

Indexes support checkpoint lookup and pass-state aggregation: `(LessonId, Placement, VideoTimestampSeconds)` and `(StudentId, QuestionId, IsCorrect)`.
