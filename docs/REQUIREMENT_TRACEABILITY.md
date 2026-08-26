# REQUIREMENT_TRACEABILITY.md — Truy vết yêu cầu gốc -> tài liệu/implementation

## 1. Mục đích

File này giúp reviewer, AI agent và người nhận bàn giao phân biệt:

- **SOURCE REQUIREMENT:** yêu cầu nghiệp vụ có trong đề bài E-Learning gốc.
- **ARCHITECTURE DECISION:** quyết định kỹ thuật được bổ sung để đáp ứng yêu cầu về .NET 10, Vue 3, bảo mật, hiệu năng và dễ bàn giao.
- **MVP ASSUMPTION:** lựa chọn tối thiểu khi đề bài cho nhiều phương án hoặc chưa chốt chi tiết.

Nếu Product Owner thay đổi requirement, cập nhật `PROJECT_SPEC.md` trước rồi cập nhật mapping này nếu thay đổi lớn.

## 2. Mapping theo đề bài gốc

| Phần yêu cầu                                    | Loại                           | Tài liệu canonical                   | Ghi chú implementation                                                                  |
| ----------------------------------------------- | ------------------------------ | ------------------------------------ | --------------------------------------------------------------------------------------- |
| Mục tiêu E-Learning cơ bản                      | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Giữ nguyên flow Admin tạo nội dung -> Student học -> progress                           |
| Role ADMIN/STUDENT                              | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `SECURITY.md`     | Backend role policy bắt buộc                                                            |
| Login/Logout                                    | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `API_CONTRACT.md` | Identity + cookie là architecture decision                                              |
| Password phải hash                              | SOURCE REQUIREMENT             | `SECURITY.md`                        | ASP.NET Core Identity hasher                                                            |
| Admin Dashboard                                 | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Aggregate query phải bounded                                                            |
| Course CRUD/Publish                             | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `API_CONTRACT.md` | Archive thay hard delete là architecture decision                                       |
| Course title/description/thumbnail/status/order | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `DATABASE.md`     | Thumbnail URL trong MVP                                                                 |
| Course có nhiều Lesson                          | SOURCE REQUIREMENT             | `DATABASE.md`                        | FK CourseId                                                                             |
| Lesson CRUD/order                               | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Effective sort order phải deterministic                                                 |
| YouTube URL                                     | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `SECURITY.md`     | Normalize/allowlist URL là security decision                                            |
| Rich Text content                               | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `SECURITY.md`     | Sanitization bắt buộc do XSS risk                                                       |
| Video player 16:9/basic controls                | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Ưu tiên iframe/provider player                                                          |
| Multiple Choice                                 | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `DATABASE.md`     | Min 2, exactly 1 correct                                                                |
| True/False                                      | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `DATABASE.md`     | Exactly 2 choices, 1 correct                                                            |
| Admin tạo Question                              | SOURCE REQUIREMENT             | `API_CONTRACT.md`                    | Create/update options atomic                                                            |
| Student course list                             | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `API_CONTRACT.md` | Chỉ Active Enrollment + Published Course                                                |
| Course progress %                               | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `DATABASE.md`     | Derived, không lưu % riêng                                                              |
| Student course detail                           | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Backend-derived lesson state                                                            |
| Learning page/sidebar/prev-next                 | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Prev/next không bypass authorization                                                    |
| Start lesson                                    | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `API_CONTRACT.md` | Idempotent là architecture decision                                                     |
| Complete lesson                                 | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `API_CONTRACT.md` | Idempotent, CompletedAt ổn định                                                         |
| Progress formula completed/total                | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `DATABASE.md`     | Published lessons là denominator hiện tại                                               |
| Student submit answer                           | SOURCE REQUIREMENT             | `API_CONTRACT.md`                    | StudentId lấy từ auth principal                                                         |
| Lưu Student/Question/Option/correct/time        | SOURCE REQUIREMENT             | `DATABASE.md`                        | Mỗi submit là attempt mới: MVP assumption                                               |
| Student Management                              | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Identity profile + status                                                               |
| Disable Student                                 | SOURCE REQUIREMENT             | `SECURITY.md`                        | Security stamp invalidation bổ sung                                                     |
| Enrollment                                      | SOURCE REQUIREMENT             | `DATABASE.md`, `API_CONTRACT.md`     | Unique pair + reactivate semantics bổ sung                                              |
| Admin Progress                                  | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `API_CONTRACT.md` | Pagination bắt buộc theo engineering rule                                               |
| Database entities đề xuất                       | SOURCE REQUIREMENT             | `DATABASE.md`                        | Identity tables thay custom password table là architecture decision                     |
| REST API tối thiểu                              | SOURCE REQUIREMENT             | `API_CONTRACT.md`                    | Route nhóm Admin/Student được làm rõ                                                    |
| Frontend routes                                 | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Giữ route chính                                                                         |
| React/Node/Prisma stack gốc                     | SOURCE SUGGESTION, SUPERSEDED  | `ARCHITECTURE.md`                    | User đã chốt .NET 10 + Vue 3                                                            |
| MySQL/PostgreSQL                                | SOURCE SUGGESTION              | `ARCHITECTURE.md`                    | Chốt PostgreSQL                                                                         |
| JWT/bcrypt                                      | SOURCE SUGGESTION, REFINED     | `DECISIONS.md`                       | Identity PasswordHasher + short JWT access + hashed rotating refresh cookie per ADR-013 |
| UI tối thiểu                                    | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Loading/empty/error bắt buộc                                                            |
| Validation course/lesson/question/email         | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Server authoritative                                                                    |
| Security cơ bản                                 | SOURCE REQUIREMENT             | `SECURITY.md`                        | Mở rộng CSRF/XSS/rate limit/IDOR                                                        |
| Student không xem course chưa enroll            | SOURCE REQUIREMENT             | `SECURITY.md`                        | Resource authorization server-side                                                      |
| Seed data                                       | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `HANDOVER.md`     | Demo seed cấm ở production                                                              |
| Test cases bắt buộc                             | SOURCE REQUIREMENT             | `TESTING_AND_DOD.md`                 | Bổ sung security/query race tests                                                       |
| Definition of Done                              | SOURCE REQUIREMENT             | `TESTING_AND_DOD.md`                 | Mở rộng proof/security/performance                                                      |
| README bắt buộc                                 | SOURCE REQUIREMENT             | `HANDOVER.md`                        | README là tài liệu vận hành ngắn                                                        |
| 12 nhóm task                                    | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Có thể triển khai theo vertical slices nhưng không bỏ scope                             |
| Demo cuối task                                  | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `HANDOVER.md`     | E2E acceptance flow                                                                     |
| Những phần chưa cần làm                         | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Non-goals cứng của MVP                                                                  |
| Thứ tự ưu tiên Login -> Admin Report            | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`                    | Core flow trước UI polish                                                               |
| Learning Path theo thứ tự                       | SOURCE REQUIREMENT             | `PROJECT_SPEC.md`, `ARCHITECTURE.md` | Backend enforce                                                                         |
| 403 nếu lesson trước chưa complete              | SOURCE REQUIREMENT             | `API_CONTRACT.md`, `SECURITY.md`     | Error code `LESSON_LOCKED` bổ sung                                                      |
| Score >=70% để unlock                           | SOURCE OPTIONAL RECOMMENDATION | `DECISIONS.md`                       | Không bật trong MVP mặc định                                                            |

## 3. Các architecture decisions bổ sung

Các mục sau **không phải yêu cầu nghiệp vụ gốc**, nhưng được chốt để đạt mục tiêu mới “.NET 10 + Vue 3, mượt, bảo mật cao, dễ bàn giao”:

1. Modular monolith.
2. PostgreSQL + EF Core 10.
3. ASP.NET Core Identity.
4. Same-origin JWT Bearer access token in-memory.
5. Hashed rotating refresh token trong Secure/HttpOnly/SameSite=Strict cookie.
6. Không lazy loading.
7. Không Generic Repository mặc định.
8. Không MediatR/CQRS/event bus mặc định.
9. DTO projection + AsNoTracking + pagination.
10. Query-count regression test cho endpoint dễ N+1.
11. PostgreSQL thật qua Testcontainers cho integration test.
12. Course progress derived thay persisted percentage.
13. Learning unlock derived thay `IsUnlocked` column.
14. Archive Course/Lesson thay hard delete history.
15. Multiple answer attempts + correctness snapshot.
16. Rich Text sanitization + CSP defense-in-depth.
17. YouTube provider allowlist/normalization.
18. Rate limit login + account lockout.
19. Structured ProblemDetails error contract.
20. Production migration là deployment step, không destructive auto-migrate startup.

Lý do chi tiết ở `DECISIONS.md`.

## 4. Các điểm đề bài chưa chốt và MVP đã chọn convention

### Exercise có bắt buộc để Complete không?

Đề bài có flow làm bài rồi complete, đồng thời nêu score threshold như khuyến nghị tùy chọn.

**MVP convention:** manual “Đánh dấu hoàn thành”; không bắt buộc >=70%.

### Student có được làm lại câu hỏi không?

Đề bài yêu cầu lưu kết quả nhưng không giới hạn attempt.

**MVP convention:** cho làm lại; mỗi submit lưu attempt mới.

### Delete có xóa vật lý không?

Đề bài dùng “xóa/ẩn”.

**MVP convention:** Course/Lesson có history dùng Archived để tránh mất progress.

### Course progress khi Admin publish thêm lesson?

Đề bài định nghĩa completed/total lesson.

**MVP convention:** tính theo Published lessons hiện tại; progress có thể giảm khi course có lesson mới.

### JWT hay cookie?

Đề bài ban đầu gợi ý JWT, nhưng user sau đó yêu cầu stack .NET 10 + Vue 3 và security cao.

**Architecture decision:** browser same-origin dùng JWT Bearer access token giữ trong memory + refresh token opaque trong Secure/HttpOnly/SameSite=Strict cookie; xem ADR-013.

## 5. Requirement change protocol

Khi user đưa requirement mới:

1. Xác định nó bổ sung hay mâu thuẫn requirement hiện tại.
2. Update `PROJECT_SPEC.md`.
3. Nếu ảnh hưởng architecture/security/data -> thêm/supersede ADR trong `DECISIONS.md`.
4. Update `DATABASE.md`/`API_CONTRACT.md` nếu contract đổi.
5. Update tests/DoD.
6. Update traceability row này nếu thay đổi đáng kể.
7. Không để code là nơi duy nhất thể hiện requirement mới.

## 6. Definition of traceability success

Một reviewer phải có thể chọn bất kỳ chức năng chính nào và lần theo:

```text
Requirement
-> Product behavior
-> Architecture/security rule
-> Data/API contract
-> Test proof
```

Ví dụ:

```text
Student không được xem lesson khóa
-> PROJECT_SPEC Learning Path
-> SECURITY resource authorization
-> API 403 LESSON_LOCKED
-> Integration test locked lesson
```

Đây là tiêu chuẩn để tài liệu phục vụ bàn giao, không chỉ “có docs cho đủ”.
