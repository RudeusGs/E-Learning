# ARCHITECTURE.md — Kiến trúc hệ thống E-Learning MVP

## 1. Mục tiêu kiến trúc

Kiến trúc phải tối ưu cho 4 tiêu chí:

1. **Correctness:** đúng nghiệp vụ và quyền truy cập.
2. **Maintainability:** người mới nhận bàn giao có thể hiểu hệ thống nhanh.
3. **Security:** backend luôn kiểm soát identity, authorization và input không tin cậy.
4. **Performance predictable:** tránh N+1, unbounded query, duplicate work và state dư thừa.

Không tối ưu cho scale giả định chưa tồn tại.

## 2. Kiểu kiến trúc

Chọn **modular monolith**.

Lý do:

- MVP có một domain tương đối gọn.
- Các flow Course/Lesson/Enrollment/Progress liên quan chặt nhau.
- Một database transaction là lợi thế lớn.
- Dễ debug, deploy và bàn giao hơn microservices.
- Không tạo network hop, distributed tracing, retry/idempotency liên service hoặc deployment orchestration không cần thiết.

Không dùng microservices, service mesh, Kafka/RabbitMQ, distributed cache hoặc saga trong MVP.

## 3. Repository layout mục tiêu

```text
/
├── Elearning.sln
├── server/
│   ├── src/
│   │   ├── Elearning.Api/
│   │   ├── Elearning.Application/
│   │   ├── Elearning.Domain/
│   │   └── Elearning.Infrastructure/
│   └── tests/
│       ├── Elearning.UnitTests/
│       └── Elearning.IntegrationTests/
├── client/
│   ├── package.json
│   └── src/
│       ├── api/
│       ├── components/
│       ├── layouts/
│       ├── modules/
│       ├── router/
│       ├── stores/
│       ├── types/
│       └── utils/
├── e2e/
│   └── ... Playwright tests
├── docker-compose.yml
├── .env.example
├── docs/
│   ├── PROJECT_SPEC.md
│   ├── ARCHITECTURE.md
│   ├── SECURITY.md
│   ├── DATABASE.md
│   ├── API_CONTRACT.md
│   └── ...
```

Nếu repo hiện tại đã có cấu trúc khác nhưng vẫn đáp ứng dependency rule, agent không được refactor toàn bộ chỉ để giống sơ đồ này.

## 4. Backend layers

### 4.1 Elearning.Domain

Chứa:

- Entity/domain model cốt lõi.
- Enum/value rules không phụ thuộc framework.
- Domain exception nếu thực sự cần.
- Pure business logic có thể test độc lập.

Không chứa:

- Controller.
- DbContext.
- EF attribute nếu có thể tránh.
- HTTP types.
- PostgreSQL-specific logic.
- Identity framework implementation.

### 4.2 Elearning.Application

Chứa use case theo feature:

```text
Auth/
Courses/
Lessons/
Students/
Enrollments/
Exercises/
Progress/
```

Mỗi feature có thể có:

- Request/command DTO nội bộ.
- Query DTO.
- Application service/use-case class.
- Validation/business orchestration.
- Interface cho infrastructure boundary nếu cần thật.

Không bắt buộc CQRS framework. Có thể phân biệt query/mutation bằng naming đơn giản.

### 4.3 Elearning.Infrastructure

Chứa:

- `AppDbContext`.
- EF Core configurations.
- Migrations.
- ASP.NET Core Identity persistence/config.
- Seed Development/Demo.
- Time/clock adapter nếu không dùng `TimeProvider` trực tiếp.
- Sanitizer/provider adapter nếu dependency external cần bọc.

Không đặt business authorization rule ở đây nếu rule thuộc application behavior.

### 4.4 Elearning.Api

Chứa:

- Controllers/endpoints.
- Authentication/authorization policy wiring.
- Middleware.
- ProblemDetails mapping.
- Dependency injection composition root.
- Rate limiting.
- OpenAPI.
- Health checks.

Controller chỉ:

1. nhận HTTP input,
2. bind/validate shape,
3. lấy current user identity,
4. gọi application use case,
5. map output/status.

Controller không được chứa query phức tạp hoặc business flow dài.

## 5. Dependency rule

```text
Elearning.Domain
      ↑
Elearning.Application
      ↑              ↑
Elearning.Api     Elearning.Infrastructure
```

`Application` không phụ thuộc `Infrastructure` concrete type.

`Api` có thể reference Infrastructure để đăng ký DI ở composition root, nhưng business code trong Api không sử dụng concrete infrastructure trực tiếp ngoài wiring.

## 6. Feature boundaries

### Auth

Owns:

- Login/logout/current user.
- Account status integration.
- Session behavior.

### Courses

Owns:

- Course lifecycle/status.
- Admin course CRUD/read.
- Student course visibility query phối hợp Enrollment.

### Lessons

Owns:

- Lesson lifecycle/status/order.
- Lesson content/video metadata.
- Learning path access rule phối hợp Progress.

### Students

Owns:

- Student profile/status management.

### Enrollments

Owns:

- Quan hệ Student-Course.
- Active/inactive enrollment.
- Duplicate/reactivation semantics.

### Exercises

Owns:

- Question/options.
- Grading answer.
- Answer attempts.

### Progress

Owns:

- Start/complete lesson.
- Course progress calculation.
- Admin progress projections.

Không tạo module dependency vòng. Application service có thể phối hợp dữ liệu từ nhiều table trong cùng DbContext/transaction khi use case yêu cầu.

## 7. Authentication architecture

### 7.1 Strategy

Vue SPA và API production ưu tiên cùng origin:

```text
https://elearning.school.example/
├── /              -> Vue static app
└── /api/*         -> ASP.NET Core
```

Dùng ASP.NET Core Identity cho password/account state và JWT Bearer cho access token.

- Access JWT lifetime ngắn (default 10 phút), frontend chỉ giữ trong memory.
- Refresh token là opaque random token, browser chỉ giữ trong `Secure + HttpOnly + SameSite=Strict` cookie.
- Refresh token plaintext không được lưu database; DB chỉ lưu SHA-256 hash.
- Refresh rotation single-use, có token-family replay detection và revoke.
- Production/Staging signing key bắt buộc đến từ environment/secret store; không có source-code fallback.

### 7.2 JWT access token

JWT tối thiểu chứa:

- `sub`/NameIdentifier user id.
- username/email khi consumer cần.
- role.
- `jti`.
- hash security stamp để invalidate session sau security-sensitive account change.

Validation bắt buộc issuer, audience, lifetime, signing key, algorithm, account Active, security-stamp hash và blacklist `jti`.

### 7.3 Refresh lifecycle

```text
login -> AT1 + RT1(cookie)
refresh RT1 -> atomically consume RT1 -> AT2 + RT2(cookie)
replay RT1 ngoài grace -> revoke family + blacklist live access tokens
logout/disable -> revoke family/session
```

Rotation không kéo dài session vô hạn: token thay thế giữ absolute expiry của family hiện tại.

### 7.4 API unauthorized behavior

- `401` nếu chưa authenticated/token invalid/session revoked.
- `403` nếu authenticated nhưng không authorized.
- API không redirect sang HTML login page.

## 8. CSRF architecture

Business API dùng Bearer token trong `Authorization` header nên browser không tự gắn credential đó vào cross-site request; không dùng antiforgery token cho các API Bearer mutation. Refresh token chỉ nằm trong `SameSite=Strict` HttpOnly cookie và auth endpoints được rate-limit. Nếu topology đổi sang cross-site cookie hoặc access-cookie authentication, phải review CSRF lại bằng ADR trước khi code.

## 9. Authorization architecture

### 9.1 Role-level

Policies tối thiểu:

```text
RequireAdmin
RequireStudent
```

### 9.2 Resource-level

Role không đủ. Student resource access phải kiểm tra:

- current user id.
- account status hợp lệ.
- enrollment active.
- course published.
- lesson published.
- learning path predecessor completed khi đọc lesson.

Không load entity trước rồi trả dữ liệu sau mới kiểm tra quyền.

Ưu tiên query có predicate quyền ngay trong DB.

Ví dụ conceptual:

```csharp
var course = await db.Courses
    .AsNoTracking()
    .Where(c => c.Id == courseId)
    .Where(c => c.Status == CourseStatus.Published)
    .Where(c => c.Enrollments.Any(e =>
        e.StudentId == currentUserId &&
        e.Status == EnrollmentStatus.Active))
    .Select(...)
    .SingleOrDefaultAsync(ct);
```

## 10. Data access strategy

Không generic repository.

`DbContext` là unit-of-work/data access boundary chính trong Infrastructure/Application implementation.

### Read path

Mặc định:

```text
AsNoTracking
-> Where authorization/filter
-> OrderBy
-> Select DTO
-> Skip/Take nếu list
-> materialize
```

### Write path

Mặc định:

1. Load đúng aggregate/minimum rows cần mutate.
2. Kiểm tra business rule.
3. Apply mutation.
4. `SaveChangesAsync(ct)`.
5. Transaction explicit nếu một use case gồm nhiều thay đổi phải atomic.

Không dùng explicit transaction cho single `SaveChanges` đơn giản vì EF đã transaction hóa phần đó.

## 11. N+1 prevention contract

Không cài/bật lazy-loading proxies.

Không:

```csharp
var courses = await db.Courses.ToListAsync(ct);
foreach (var c in courses)
{
    var lessonCount = c.Lessons.Count;
}
```

Ưu tiên:

```csharp
var items = await db.Courses
    .AsNoTracking()
    .OrderBy(c => c.SortOrder)
    .Select(c => new CourseListItemDto(
        c.Id,
        c.Title,
        c.Lessons.Count(l => l.Status == LessonStatus.Published),
        c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active)))
    .Skip(offset)
    .Take(pageSize)
    .ToListAsync(ct);
```

Nếu cần nhiều collection:

- ưu tiên projection.
- chỉ dùng `Include` khi entity graph thực sự cần để mutate.
- đánh giá cartesian explosion.
- split query chỉ dùng có chủ đích và có test.

## 12. Progress architecture

Không có `CourseProgress` mutable source of truth.

Source of truth:

- Lesson status hiện tại.
- `LessonProgress` của Student.

Course progress query trả:

```text
completedLessons
totalLessons
percentage
```

`percentage` tính tại query time.

Lợi ích:

- Không cần cache invalidation khi publish/unpublish lesson.
- Không có race giữa complete lesson và update progress aggregate.
- Dễ chứng minh tính đúng.

## 13. Learning path architecture

Không lưu cột `IsUnlocked`.

Unlock là derived state:

```text
Is first published lesson?
  yes -> open
  no  -> predecessor published lesson completed?
          yes -> open
          no  -> locked
```

Khi query lesson detail, backend xác định predecessor theo Course + Status Published + SortOrder.

Nếu SortOrder có conflict, dữ liệu phải bị validation từ admin mutation để không tạo thứ tự mơ hồ.

## 14. Exercise grading architecture

Server là authoritative grader.

Client chỉ gửi:

```json
{
  "optionId": 10
}
```

Server:

1. xác định current student.
2. kiểm tra quyền lesson/course.
3. kiểm tra question thuộc lesson có thể truy cập.
4. kiểm tra option thuộc question.
5. đọc `IsCorrect` server-side.
6. tạo StudentAnswer snapshot.
7. trả `correct` + `explanation`.

Không gửi `isCorrect` của options trong endpoint Student trước khi submit.

## 15. Error architecture

Dùng `ProblemDetails` thống nhất.

Shape mở rộng:

```json
{
  "type": "https://elearning.local/errors/lesson-locked",
  "title": "Lesson is locked",
  "status": 403,
  "code": "LESSON_LOCKED",
  "detail": "Bạn cần hoàn thành bài học trước đó.",
  "traceId": "00-..."
}
```

Error categories:

- Validation -> 400/422 theo convention đã chọn.
- Unauthenticated -> 401.
- Forbidden -> 403.
- Not found -> 404.
- Conflict -> 409.
- Rate limited -> 429.
- Unexpected -> 500, không lộ exception detail ở production.

## 16. Frontend architecture

### 16.1 Feature modules

```text
modules/
├── auth/
├── courses/
├── lessons/
├── students/
├── enrollments/
├── exercises/
└── progress/
```

Mỗi module có thể chứa:

- pages/views.
- feature components.
- composables.
- API functions.
- feature types.

Không tạo “global utils/service” cho logic chỉ thuộc một feature.

### 16.2 State ownership

- Auth user/session summary -> Pinia.
- Form state -> component-local.
- Page list/detail fetched data -> local/composable state theo page; không tự đưa toàn bộ vào Pinia.
- Router URL -> source of truth cho route id/filter/pagination khi phù hợp.
- Server data -> server là authoritative.

### 16.3 Axios

Một configured client:

- Base URL `/api`.
- `withCredentials: true`.
- Timeout hợp lý.
- Correlation header chỉ nếu hệ thống cần.
- Interceptor 401 xử lý session hết hạn.
- Không nuốt error.

## 17. Rich Text boundary

Lesson content là untrusted HTML dù người nhập là Admin.

Rules:

- Sanitization policy allowlist.
- Loại bỏ script, event handlers (`on*`), javascript URLs, dangerous embeds.
- Frontend chỉ dùng `v-html` trên nội dung đã qua sanitizer contract.
- Nếu sanitizer chạy server-side, vẫn coi CSP là defense-in-depth.

Không cho Admin lưu arbitrary script vì “Admin trusted”. Account Admin vẫn có thể bị compromise.

## 18. Video boundary

MVP hỗ trợ YouTube.

Server normalize URL về provider + video id hoặc validate chặt URL trước khi trả.

Frontend tự dựng embed URL từ provider allowlist.

Không nhúng trực tiếp raw URL vào `<iframe src>`.

Không hỗ trợ arbitrary iframe HTML.

## 19. Time

Toàn backend dùng UTC.

Ưu tiên `DateTimeOffset`/`TimeProvider`.

Không dùng `DateTime.Now` trong business logic.

Database timestamps có hậu tố semantic `Utc` trong code.

Frontend format timezone cho user khi cần; dữ liệu API giữ ISO-8601 UTC.

## 20. Pagination/filter/sort

List endpoints:

- `page` mặc định 1.
- `pageSize` mặc định 20.
- max 100.
- Sort field phải whitelist.
- Không cho client truyền arbitrary SQL/property expression.
- Search text trim + limit length.

Response đề xuất:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 42,
  "totalPages": 3
}
```

Count query + page query là chấp nhận được; không cần tối ưu giả định nếu chưa đo bottleneck.

## 21. Transactions/concurrency

### Atomic cases

Dùng transaction khi cần:

- reorder nhiều lesson.
- create question + options nếu không cùng một `SaveChanges` unit đủ.
- mutation nhiều bảng mà partial success gây invalid state.

### Idempotent cases

- Start lesson.
- Complete lesson.
- Enroll/reactivate Student vào Course.

Unique constraint là lớp bảo vệ race condition cuối cùng.

Nếu có concurrency conflict, trả 409 hoặc retry bounded khi operation an toàn; không loop retry vô hạn.

## 22. Caching

MVP không dùng Redis.

Không cache progress/enrollment/authorization state trước khi có bằng chứng cần thiết.

HTTP/browser caching có thể dùng cho static assets.

Nếu sau này thêm cache server-side, phải định nghĩa rõ:

- source of truth.
- TTL.
- invalidation.
- stale behavior.
- security isolation.

## 23. Observability

Minimum production signals:

- Structured application logs.
- Request trace/correlation id.
- HTTP status metrics qua hosting platform nếu có.
- Database errors/slow query visibility.
- Authentication failures ở mức aggregate, không log password.
- `GET /health` hoặc live/readiness endpoints phù hợp deployment.

Log phải đủ phân biệt:

- validation failure.
- unauthorized/forbidden.
- dependency/database failure.
- unexpected internal error.

## 24. Deployment architecture

Production recommended:

```text
Internet
  -> Reverse proxy / ingress
      -> HTTPS termination
      -> Vue static files
      -> /api -> ASP.NET Core
                   -> PostgreSQL
```

Same-origin giúp giảm CORS complexity.

Database không public internet nếu không có yêu cầu hạ tầng đặc biệt.

Production migration chạy như deployment step có kiểm soát; không auto-run destructive migration khi app startup.

## 25. Configuration

Config qua:

- `appsettings.json` cho non-secret defaults.
- environment-specific appsettings nếu phù hợp.
- environment variables/secret store cho secret.
- `.env.example` chỉ placeholder.

Không commit:

- DB password.
- production connection string có credential.
- admin default password.
- private key/token.

## 26. Dependency policy

Trước khi thêm package:

1. Xác nhận framework/BCL chưa có giải pháp đủ.
2. Kiểm tra maintenance/security/license.
3. Chỉ thêm khi giảm complexity thực tế.
4. Pin version theo lock/manifest.
5. Cập nhật docs nếu package trở thành architecture dependency.

Không thêm package chỉ để viết ít hơn vài dòng code rõ ràng.

## 27. Quy tắc thay đổi kiến trúc

Nếu task cần thay một invariant lớn như:

- cookie -> JWT/OIDC.
- monolith -> microservice.
- PostgreSQL -> database khác.
- derived progress -> persisted aggregate.
- manual complete -> score-threshold complete.

thì trước khi code phải cập nhật `DECISIONS.md` với:

- vấn đề.
- lựa chọn.
- lý do.
- tradeoff.
- migration impact.
- security impact.
- rollback/revisit trigger.
