# Dự án E-Learning MVP

Hệ thống E-Learning (phiên bản MVP) được xây dựng trên nền tảng **.NET 10**, **Vue 3** và **PostgreSQL** với kiến trúc sẵn sàng cho môi trường Production.

> Đây là bộ khung (Skeleton) hoàn chỉnh của dự án E-Learning. Hiện tại, luồng Xác thực người dùng (Authentication) cốt lõi đã được hoàn thiện. Các tính năng nghiệp vụ khác đang trong giai đoạn phát triển. Hệ thống đã bao gồm đầy đủ Docker, CI/CD, bộ công cụ kiểm thử (Testing) và cấu trúc phân tầng (Clean Architecture).

## Công nghệ sử dụng (Tech Stack)

- **Backend (Server):** ASP.NET Core Web API (.NET 10), Entity Framework Core 10, PostgreSQL
- **Frontend (Client):** Vue 3, TypeScript (Strict Mode), Vite, Vue Router, Pinia, Axios, Tailwind CSS
- **Kiểm thử (Testing):** xUnit, WebApplicationFactory, Testcontainers PostgreSQL, Vitest, Playwright
- **Công cụ (Tooling):** npm workspaces, Prettier, ESLint, Husky, GitHub Actions, Dependabot

## Cấu trúc thư mục

```text
.
├── docs/                 # Tài liệu dự án (Kiến trúc, Đặc tả, Security...)
├── server/               # Mã nguồn Backend (.NET 10 API)
├── client/               # Mã nguồn Frontend (Vue 3 / Vite)
├── e2e/                  # Mã nguồn End-to-End Tests (Playwright)
├── .github/              # Cấu hình GitHub Actions CI/CD
├── .husky/               # Git hooks (pre-commit, pre-push)
├── docker-compose.yml    # Cấu hình Docker cho môi trường Dev & Production
├── Elearning.sln         # File Solution của .NET
└── package.json          # File quản lý package NPM Workspace
```

## Yêu cầu môi trường (Prerequisites)

Để chạy dự án, máy tính của bạn cần cài đặt:

- .NET SDK 10.x
- Node.js 22+
- npm 10+
- Docker & Docker Compose
- Git

## Cài đặt lần đầu (First setup)

Chạy các lệnh sau trong Terminal để khởi tạo môi trường:

```bash
cp .env.example .env
npm ci
dotnet tool restore
dotnet restore Elearning.sln
```

> **Lưu ý:** Công cụ `dotnet-ef` đã được tích hợp sẵn ở cấp độ dự án (local tool), không cần cài đặt toàn cục (global). File `.env` gốc được sử dụng riêng cho Docker Compose. Đối với frontend, Vite đã cấu hình sẵn các giá trị mặc định, chỉ tạo file `client/.env.local` khi cần ghi đè các cấu hình này.

## Chạy dự án (Local Development)

Khởi động Database (PostgreSQL):

```bash
docker compose up -d postgres
```

Khởi động Backend (ASP.NET Core):

```bash
dotnet run --project server/src/Elearning.Api/Elearning.Api.csproj
```

Khởi động Frontend (Vue 3):

```bash
npm run dev:client
```

**Các đường dẫn mặc định:**

- Giao diện Client: `http://localhost:5173`
- Swagger API Docs: `http://localhost:8080/swagger`
- API Endpoint gốc: `http://localhost:8080`
- Check Health: `http://localhost:8080/api/health`

## Chạy bằng Docker (Container Skeleton)

Nếu muốn chạy toàn bộ ứng dụng bằng Docker (không cần cài Node/SDK):

```bash
docker compose up --build
```

Sau đó truy cập:

- Frontend: `http://localhost:5173`
- Backend Health: `http://localhost:5173/api/health` (thông qua Reverse Proxy của Nginx)

## Kiểm tra mã nguồn (Code Quality & Testing)

Dự án sử dụng Husky để tự động kiểm tra code trước khi commit và push. Bạn có thể tự chạy thủ công:

Kiểm tra Frontend:

```bash
npm run format:check
npm run verify:client
npm run test:e2e
```

Kiểm tra Backend:

```bash
dotnet restore Elearning.sln
dotnet format Elearning.sln --verify-no-changes --no-restore
dotnet build Elearning.sln -c Release --no-restore
dotnet test Elearning.sln -c Release --no-build
```

Để chạy **toàn bộ luồng kiểm tra (Full Verification)** (Yêu cầu cài đặt sẵn .NET):

```bash
npm run verify
```

## Tài liệu tham khảo (Documentation)

Hãy bắt đầu đọc từ các tài liệu sau để nắm rõ dự án:

1. [`docs/PROJECT_SPEC.md`](docs/PROJECT_SPEC.md) — Đặc tả yêu cầu sản phẩm MVP
2. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — Kiến trúc tổng thể hệ thống
3. [`docs/SECURITY.md`](docs/SECURITY.md) — Bảo mật và Quản lý luồng Auth
4. [`docs/DATABASE.md`](docs/DATABASE.md) — Thiết kế cơ sở dữ liệu
5. [`docs/API_CONTRACT.md`](docs/API_CONTRACT.md) — Tiêu chuẩn thiết kế API
6. [`docs/TESTING_AND_DOD.md`](docs/TESTING_AND_DOD.md) — Chiến lược kiểm thử & Definition of Done
7. [`docs/HANDOVER.md`](docs/HANDOVER.md) — Hướng dẫn bàn giao code

## Tình trạng hiện tại (Current state)

Dự án đã hoàn thiện bộ khung kỹ thuật (Technical Foundation) và hệ thống **Xác thực người dùng (Authentication & Authorization)**. Các module nghiệp vụ tiếp theo như Course, Lesson, Student, Enrollment, Exercise và Progress sẽ được phát triển trong các bản cập nhật tới.
