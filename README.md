# Shutdown Scheduler

Ứng dụng Windows nhỏ gọn để lên lịch tắt máy nhanh, rõ ràng và an toàn. Chọn mốc thời gian có sẵn hoặc nhập giờ/phút/giây, kiểm tra giờ dự kiến, rồi xác nhận trước khi Windows nhận lịch tắt máy.

## Tính năng

- Các mốc nhanh: 30 phút, 1 giờ, 1,5 giờ, 2 giờ, 3 giờ, 4 giờ, 6 giờ và 8 giờ.
- Nhập thời lượng tùy chỉnh theo giờ, phút và giây.
- Hiển thị lệnh và thời điểm tắt dự kiến, cập nhật theo thời gian thực.
- Màn hình đếm ngược trực quan khi lịch đã hoạt động.
- Hủy lịch tắt máy bằng `shutdown -a` sau khi xác nhận.

## Tải và chạy

Tải file `ShutdownScheduler-v1.0.0-win-x64.zip` từ trang **Releases**, giải nén, rồi chạy `ShutdownScheduler.exe`.

Bản portable này dành cho **Windows 64-bit** và đã bao gồm .NET runtime, nên không cần cài .NET riêng.

> Windows có thể hiển thị cảnh báo SmartScreen cho ứng dụng mới chưa được ký số. Chỉ chạy file bạn tải từ trang Releases chính thức của repository này.

## Lưu ý an toàn

- Hãy lưu công việc trước khi đặt lịch tắt máy.
- Ứng dụng luôn yêu cầu xác nhận trước khi gửi lịch cho Windows.
- Đóng ứng dụng **không** tự hủy lịch đã gửi cho Windows.
- Để hủy lịch, mở lại ứng dụng và chọn **Hủy lịch tắt máy**, hoặc chạy lệnh sau trong PowerShell / Command Prompt:

```powershell
shutdown -a
```

## Chạy từ mã nguồn

Cần cài [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) trên Windows.

```powershell
dotnet run
```

## Build và publish

```powershell
dotnet build -c Release
dotnet publish -c Release
```

Output portable Windows x64 nằm tại:

```text
bin\Release\net8.0-windows\win-x64\publish\
```

## License

Phát hành theo [MIT License](LICENSE).
