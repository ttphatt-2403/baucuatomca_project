# 🎲 Bầu Cua Tôm Cá Online - BCTC88

Game Bầu Cua Tôm Cá online nhiều người chơi theo thời gian thực.

## Tech Stack

- **Backend:** ASP.NET Core 8 MVC
- **Database:** SQL Server + EF Core 8 (Code First)
- **Real-time:** SignalR
- **Auth:** Firebase Authentication (Email/Password + Google)
- **Frontend:** Razor Views + Bootstrap 5

---

## Yêu cầu cài đặt

| Công cụ | Phiên bản | Link |
|---|---|---|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download/dotnet/8.0 |
| SQL Server | 2019+ hoặc Express | https://www.microsoft.com/sql-server |
| Git | Bất kỳ | https://git-scm.com |

---

## Hướng dẫn setup cho thành viên mới

### Bước 1 — Clone repo

```bash
git clone https://github.com/YOUR_USERNAME/BauCuaTomCa.git
cd BauCuaTomCa
```

### Bước 2 — Cấu hình Firebase

Dự án dùng Firebase để xác thực người dùng. Bạn cần 2 thứ:

#### 2a. File `firebase-service-account.json` (Server Admin SDK)

> File này chứa secret key — **KHÔNG được commit lên git**

1. Liên hệ trưởng nhóm để lấy file `firebase-service-account.json`
2. Đặt file đó vào thư mục gốc của dự án (cùng cấp với `BauCuaTomCa.csproj`)

#### 2b. Cấu hình `appsettings.json`

1. Copy file mẫu:
   ```bash
   cp appsettings.example.json appsettings.json
   ```
2. Mở `appsettings.json` và điền thông tin Firebase project (lấy từ Firebase Console):
   ```json
   "Firebase": {
     "CredentialPath": "firebase-service-account.json",
     "ApiKey": "...",
     "AuthDomain": "....firebaseapp.com",
     "ProjectId": "..."
   }
   ```

> **Lấy thông tin ở đâu?**
> Firebase Console → Project Settings → General → Your apps → Config (chọn Web)

### Bước 3 — Cấu hình Database

Mặc định dự án kết nối SQL Server tại `localhost` với Windows Authentication.

Nếu máy bạn dùng SQL Authentication, sửa connection string trong `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=BauCuaTomCaDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
}
```

### Bước 4 — Restore packages và chạy Migration

```bash
# Restore NuGet packages
dotnet restore

# Cài dotnet-ef tool (nếu chưa có)
dotnet tool install --global dotnet-ef --version 8.0.*

# Chạy migration để tạo database tự động
dotnet ef database update
```

> Database sẽ được tạo tự động khi chạy app ở môi trường Development (không cần chạy lệnh trên cũng được)

### Bước 5 — Chạy dự án

```bash
dotnet run
```

Mở trình duyệt tại: **http://localhost:5067**

---

## Cấu trúc thư mục

```
BauCuaTomCa/
├── Controllers/
│   ├── AccountController.cs   # Đăng nhập / Đăng ký (Firebase)
│   ├── RoomController.cs      # Tạo phòng / Vào phòng
│   ├── HistoryController.cs   # Lịch sử cá cược
│   └── HomeController.cs
├── Hubs/
│   └── GameHub.cs             # SignalR hub — toàn bộ logic game real-time
├── Models/
│   ├── User.cs
│   ├── Room.cs
│   ├── RoomPlayer.cs
│   ├── Round.cs
│   └── Bet.cs
├── Services/
│   ├── FirebaseService.cs     # Xác thực Firebase token
│   └── GameService.cs         # Logic tung xúc xắc, tính thắng thua
├── Data/
│   └── AppDbContext.cs        # EF Core DbContext
├── Views/
│   ├── Account/               # Login, Register
│   ├── Room/                  # Danh sách phòng, Tạo phòng, Chơi game
│   └── History/               # Lịch sử
├── appsettings.json           # Config (tự tạo từ appsettings.example.json)
├── appsettings.example.json   # Mẫu config — commit lên git
└── firebase-service-account.json  # SECRET — KHÔNG commit
```

---

## Luật chơi

- **3 xúc xắc**, mỗi mặt là 1 trong 6 con: Bầu / Cua / Tôm / Cá / Gà / Nai
- Người chơi đặt cược vào 1 con trước khi hết giờ đặt cược (30 giây)
- Kết quả:
  - Con xuất hiện **1 lần** → thắng `1x` tiền cược
  - Con xuất hiện **2 lần** → thắng `2x`
  - Con xuất hiện **3 lần** → thắng `3x`
  - Không xuất hiện → thua hết tiền cược
- Mỗi tài khoản bắt đầu với **100 xu**

---

## Các lỗi thường gặp

| Lỗi | Nguyên nhân | Cách fix |
|---|---|---|
| `firebase-service-account.json not found` | Thiếu file secret | Liên hệ trưởng nhóm lấy file |
| `Cannot connect to SQL Server` | SQL Server chưa chạy hoặc sai connection string | Kiểm tra SQL Server service + sửa appsettings.json |
| `dotnet-ef not found` | Chưa cài tool | Chạy `dotnet tool install --global dotnet-ef --version 8.0.*` |
| Trang trắng / lỗi 500 | Xem log terminal | Đọc thông báo lỗi trong cửa sổ `dotnet run` |

---

## Thông tin liên hệ Firebase (để trưởng nhóm chia sẻ)

> Trưởng nhóm cần chia sẻ cho từng thành viên:
> - File `firebase-service-account.json` (qua chat riêng, KHÔNG qua git)
> - Nội dung `appsettings.json` với ApiKey, AuthDomain, ProjectId thật
