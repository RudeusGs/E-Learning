# API_CONTRACT.md — REST API Contract

## 1. Nguyên tắc

API phục vụ Vue SPA và phải:

- JSON UTF-8.
- Không expose EF entity trực tiếp.
- Không nhận `studentId` từ body cho Student self-service endpoint nếu lấy được từ auth context.
- Authorization server-side.
- Error thống nhất bằng ProblemDetails + `code`.
- List có pagination.
- Không trả correct answer trước khi Student submit.
- OpenAPI phải phản ánh contract thực tế.

Base path MVP:

```text
/api
```

Không bắt buộc `/v1` ở MVP. Nếu sau này cần breaking API versioning, tạo decision record trước.

## 2. Common response conventions

### 2.1 Success

- `200 OK`: read/update thành công.
- `201 Created`: tạo resource mới.
- `204 No Content`: delete/archive hoặc command không cần body.

### 2.2 Error

Dùng ProblemDetails mở rộng:

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

Codes nên stable để frontend không parse `detail`.

### 2.3 HTTP semantics

```text
400/422 -> invalid request/validation theo convention codebase
401 -> unauthenticated/session invalid
403 -> authenticated but forbidden
404 -> resource not found
409 -> conflict/concurrency/duplicate invariant
429 -> rate limited
500 -> unexpected internal error
```

Không trả `200 { success:false }` cho error HTTP bình thường.

## 3. Pagination

Query:

```text
?page=1&pageSize=20
```

Rules:

- page >= 1.
- pageSize default 20.
- pageSize max 100.

Response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

Search/sort parameters phải whitelist theo endpoint.

## 4. Authentication API

### POST `/api/auth/login`

Request:

```json
{
  "email": "student@example.com",
  "password": "********"
}
```

Success `200`:

```json
{
  "user": {
    "id": 15,
    "fullName": "Nguyễn Văn A",
    "email": "student@example.com",
    "role": "STUDENT"
  },
  "accessToken": "<jwt>",
  "accessTokenExpiresAtUtc": "2026-08-25T13:10:00Z"
}
```

Side effect: set opaque refresh token in HttpOnly cookie. Response không bao giờ chứa refresh token.

Failure:

- invalid/unknown/disabled/locked credential -> generic 401 `INVALID_CREDENTIALS`.
- rate limited -> 429.

### POST `/api/auth/refresh`

Request body: none. Refresh credential lấy từ HttpOnly cookie.

Success `200`: cùng shape login response, đồng thời rotate refresh cookie.

Failure chính:

- missing/invalid/revoked -> 401 `INVALID_REFRESH_TOKEN`.
- expired -> 401 `REFRESH_TOKEN_EXPIRED`.
- used token replay ngoài grace -> 401 `REFRESH_TOKEN_REUSE_DETECTED`, revoke cả family.
- concurrent legitimate rotation -> 409 `REFRESH_RETRY_REQUIRED`; browser retry một lần bằng cookie mới.
- rate limited -> 429.

### GET `/api/auth/me`

Yêu cầu Bearer access JWT hợp lệ. Success trả `UserDto`; unauthenticated/session revoked -> 401.

### POST `/api/auth/logout`

Cho phép gọi cả khi access token vừa hết hạn. Backend:

- revoke refresh family nếu cookie còn tồn tại;
- blacklist access `jti` hiện tại nếu Bearer còn hợp lệ;
- xóa refresh cookie;
- response `204`.

Frontend không lưu refresh token và không gửi refresh token trong body/header JavaScript.

## 5. Admin Course API

### GET `/api/admin/courses`

Query optional:

```text
page
pageSize
search
status
sort
```

Response item:

```json
{
  "id": 3,
  "title": "Python Basic",
  "description": "Khóa học Python dành cho người mới bắt đầu.",
  "thumbnailUrl": "https://...",
  "status": "PUBLISHED",
  "sortOrder": 1,
  "lessonCount": 5,
  "studentCount": 25
}
```

Không load lesson content/options để tạo response này.

### GET `/api/admin/courses/{id}`

Trả course admin detail, có thể gồm lesson summary nhưng không bắt buộc full exercise graph.

Not found -> 404.

### POST `/api/admin/courses`

Request:

```json
{
  "title": "Python Basic",
  "description": "Khóa học Python dành cho người mới bắt đầu.",
  "thumbnailUrl": "https://...",
  "status": "DRAFT",
  "sortOrder": 1
}
```

Success -> 201 + created resource/Location nếu convention hỗ trợ.

Validation errors -> 400/422.

### PUT `/api/admin/courses/{id}`

Request cùng editable fields.

Không cho client sửa audit fields/Id.

### DELETE `/api/admin/courses/{id}`

MVP semantics: archive.

Success -> 204.

Nếu đã Archived -> idempotent 204 hoặc 409, phải chọn một convention và test. Khuyến nghị idempotent 204.

### POST `/api/admin/courses/{id}/publish`

Optional explicit command nếu UI không dùng PUT status.

Nếu có endpoint này thì publish rule nằm server-side.

Không cần tồn tại cả explicit publish command và PUT status nếu làm contract dư thừa; codebase chọn một cách nhất quán.

## 6. Admin Lesson API

### GET `/api/admin/courses/{courseId}/lessons`

Trả lesson admin summary theo SortOrder.

### GET `/api/admin/lessons/{id}`

Trả:

```json
{
  "id": 10,
  "courseId": 3,
  "title": "Bài 1 – Làm quen với Python",
  "description": "...",
  "contentHtml": "<p>...</p>",
  "video": {
    "provider": "YOUTUBE",
    "externalId": "abc123"
  },
  "sortOrder": 1,
  "status": "PUBLISHED"
}
```

Nếu implementation lưu raw VideoUrl, API vẫn nên trả normalized representation hoặc URL đã validate.

### POST `/api/admin/courses/{courseId}/lessons`

Request:

```json
{
  "title": "Bài 1 – Làm quen với Python",
  "description": "...",
  "contentHtml": "<p>...</p>",
  "videoUrl": "https://www.youtube.com/watch?v=abc123",
  "sortOrder": 1,
  "status": "DRAFT"
}
```

Backend sanitize content + validate video.

### PUT `/api/admin/lessons/{id}`

Update editable fields.

### DELETE `/api/admin/lessons/{id}`

MVP ưu tiên archive.

### PUT/POST reorder

Nếu UI hỗ trợ reorder nhiều lesson, ưu tiên endpoint batch rõ ràng thay vì N request lẻ:

```text
PUT /api/admin/courses/{courseId}/lessons/order
```

Request:

```json
{
  "items": [
    { "lessonId": 10, "sortOrder": 1 },
    { "lessonId": 11, "sortOrder": 2 }
  ]
}
```

Backend validate:

- tất cả lesson thuộc course.
- no duplicate lesson id.
- no duplicate effective order.
- transaction atomic.

Chỉ thêm endpoint này khi UI reorder được implement.

## 7. Admin Question API

### GET `/api/admin/lessons/{lessonId}/questions`

Admin nhìn thấy question + options + correct marker.

### POST `/api/admin/lessons/{lessonId}/questions`

Multiple choice request:

```json
{
  "text": "Python dùng hàm nào để hiển thị dữ liệu?",
  "type": "MULTIPLE_CHOICE",
  "explanation": "print() dùng để xuất dữ liệu trong Python.",
  "sortOrder": 1,
  "options": [
    { "content": "show()", "isCorrect": false, "sortOrder": 1 },
    { "content": "print()", "isCorrect": true, "sortOrder": 2 },
    { "content": "display()", "isCorrect": false, "sortOrder": 3 },
    { "content": "echo()", "isCorrect": false, "sortOrder": 4 }
  ]
}
```

Validation:

- lesson exists.
- text non-empty.
- type valid.
- min 2 options for MC.
- exactly one correct.
- True/False structure valid.

### PUT `/api/admin/questions/{id}`

Update question/options atomically.

Nếu answer history đã tồn tại, update không được làm mất historical `StudentAnswer.IsCorrect` snapshot.

### DELETE `/api/admin/questions/{id}`

Delete/archive policy phụ thuộc history, xem `DATABASE.md`.

## 8. Admin Student API

### GET `/api/admin/students`

Paginated.

Item:

```json
{
  "id": 15,
  "fullName": "Nguyễn Văn A",
  "email": "a@example.com",
  "courseCount": 2,
  "status": "ACTIVE"
}
```

### GET `/api/admin/students/{id}`

Trả profile + enrollment summary cần cho detail page.

Không trả Identity internal security fields.

### POST `/api/admin/students`

Request example:

```json
{
  "fullName": "Nguyễn Văn A",
  "email": "a@example.com",
  "initialPassword": "..."
}
```

Yêu cầu:

- email unique.
- password policy.
- role fixed STUDENT.

Nếu production policy không cho Admin biết permanent password, future flow reset/invite phải là requirement mới; MVP có thể dùng initial password theo yêu cầu demo.

### PUT `/api/admin/students/{id}`

Chỉ update business fields cho phép.

### POST `/api/admin/students/{id}/disable`

Disable account + invalidate security stamp.

Có thể dùng PUT status thay endpoint command nếu codebase chọn một convention.

## 9. Enrollment API

### POST `/api/admin/enrollments`

Request:

```json
{
  "studentId": 15,
  "courseId": 3
}
```

Success:

- first enrollment -> 201/200.
- already active -> idempotent 200/204.
- inactive -> reactivate.

Conflict/ineligible course -> 409/400 theo reason.

Không cho enroll Archived course.

### DELETE `/api/admin/enrollments/{id}` hoặc deactivate endpoint

Nếu cần remove enrollment, ưu tiên Inactive để giữ history.

Không tự thêm endpoint nếu UI chưa cần.

## 10. Admin Progress API

### GET `/api/admin/progress`

Paginated.

Filters có thể hỗ trợ:

```text
studentId
courseId
search
page
pageSize
```

Item:

```json
{
  "studentId": 15,
  "studentName": "Nguyễn Văn A",
  "courseId": 3,
  "courseTitle": "Python Basic",
  "completedLessons": 1,
  "totalLessons": 5,
  "percentage": 20
}
```

### GET `/api/admin/students/{id}/progress`

Trả course progress + lesson-level status cho Student đó.

## 11. Student Course API

### GET `/api/student/courses`

Không nhận studentId.

Current Student từ auth principal.

Chỉ trả Active enrollment + Published Course.

Item:

```json
{
  "id": 3,
  "title": "Python Basic",
  "thumbnailUrl": "https://...",
  "totalLessons": 5,
  "completedLessons": 1,
  "percentage": 20
}
```

### GET `/api/student/courses/{courseId}`

Authorization:

- active enrollment.
- course published.

Response:

```json
{
  "id": 3,
  "title": "Python Basic",
  "description": "...",
  "completedLessons": 1,
  "totalLessons": 5,
  "percentage": 20,
  "lessons": [
    {
      "id": 10,
      "title": "Bài 1",
      "sortOrder": 1,
      "state": "COMPLETED",
      "canAccess": true
    },
    {
      "id": 11,
      "title": "Bài 2",
      "sortOrder": 2,
      "state": "AVAILABLE",
      "canAccess": true
    },
    {
      "id": 12,
      "title": "Bài 3",
      "sortOrder": 3,
      "state": "LOCKED",
      "canAccess": false
    }
  ]
}
```

`canAccess/state` là server-derived.

## 12. Student Lesson API

### GET `/api/student/lessons/{lessonId}`

Checks:

- authenticated Student.
- Active account.
- Active enrollment.
- Published Course.
- Published Lesson.
- learning path unlocked.

Locked -> 403:

```json
{
  "status": 403,
  "code": "LESSON_LOCKED",
  "detail": "Bạn cần hoàn thành bài học trước đó."
}
```

Success:

```json
{
  "id": 11,
  "courseId": 3,
  "title": "Bài 2 – Biến",
  "description": "...",
  "contentHtml": "<p>...</p>",
  "video": {
    "provider": "YOUTUBE",
    "externalId": "abc123"
  },
  "progressStatus": "IN_PROGRESS",
  "previousLessonId": 10,
  "nextLessonId": null,
  "questions": [
    {
      "id": 100,
      "text": "...",
      "type": "MULTIPLE_CHOICE",
      "options": [
        { "id": 1001, "content": "A" },
        { "id": 1002, "content": "B" }
      ]
    }
  ]
}
```

Student response **không chứa `isCorrect`** của option.

`nextLessonId` chỉ trả khi next lesson đã/có thể truy cập theo rule hiện tại; frontend không được dùng ID để bypass backend.

## 13. Lesson start/complete API

### POST `/api/student/lessons/{lessonId}/start`

Idempotent.

Success `200`:

```json
{
  "status": "IN_PROGRESS",
  "startedAtUtc": "2026-08-25T05:00:00Z"
}
```

Nếu đã Completed:

```json
{
  "status": "COMPLETED",
  "startedAtUtc": "...",
  "completedAtUtc": "..."
}
```

Locked -> 403.

### POST `/api/student/lessons/{lessonId}/complete`

Idempotent.

Success:

```json
{
  "status": "COMPLETED",
  "completedAtUtc": "2026-08-25T05:30:00Z",
  "courseProgress": {
    "completedLessons": 1,
    "totalLessons": 5,
    "percentage": 20
  }
}
```

Không nhận `studentId`.

Không yêu cầu score >=70 trong MVP.

## 14. Student Exercise API

Có thể dùng question embedded trong lesson response hoặc endpoint riêng:

```text
GET /api/student/lessons/{lessonId}/questions
```

Chọn một cách để tránh duplicate fetch contract không cần thiết.

### POST `/api/student/questions/{questionId}/answer`

Request:

```json
{
  "optionId": 1002
}
```

Server checks option thuộc question.

Success:

```json
{
  "correct": true,
  "explanation": "print() dùng để xuất dữ liệu trong Python.",
  "answeredAtUtc": "2026-08-25T05:20:00Z"
}
```

Không cho client gửi `isCorrect`.

## 15. Student Progress API

### GET `/api/student/courses/{courseId}/progress`

Current Student inferred.

Response:

```json
{
  "courseId": 3,
  "completedLessons": 2,
  "totalLessons": 5,
  "percentage": 40,
  "lessons": [
    {
      "lessonId": 10,
      "status": "COMPLETED",
      "startedAtUtc": "...",
      "completedAtUtc": "..."
    }
  ]
}
```

Không trả progress Student khác.

## 16. Error codes chuẩn

Danh sách khởi điểm:

```text
INVALID_CREDENTIALS
ACCOUNT_DISABLED
VALIDATION_FAILED
RESOURCE_NOT_FOUND
FORBIDDEN
COURSE_NOT_ENROLLED
COURSE_NOT_PUBLISHED
LESSON_NOT_PUBLISHED
LESSON_LOCKED
INVALID_QUESTION_OPTION
DUPLICATE_ENROLLMENT
CONCURRENCY_CONFLICT
RATE_LIMITED
REFRESH_TOKEN_EXPIRED
REFRESH_TOKEN_REUSE_DETECTED
REFRESH_RETRY_REQUIRED
SESSION_REVOKED
```

Không tạo hàng chục code trùng nghĩa. Error code mới phải có consumer/use case rõ.

## 17. API security invariants

- Admin request phải policy Admin.
- Student current user lấy từ claims.
- Không tin role/client flags từ body/header tự tạo.
- Không trả hidden/correct answer trước submit.
- Không trả Draft/Archived content cho Student.
- Bearer access token là security boundary của business API; refresh credential không được xuất hiện trong JavaScript/request body.
- OpenAPI production exposure tùy environment/security policy; dev luôn có thể dùng để bàn giao.

## 18. API performance invariants

- List response bounded.
- Không N+1.
- Không full entity graph nếu DTO không cần.
- Course list không load Lesson.ContentHtml.
- Student lesson detail có thể load questions/options cho đúng 1 lesson.
- Admin progress query phải aggregate trong DB.
- CancellationToken đi xuống EF call.

## 19. Contract change rule

Nếu thay request/response/error semantics:

1. Update DTO/OpenAPI.
2. Update frontend consumer.
3. Update integration tests.
4. Update E2E nếu user flow đổi.
5. Update tài liệu này nếu contract public/stable đổi.

Không silently đổi field name/status code mà chỉ sửa frontend cùng lúc vì sẽ làm bàn giao khó kiểm soát.

## Learning Guardrails — 2026-08-29

`LessonWriteRequest` adds nullable `videoDurationSeconds`. A Published lesson with video requires a trusted duration.

`QuestionWriteRequest` adds:

- `placement`: `REINFORCEMENT | VIDEO_CHECKPOINT`;
- `videoTimestampSeconds`: required for `VIDEO_CHECKPOINT`, null for reinforcement.

Student lesson response adds `videoProgress` and `completion` gate state. Student question response adds placement, timestamp and whether the current learner has already passed the question.

New endpoint:

```text
POST /api/student/lessons/{lessonId}/video-heartbeat
{ "positionSeconds": 120 }
```

The response returns the accepted maximum position, trusted duration, video-completed state and an optional blocking checkpoint question id.

`POST /api/student/lessons/{lessonId}/complete` may return 409 with codes including `LESSON_NOT_STARTED`, `VIDEO_DURATION_REQUIRED`, `VIDEO_NOT_COMPLETED`, `CHECKPOINT_REQUIRED`, or `QUIZ_NOT_PASSED`.
