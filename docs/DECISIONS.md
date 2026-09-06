# DECISIONS.md — Architecture Decision Record tóm tắt

Tài liệu này giữ các quyết định quan trọng để AI agent không tự “thiết kế lại” dự án trong từng task nhỏ.

Mỗi quyết định chỉ đổi khi có requirement/evidence đủ mạnh. Khi đổi, thêm ADR mới thay vì xóa lý do cũ.

---

## ADR-001 — Modular Monolith

**Status:** Accepted

### Context

MVP có các module Auth, Course, Lesson, Student, Enrollment, Exercise và Progress dùng chung workflow và database.

### Decision

Dùng một ASP.NET Core application theo modular monolith và một PostgreSQL database.

### Không chọn

- Microservices.
- Message broker.
- Distributed transactions.

### Lý do

- Deployment/debug đơn giản.
- Transaction local.
- Dễ bàn giao.
- Không có scale/organizational boundary chứng minh cần microservices.

### Revisit khi

Có team/deployment boundary thật, independent scaling thật hoặc domain isolation có bằng chứng.

---

## ADR-002 — ASP.NET Core Identity + secure cookie cho browser SPA

**Status:** Superseded by ADR-013

### Decision

Dùng ASP.NET Core Identity và cookie authentication cho Vue SPA cùng origin.

### Không chọn mặc định

JWT lưu localStorage/sessionStorage.

### Lý do

- Giảm exposure token với XSS.
- Browser session phù hợp web app cùng origin.
- Identity giải quyết password hashing, lockout, security stamp.

### Tradeoff

Cookie auth yêu cầu CSRF/antiforgery protection.

### Revisit khi

Có mobile app, third-party API client, cross-domain architecture hoặc OIDC/SSO requirement.

---

## ADR-003 — Không Generic Repository cho EF Core

**Status:** Accepted

### Decision

Không tạo `IGenericRepository<T>`/`GenericRepository<T>` bọc CRUD EF Core.

### Lý do

- DbContext/DbSet đã cung cấp unit-of-work/repository semantics.
- Generic wrapper thường che mất query composition/projection.
- Dễ tạo abstraction không phản ánh authorization/performance của domain.

### Revisit khi

Có storage boundary thật cần thay implementation hoặc test seam có giá trị cụ thể.

---

## ADR-004 — Lazy Loading bị cấm

**Status:** Accepted

### Decision

Không bật EF Core lazy-loading proxy.

### Lý do

- Tránh hidden database roundtrip.
- Giảm nguy cơ N+1.
- Query behavior nhìn thấy trong source.

### Pattern thay thế

- Projection.
- Eager loading có chủ đích.
- Explicit query.

---

## ADR-005 — Course progress là derived state

**Status:** Accepted

### Decision

Không lưu `CourseProgressPercent` như source of truth.

Progress tính từ:

```text
Completed Published Lessons / Total Published Lessons
```

### Lý do

- Tránh stale progress khi publish/unpublish lesson.
- Không cần invalidation/cross-table update.
- Một nguồn sự thật.

### Tradeoff

Admin progress query cần aggregate DB tốt và index phù hợp.

### Revisit khi

Dataset/traffic thực tế chứng minh aggregate query là bottleneck và có benchmark trước/sau.

---

## ADR-006 — Learning unlock là derived state

**Status:** Accepted

### Decision

Không lưu `Lesson.IsUnlocked` theo từng Student.

Lesson mở nếu là first Published lesson hoặc predecessor Published lesson đã Completed.

### Lý do

- Tránh stale unlock state.
- Learning path thay đổi theo publish/order mà không phải backfill.

### Revisit khi

Future learning path trở thành graph/prerequisite phức tạp thay vì tuyến tính.

---

## ADR-007 — Manual complete trong MVP

**Status:** Accepted

### Decision

Student chủ động “Đánh dấu hoàn thành” lesson. Exercise score >=70% **không phải điều kiện bắt buộc** của MVP.

### Lý do

Đây là flow tối thiểu phù hợp yêu cầu gốc; score threshold chỉ được nêu như lựa chọn chặt chẽ hơn.

### Revisit khi

Product Owner yêu cầu completion rule theo score/video/view time.

---

## ADR-008 — Archive thay vì hard delete cho Course/Lesson có history

**Status:** Accepted

### Decision

MVP ưu tiên `ARCHIVED` để đáp ứng hành vi xóa/ẩn mà không mất enrollment/progress/answer history.

### Lý do

- Bảo toàn dữ liệu học tập.
- Giảm cascade-delete risk.
- Dễ audit/bàn giao.

### Tradeoff

Query phải filter status đúng.

### Revisit khi

Có formal retention/deletion policy yêu cầu physical deletion.

---

## ADR-009 — Mỗi answer submit là một attempt

**Status:** Accepted

### Decision

Mỗi lần Student submit đáp án tạo một `StudentAnswer` mới.

`IsCorrect` là snapshot tại thời điểm submit.

### Lý do

- Requirement yêu cầu lưu kết quả làm bài.
- Hỗ trợ retry mà không mất lịch sử.
- Admin sửa đáp án sau này không làm lịch sử đổi ngược.

### Revisit khi

Product Owner yêu cầu chỉ lưu latest answer hoặc grading model khác.

---

## ADR-010 — Không Redis/cache server-side trong MVP

**Status:** Accepted

### Decision

Không dùng Redis hoặc cache enrollment/progress/auth data mặc định.

### Lý do

- Chưa có bottleneck đo được.
- Cache thêm invalidation/security complexity.
- Query/index đúng đủ cho MVP.

### Revisit khi

Profiling production-like chứng minh database/query là bottleneck và có cache contract rõ.

---

## ADR-011 — PostgreSQL integration tests, không EF InMemory

**Status:** Accepted

### Decision

Backend integration test dùng PostgreSQL thật qua Testcontainers.

### Lý do

Cần chứng minh behavior thực của constraint, transaction, provider, SQL translation và migration.

### Revisit khi

Không cần; unit tests vẫn có thể dùng fake/pure logic nhưng integration oracle phải là provider thật.

---

## ADR-012 — Same-origin deployment preferred

**Status:** Accepted

### Decision

Production ưu tiên Vue và API cùng origin, API ở `/api`.

### Lý do

- CORS đơn giản hơn.
- Same-origin giảm bề mặt CORS và giữ refresh cookie trong first-party context.
- Bàn giao/deploy dễ hơn.

### Revisit khi

Frontend/API buộc deploy ở domain khác hoặc có multi-client requirement.

---

---

## ADR-013 — JWT access token in-memory + hashed rotating refresh cookie

**Status:** Accepted

### Context

Ứng dụng browser SPA cần JWT Bearer cho API nhưng không được lưu credential dài hạn trong `localStorage`/`sessionStorage`. Refresh token phải có khả năng revoke, rotate và phát hiện replay.

### Decision

- Password hashing/lockout dùng ASP.NET Core Identity.
- Access token là JWT HS256, lifetime ngắn, frontend chỉ giữ trong memory và gửi qua `Authorization: Bearer`.
- Refresh token là opaque random 256-bit+, chỉ lưu trong `Secure + HttpOnly + SameSite=Strict` cookie.
- Database chỉ lưu SHA-256 hash của refresh token, không lưu plaintext.
- Refresh token là single-use; rotation dùng atomic conditional update.
- Mỗi login tạo một token family. Replay ngoài concurrent grace window revoke toàn family và blacklist access JWT còn sống.
- Logout revoke family + blacklist access JWT hiện tại.
- Disable account update security stamp, revoke toàn session; JWT validation kiểm tra account status + hash security stamp.
- Production/Staging không có signing-key fallback; thiếu `Jwt:Key` phải fail startup.

### Consequences

- Page reload phải bootstrap session qua `/api/auth/refresh`.
- Multi-tab refresh có thể race; backend trả `409 REFRESH_RETRY_REQUIRED` trong grace window để frontend retry một lần bằng cookie mới.
- Migration hardening token tables làm logout toàn bộ session hiện có một lần; không ảnh hưởng dữ liệu nghiệp vụ.
- CSRF truyền thống không áp dụng cho API Bearer requests; refresh/logout phụ thuộc first-party `SameSite=Strict` cookie và same-origin deployment.

### Revisit when

Có mobile app, third-party API client, OIDC/SSO, multi-domain frontend hoặc yêu cầu MFA/passkey bắt buộc.

## Cách thêm quyết định mới

Template:

```markdown
## ADR-XXX — Tên quyết định

**Status:** Proposed | Accepted | Superseded

### Context

...

### Decision

...

### Alternatives

...

### Consequences

...

### Revisit when

...
```

Chỉ thêm ADR cho quyết định ảnh hưởng architecture/data/security/contract lâu dài. Không dùng ADR cho tên button hoặc refactor nhỏ.
## ADR — Server-authoritative lesson completion guardrails (2026-08-29)

Status: Accepted. Supersedes the previous MVP manual-complete/no-threshold convention.

Completion is derived from server records, not a browser button. Video watch state is tracked by bounded heartbeats, checkpoint questions can gate playback, reinforcement is unlocked after video completion, and the required reinforcement threshold is strictly greater than 80%.

We intentionally do not claim YouTube is tamper-proof. Browser-side seek prevention is UX enforcement; server-side eligibility is the security boundary. High-stakes anti-tamper video would require controlled media delivery rather than public YouTube.
