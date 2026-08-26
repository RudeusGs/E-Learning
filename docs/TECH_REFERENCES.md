# TECH_REFERENCES.md — Tài liệu kỹ thuật chính thức tham chiếu

> File này là reference hỗ trợ implementation, không thay thế requirement/architecture contract trong repo. Khi upgrade dependency, luôn kiểm tra tài liệu chính thức theo version đang dùng.

## .NET / ASP.NET Core 10

### Cookie authentication cho API

Microsoft Learn:

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/api-endpoint-auth?view=aspnetcore-10.0

Điểm cần nhớ:

- Với .NET 10, known API endpoints dùng cookie authentication trả 401/403 thay vì redirect sang login/access-denied page.
- Integration tests phải verify API behavior này.

### JWT bearer authentication

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0

Dùng để kiểm tra:

- `AddAuthentication().AddJwtBearer()`.
- issuer/audience/signing-key/lifetime validation.
- middleware ordering và 401/403 behavior.

### Identity configuration

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0

Dùng để kiểm tra:

- password policy.
- lockout.
- cookie settings.
- security stamp behavior.

### Rate limiting

https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0

Dùng cho login/abuse protection.

### Middleware ordering

https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0

Middleware order là behavior, không phải formatting. Khi sửa Program.cs phải kiểm tra docs/version.

## EF Core

### Efficient querying

https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying

Điểm bắt buộc của project:

- Tránh lazy loading vì dễ gây N+1.
- Projection chỉ lấy field cần.
- Eager/explicit loading phải có chủ đích.
- Xem xét split query khi nhiều collection gây cartesian explosion.

## Vue 3 + TypeScript

### TypeScript with Vue

https://vuejs.org/guide/typescript/overview

Điểm cần nhớ:

- Vue có first-class TypeScript support.
- Vite dev/build pipeline không thay thế type checking đầy đủ.
- CI phải có bước type-check riêng, thường qua `vue-tsc`/script tương đương.

## Security references

Khi review security rộng hơn, ưu tiên:

- OWASP ASVS.
- OWASP Top 10.
- OWASP Cheat Sheet Series cho CSRF, XSS, authentication và access control.

Không copy code security từ blog ngẫu nhiên nếu official framework docs đã có hướng dẫn tương ứng.

## Reference policy cho AI agent

Khi cần xác nhận behavior có thể thay đổi theo version:

1. Xác định exact package/runtime version trong repo.
2. Ưu tiên official documentation và release/breaking-change notes.
3. Không dựa vào memory của model cho API/version-sensitive behavior.
4. Không upgrade chỉ vì docs mới hơn nếu task không yêu cầu.
5. Nếu official behavior mâu thuẫn với repo test/runtime, runtime + exact installed version là evidence cần điều tra trước khi sửa.
