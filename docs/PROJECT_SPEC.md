# PROJECT_SPEC.md — Hợp đồng nghiệp vụ E-Learning MVP

## 1. Mục đích

Tài liệu này mô tả **hành vi nghiệp vụ bắt buộc** của hệ thống E-Learning MVP cho trường học. Đây là nguồn sự thật chính cho các câu hỏi: hệ thống phải cho phép ai làm gì, dữ liệu nào được nhìn thấy, learning path hoạt động ra sao và flow demo nào phải chạy được.

Các quyết định kỹ thuật như framework, JWT, cấu trúc project hoặc cách query được mô tả ở các tài liệu khác.

## 2. Mục tiêu sản phẩm

Hệ thống cho phép:

- Admin tạo và quản lý khóa học.
- Mỗi khóa học có nhiều bài học.
- Mỗi bài học có video tự học, nội dung/mô tả và bài tập tự luyện.
- Học viên đăng nhập và chỉ học các khóa được cấp.
- Hệ thống ghi nhận trạng thái học bài và tiến độ khóa học.
- Học viên làm bài tập và xem đúng/sai, giải thích đáp án.
- Admin xem tiến độ học viên.

Đây là **MVP**, ưu tiên chứng minh flow end-to-end trước khi mở rộng chức năng.

## 3. Actors

### 3.1 ADMIN

Admin có quyền:

- Đăng nhập/đăng xuất.
- Xem dashboard tổng quan.
- Xem danh sách khóa học.
- Tạo/sửa/publish/unpublish/archive khóa học.
- Tạo/sửa/publish/unpublish/archive bài học.
- Gắn video YouTube cho bài học.
- Tạo/sửa/xóa câu hỏi tự luyện.
- Xem danh sách học viên.
- Tạo/sửa/disable học viên.
- Gán khóa học cho học viên.
- Xem tiến độ học tập tổng quan và chi tiết.

Admin không được phép bỏ qua các validation dữ liệu cốt lõi chỉ vì có role Admin.

### 3.2 STUDENT

Student có quyền:

- Đăng nhập/đăng xuất.
- Xem các khóa học mình được enrollment active.
- Xem chi tiết khóa học được cấp và đang Published.
- Xem danh sách lesson Published theo thứ tự.
- Truy cập lesson đã mở khóa.
- Xem video và nội dung bài học.
- Làm bài tập.
- Xem kết quả câu trả lời và giải thích.
- Đánh dấu lesson hoàn thành.
- Xem tiến độ khóa học của bản thân.

Student không được:

- Truy cập endpoint/trang Admin.
- Xem course chưa được enrollment.
- Xem course Draft/Archived.
- Xem lesson Draft/Archived.
- Bỏ qua lesson trước trong learning path bằng cách nhập URL hoặc gọi API trực tiếp.
- Xem tiến độ hoặc câu trả lời của student khác.

## 4. Authentication

### 4.1 Login

Màn hình `/login` gồm:

- Email hoặc username theo implementation đã chốt.
- Password.
- Nút Đăng nhập.

Sau đăng nhập:

- Admin -> `/admin/dashboard`
- Student -> `/student/courses`

Yêu cầu:

- Chưa đăng nhập không truy cập được trang nội bộ.
- Sai credential trả thông báo chung, không tiết lộ email có tồn tại hay không.
- Account Disabled không đăng nhập được.
- Logout kết thúc session hiện tại.

### 4.2 Authorization

Frontend route guard chỉ hỗ trợ UX. Backend phải kiểm tra lại toàn bộ quyền.

## 5. Admin Dashboard

Route: `/admin/dashboard`

Hiển thị tối thiểu:

- Tổng số khóa học không Archived.
- Tổng số học viên Active.
- Tổng số bài học không Archived.
- Tổng lượt hoàn thành bài học.

Menu tối thiểu:

- Dashboard
- Khóa học
- Học viên
- Tiến độ học tập
- Đăng xuất

Các số liệu phải được query theo cách bounded, không tạo N+1.

## 6. Course Management

### 6.1 Danh sách khóa học

Route: `/admin/courses`

Hiển thị tối thiểu:

- Tên khóa học.
- Số bài học.
- Số học viên enrollment active.
- Trạng thái.
- Thao tác.

Trạng thái nghiệp vụ:

- `DRAFT`
- `PUBLISHED`
- `ARCHIVED` — quyết định kỹ thuật tương ứng hành vi xóa/ẩn an toàn.

### 6.2 Tạo/sửa khóa học

Thông tin:

- Tên khóa học: bắt buộc.
- Mô tả ngắn: optional nhưng khuyến nghị.
- Thumbnail URL: optional trong MVP.
- Trạng thái Draft/Published.
- Thứ tự hiển thị.

Validation:

- Không có tên -> reject.
- Thumbnail URL nếu có phải hợp lệ theo policy URL.
- SortOrder phải là số nguyên hợp lệ.

### 6.3 Archive

`DELETE` trong API có thể được implement như archive để không phá enrollment/progress lịch sử.

Archived course:

- Không hiện cho Student.
- Không cho enrollment mới.
- Dữ liệu lịch sử không bị xóa vật lý.

## 7. Lesson Management

### 7.1 Quan hệ

Một Course có nhiều Lesson.

Lesson có thứ tự xác định bằng `SortOrder`.

### 7.2 Tạo/sửa lesson

Thông tin:

- Course: bắt buộc.
- Tên lesson: bắt buộc.
- Mô tả.
- Video URL.
- Rich Text content.
- SortOrder.
- Status: Draft/Published/Archived.

MVP hỗ trợ YouTube URL. Upload MP4, Vimeo, CDN là ngoài phạm vi hiện tại.

### 7.3 Video player

Yêu cầu UX:

- Tỷ lệ 16:9.
- Play/Pause.
- Volume.
- Fullscreen.
- Timeline.

Không tự code player nếu iframe/provider có sẵn đáp ứng đủ.

## 8. Exercise

Mỗi lesson có thể có nhiều câu hỏi.

### 8.1 Question types

MVP hỗ trợ:

- `MULTIPLE_CHOICE`
- `TRUE_FALSE`

### 8.2 Multiple Choice

Yêu cầu:

- Nội dung câu hỏi bắt buộc.
- Tối thiểu 2 lựa chọn.
- MVP UI chuẩn hỗ trợ tối đa 4 lựa chọn A-D.
- Chính xác 1 đáp án đúng.
- Explanation optional.

### 8.3 True / False

- Có đúng 2 lựa chọn logic: Đúng/Sai.
- Chính xác 1 đáp án đúng.

### 8.4 Submit answer

Khi Student submit:

Hệ thống lưu tối thiểu:

- Student.
- Question.
- Option đã chọn.
- Có đúng hay không.
- Thời gian trả lời.

Response cho Student gồm:

- `correct: true|false`
- `explanation` nếu có.

MVP cho phép Student làm lại câu hỏi. Mỗi lần submit là một attempt mới để giữ lịch sử; UI có thể hiển thị kết quả attempt vừa submit.

`isCorrect` trong StudentAnswer là snapshot tại thời điểm chấm để lịch sử không thay đổi nếu Admin chỉnh đáp án sau này.

## 9. Student Course List

Route: `/student/courses`

Chỉ hiển thị course đáp ứng tất cả:

- Student hiện tại có Enrollment Active.
- Course Published.
- Course không Archived.

Mỗi card tối thiểu:

- Tên course.
- Số lesson Published.
- Tiến độ %.
- `completed / total`.
- Nút “Tiếp tục học” hoặc “Xem lại”.

## 10. Student Course Detail

Route: `/student/courses/:courseId`

Hiển thị:

- Tên khóa học.
- Mô tả.
- Tiến độ.
- Danh sách lesson Published theo `SortOrder`.
- Trạng thái từng lesson.

Trạng thái UX có thể biểu diễn:

- Completed.
- In progress / available.
- Locked.

Không hiển thị Draft/Archived lesson cho Student.

## 11. Learning Page

Route: `/student/learn/:lessonId`

Layout tối thiểu:

### Sidebar

- Danh sách lesson Published của course.
- Lesson hiện tại.
- Completed state.
- Locked state.

### Main content

- Tên lesson.
- Video.
- Rich Text content.
- Exercise.
- Previous/Next khi hợp lệ.
- Nút “Đánh dấu hoàn thành”.

Không được dùng `Next` để bypass learning path.

## 12. Learning Path

### 12.1 Quy tắc chuẩn

Các lesson Published được học theo `SortOrder` tăng dần.

Ví dụ:

```text
Lesson 1 -> Lesson 2 -> Lesson 3 -> Lesson 4
```

Khi mới vào:

```text
Lesson 1: OPEN
Lesson 2: LOCKED
Lesson 3: LOCKED
Lesson 4: LOCKED
```

Sau khi Lesson 1 = Completed:

```text
Lesson 1: COMPLETED
Lesson 2: OPEN
Lesson 3: LOCKED
Lesson 4: LOCKED
```

### 12.2 Backend enforcement

Khi Student gọi lesson N:

1. Account phải Active.
2. Course phải Published.
3. Enrollment phải Active.
4. Lesson phải Published.
5. Nếu là lesson Published đầu tiên -> cho phép.
6. Nếu không -> lesson Published ngay trước phải `COMPLETED`.
7. Nếu không đạt -> `403` với `LESSON_LOCKED`.

Không chỉ disable link ở Vue.

### 12.3 Điều kiện Complete trong MVP

Một lesson được tính `COMPLETED` khi Student thực hiện hành động “Đánh dấu hoàn thành” và backend xác nhận Student có quyền truy cập lesson đó.

**Không bắt buộc score >=70% trong MVP hiện tại.** Quy tắc score threshold là khả năng mở rộng sau và chỉ được bật khi Product Owner yêu cầu rõ.

### 12.4 Draft/Archived lesson trong path

Draft/Archived không tham gia chuỗi mở khóa Student.

Ví dụ thứ tự vật lý:

```text
Lesson A Published sort=1
Lesson B Draft     sort=2
Lesson C Published sort=3
```

Với Student, predecessor của Lesson C là Lesson A.

## 13. Lesson Progress

Các trạng thái:

- `NOT_STARTED` — trạng thái suy ra khi chưa có record hoặc record explicit tùy implementation.
- `IN_PROGRESS`
- `COMPLETED`

### Start lesson

Khi Student mở lesson hợp lệ:

- Nếu chưa có progress -> tạo `IN_PROGRESS`, set `StartedAtUtc`.
- Nếu đã IN_PROGRESS -> giữ nguyên `StartedAtUtc`.
- Nếu đã COMPLETED -> không hạ status.

Endpoint phải idempotent.

### Complete lesson

- Chỉ Student có quyền access mới complete được.
- Nếu chưa có progress, có thể tạo record với started/completed theo thời điểm hợp lệ.
- Nếu đã Completed, gọi lại không tạo duplicate và không thay completed timestamp tùy tiện.
- Sau complete, lesson kế tiếp được mở theo rule, không cần lưu cột `Unlocked`.

## 14. Course Progress

Công thức MVP:

```text
ProgressPercent = CompletedPublishedLessons / TotalPublishedLessons * 100
```

Quy tắc:

- Chỉ lesson Published hiện tại tham gia denominator.
- Completed lesson chỉ tính nếu lesson đó còn Published.
- Nếu `TotalPublishedLessons = 0`, progress = 0.
- Không lưu `ProgressPercent` thành source of truth riêng.
- API có thể trả cả `completedLessons`, `totalLessons`, `percentage`.

Ví dụ:

```text
5 published lessons
2 completed
=> 40%
```

Nếu Admin publish thêm lesson, % có thể giảm. Đây là hành vi chấp nhận được của MVP vì progress phản ánh course hiện tại.

## 15. Student Management

Route: `/admin/students`

Hiển thị:

- Họ tên.
- Email.
- Số course active.
- Status.
- Thao tác.

Admin có thể:

- Tạo Student.
- Sửa thông tin không nhạy cảm.
- Disable Student.
- Gán course.

Email phải unique theo normalization của Identity/database.

Disable account:

- Không login mới.
- Session cũ phải bị vô hiệu hóa trong khoảng thời gian security policy cho phép; xem `SECURITY.md`.

## 16. Enrollment

Enrollment xác định Student được học Course nào.

Yêu cầu:

- `(StudentId, CourseId)` unique.
- Có trạng thái Active/Inactive.
- Gán course đã tồn tại -> cập nhật/reactivate thay vì insert duplicate.
- Không gán Archived course.
- Student chỉ thấy Active enrollment.

## 17. Admin Progress

Route: `/admin/progress`

Danh sách tối thiểu:

- Student.
- Course.
- Completed lessons.
- Total published lessons.
- Progress %.

Có thể click Student/Course để xem chi tiết từng lesson.

Danh sách phải phân trang hoặc có limit hợp lý khi dữ liệu tăng.

## 18. Frontend routes

### Public

```text
/login
```

### Student

```text
/student/courses
/student/courses/:courseId
/student/learn/:lessonId
/student/profile
```

`/student/profile` có thể là tối thiểu/placeholder nếu chưa có requirement chỉnh profile chi tiết; không tự mở rộng chức năng.

### Admin

```text
/admin/dashboard
/admin/courses
/admin/courses/create
/admin/courses/:id
/admin/courses/:id/edit
/admin/courses/:courseId/lessons/create
/admin/lessons/:id/edit
/admin/lessons/:lessonId/exercises
/admin/students
/admin/students/:id
/admin/progress
```

## 19. UI minimum

Không cần UI cầu kỳ trước khi core flow chạy.

Bắt buộc có:

- Responsive desktop hợp lý.
- Admin sidebar.
- Header.
- Course card.
- Lesson sidebar.
- Video player.
- Exercise card.
- Progress bar.
- Loading state.
- Empty state.
- Error state.

Ưu tiên usability, accessibility cơ bản và trạng thái rõ ràng hơn animation/phức tạp hình ảnh.

## 20. Validation nghiệp vụ

### Course

Reject nếu:

- Title trống.

### Lesson

Reject nếu:

- Title trống.
- Không thuộc Course hợp lệ.
- SortOrder không hợp lệ.

### Question

Reject nếu:

- Nội dung trống.
- Không có đáp án đúng.
- Có nhiều hơn một đáp án đúng.
- Multiple Choice có ít hơn 2 option.
- True/False không đúng cấu trúc 2 lựa chọn.

### User

Reject nếu:

- Email invalid.
- Email đã tồn tại theo normalized uniqueness.
- Password không đạt password policy.

## 21. Seed Data

Development/Demo cần seed để demo nhanh:

- 1 Admin.
- 1 Student.
- 1 course “Python Basic”.
- 5 lessons.
- Mỗi lesson có video URL demo.
- Mỗi lesson có 3 questions.
- Student được enrollment course demo nếu cần cho smoke test.

Credential demo phải lấy từ config/secret development hoặc được ghi rõ chỉ dành cho môi trường demo.

**Không seed default credential trên production.**

## 22. Test cases bắt buộc từ yêu cầu

1. Student login đúng -> thành công.
2. Sai password -> thông báo chung.
3. Student truy cập Admin -> bị chặn.
4. Admin tạo Course -> xuất hiện danh sách.
5. Admin tạo Lesson -> xuất hiện trong Course.
6. Admin enroll Student -> Student nhìn thấy Course.
7. Student complete Lesson -> DB có Completed.
8. 2/5 lessons Completed -> progress 40%.
9. Submit đúng -> correct=true.
10. Submit sai -> correct=false.
11. Student gọi Course chưa enroll -> 403/404 theo API contract đã chốt, không lộ dữ liệu.
12. Student gọi locked Lesson -> 403 `LESSON_LOCKED`.
13. Draft/Archived content không xuất hiện cho Student.
14. Gọi Start/Complete lặp lại không tạo duplicate progress.
15. Enroll cùng course lặp lại không tạo duplicate enrollment.

## 23. Flow demo cuối MVP

Flow bắt buộc phải demo được end-to-end:

1. Login Admin.
2. Admin tạo `Python Basic`.
3. Admin tạo `Bài 1 – Hello Python`.
4. Admin thêm video.
5. Admin tạo 3 câu hỏi.
6. Admin tạo Student `Nguyễn Văn A`.
7. Admin gán `Nguyễn Văn A -> Python Basic`.
8. Logout Admin.
9. Login Student.
10. Student thấy `Python Basic`.
11. Student vào Lesson 1.
12. Student xem video/nội dung.
13. Student làm bài tập.
14. Student đánh dấu hoàn thành.
15. Progress đổi từ 0% -> 20% nếu course có 5 published lessons.
16. Logout/login Admin.
17. Admin xem Student và thấy `1/5`, `20%`.

Nếu flow này không chạy ổn định thì MVP chưa đạt.

## 24. Non-goals

Không triển khai trong MVP trừ khi có task mới rõ ràng:

- Livestream.
- Zoom integration.
- Chat realtime.
- Thanh toán.
- Voucher.
- Certificate.
- Mobile app native.
- Notification.
- Email automation.
- AI.
- Discussion forum.
- Upload bài tập dạng file.
- Coding judge.
- Chống tua video.
- Thi countdown/proctoring.
- Random câu hỏi.
- Leaderboard.
- Điểm danh.
- Parent role.
- Teacher role.
- Permission nhiều cấp.

## 25. Thứ tự ưu tiên sản phẩm

Khi phải chọn thứ tự triển khai:

```text
LOGIN
  -> COURSE
  -> LESSON
  -> STUDENT
  -> ENROLLMENT
  -> LEARNING PAGE
  -> EXERCISE
  -> PROGRESS
  -> ADMIN REPORT
```

UI polish không được chặn core flow.

## 26. Các assumption kỹ thuật được phép nhưng không phải requirement gốc

Các điểm sau là lựa chọn kiến trúc hiện tại, có thể thay đổi bằng decision record nếu có lý do mạnh:

- Dùng `ARCHIVED` thay hard delete Course/Lesson.
- Cho phép nhiều answer attempt và lưu mỗi attempt.
- Progress tính theo lesson Published hiện tại.
- Dùng JWT Bearer access token ngắn hạn giữ trong memory; refresh token opaque nằm HttpOnly cookie và rotate server-side.

Nếu thay đổi các assumption này, phải đánh giá migration, API, test và hành vi người dùng trước khi code.

## Learning Guardrails — 2026-08-29

The previous MVP convention allowing manual lesson completion without a score threshold is superseded.

- Student cannot complete a lesson immediately after opening it.
- A video lesson requires trusted video duration and server-tracked watch progress.
- Forward seeking is not accepted as watched progress; tab/window loss of focus pauses the player UX.
- Admin questions have placement: `REINFORCEMENT` or `VIDEO_CHECKPOINT`.
- `VIDEO_CHECKPOINT` requires a timestamp and must be answered correctly before video may continue beyond that point.
- Reinforcement questions are locked until required video completion.
- Completion requires >80% reinforcement questions passed, plus all video checkpoints passed.
- Backend completion endpoint is authoritative and revalidates every condition.

See `LEARNING_GUARDRAILS.md`.
