# SECURITY.md — Security Baseline cho E-Learning MVP

## 1. Mục tiêu

Security là yêu cầu bắt buộc của dự án, không phải bước “hardening sau cùng”. Mọi feature mới phải giữ các invariant trong tài liệu này.

Threat model chính của MVP:

- Credential stuffing/brute-force login.
- Student truy cập dữ liệu Admin.
- Student truy cập course/lesson chưa được cấp.
- Student bypass learning path bằng URL/API trực tiếp.
- IDOR/BOLA do chỉ kiểm tra ID tồn tại.
- XSS có thể chiếm quyền thao tác trong origin; refresh secret phải không đọc được từ JavaScript.
- Refresh-token theft/replay và session tiếp tục sống sau logout/disable.
- XSS từ Rich Text lesson content.
- Arbitrary iframe/video URL.
- Mass assignment/over-posting.
- SQL injection/query abuse.
- Secret leakage trong Git/log/error.
- Session tiếp tục hoạt động sau khi account bị disable.
- Dependency vulnerability.
- Unbounded request/query gây resource exhaustion.

## 2. Identity và password

Dùng ASP.NET Core Identity làm identity store và password hashing implementation.

Không:

- Tự viết password hash.
- Lưu plaintext/reversible password.
- Trả password hash qua API.
- Log credential.

Password policy phải được cấu hình explicit trong startup để người bàn giao biết behavior, không phụ thuộc mơ hồ vào default.

Baseline đề xuất cho môi trường trường học:

- Minimum length >= 10 ký tự.
- Không ép rule ký tự quá phức tạp nếu làm giảm usability; ưu tiên length.
- Lockout sau số lần login sai liên tiếp hợp lý, ví dụ 5 lần.
- Lockout window ví dụ 15 phút.
- Error login chung: “Email hoặc mật khẩu không chính xác.”

Các con số là configuration và có thể điều chỉnh, nhưng không được disable lockout/rate limiting production mà không có lý do.

## 3. Authentication (JWT + rotating refresh)

Production invariants:

- Password verification/hashing dùng ASP.NET Core Identity; không tự hash password.
- Access JWT lifetime ngắn, frontend chỉ giữ trong memory, không `localStorage`/`sessionStorage`.
- Access JWT gửi qua `Authorization: Bearer <token>`.
- Refresh token opaque random >=256-bit, chỉ nằm trong `Secure + HttpOnly + SameSite=Strict` cookie.
- Database chỉ lưu SHA-256 hash refresh token.
- Refresh token single-use, rotation atomic; replay ngoài grace window revoke toàn token family.
- Signing key production/staging bắt buộc từ secret store/environment và >=32 bytes; app phải fail startup nếu thiếu/yếu.
- Không log access token, refresh token, auth request body, password hoặc signing key.
- Refresh rotation giữ absolute session expiry; không sliding vô hạn.

## 4. Account disable/session invalidation

Khi Admin disable Student:

1. Set business status `DISABLED`.
2. Update Identity security stamp.
3. Revoke toàn bộ refresh token của user.
4. Blacklist access `jti` còn sống đã phát hành từ các session được biết.
5. JWT validation vẫn kiểm tra `Status == ACTIVE` và security-stamp hash để revoke tức thì ngay cả khi blacklist thiếu row.
6. Login/refresh mới phải reject.

Security-sensitive account change khác (password reset, role change nếu thêm sau này) phải update security stamp và revoke session theo cùng invariant.

## 5. Authorization

### 5.1 Role authorization

- `/api/admin/*` yêu cầu Admin.
- `/api/student/*` yêu cầu Student hoặc policy cụ thể.

Không dựa vào route frontend.

### 5.2 Resource authorization

Student access Course phải kiểm tra:

- Current user chính là Student request.
- User Active.
- Enrollment `(currentUserId, courseId)` Active.
- Course Published.

Student access Lesson phải kiểm tra thêm:

- Lesson thuộc course Student được học.
- Lesson Published.
- Learning path predecessor Completed.

Submit answer phải kiểm tra:

- Question tồn tại.
- Question thuộc lesson Student được truy cập.
- Option thuộc đúng Question.

Không chấp nhận `studentId` từ body cho các API Student nếu có thể suy ra từ authenticated principal.

Ví dụ **không làm**:

```json
POST /api/questions/12/answer
{
  "studentId": 15,
  "optionId": 10
}
```

StudentId phải lấy từ auth context.

## 6. IDOR/BOLA prevention

Mỗi endpoint đọc/sửa resource theo ID phải trả lời câu hỏi:

> Current actor có quyền với resource này không?

Không dùng “ID khó đoán” làm security control.

Ưu tiên query scoped ngay từ database:

```text
WHERE ResourceId = @id
AND Enrollment.StudentId = @currentUser
AND Enrollment.Status = Active
...
```

Test bắt buộc dùng ít nhất 2 student để chứng minh Student A không truy cập dữ liệu Student B.

## 7. CSRF

Business API dùng Bearer header nên browser không tự gửi access credential trong cross-site request. Refresh cookie dùng `SameSite=Strict` và auth endpoints chỉ dùng same-origin deployment contract. Không thêm antiforgery giả tạo cho Bearer API; nếu chuyển auth sang access cookie/cross-site cookie thì phải review CSRF lại.

## 8. CORS

Production cùng origin -> không cần mở CORS rộng.

Development chỉ allow origin chính xác của Vite, ví dụ:

```text
http://localhost:5173
```

Nếu credentials bật:

- Không dùng `AllowAnyOrigin`.
- Không phản chiếu origin tùy ý.
- Chỉ whitelist environment-controlled origins.

## 9. Rate limiting

Dùng ASP.NET Core rate limiting middleware ở các endpoint có risk abuse.

Bắt buộc ưu tiên:

- Login, partition theo client IP để một IP không dùng hết quota của toàn trường.
- Refresh/logout auth-sensitive endpoints.
- Endpoint search/list tốn tài nguyên nếu có abuse signal.

Khi rate limit:

- trả 429.
- có thể gửi `Retry-After`.
- log aggregate/rate-limit event nhưng không log credential.

Rate limit không thay thế lockout và không được coi là DDoS protection hoàn chỉnh.

## 10. Input validation

Server validate toàn bộ untrusted input.

### General

- Trim string nơi phù hợp.
- Giới hạn length explicit.
- Reject invalid enum.
- Reject invalid ID <=0 nếu dùng numeric ID.
- URL validate bằng parser, không regex đơn giản.
- Pagination bounded.
- Sort/filter whitelist.

### Mass assignment

Không bind EF entity trực tiếp từ request body.

Dùng request DTO chỉ chứa field cho phép sửa.

Ví dụ Student update DTO không được có:

- Role nếu endpoint không cho đổi role.
- PasswordHash.
- SecurityStamp.
- IsAdmin.
- CreatedAt.

## 11. Rich Text / XSS

Lesson content có thể là Rich Text và phải coi là untrusted HTML.

Sanitizer policy phải allowlist rõ:

Có thể cho phép tùy UI:

- p, br.
- strong, em.
- ul, ol, li.
- h1-h6 ở mức cần thiết.
- pre, code.
- blockquote.
- a với safe href.

Không cho phép mặc định:

- script.
- iframe trong rich text.
- object/embed.
- form/input.
- style tùy ý nếu chưa có sanitizer CSS an toàn.
- `onload`, `onclick`, mọi `on*` event.
- `javascript:` URL.
- dangerous data URLs.

Nếu lưu raw + sanitized version, phải định nghĩa rõ version nào render. MVP ưu tiên lưu/render sanitized content để giảm ambiguity.

Frontend chỉ dùng `v-html` với field đã qua sanitizer contract.

## 12. Video URL / iframe

MVP chỉ hỗ trợ YouTube.

Backend phải:

- parse URL.
- xác định provider supported.
- extract/normalize video ID.
- reject unknown domain/schema.

Frontend dựng embed URL từ normalized video id/provider.

Không làm:

```html
<iframe :src="lesson.videoUrl"></iframe>
```

nếu `videoUrl` là raw arbitrary user input.

CSP `frame-src` chỉ allow YouTube domain cần thiết.

## 13. Security headers

Production reverse proxy/app phải thiết lập hợp lý:

- Strict-Transport-Security.
- Content-Security-Policy.
- X-Content-Type-Options: nosniff.
- Referrer-Policy.
- Frame ancestors policy qua CSP.
- Permissions-Policy tối thiểu theo nhu cầu.

Không dùng `X-Frame-Options: ALLOWALL`.

CSP phải được test với YouTube embed và frontend build; không mở `script-src *` để chữa CSP error.

## 14. HTTPS và proxy

Production bắt buộc HTTPS.

Nếu chạy sau reverse proxy:

- Configure forwarded headers chỉ cho known proxies/networks.
- Không trust arbitrary `X-Forwarded-*` từ Internet.
- HSTS ở production.
- Cookie Secure luôn true production.

## 15. Secrets

Không commit secret.

`.env.example` chỉ chứa key + placeholder:

```text
DB_HOST=
DB_PORT=
DB_NAME=
DB_USER=
DB_PASSWORD=
```

Production secrets qua secret manager/environment của hạ tầng.

Nếu secret từng commit:

- Không chỉ xóa file.
- Rotate/revoke secret.
- Xem xét Git history exposure.

## 16. Logging và privacy

Không log:

- Password.
- Password hash.
- Auth JWT Token.
- Connection string có password.
- Full request body cho auth endpoints.
- Student answer/content cá nhân nếu không cần debug.

Nên log:

- TraceId.
- Route/HTTP method/status.
- Duration.
- Current user ID dạng internal identifier khi cần audit, không email nếu không cần.
- Admin mutation quan trọng.
- Authorization denial category ở mức vừa đủ.

## 17. Error handling

Production:

- Không trả stack trace.
- Không trả SQL error/raw exception.
- Không tiết lộ account existence trong login/reset-like flows.
- Trả ProblemDetails sanitized + traceId.

Log server có exception detail theo access policy phù hợp.

## 18. Database security

- App DB user dùng least privilege phù hợp runtime.
- Migration credential có thể tách quyền cao hơn nếu hạ tầng cho phép.
- PostgreSQL không expose public Internet nếu không cần.
- Backup được mã hóa/giới hạn access theo policy hạ tầng.
- Unique constraints chống race ở enrollment/progress.
- Không dùng string concatenation tạo SQL.
- Raw SQL nếu cần phải parameterized và reviewed.

## 19. Dependency security

Mỗi dependency mới:

- Nguồn package chính thức.
- Version được pin theo project manifest/lock.
- License phù hợp.
- Không dùng package abandoned khi framework có giải pháp native.

CI nên có vulnerability scan định kỳ và dependency update review.

Không auto-upgrade major version trong cùng feature task nếu không cần.

## 20. File upload

MVP hiện **không cần file upload assignment/video**.

Thumbnail MVP ưu tiên URL.

Nếu task mới thêm upload, security design bắt buộc gồm:

- size limit.
- content type + file signature validation.
- filename normalization.
- storage outside executable web root hoặc object storage.
- randomized storage key.
- malware scanning theo risk.
- authorization download.

Không tự thêm upload trước khi requirement có.

## 21. Data exposure minimization

DTO chỉ trả field cần cho màn hình.

Student endpoint không được trả:

- Correct answer trước submit.
- Password/security fields.
- Enrollment/student data của người khác.
- Draft content.

Admin endpoint cũng không trả password hash/security stamp.

## 22. Security test matrix bắt buộc

### Authentication

- Unauthenticated internal endpoint -> 401.
- Wrong password -> failure generic.
- Disabled user -> login reject.
- Logout -> old session không dùng được.
- Login rate limit -> 429 khi vượt policy.

### Authorization

- Student -> Admin endpoint: 403.
- Student A -> Course chỉ Student B enroll: denied.
- Student -> Draft course: denied.
- Student -> Locked lesson: 403 `LESSON_LOCKED`.
- Student -> Question/Option không thuộc accessible lesson: denied.

### Session/token lifecycle

- Refresh token không xuất hiện trong JSON/localStorage.
- Refresh hash-at-rest đúng.
- Concurrent refresh chỉ một consume thành công.
- Replay ngoài grace revoke token family.
- Logout revoke access + refresh.
- Disable account revoke session tức thì.

### XSS

Sanitizer test payload tối thiểu:

```html
<script>
  alert(1)
</script>
<img src="x" onerror="alert(1)" />
<a href="javascript:alert(1)">x</a>
```

Output render không được giữ executable payload.

### Over-posting

Gửi field `role`, `passwordHash`, `studentId` ngoài contract phải bị ignore/reject tùy DTO binding, không thay quyền.

## 23. Security review trigger

Phải review lại `SECURITY.md` nếu thêm:

- OAuth/OIDC/SSO.
- Reset password/email verification.
- File upload.
- External storage/CDN.
- Email/SMS.
- Payment.
- AI integration.
- Teacher/Parent role.
- Multi-school/multi-tenant.
- Public sharing link.
- Mobile app/token auth.

Không copy security design hiện tại sang feature mới nếu trust boundary thay đổi.
## Learning interaction abuse controls — 2026-08-29

Video heartbeat and answer-submit endpoints use the `student-interaction` fixed-window limiter partitioned by authenticated user id (default 120 requests/minute). This is an abuse ceiling, not the primary anti-skip mechanism; trusted video advancement is still capped by server elapsed time and checkpoint state.
