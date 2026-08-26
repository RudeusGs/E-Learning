# HANDOVER.md — Bàn giao, vận hành và tiếp nhận dự án

## 1. Mục tiêu

Tài liệu này giúp một developer/AI agent mới nhận repository có thể:

- hiểu hệ thống;
- chạy local;
- biết cấu hình nào là secret;
- chạy migration/seed/test;
- demo MVP;
- deploy có kiểm soát;
- biết chỗ nào không được tự ý thay đổi.

README chính của repo nên chứa hướng dẫn ngắn. File này giữ hướng dẫn vận hành chi tiết hơn.

## 2. Thứ tự đọc khi nhận dự án

2. `PROJECT_SPEC.md`
3. `ARCHITECTURE.md`
4. `SECURITY.md`
5. `DATABASE.md`
6. `API_CONTRACT.md`
7. `ENGINEERING_STANDARDS.md`
8. `TESTING_AND_DOD.md`
9. `DECISIONS.md`
10. README + code hiện tại

## 3. Prerequisites local

Expected:

- .NET SDK 10.x
- Node.js LTS phù hợp `package.json`/lockfile
- npm hoặc package manager đã chốt trong repo
- Docker + Docker Compose
- Git

Không tự đổi npm -> pnpm/yarn hoặc ngược lại giữa task.

Luôn dùng `npm ci` cho deterministic dependencies. CI và client Dockerfile yêu cầu `package-lock.json` phải tồn tại. Không xóa lockfile.

## 4. Environment variables

Root `.env` được Docker Compose đọc; nó chỉ chứa local/demo placeholders theo `.env.example`. Vite overrides nằm trong `client/.env.local`, bắt đầu từ `client/.env.example`.

Khi persistence/auth được implement, server secret cho chạy trực tiếp bằng `dotnet run` phải đi qua environment variables, .NET user-secrets hoặc secret store phù hợp. Không dựa vào việc ASP.NET Core tự đọc file `.env` vì framework không làm việc đó mặc định.

Production secret không commit Git.

## 5. Local database

Khuyến nghị local dùng Docker Compose PostgreSQL.

Flow dự kiến:

```bash
docker compose up -d postgres
```

Skeleton hiện chưa có `DbContext` hoặc migration. Sau khi persistence slice tạo migration thật, apply từ root bằng command:

```bash
dotnet ef database update \
  --project server/src/Elearning.Infrastructure \
  --startup-project server/src/Elearning.Api
```

Nếu repo có wrapper script/Makefile thì README phải ưu tiên command chính thức đó.

## 6. Backend local

Expected flow:

```bash
dotnet tool restore
dotnet restore Elearning.sln
dotnet build Elearning.sln
dotnet run --project server/src/Elearning.Api
```

Backend Development phải expose OpenAPI theo policy project để debug/bàn giao.

Không bật production secrets trong local source.

## 7. Frontend local

Expected:

```bash
npm ci
npm run dev:client
```

Default dev flow dùng Vite proxy `/api`, nên browser vẫn gọi same-origin và backend không cần mở CORS. Nếu sau này có direct cross-origin development flow, chỉ allow origin cụ thể; không dùng wildcard credentials. Production vẫn ưu tiên same-origin.

## 8. Seed

### Structural seed

- ADMIN role.
- STUDENT role.

Phải idempotent.

### Demo seed

Development/Demo có thể tạo:

- Admin demo.
- Student demo.
- Python Basic.
- 5 lessons.
- 3 questions/lesson.

Credential demo phải được đánh dấu rõ environment và không có production.

## 9. Cách chạy test

Backend:

```bash
dotnet test Elearning.sln
```

Frontend, từ `client/`:

```bash
npm run type-check
npm run lint
npm run test
npm run build
```

E2E theo script repo, ví dụ:

```bash
npm run test:e2e
```

Integration tests dùng Testcontainers cần Docker đang hoạt động.

## 10. Smoke test sau khi setup

Ở trạng thái skeleton, smoke test chỉ xác nhận foundation:

1. `GET http://localhost:8080/api/health` trả healthy response.
2. Mở `http://localhost:5173` và thấy Vue application shell.
3. `GET http://localhost:5173/api/health` đi qua Nginx reverse proxy khi chạy Docker Compose.

Không yêu cầu login/course/progress ở trạng thái skeleton. Các smoke flow nghiệp vụ chỉ có hiệu lực sau khi feature tương ứng đã được implement.

## 11. Flow demo chính thức

Flow acceptance:

```text
Admin login
-> Create Python Basic
-> Create Lesson 1
-> Add YouTube video/content
-> Create 3 questions
-> Create Nguyễn Văn A
-> Enroll Nguyễn Văn A to Python Basic
-> Admin logout
-> Student login
-> Student sees Python Basic
-> Student opens Lesson 1
-> Student answers exercise
-> Student completes Lesson 1
-> Progress 0% -> 20% for 5 lessons
-> Admin login
-> Admin progress shows 1/5 = 20%
```

Nếu flow này fail thì MVP chưa đủ để bàn giao.

## 12. Production deployment assumptions

Preferred topology:

```text
HTTPS reverse proxy
  -> Vue static app
  -> /api -> ASP.NET Core
             -> private PostgreSQL
```

Production checklist:

- HTTPS certificate valid.
- HSTS active.
- Refresh cookie Secure/HttpOnly/SameSite=Strict.
- Access JWT không persisted trong browser storage.
- JWT signing key lấy từ production secret store; startup fail nếu thiếu/yếu.
- CORS không wildcard credentials.
- CSP/security headers hoạt động với Vue + YouTube.
- Production DB credential từ secret store/environment.
- Demo seed disabled.
- Logging không leak secret.
- Backup configured.
- Health check integrated.

## 13. Migration deployment

Không auto-run destructive migration trong production app startup.

Deployment sequence cơ bản:

1. Backup/verify restore point theo policy.
2. Review migration SQL nếu thay schema quan trọng.
3. Apply migration.
4. Deploy server/frontend compatible version.
5. Run smoke checks.
6. Monitor errors/DB failures.

Nếu migration không backward compatible và cần zero-downtime, phải có plan expand/contract riêng; MVP không giả định tự động giải quyết.

## 14. Rollback

Rollback application code chỉ an toàn nếu database schema vẫn tương thích.

Trước migration destructive phải ghi:

- dữ liệu nào mất/thay đổi;
- backup ở đâu;
- code cũ có chạy trên schema mới không;
- rollback thực tế là code revert hay DB restore.

Không hứa “rollback bằng git revert” nếu migration đã làm mất dữ liệu.

## 15. Operational logs

Khi debug production-like issue, bắt đầu từ:

- traceId user nhận được;
- HTTP route/status/time;
- server exception log;
- DB connectivity/query issue;
- auth/authorization denial category;
- deployment/migration version.

Không yêu cầu người dùng gửi password/auth cookie.

## 16. Common failure checklist

### Login 401

Kiểm tra:

- credential/demo seed đúng environment?
- account Active?
- access token còn hợp lệ và Bearer header có được gắn?
- refresh HttpOnly cookie còn tồn tại?
- HTTPS/Secure/SameSite cookie mismatch?
- Identity status/security stamp?
- rate limit/lockout?

### API 403

Kiểm tra:

- role?
- enrollment?
- course/lesson Published?
- learning path predecessor?

Không “fix” 403 bằng cách bỏ `[Authorize]`.

### Refresh/session fail

Kiểm tra:

- refresh cookie có bị browser chặn vì Secure/SameSite/path?
- `/api/auth/refresh` trả `INVALID_REFRESH_TOKEN`, `REFRESH_RETRY_REQUIRED` hay reuse detection?
- access JWT có bị blacklist/session stamp invalidated?
- frontend có dùng auth client riêng để tránh recursive refresh?

### Course list chậm

Kiểm tra:

- N+1?
- projection?
- pagination?
- index?
- `Include`/content graph thừa?

Không thêm Redis trước khi đo query.

### Student thấy content không nên thấy

Đây là security incident/bug ưu tiên cao.

Kiểm tra backend authorization trước frontend route guard.

## 17. Dependency upgrades

Khi nâng .NET/Vue/package major:

1. Đọc release/breaking changes chính thức.
2. Update lockfiles.
3. Build/test all.
4. Run auth/security/E2E regression.
5. Không trộn với feature lớn nếu tránh được.

## 18. Ownership map

Khi bug thuộc:

- Login/session -> Auth.
- Course visibility -> Course + Enrollment authorization.
- Lesson locked sai -> Lesson + Progress learning-path logic.
- Answer grading -> Exercise.
- % sai -> Progress query.
- Admin report chậm -> Progress projection/index.
- XSS/video iframe -> Security/content boundary.
- Query explosion -> Data access/performance rule.

Dùng map này để đọc đúng module, không refactor toàn hệ thống.

## 19. Handover package tối thiểu cho sếp/team mới

Phải có:

- Git repository sạch.
- README.
- Root AI/docs bộ này.
- `.env.example`.
- Docker Compose hoặc setup DB rõ.
- Migrations.
- Seed Development/Demo.
- Demo credentials tách khỏi production.
- OpenAPI.
- Test commands.
- E2E demo flow.
- Screenshot/demo video nếu team cần.
- Known limitations/non-goals.
- Deployment notes.
- Backup/restore owner.

## 20. Known non-goals cần nhắc khi bàn giao

MVP không bao gồm:

- livestream/chat/payment/certificate;
- mobile app;
- notification/email automation;
- AI;
- forum;
- file assignment;
- coding judge;
- exam proctoring/countdown/random question;
- leaderboard/attendance;
- parent/teacher/multi-level roles.

Không đánh giá MVP thiếu các feature này nếu scope chưa đổi.

## 21. Cách AI agent tiếp tục dự án an toàn

Khi nhận task mới:

1. Đọc module docs liên quan.
2. Xác nhận baseline build/test.
3. Tìm entrypoint/query/path nhỏ nhất.
4. Không redesign toàn repo.
5. Viết test cho behavior + negative security case.
6. Implement smallest coherent change.
7. Chạy targeted test rồi full relevant suite.
8. Review query count/data exposure.
9. Update docs nếu contract đổi.
10. Báo rõ residual risk thay vì giấu bằng fallback.

## 22. Tiêu chí bàn giao tốt

Một developer mới phải có thể trả lời trong thời gian ngắn:

- Hệ thống làm gì?
- Ai có quyền gì?
- Progress tính thế nào?
- Lesson mở khóa thế nào?
- Auth/session hoạt động ra sao?
- Schema nằm đâu?
- API lỗi trả kiểu gì?
- Làm sao chạy local?
- Làm sao migrate/seed/test?
- Làm sao biết Student không vượt quyền?
- Làm sao tránh N+1?
- Demo MVP theo flow nào?

Nếu docs/code không giúp trả lời các câu này, handover chưa đạt.
