# TESTING_AND_DOD.md — Test Strategy và Definition of Done

## 1. Mục tiêu

Test phải chứng minh **hành vi thật** của MVP, đặc biệt ở các boundary dễ sai: authentication, authorization, database constraint, learning path, progress và query behavior.

Không đánh đồng “unit tests xanh” với hệ thống đúng.

## 2. Test pyramid thực dụng

### Unit tests

Dùng cho logic thuần:

- progress percentage.
- learning path decision nếu tách pure function.
- question validation.
- answer grading logic.
- status transition.
- validation utility.

### Integration tests

Đây là lớp test quan trọng nhất của backend.

Dùng:

- ASP.NET Core `WebApplicationFactory`.
- PostgreSQL thật qua Testcontainers.
- authentication test helpers hợp lệ hoặc login thật tùy case.

Không dùng EF Core InMemory làm database integration oracle.

### Frontend unit/component tests

Dùng Vitest cho:

- composable logic.
- formatter/progress mapping.
- component behavior quan trọng.
- auth/error handling logic phù hợp.

Không cần snapshot test khổng lồ dễ vỡ.

### E2E

Dùng Playwright cho flow người dùng end-to-end quan trọng.

## 3. Backend integration test matrix

Mỗi feature phải chọn case phù hợp từ các nhóm:

### Success

- request hợp lệ -> đúng status/body/state.

### Authentication

- chưa login -> 401.
- session hết hạn/invalid -> 401.

### Authorization

- role sai -> 403.
- resource không thuộc actor -> denied.

### Validation

- field required thiếu.
- invalid enum/url/id.
- duplicate/conflict.

### Persistence

- DB state đúng sau mutation.
- unique constraint giữ invariant.

### Idempotency

- retry start/complete/enroll/archive không duplicate.

### Security

- JWT tamper/expiry/revocation.
- refresh replay/concurrency/hash-at-rest.
- over-posting.
- XSS sanitizer.

## 4. Authentication tests

Bắt buộc:

1. Student/Admin login đúng -> access JWT usable, refresh token chỉ ở HttpOnly cookie.
2. Sai password / unknown / disabled -> generic 401.
3. Lockout đạt threshold -> credential đúng vẫn bị reject tới hết lockout.
4. Anonymous -> 401; Student -> Admin endpoint 403.
5. JWT bị tamper/wrong signature/expired -> 401.
6. Refresh token chỉ lưu hash trong DB.
7. Refresh rotation -> token cũ single-use; concurrent consume chỉ một winner.
8. Replay token cũ ngoài grace -> revoke toàn family.
9. Logout -> access cũ + refresh session không dùng được.
10. Disable account -> access + refresh session hiện tại bị revoke tức thì.
11. Login/refresh vượt rate limit -> 429 theo policy.
12. Frontend 401 concurrency -> một refresh request mỗi tab; refresh failure không recursive/deadlock.

## 5. Course tests

- Admin create Course hợp lệ.
- Thiếu title -> validation error.
- Admin update.
- Admin publish/unpublish.
- Archive -> Student không thấy.
- Student chưa enroll -> không xem được.
- Student enroll Active + Course Published -> xem được.
- Draft Course -> Student không xem được.
- Admin course list count lesson/student đúng.

## 6. Lesson tests

- Admin create Lesson thuộc Course.
- Missing title/course invalid -> reject.
- Publish/Draft/Archive behavior.
- Sort order deterministic.
- Student chỉ thấy Published.
- First Published lesson -> access được.
- Next lesson khi predecessor chưa complete -> 403 `LESSON_LOCKED`.
- Next lesson sau predecessor complete -> access được.
- Draft lesson ở giữa không chặn published successor sai cách.
- Student cố nhập URL trực tiếp -> backend vẫn chặn.

## 7. Exercise tests

### Validation

- Multiple Choice <2 options -> reject.
- 0 correct -> reject.
- > 1 correct -> reject.
- True/False invalid options -> reject.

### Student

- Endpoint Student không trả `isCorrect` trước submit.
- Option đúng -> `correct=true`.
- Option sai -> `correct=false`.
- Explanation trả sau submit.
- Option thuộc question khác -> reject.
- Question thuộc lesson locked/not enrolled -> denied.
- Submit tạo StudentAnswer.
- Submit lần hai tạo attempt mới nhưng không sửa history cũ.

## 8. Progress tests

### Start

- first start -> one IN_PROGRESS row.
- start lần hai -> không duplicate, StartedAt không đổi.
- start Completed lesson -> vẫn Completed.

### Complete

- complete hợp lệ -> COMPLETED.
- CompletedAt set.
- complete lần hai -> không duplicate, CompletedAt ổn định.
- locked lesson -> không complete được.
- course chưa enroll -> không complete được.

### Percentage

- 0/0 -> 0%.
- 0/5 -> 0%.
- 1/5 -> 20%.
- 2/5 -> 40%.
- 5/5 -> 100%.
- Draft lesson không vào denominator.
- Publish thêm lesson làm denominator thay đổi đúng.

## 9. Enrollment tests

- Enroll first time -> one row Active.
- Enroll cùng pair lần hai -> không duplicate.
- Reactivate Inactive -> same pair Active.
- Archived Course -> reject enrollment mới.
- Student A không thấy Course chỉ Student B enroll.
- Concurrent duplicate enroll -> DB chỉ có một pair.

## 10. IDOR tests

Tạo ít nhất:

```text
Student A
Student B
Course A -> enroll A
Course B -> enroll B
```

Kiểm tra:

- A không read Course B.
- A không read Lesson B.
- A không submit Question B.
- A không read Progress B.
- A không dùng ID hợp lệ của B để vượt quyền.

## 11. Query/N+1 regression tests

Các endpoint ưu tiên có query-count proof:

- GET admin courses.
- GET student courses.
- GET admin progress.
- GET student course detail.

Test pattern:

1. Seed dataset nhỏ.
2. Capture số DbCommand.
3. Seed dataset lớn hơn trong cùng page shape.
4. Gọi endpoint.
5. Assert command count không tăng tuyến tính theo số row.

Không nhất thiết assert đúng 1 query; assert **bounded count**.

Ví dụ:

```text
5 courses  -> 2-4 commands
50 courses -> vẫn 2-4 commands
```

Threshold cụ thể được chốt theo implementation thực tế và ghi trong test để regression nhìn thấy.

## 12. Migration tests

CI hoặc integration setup phải chứng minh:

- PostgreSQL clean database tạo được.
- Apply tất cả migrations theo thứ tự thành công.
- App start/query được sau migration.
- Structural seed idempotent.
- Demo seed chỉ chạy environment cho phép.

Nếu migration destructive, phải có targeted test hoặc manual proof mô tả migration impact.

## 13. Frontend tests

Tối thiểu kiểm tra:

- login success/error rendering.
- route role guard UX.
- 401 global handling.
- progress rendering.
- lesson locked state.
- exercise submit đúng/sai.
- loading/empty/error state của page chính.

`npm run build` không thay thế `type-check` vì Vite build không phải type checker đầy đủ.

## 14. E2E MVP demo flow

Playwright phải có một happy-path gần flow demo:

```text
Admin login
-> create Python Basic
-> create 5 lessons hoặc dùng seed phù hợp
-> add video/content
-> create questions
-> create Student
-> enroll Student
-> logout
-> Student login
-> see Python Basic
-> open Lesson 1
-> submit exercise
-> complete Lesson 1
-> progress = 20%
-> logout
-> Admin login
-> progress report shows 1/5 = 20%
```

Nếu test setup dùng seed để rút ngắn bước create, phải có integration tests riêng chứng minh CRUD admin.

## 15. E2E negative flow

Ít nhất một E2E/integration representative path:

```text
Student login
-> cố mở Lesson 2 trước Lesson 1 complete
-> UI hiển thị locked/forbidden rõ
-> API trả 403
```

Không chỉ test disabled button.

## 16. Frontend console/network quality

Trước Done:

- không uncaught error.
- không Vue warning nghiêm trọng.
- không request loop.
- không repeated 401 storm.
- không duplicate mutation do double-click.
- không 404 asset/API do route config sai.

## 17. Static quality gates

Backend:

```text
dotnet restore
dotnet build -c Release
dotnet test
```

Frontend expected after the root lockfile has been generated and committed:

```text
npm ci
npm run type-check
npm run lint
npm run test
npm run build
```

Always use `npm ci` for deterministic dependencies. Do not keep feature development on an unlocked dependency graph.

Tên script có thể khác theo repo nhưng capability tương đương phải tồn tại.

E2E:

```text
npm run test:e2e
```

Không sửa CI bằng cách bỏ quality gate đang bắt lỗi thật.

## 18. Security quality gates

Trước release/demo có dữ liệu thật:

- refresh cookie Secure/HttpOnly/SameSite=Strict production.
- HTTPS.
- JWT key từ secret store, không fallback public.
- access/refresh credential không nằm localStorage/sessionStorage.
- CORS không wildcard credentials.
- demo credential không tồn tại production.
- Swagger/OpenAPI exposure phù hợp environment.
- logs không leak secret.
- dependency vulnerability review.
- Rich Text sanitizer test pass.

## 19. Performance quality gates MVP

Không đặt SLA latency tuyệt đối nếu chưa có môi trường benchmark chuẩn.

Thay vào đó bắt buộc structural performance:

- list pagination.
- max page size.
- no N+1.
- projection DTO.
- no lazy loading.
- no full Rich Text/exercise graph trong list.
- indexes cho join/access path chính.
- bounded query count.

Nếu performance thực tế trở thành acceptance criterion, tạo benchmark environment + p50/p95 target có hardware/data profile rõ.

## 20. Definition of Done — feature

Feature chỉ Done khi:

- Behavior đúng `PROJECT_SPEC.md`.
- API/UI contract nhất quán.
- Build pass.
- Unit/integration tests phù hợp pass.
- Authorization negative case có test nếu resource protected.
- Validation case có test.
- Không N+1 ở path list/aggregate liên quan.
- Migration có và test được nếu schema đổi.
- Frontend loading/error/empty hợp lý.
- Không console error nghiêm trọng.
- Security controls không bị bypass.
- Docs cập nhật nếu contract/decision thay đổi.

## 21. Definition of Done — toàn MVP

MVP hoàn thành khi:

- Project chạy local theo README.
- `.env.example` đầy đủ placeholder.
- PostgreSQL connect.
- Migration chạy từ DB mới.
- Seed demo Development/Demo chạy.
- Admin login.
- Student login.
- Admin CRUD Course.
- Admin CRUD Lesson.
- Admin tạo Question.
- Admin tạo/disable Student.
- Admin enroll Course cho Student.
- Student chỉ thấy Course được cấp.
- Student xem lesson/video/content.
- Learning path backend enforce.
- Student làm exercise và xem result.
- Student complete lesson.
- Course progress đúng.
- Admin xem progress.
- Role/resource authorization đúng.
- Security baseline pass.
- Query baseline/no N+1 pass.
- E2E demo flow pass.
- README/architecture/security/handover docs hiện hành.
- Source code được push Git.

## 22. Không được gọi là Done nếu

- chỉ “chạy trên máy dev” nhưng test fail.
- frontend chặn route nhưng API vẫn mở.
- dùng hard-coded user/role để demo.
- có N+1 đã biết.
- list load toàn table.
- production cần demo password mặc định.
- migration không apply được clean DB.
- error production lộ stack trace/secret.
- Student có thể truy cập ID của người khác.
- test bị skip/delete để CI xanh.

## 23. Proof Pack khi bàn giao task lớn

Task lớn nên ghi ngắn trong PR/report:

- Mục tiêu.
- Files/contract thay đổi.
- Migration nếu có.
- Test commands đã chạy.
- Kết quả integration/E2E.
- Query/performance evidence nếu liên quan.
- Security negative cases.
- Known limitations.
- Rollback/revert note nếu thay đổi rủi ro.
## Learning Guardrails DoD — 2026-08-29

Required automated coverage:

- Complete before Start -> rejected.
- Video lesson without trusted duration -> cannot be completed.
- Heartbeat cannot jump far beyond elapsed wall time.
- Heartbeat cannot advance past an unanswered checkpoint.
- Checkpoint answer before its timestamp -> rejected.
- Reinforcement answer before video completion -> rejected.
- Reinforcement score at or below 80% -> complete rejected.
- Score above 80%, video complete and checkpoints passed -> complete succeeds.
- Completed lesson remains idempotently completed.
- Query-count/load regression checks remain bounded.

Required browser/E2E coverage:

- forward seek snaps back;
- rewind works;
- tab/window switch pauses video;
- checkpoint modal appears at configured timestamp and blocks continuation until correct;
- reinforcement cards remain locked until video completion;
- completion button remains disabled until all server-visible conditions are satisfied.
