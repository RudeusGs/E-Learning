# ENGINEERING_STANDARDS.md — Quy tắc code và implementation

## 1. Mục tiêu

Code phải dễ đọc, dễ review, dễ test và dễ bàn giao. Tránh abstraction/pattern không tạo giá trị thật.

## 2. C#/.NET

- Enable nullable reference types.
- Async toàn bộ I/O path.
- Không `.Result`, `.Wait()` trong request path.
- Không `Task.Run` để bọc database/network I/O.
- Truyền `CancellationToken` từ controller xuống EF/API call.
- Dùng `TimeProvider` hoặc UTC abstraction thay `DateTime.Now` trong logic cần test.
- DTO/request/response tách khỏi EF entity.
- Dùng explicit mapping/projection.
- Exception không dùng cho flow validation thông thường nếu result/error rõ hơn.
- Không catch exception chỉ để log rồi swallow.

## 3. EF Core

Bắt buộc:

- Lazy loading OFF.
- `AsNoTracking()` cho read-only.
- `Select()` projection cho list.
- Pagination server-side.
- Không query trong foreach.
- Không `ToListAsync()` trước khi filter/order nếu không cần.
- Không `Include` collection hàng loạt theo thói quen.
- Transaction chỉ khi mutation nhiều bước phải atomic.
- Unique constraint bảo vệ invariant có race.

Không tạo Generic Repository để bọc EF Core CRUD.

## 4. Application service

Một use case nên có một entry point rõ, ví dụ:

```text
CourseService.CreateAsync
LessonService.UpdateAsync
EnrollmentService.EnrollAsync
LearningService.GetLessonAsync
ProgressService.CompleteLessonAsync
```

Tên phải nói được hành vi.

Không tạo `BaseService<T>` chung chứa CRUD nếu mỗi domain có authorization/validation khác nhau.

## 5. Controllers

Controller không chứa:

- business rule dài;
- query LINQ phức tạp nhiều join;
- transaction;
- password/security handling chi tiết;
- mapping domain lớn.

Controller chỉ orchestration HTTP mỏng.

## 6. Validation

- Request shape validation ở API boundary.
- Business invariant ở Application/Domain.
- Database invariant có unique/FK/constraint khi phù hợp.

Không chỉ validation frontend.

## 7. Error handling

Có global exception/error mapping về ProblemDetails.

Không:

```csharp
catch (Exception ex)
{
    return Ok(new { success = false, message = ex.Message });
}
```

Unexpected exception -> 500 sanitized + traceId, detail thật ở server log.

## 8. Logging

Dùng structured logging:

```csharp
logger.LogInformation(
    "Student {StudentId} completed lesson {LessonId}",
    studentId,
    lessonId);
```

Không string concatenate log.

Không log password/token/cookie/secret.

Không log toàn request body cho auth.

## 9. Frontend TypeScript

- TypeScript strict.
- Không `any` nếu có thể mô hình hóa type.
- Không duplicate API model lung tung; type theo feature.
- Composition API + `<script setup>`.
- Component nhỏ theo trách nhiệm thực tế, không chia vụn cực đoan.
- Không business authorization ở component.
- Không giữ credential trong browser storage.

## 10. Axios client

Hai Axios instance có trách nhiệm rõ:

- normal API client: `baseURL: '/api'`, Bearer access token lấy từ in-memory auth state, 401 refresh single-flight.
- auth lifecycle client: login/refresh/logout, `withCredentials: true`, **không** gắn auto-refresh interceptor để tránh refresh recursion/deadlock.
- timeout hợp lý và không nuốt error.

Không lưu access/refresh credential trong `localStorage`/`sessionStorage`.

## 11. Pinia

Dùng cho state xuyên app thực sự như:

- current user/session metadata;
- UI state toàn cục nếu cần.

Không dùng Pinia làm cache mặc định cho toàn bộ server data.

## 12. Router

Route meta có thể dùng role để redirect UX, nhưng backend vẫn authorize.

Không coi hidden menu là security.

## 13. Rich text

- Admin editor output -> backend sanitizer.
- Frontend render sanitized content.
- Không thêm arbitrary iframe/script support.

## 14. Naming

Backend:

```text
Course
Lesson
Enrollment
Question
QuestionOption
StudentAnswer
LessonProgress
```

Enums dùng tên rõ:

```text
CourseStatus.Draft
CourseStatus.Published
CourseStatus.Archived
LessonProgressStatus.InProgress
LessonProgressStatus.Completed
```

API JSON dùng convention nhất quán, ưu tiên camelCase.

## 15. Magic numbers/strings

Không rải:

```text
100
5
"ADMIN"
"PUBLISHED"
```

ở nhiều chỗ nếu đó là policy/domain value.

Dùng enum/config/constant có tên.

Nhưng không tạo constant cho mọi literal UI nhỏ.

## 16. Dependency policy

Không thêm mặc định:

- MediatR.
- AutoMapper.
- Generic Repository packages.
- Redis.
- Message broker.
- GraphQL.
- Dynamic LINQ package chỉ để sort/filter đơn giản.

Nếu thêm dependency phải trả lời:

1. vấn đề cụ thể gì?
2. framework hiện tại không đủ ở đâu?
3. security/license/maintenance thế nào?
4. nếu bỏ package thì cost ra sao?

## 17. Database query review

PR chạm query phải kiểm tra:

- generated SQL/query plan nếu query phức tạp;
- query count;
- row count bounded;
- index predicate;
- projection size.

Đặc biệt review:

- admin dashboard aggregates;
- course list counts;
- student course progress;
- admin progress table;
- learning path check.

## 18. Reordering lesson

Reorder phải atomic.

Không gửi N request update từng row nếu có thể batch.

Backend validate toàn bộ lesson thuộc course và thứ tự không duplicate.

## 19. Idempotency

Các command sau phải idempotent:

- Start lesson.
- Complete lesson.
- Archive resource.
- Enroll pair đã Active.

Idempotent ở đây nghĩa là retry request không tạo duplicate state hoặc làm sai timestamp quan trọng.

## 20. Security coding

- Không nhận `studentId` từ Student self-service body.
- Không render raw arbitrary HTML.
- Không arbitrary iframe URL.
- Không CORS wildcard với credentials.
- Không raw SQL concat.
- Không expose exception production.

Xem đầy đủ `SECURITY.md`.

## 21. Migration coding

- Mỗi schema change có migration riêng có tên mô tả.
- Không sửa migration cũ đã shared/applied.
- Không auto-drop column/table mà không đánh giá dữ liệu.
- Update database test từ empty schema.

## 22. Testability

Business logic quan trọng không phụ thuộc trực tiếp clock/static global state nếu test cần kiểm soát.

Integration test dùng PostgreSQL thật qua Testcontainers; không dùng EF InMemory để giả integration DB behavior.

## 23. Frontend UX state

Mỗi page data-driven phải có:

```text
loading
success
empty
error
```

Mutation phải:

- disable/dedupe double submit khi cần;
- hiển thị pending state;
- xử lý 400/401/403/409 rõ;
- không optimistic update cho state nhạy cảm nếu rollback khó.

## 24. Accessibility cơ bản

- Form field có label.
- Button semantic.
- Keyboard usable.
- Focus visible.
- Error message liên kết field khi có thể.
- Color không là tín hiệu duy nhất cho completed/locked/error.
- Video iframe có title.

## 25. Git/PR discipline

Một task nên là smallest coherent vertical slice.

Không trộn:

- feature + unrelated refactor;
- dependency major upgrade + feature;
- formatting toàn repo + bug fix.

Trước merge:

- inspect diff;
- không commit secret;
- test pass;
- docs update nếu contract đổi.

## 26. Comments

Comment giải thích **why**, không kể lại code.

Bad:

```csharp
// Set status to completed
progress.Status = Completed;
```

Good:

```csharp
// Preserve the first completion timestamp so retries stay idempotent.
```

## 27. TODO policy

TODO phải có reason/context. Không để TODO cho security/authorization bắt buộc rồi ship MVP.

Nếu việc chưa làm là non-goal, ghi ở backlog/issue thay vì TODO rải code.

## 28. Performance policy

Tối ưu trước các failure mode biết chắc:

- N+1.
- unbounded list.
- thiếu index rõ ràng.
- load content lớn không cần.

Không tự thêm:

- caching phức tạp.
- compiled query.
- Redis.
- background precomputation.

nếu chưa đo bottleneck.

## 29. Code review checklist ngắn

- Behavior đúng spec?
- Authorization ở backend?
- Validation đủ?
- Query bounded/no N+1?
- DB constraint bảo vệ race?
- DTO không leak data?
- Error semantics đúng?
- Test success + negative case?
- Docs cần đổi không?
- Có abstraction/dependency thừa không?
